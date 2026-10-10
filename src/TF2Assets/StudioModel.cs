using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TF2Assets
{
    /// <summary>Source engine model (MDL v44-49 + VVD + DX90 VTX), in Source space (X forward, Y left, Z up, inches).</summary>
    public sealed class StudioModel
    {
        public sealed class Bone
        {
            public string Name;
            public int Parent;
            public Vec3 Pos;
            public Quat Rot;
            public Vec3 RotEuler;   // RadianEuler default
            public Vec3 PosScale;
            public Vec3 RotScale;
            public Mat34 PoseToBone;
        }

        public sealed class Attachment
        {
            public string Name;
            public int Bone;
            public Mat34 Local;
        }

        public sealed class SubMesh
        {
            public int MaterialIndex;   // index into Materials (after skin table)
            public List<int> Indices = new List<int>();
        }

        public sealed class AnimDesc
        {
            public string Name;
            public float Fps;
            public int Flags;
            public int NumFrames;
            public int AnimBlock;
            public int DescStart;      // absolute offset of this mstudioanimdesc_t
            public int DataOffset;     // absolute offset in file of first mstudioanim_t (block 0)
            public int SectionFrames;
            public int SectionOffset;  // absolute offset of section table
        }

        public sealed class Sequence
        {
            public string Name;
            public int Flags;
            public int[] AnimIndices;   // blend grid, row-major
            public int GroupX, GroupY;
        }

        public string Name;
        public int Version;
        public readonly List<Bone> Bones = new List<Bone>();
        public readonly List<Attachment> Attachments = new List<Attachment>();
        public readonly List<string> TextureNames = new List<string>();
        public readonly List<string> TextureDirs = new List<string>();
        public short[][] SkinTable = new short[0][];
        public readonly List<string> IncludeModels = new List<string>();
        public readonly List<AnimDesc> Anims = new List<AnimDesc>();
        public readonly List<Sequence> Sequences = new List<Sequence>();

        // Geometry (LOD 0, first model of every body part)
        public Vec3[] Positions = new Vec3[0];
        public Vec3[] Normals = new Vec3[0];
        public float[] UVs = new float[0];          // u,v pairs
        public byte[] BoneIds = new byte[0];        // 3 per vertex
        public float[] BoneWeights = new float[0];  // 3 per vertex
        public byte[] BoneCounts = new byte[0];
        public readonly List<SubMesh> SubMeshes = new List<SubMesh>();

        internal byte[] Mdl;

        public static StudioModel Load(Tf2FileSystem fs, string mdlPath, bool geometry = true)
        {
            string stem = mdlPath.Substring(0, mdlPath.Length - 4);
            byte[] mdl = fs.Read(mdlPath);
            byte[] vvd = null, vtx = null;
            if (geometry)
            {
                fs.TryRead(stem + ".vvd", out vvd);
                fs.TryRead(stem + ".dx90.vtx", out vtx);
            }
            return Parse(mdl, vvd, vtx);
        }

        public static StudioModel Parse(byte[] mdl, byte[] vvd, byte[] vtx)
        {
            var m = new StudioModel { Mdl = mdl };
            var r = new Reader(mdl);
            if (r.I32(0) != 0x54534449) throw new InvalidDataException("Not an MDL (IDST)");
            m.Version = r.I32(4);
            if (m.Version < 44 || m.Version > 49) throw new InvalidDataException("Unsupported MDL version " + m.Version);
            m.Name = r.FixedStr(12, 64);

            // Bones
            int numBones = r.I32(156), boneIndex = r.I32(160);
            for (int i = 0; i < numBones; i++)
            {
                int o = boneIndex + i * 216;
                var b = new Bone
                {
                    Name = r.CStr(o + r.I32(o)),
                    Parent = r.I32(o + 4),
                    Pos = r.V3(o + 32),
                    Rot = new Quat(r.F32(o + 44), r.F32(o + 48), r.F32(o + 52), r.F32(o + 56)),
                    RotEuler = r.V3(o + 60),
                    PosScale = r.V3(o + 72),
                    RotScale = r.V3(o + 84),
                    PoseToBone = r.M34(o + 96),
                };
                m.Bones.Add(b);
            }

            // Attachments
            int numAtt = r.I32(240), attIndex = r.I32(244);
            for (int i = 0; i < numAtt; i++)
            {
                int o = attIndex + i * 92;
                m.Attachments.Add(new Attachment { Name = r.CStr(o + r.I32(o)), Bone = r.I32(o + 8), Local = r.M34(o + 12) });
            }

            // Textures / dirs / skins
            int numTex = r.I32(204), texIndex = r.I32(208);
            for (int i = 0; i < numTex; i++)
            {
                int o = texIndex + i * 64;
                m.TextureNames.Add(r.CStr(o + r.I32(o)));
            }
            int numCd = r.I32(212), cdIndex = r.I32(216);
            for (int i = 0; i < numCd; i++) m.TextureDirs.Add(r.CStr(r.I32(cdIndex + i * 4)));
            int numSkinRef = r.I32(220), numSkinFam = r.I32(224), skinIndex = r.I32(228);
            m.SkinTable = new short[numSkinFam][];
            for (int f = 0; f < numSkinFam; f++)
            {
                m.SkinTable[f] = new short[numSkinRef];
                for (int k = 0; k < numSkinRef; k++) m.SkinTable[f][k] = r.I16(skinIndex + (f * numSkinRef + k) * 2);
            }

            // Included models (animation libraries)
            int numInc = r.I32(336), incIndex = r.I32(340);
            for (int i = 0; i < numInc; i++)
            {
                int o = incIndex + i * 8;
                m.IncludeModels.Add(r.CStr(o + r.I32(o + 4)));
            }

            // Animation descriptions
            int numAnim = r.I32(180), animIndex = r.I32(184);
            for (int i = 0; i < numAnim; i++)
            {
                int o = animIndex + i * 100;
                var a = new AnimDesc
                {
                    Name = r.CStr(o + r.I32(o + 4)),
                    Fps = r.F32(o + 8),
                    Flags = r.I32(o + 12),
                    NumFrames = r.I32(o + 16),
                    AnimBlock = r.I32(o + 52),
                    DescStart = o,
                    DataOffset = o + r.I32(o + 56),
                    SectionFrames = m.Version >= 48 ? r.I32(o + 84) : 0,
                };
                int sectionIndex = m.Version >= 48 ? r.I32(o + 80) : 0;
                a.SectionOffset = sectionIndex != 0 ? o + sectionIndex : 0;
                m.Anims.Add(a);
            }

            // Sequences
            int numSeq = r.I32(188), seqIndex = r.I32(192);
            for (int i = 0; i < numSeq; i++)
            {
                int o = seqIndex + i * 212;
                int gx = r.I32(o + 68), gy = r.I32(o + 72);
                int aii = o + r.I32(o + 60);
                var idx = new int[Math.Max(1, gx * gy)];
                for (int k = 0; k < idx.Length; k++) idx[k] = r.I16(aii + k * 2);
                m.Sequences.Add(new Sequence { Name = r.CStr(o + r.I32(o + 4)), Flags = r.I32(o + 12), AnimIndices = idx, GroupX = gx, GroupY = gy });
            }

            if (vvd != null && vtx != null) m.ReadGeometry(r, vvd, vtx);
            return m;
        }

        void ReadGeometry(Reader r, byte[] vvdBytes, byte[] vtxBytes)
        {
            // ---- VVD ----
            var v = new Reader(vvdBytes);
            if (v.I32(0) != 0x56534449) throw new InvalidDataException("Not a VVD (IDSV)");
            int numLodVerts0 = v.I32(16);
            int numFixups = v.I32(48), fixupStart = v.I32(52), vertStart = v.I32(56);
            var order = new List<int>(numLodVerts0);
            if (numFixups > 0)
            {
                for (int i = 0; i < numFixups; i++)
                {
                    int o = fixupStart + i * 12;
                    int lod = v.I32(o), src = v.I32(o + 4), n = v.I32(o + 8);
                    if (lod >= 0) for (int k = 0; k < n; k++) order.Add(src + k);
                }
            }
            else for (int i = 0; i < numLodVerts0; i++) order.Add(i);

            int nv = order.Count;
            Positions = new Vec3[nv]; Normals = new Vec3[nv]; UVs = new float[nv * 2];
            BoneIds = new byte[nv * 3]; BoneWeights = new float[nv * 3]; BoneCounts = new byte[nv];
            for (int i = 0; i < nv; i++)
            {
                int o = vertStart + order[i] * 48;
                for (int k = 0; k < 3; k++)
                {
                    BoneWeights[i * 3 + k] = v.F32(o + k * 4);
                    BoneIds[i * 3 + k] = vvdBytes[o + 12 + k];
                }
                BoneCounts[i] = vvdBytes[o + 15];
                Positions[i] = v.V3(o + 16);
                Normals[i] = v.V3(o + 28);
                UVs[i * 2] = v.F32(o + 40);
                UVs[i * 2 + 1] = v.F32(o + 44);
            }

            // ---- MDL body parts → models → meshes ----
            var x = new Reader(vtxBytes);
            if (x.I32(0) != 7) throw new InvalidDataException("Unsupported VTX version " + x.I32(0));
            int vtxBodyParts = x.I32(28), vtxBodyOff = x.I32(32);
            int numBodyParts = r.I32(232), bodyIndex = r.I32(236);
            var byMaterial = new Dictionary<int, SubMesh>();

            for (int bp = 0; bp < numBodyParts && bp < vtxBodyParts; bp++)
            {
                int bo = bodyIndex + bp * 16;
                int numModels = r.I32(bo + 4), modelIndex = bo + r.I32(bo + 12);
                if (numModels == 0) continue;
                int mo = modelIndex; // first model of the part = default look
                int numMeshes = r.I32(mo + 72), meshIndex = mo + r.I32(mo + 76);
                int modelVertStart = r.I32(mo + 84) / 48;

                int xbp = vtxBodyOff + bp * 8;
                int xmo = xbp + x.I32(xbp + 4);              // model 0
                int xlod = xmo + x.I32(xmo + 4);             // lod 0
                int xNumMeshes = x.I32(xlod), xMeshOff = xlod + x.I32(xlod + 4);

                for (int me = 0; me < numMeshes && me < xNumMeshes; me++)
                {
                    int mm = meshIndex + me * 116;
                    int material = r.I32(mm);
                    int meshVertOffset = r.I32(mm + 12);
                    if (!byMaterial.TryGetValue(material, out var sub))
                    {
                        sub = new SubMesh { MaterialIndex = material };
                        byMaterial[material] = sub;
                        SubMeshes.Add(sub);
                    }
                    int xm = xMeshOff + me * 9;
                    int numSg = x.I32(xm), sgOff = xm + x.I32(xm + 4);
                    int sgSize = DetectStripGroupSize(x, sgOff, numSg);
                    for (int g = 0; g < numSg; g++)
                    {
                        int sg = sgOff + g * sgSize;
                        int sgNumVerts = x.I32(sg), sgVertOff = sg + x.I32(sg + 4);
                        int sgNumIdx = x.I32(sg + 8), sgIdxOff = sg + x.I32(sg + 12);
                        int numStrips = x.I32(sg + 16), stripOff = sg + x.I32(sg + 20);
                        int stripSize = sgSize == 25 ? 27 : 35;
                        for (int s = 0; s < numStrips; s++)
                        {
                            int so = stripOff + s * stripSize;
                            int sNumIdx = x.I32(so), sIdxStart = x.I32(so + 4);
                            byte flags = vtxBytes[so + 18];
                            Func<int, int> vert = k =>
                            {
                                int vi = x.U16(sgIdxOff + (sIdxStart + k) * 2);
                                int orig = x.U16(sgVertOff + vi * 9 + 4);
                                return modelVertStart + meshVertOffset + orig;
                            };
                            if ((flags & 2) != 0) // tri strip
                            {
                                for (int k = 0; k + 2 < sNumIdx; k++)
                                {
                                    int a = vert(k), b = vert(k + 1), c = vert(k + 2);
                                    if (a == b || b == c || a == c) continue;
                                    if ((k & 1) == 0) { sub.Indices.Add(a); sub.Indices.Add(b); sub.Indices.Add(c); }
                                    else { sub.Indices.Add(b); sub.Indices.Add(a); sub.Indices.Add(c); }
                                }
                            }
                            else
                            {
                                for (int k = 0; k + 2 < sNumIdx; k += 3)
                                {
                                    sub.Indices.Add(vert(k)); sub.Indices.Add(vert(k + 1)); sub.Indices.Add(vert(k + 2));
                                }
                            }
                        }
                    }
                }
            }
        }

        static int DetectStripGroupSize(Reader x, int sgOff, int numSg)
        {
            // Classic layout is 25 bytes; some later builds add two ints (33). Pick the one whose
            // first strip group's strip table holds a sane strip.
            foreach (int size in new[] { 25, 33 })
            {
                try
                {
                    bool ok = true;
                    for (int g = 0; g < numSg && ok; g++)
                    {
                        int sg = sgOff + g * size;
                        int nVerts = x.I32(sg), nIdx = x.I32(sg + 8), nStrips = x.I32(sg + 16);
                        if (nVerts < 0 || nIdx < 0 || nStrips < 0 || nStrips > 4096 || nIdx > 10_000_000) { ok = false; break; }
                        int stripOff = sg + x.I32(sg + 20);
                        int stripSize = size == 25 ? 27 : 35;
                        for (int s = 0; s < nStrips; s++)
                        {
                            int so = stripOff + s * stripSize;
                            int sNumIdx = x.I32(so), sIdx = x.I32(so + 4);
                            if (sNumIdx < 0 || sIdx < 0 || sIdx + sNumIdx > nIdx) { ok = false; break; }
                        }
                    }
                    if (ok) return size;
                }
                catch (IndexOutOfRangeException) { }
                catch (ArgumentOutOfRangeException) { }
                catch (ArgumentException) { }
            }
            return 25;
        }

        public int FindBone(string name)
        {
            for (int i = 0; i < Bones.Count; i++) if (string.Equals(Bones[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        public Sequence FindSequence(string name)
        {
            foreach (var s in Sequences) if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }

        public Attachment FindAttachment(string name)
        {
            foreach (var a in Attachments) if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return a;
            return null;
        }

        /// <summary>Material (VMT) path for a sub-mesh, resolved via skin family and texture dirs.</summary>
        public string ResolveMaterial(Tf2FileSystem fs, int materialIndex, int skinFamily = 0)
        {
            int tex = materialIndex;
            if (SkinTable.Length > 0)
            {
                var fam = SkinTable[Math.Min(skinFamily, SkinTable.Length - 1)];
                if (materialIndex < fam.Length) tex = fam[materialIndex];
            }
            if (tex < 0 || tex >= TextureNames.Count) return null;
            string name = TextureNames[tex].Replace('\\', '/');
            foreach (var d in TextureDirs)
            {
                string dir = d.Replace('\\', '/').Trim('/');
                string p = "materials/" + (dir.Length > 0 ? dir + "/" : "") + name + ".vmt";
                if (fs == null || fs.Exists(p)) return VpkPackage.Normalize(p);
            }
            string direct = "materials/" + name.TrimStart('/') + ".vmt";
            return fs == null || fs.Exists(direct) ? VpkPackage.Normalize(direct) : null;
        }

        /// <summary>World (model-space) bind matrices of each bone from its default local pose.</summary>
        public Mat34[] BindPose()
        {
            var w = new Mat34[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                var local = Mat34.FromQuatPos(Bones[i].Rot, Bones[i].Pos);
                w[i] = Bones[i].Parent >= 0 ? w[Bones[i].Parent] * local : local;
            }
            return w;
        }

        internal sealed class Reader
        {
            readonly byte[] _b;
            public Reader(byte[] b) { _b = b; }
            public int I32(int o) => BitConverter.ToInt32(_b, o);
            public short I16(int o) => BitConverter.ToInt16(_b, o);
            public int U16(int o) => BitConverter.ToUInt16(_b, o);
            public float F32(int o) => BitConverter.ToSingle(_b, o);
            public Vec3 V3(int o) => new Vec3(F32(o), F32(o + 4), F32(o + 8));
            public Mat34 M34(int o)
            {
                var m = new Mat34();
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 4; col++)
                        m[row, col] = F32(o + (row * 4 + col) * 4);
                return m;
            }
            public string CStr(int o)
            {
                if (o < 0 || o >= _b.Length) return "";
                int e = o;
                while (e < _b.Length && _b[e] != 0) e++;
                return Encoding.ASCII.GetString(_b, o, e - o);
            }
            public string FixedStr(int o, int len)
            {
                int e = o;
                while (e < o + len && _b[e] != 0) e++;
                return Encoding.ASCII.GetString(_b, o, e - o);
            }
        }
    }
}
