using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TF2Assets
{
    /// <summary>
    /// Reads Valve VPK (v1/v2) packages: a "*_dir.vpk" tree plus numbered "*_NNN.vpk" archives.
    /// Only reads; never writes to the game folder.
    /// </summary>
    public sealed class VpkPackage
    {
        public struct Entry
        {
            public ushort ArchiveIndex;
            public uint Offset;
            public uint Length;
            public byte[] Preload;
        }

        const uint Signature = 0x55AA1234;
        const ushort DirArchive = 0x7FFF;

        readonly string _dirPath;
        readonly long _dataStart;
        readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public string DirPath => _dirPath;
        public int Count => _entries.Count;
        public IEnumerable<string> Paths => _entries.Keys;

        public VpkPackage(string dirPath)
        {
            _dirPath = dirPath;
            using (var fs = File.OpenRead(dirPath))
            using (var br = new BinaryReader(fs))
            {
                uint sig = br.ReadUInt32();
                if (sig != Signature) throw new InvalidDataException("Not a VPK directory: " + dirPath);
                uint version = br.ReadUInt32();
                uint treeSize = br.ReadUInt32();
                int header = version == 1 ? 12 : version == 2 ? 28 : throw new InvalidDataException("Unsupported VPK version " + version);
                fs.Position = header;
                byte[] tree = br.ReadBytes((int)treeSize);
                _dataStart = header + treeSize;
                ParseTree(tree);
            }
        }

        void ParseTree(byte[] t)
        {
            int p = 0;
            while (true)
            {
                string ext = CStr(t, ref p);
                if (ext.Length == 0) break;
                while (true)
                {
                    string dir = CStr(t, ref p);
                    if (dir.Length == 0) break;
                    while (true)
                    {
                        string name = CStr(t, ref p);
                        if (name.Length == 0) break;
                        // crc(4) preloadBytes(2) archiveIndex(2) offset(4) length(4) terminator(2)
                        ushort preload = BitConverter.ToUInt16(t, p + 4);
                        var e = new Entry
                        {
                            ArchiveIndex = BitConverter.ToUInt16(t, p + 6),
                            Offset = BitConverter.ToUInt32(t, p + 8),
                            Length = BitConverter.ToUInt32(t, p + 12),
                        };
                        p += 18;
                        if (preload > 0)
                        {
                            e.Preload = new byte[preload];
                            Buffer.BlockCopy(t, p, e.Preload, 0, preload);
                            p += preload;
                        }
                        string full = (dir == " " ? "" : dir + "/") + name + (ext == " " ? "" : "." + ext);
                        _entries[full] = e;
                    }
                }
            }
        }

        static string CStr(byte[] t, ref int p)
        {
            int s = p;
            while (t[p] != 0) p++;
            string r = Encoding.UTF8.GetString(t, s, p - s);
            p++;
            return r;
        }

        public static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/').ToLowerInvariant();

        public bool Contains(string path) => _entries.ContainsKey(Normalize(path));

        public bool TryRead(string path, out byte[] data)
        {
            data = null;
            if (!_entries.TryGetValue(Normalize(path), out var e)) return false;
            int pre = e.Preload?.Length ?? 0;
            data = new byte[pre + e.Length];
            if (pre > 0) Buffer.BlockCopy(e.Preload, 0, data, 0, pre);
            if (e.Length == 0) return true;
            string archive;
            long offset;
            if (e.ArchiveIndex == DirArchive)
            {
                archive = _dirPath;
                offset = _dataStart + e.Offset;
            }
            else
            {
                archive = _dirPath.Substring(0, _dirPath.Length - "_dir.vpk".Length) + "_" + e.ArchiveIndex.ToString("D3") + ".vpk";
                offset = e.Offset;
            }
            if (!File.Exists(archive)) { data = null; return false; }
            using (var fs = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Position = offset;
                int read = 0;
                while (read < e.Length)
                {
                    int n = fs.Read(data, pre + read, (int)e.Length - read);
                    if (n <= 0) throw new EndOfStreamException("Truncated VPK archive " + archive);
                    read += n;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// The player's own TF2 content: every "tf/*_dir.vpk" plus loose files under tf/custom and tf/.
    /// Loose files win, like the game's own search path.
    /// </summary>
    public sealed class Tf2FileSystem
    {
        readonly List<string> _looseRoots = new List<string>();
        readonly List<VpkPackage> _vpks = new List<VpkPackage>();

        public string TfFolder { get; }
        public IReadOnlyList<VpkPackage> Packages => _vpks;

        /// <param name="gameFolder">The TF2 install folder (the one holding "tf" and "tf_win64.exe"), or its "tf" folder.</param>
        public Tf2FileSystem(string gameFolder, Action<string> log = null)
        {
            string tf = Directory.Exists(Path.Combine(gameFolder, "tf")) ? Path.Combine(gameFolder, "tf") : gameFolder;
            TfFolder = tf;
            string custom = Path.Combine(tf, "custom");
            if (Directory.Exists(custom))
                foreach (var d in Directory.GetDirectories(custom)) _looseRoots.Add(d);
            _looseRoots.Add(tf);
            // Base game content first; tf2_*_dir.vpk in tf/, then hl2 shared content.
            foreach (var name in new[] { "tf2_misc_dir.vpk", "tf2_textures_dir.vpk", "tf2_sound_misc_dir.vpk" })
            {
                string p = Path.Combine(tf, name);
                if (File.Exists(p))
                {
                    try { _vpks.Add(new VpkPackage(p)); }
                    catch (Exception ex) { log?.Invoke("Could not read " + p + ": " + ex.Message); }
                }
                else log?.Invoke("Missing TF2 package " + p);
            }
        }

        public bool TryRead(string path, out byte[] data)
        {
            string norm = VpkPackage.Normalize(path);
            foreach (var root in _looseRoots)
            {
                string p = Path.Combine(root, norm.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(p)) { data = File.ReadAllBytes(p); return true; }
            }
            foreach (var v in _vpks)
                if (v.TryRead(norm, out data)) return true;
            data = null;
            return false;
        }

        public byte[] Read(string path)
        {
            if (TryRead(path, out var d)) return d;
            throw new FileNotFoundException("TF2 file not found in your Team Fortress 2 install: " + path);
        }

        public bool Exists(string path)
        {
            string norm = VpkPackage.Normalize(path);
            foreach (var root in _looseRoots)
                if (File.Exists(Path.Combine(root, norm.Replace('/', Path.DirectorySeparatorChar)))) return true;
            foreach (var v in _vpks) if (v.Contains(norm)) return true;
            return false;
        }
    }
}
