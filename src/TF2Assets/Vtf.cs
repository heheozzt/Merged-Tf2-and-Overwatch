using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TF2Assets
{
    /// <summary>Valve Texture Format (7.0-7.5) → RGBA32 pixels (top row first) for one mip level.</summary>
    public sealed class VtfImage
    {
        public int Width, Height;
        public byte[] Rgba;   // Width*Height*4, rows top to bottom

        enum Fmt
        {
            RGBA8888 = 0, ABGR8888 = 1, RGB888 = 2, BGR888 = 3, RGB565 = 4, I8 = 5, IA88 = 6, A8 = 8,
            RGB888_BLUESCREEN = 9, BGR888_BLUESCREEN = 10, ARGB8888 = 11, BGRA8888 = 12,
            DXT1 = 13, DXT3 = 14, DXT5 = 15, BGRX8888 = 16, BGR565 = 17, BGRX5551 = 18, BGRA4444 = 19,
            DXT1_ONEBITALPHA = 20, BGRA5551 = 21, UV88 = 22,
        }

        /// <param name="maxSize">Largest edge wanted; picks the first mip at or below it.</param>
        public static VtfImage Decode(byte[] f, int maxSize = 1024)
        {
            if (f.Length < 64 || f[0] != 'V' || f[1] != 'T' || f[2] != 'F' || f[3] != 0) throw new InvalidDataException("Not a VTF");
            int major = BitConverter.ToInt32(f, 4), minor = BitConverter.ToInt32(f, 8);
            int headerSize = BitConverter.ToInt32(f, 12);
            int width = BitConverter.ToUInt16(f, 16), height = BitConverter.ToUInt16(f, 18);
            int frames = BitConverter.ToUInt16(f, 24);
            var fmt = (Fmt)BitConverter.ToInt32(f, 52);
            int mips = f[56];
            var lowFmt = BitConverter.ToInt32(f, 57);
            int lowW = f[61], lowH = f[62];
            int depth = (major == 7 && minor >= 2) ? Math.Max(1, (int)BitConverter.ToUInt16(f, 63)) : 1;
            if (major != 7) throw new InvalidDataException("Unsupported VTF version " + major + "." + minor);
            if (frames < 1) frames = 1;

            // Locate high-res data
            int dataStart;
            if (minor >= 3)
            {
                int numRes = BitConverter.ToInt32(f, 68);
                dataStart = -1;
                for (int i = 0; i < numRes; i++)
                {
                    int o = 80 + i * 8;
                    uint tag = (uint)(f[o] | f[o + 1] << 8 | f[o + 2] << 16);
                    int data = BitConverter.ToInt32(f, o + 4);
                    if (tag == 0x000030) dataStart = data;   // "\x30\0\0" high-res image
                }
                if (dataStart < 0) throw new InvalidDataException("VTF has no image resource");
            }
            else
            {
                int lowSize = lowFmt == -1 || lowW == 0 ? 0 : MipBytes(Fmt.DXT1, lowW, lowH);
                dataStart = headerSize + lowSize;
            }

            // Mips are stored smallest first; frames (and faces/slices) within each mip.
            int pick = 0;
            while (pick < mips - 1 && Math.Max(Math.Max(1, width >> pick), Math.Max(1, height >> pick)) > maxSize) pick++;
            int offset = dataStart;
            for (int mip = mips - 1; mip > pick; mip--)
                offset += MipBytes(fmt, Math.Max(1, width >> mip), Math.Max(1, height >> mip)) * frames * depth;
            int w = Math.Max(1, width >> pick), h = Math.Max(1, height >> pick);
            var img = new VtfImage { Width = w, Height = h, Rgba = new byte[w * h * 4] };
            DecodeSurface(fmt, f, offset, w, h, img.Rgba);
            return img;
        }

        static int MipBytes(Fmt fmt, int w, int h)
        {
            switch (fmt)
            {
                case Fmt.DXT1: case Fmt.DXT1_ONEBITALPHA: return Math.Max(1, (w + 3) / 4) * Math.Max(1, (h + 3) / 4) * 8;
                case Fmt.DXT3: case Fmt.DXT5: return Math.Max(1, (w + 3) / 4) * Math.Max(1, (h + 3) / 4) * 16;
                case Fmt.RGBA8888: case Fmt.ABGR8888: case Fmt.ARGB8888: case Fmt.BGRA8888: case Fmt.BGRX8888: return w * h * 4;
                case Fmt.RGB888: case Fmt.BGR888: case Fmt.RGB888_BLUESCREEN: case Fmt.BGR888_BLUESCREEN: return w * h * 3;
                case Fmt.RGB565: case Fmt.BGR565: case Fmt.IA88: case Fmt.BGRX5551: case Fmt.BGRA4444: case Fmt.BGRA5551: case Fmt.UV88: return w * h * 2;
                case Fmt.I8: case Fmt.A8: return w * h;
            }
            throw new NotSupportedException("VTF format " + fmt);
        }

        static void DecodeSurface(Fmt fmt, byte[] s, int o, int w, int h, byte[] dst)
        {
            switch (fmt)
            {
                case Fmt.DXT1: case Fmt.DXT1_ONEBITALPHA: DecodeDxt(s, o, w, h, dst, 1); return;
                case Fmt.DXT3: DecodeDxt(s, o, w, h, dst, 3); return;
                case Fmt.DXT5: DecodeDxt(s, o, w, h, dst, 5); return;
            }
            for (int i = 0; i < w * h; i++)
            {
                byte r = 0, g = 0, b = 0, a = 255;
                switch (fmt)
                {
                    case Fmt.RGBA8888: r = s[o + i * 4]; g = s[o + i * 4 + 1]; b = s[o + i * 4 + 2]; a = s[o + i * 4 + 3]; break;
                    case Fmt.ABGR8888: a = s[o + i * 4]; b = s[o + i * 4 + 1]; g = s[o + i * 4 + 2]; r = s[o + i * 4 + 3]; break;
                    case Fmt.ARGB8888: a = s[o + i * 4]; r = s[o + i * 4 + 1]; g = s[o + i * 4 + 2]; b = s[o + i * 4 + 3]; break;
                    case Fmt.BGRA8888: b = s[o + i * 4]; g = s[o + i * 4 + 1]; r = s[o + i * 4 + 2]; a = s[o + i * 4 + 3]; break;
                    case Fmt.BGRX8888: b = s[o + i * 4]; g = s[o + i * 4 + 1]; r = s[o + i * 4 + 2]; break;
                    case Fmt.RGB888: case Fmt.RGB888_BLUESCREEN: r = s[o + i * 3]; g = s[o + i * 3 + 1]; b = s[o + i * 3 + 2]; break;
                    case Fmt.BGR888: case Fmt.BGR888_BLUESCREEN: b = s[o + i * 3]; g = s[o + i * 3 + 1]; r = s[o + i * 3 + 2]; break;
                    case Fmt.I8: r = g = b = s[o + i]; break;
                    case Fmt.A8: r = g = b = 255; a = s[o + i]; break;
                    case Fmt.IA88: r = g = b = s[o + i * 2]; a = s[o + i * 2 + 1]; break;
                    case Fmt.UV88: r = s[o + i * 2]; g = s[o + i * 2 + 1]; b = 255; break;
                    case Fmt.RGB565: case Fmt.BGR565:
                    {
                        int c = BitConverter.ToUInt16(s, o + i * 2);
                        byte hi = (byte)((c >> 11) * 255 / 31), mid = (byte)(((c >> 5) & 63) * 255 / 63), lo = (byte)((c & 31) * 255 / 31);
                        if (fmt == Fmt.RGB565) { r = lo; g = mid; b = hi; } else { b = lo; g = mid; r = hi; }
                        break;
                    }
                    case Fmt.BGRA4444:
                    {
                        int c = BitConverter.ToUInt16(s, o + i * 2);
                        b = (byte)((c & 15) * 17); g = (byte)(((c >> 4) & 15) * 17); r = (byte)(((c >> 8) & 15) * 17); a = (byte)(((c >> 12) & 15) * 17);
                        break;
                    }
                    case Fmt.BGRX5551: case Fmt.BGRA5551:
                    {
                        int c = BitConverter.ToUInt16(s, o + i * 2);
                        b = (byte)((c & 31) * 255 / 31); g = (byte)(((c >> 5) & 31) * 255 / 31); r = (byte)(((c >> 10) & 31) * 255 / 31);
                        a = fmt == Fmt.BGRA5551 ? (byte)((c >> 15) != 0 ? 255 : 0) : (byte)255;
                        break;
                    }
                }
                dst[i * 4] = r; dst[i * 4 + 1] = g; dst[i * 4 + 2] = b; dst[i * 4 + 3] = a;
            }
        }

        static void DecodeDxt(byte[] s, int o, int w, int h, byte[] dst, int kind)
        {
            int bw = Math.Max(1, (w + 3) / 4), bh = Math.Max(1, (h + 3) / 4);
            int blockBytes = kind == 1 ? 8 : 16;
            var col = new byte[16];
            var alpha = new byte[16];
            for (int by = 0; by < bh; by++)
            {
                for (int bx = 0; bx < bw; bx++)
                {
                    int b = o + (by * bw + bx) * blockBytes;
                    int c = b;
                    for (int i = 0; i < 16; i++) alpha[i] = 255;
                    if (kind == 3)
                    {
                        for (int i = 0; i < 16; i++) { int nib = (s[b + i / 2] >> ((i & 1) * 4)) & 15; alpha[i] = (byte)(nib * 17); }
                        c = b + 8;
                    }
                    else if (kind == 5)
                    {
                        int a0 = s[b], a1 = s[b + 1];
                        ulong bits = 0;
                        for (int i = 0; i < 6; i++) bits |= (ulong)s[b + 2 + i] << (8 * i);
                        for (int i = 0; i < 16; i++)
                        {
                            int code = (int)((bits >> (3 * i)) & 7);
                            int av;
                            if (code == 0) av = a0;
                            else if (code == 1) av = a1;
                            else if (a0 > a1) av = ((8 - code) * a0 + (code - 1) * a1) / 7;
                            else if (code == 6) av = 0;
                            else if (code == 7) av = 255;
                            else av = ((6 - code) * a0 + (code - 1) * a1) / 5;
                            alpha[i] = (byte)av;
                        }
                        c = b + 8;
                    }
                    int c0 = BitConverter.ToUInt16(s, c), c1 = BitConverter.ToUInt16(s, c + 2);
                    uint idx = BitConverter.ToUInt32(s, c + 4);
                    var pal = new int[4, 4];
                    Rgb565(c0, out pal[0, 0], out pal[0, 1], out pal[0, 2]); pal[0, 3] = 255;
                    Rgb565(c1, out pal[1, 0], out pal[1, 1], out pal[1, 2]); pal[1, 3] = 255;
                    if (c0 > c1 || kind != 1)
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            pal[2, k] = (2 * pal[0, k] + pal[1, k]) / 3;
                            pal[3, k] = (pal[0, k] + 2 * pal[1, k]) / 3;
                        }
                        pal[2, 3] = pal[3, 3] = 255;
                    }
                    else
                    {
                        for (int k = 0; k < 3; k++) { pal[2, k] = (pal[0, k] + pal[1, k]) / 2; pal[3, k] = 0; }
                        pal[2, 3] = 255; pal[3, 3] = 0;
                    }
                    for (int py = 0; py < 4; py++)
                    {
                        for (int px = 0; px < 4; px++)
                        {
                            int x = bx * 4 + px, y = by * 4 + py;
                            if (x >= w || y >= h) continue;
                            int i = py * 4 + px;
                            int sel = (int)((idx >> (2 * i)) & 3);
                            int d = (y * w + x) * 4;
                            dst[d] = (byte)pal[sel, 0]; dst[d + 1] = (byte)pal[sel, 1]; dst[d + 2] = (byte)pal[sel, 2];
                            dst[d + 3] = kind == 1 ? (byte)pal[sel, 3] : alpha[i];
                        }
                    }
                }
            }
        }

        static void Rgb565(int c, out int r, out int g, out int b)
        {
            r = ((c >> 11) & 31) * 255 / 31; g = ((c >> 5) & 63) * 255 / 63; b = (c & 31) * 255 / 31;
        }
    }

    /// <summary>Minimal VMT reader: the shader and the keys we need ($basetexture, $translucent, $alphatest...).</summary>
    public sealed class VmtMaterial
    {
        public string Shader;
        public readonly Dictionary<string, string> Keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string BaseTexture => Get("$basetexture");
        public bool Translucent => Get("$translucent") == "1";
        public bool AlphaTest => Get("$alphatest") == "1";
        public string Get(string key) => Keys.TryGetValue(key, out var v) ? v : null;

        public static VmtMaterial Load(Tf2FileSystem fs, string vmtPath, int depth = 0)
        {
            var m = Parse(Encoding.UTF8.GetString(fs.Read(vmtPath)));
            if (depth < 4 && string.Equals(m.Shader, "patch", StringComparison.OrdinalIgnoreCase) && m.Get("include") != null)
            {
                var baseMat = Load(fs, m.Get("include"), depth + 1);
                foreach (var kv in m.Keys) if (kv.Key != "include") baseMat.Keys[kv.Key] = kv.Value;
                return baseMat;
            }
            return m;
        }

        public static VmtMaterial Parse(string text)
        {
            var m = new VmtMaterial();
            var tokens = Tokenize(text);
            int i = 0;
            if (i < tokens.Count) m.Shader = tokens[i++];
            // Keys at the top level, plus those inside a patch material's "replace"/"insert" blocks.
            var stack = new Stack<string>();
            string pendingBlock = "";
            while (i < tokens.Count)
            {
                string t = tokens[i++];
                if (t == "{") { stack.Push(pendingBlock); pendingBlock = ""; continue; }
                if (t == "}") { if (stack.Count > 0) stack.Pop(); continue; }
                if (i < tokens.Count && tokens[i] == "{") { pendingBlock = t.ToLowerInvariant(); continue; }
                if (i >= tokens.Count) break;
                string v = tokens[i++];
                bool top = stack.Count == 1;
                bool patchBlock = stack.Count == 2 && (stack.Peek() == "replace" || stack.Peek() == "insert");
                if (top || patchBlock) m.Keys[t] = v;
            }
            return m;
        }

        static List<string> Tokenize(string s)
        {
            var list = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '/') { while (i < s.Length && s[i] != '\n') i++; continue; }
                if (c == '{' || c == '}') { list.Add(c.ToString()); i++; continue; }
                if (c == '"')
                {
                    int e = s.IndexOf('"', i + 1);
                    if (e < 0) e = s.Length;
                    list.Add(s.Substring(i + 1, e - i - 1));
                    i = e + 1;
                    continue;
                }
                int st = i;
                while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] != '{' && s[i] != '}' && s[i] != '"') i++;
                list.Add(s.Substring(st, i - st));
            }
            return list;
        }
    }
}
