using System;
using System.Collections.Generic;

namespace TF2Assets
{
    /// <summary>
    /// Decodes Source "bone by frame" (MDL v44-49, block 0) animation into per-frame local bone poses,
    /// and poses a skeleton (with optional bone-merged child model) into model-space matrices.
    /// </summary>
    public sealed class DecodedAnimation
    {
        public string Name;
        public float Fps;
        public bool Looping;
        public int NumFrames;
        /// <summary>[frame][targetBone] local position/rotation, already in the target skeleton's bone order.</summary>
        public Vec3[][] Pos;
        public Quat[][] Rot;
        public float Duration => NumFrames > 1 ? (NumFrames - 1) / Fps : 0f;

        const int RawPos = 0x01, RawRot = 0x02, AnimPos = 0x04, AnimRot = 0x08, Delta = 0x10, RawRot2 = 0x20;

        /// <summary>
        /// Decodes sequence <paramref name="seqName"/> found in <paramref name="source"/> (the model or one of its
        /// animation libraries) for the bones of <paramref name="target"/>, matched by name.
        /// </summary>
        public static DecodedAnimation Decode(StudioModel source, string seqName, StudioModel target)
        {
            var seq = source.FindSequence(seqName) ?? throw new ArgumentException("No sequence " + seqName + " in " + source.Name);
            int animIndex = seq.AnimIndices[0];
            var desc = source.Anims[animIndex];
            if (desc.AnimBlock != 0) throw new NotSupportedException("Animation " + desc.Name + " lives in an external .ani block");
            if ((desc.Flags & 0x40) != 0) throw new NotSupportedException("Frame-by-bone animation " + desc.Name + " not supported");

            // map source bone → target bone
            var map = new int[source.Bones.Count];
            for (int i = 0; i < map.Length; i++) map[i] = target.FindBone(source.Bones[i].Name);

            var d = new DecodedAnimation
            {
                Name = seqName,
                Fps = desc.Fps > 0 ? desc.Fps : 30f,
                Looping = (desc.Flags & 0x01) != 0 || (seq.Flags & 0x01) != 0,
                NumFrames = Math.Max(1, desc.NumFrames),
                Pos = new Vec3[Math.Max(1, desc.NumFrames)][],
                Rot = new Quat[Math.Max(1, desc.NumFrames)][],
            };
            byte[] b = source.Mdl;
            for (int f = 0; f < d.NumFrames; f++)
            {
                var pos = new Vec3[target.Bones.Count];
                var rot = new Quat[target.Bones.Count];
                for (int i = 0; i < target.Bones.Count; i++) { pos[i] = target.Bones[i].Pos; rot[i] = target.Bones[i].Rot; }

                int frame = f;
                int dataOff = desc.DataOffset;
                if (desc.SectionFrames > 0 && desc.SectionOffset != 0)
                {
                    int section;
                    if (desc.NumFrames > desc.SectionFrames && frame == desc.NumFrames - 1)
                    {
                        // the last frame of a long clip is stored in an extra final section
                        section = desc.NumFrames / desc.SectionFrames + 1;
                        frame = 0;
                    }
                    else
                    {
                        section = frame / desc.SectionFrames;
                        frame -= section * desc.SectionFrames;
                    }
                    // section table: (animblock, animindex) pairs, animindex relative to the anim desc
                    int secBlock = BitConverter.ToInt32(b, desc.SectionOffset + section * 8);
                    int secIndex = BitConverter.ToInt32(b, desc.SectionOffset + section * 8 + 4);
                    if (secBlock != 0) throw new NotSupportedException("Sectioned animation in external block");
                    dataOff = desc.DescStart + secIndex;
                }

                int p = dataOff;
                for (int guard = 0; guard < 512; guard++)
                {
                    int bone = b[p];
                    int flags = b[p + 1];
                    int next = BitConverter.ToInt16(b, p + 2);
                    if (bone < source.Bones.Count)
                    {
                        var sb = source.Bones[bone];
                        int data = p + 4;
                        Quat q = sb.Rot;
                        Vec3 v = sb.Pos;
                        bool delta = (flags & Delta) != 0;
                        if ((flags & RawRot) != 0) q = Quat48(b, data);
                        else if ((flags & RawRot2) != 0) q = Quat64(b, data);
                        else if ((flags & AnimRot) != 0)
                        {
                            var e = new Vec3(
                                ExtractValue(b, data, 0, frame, sb.RotScale.X),
                                ExtractValue(b, data, 1, frame, sb.RotScale.Y),
                                ExtractValue(b, data, 2, frame, sb.RotScale.Z));
                            if (!delta) e = e + sb.RotEuler;
                            q = Quat.FromRadianEuler(e);
                        }
                        else if (delta) q = Quat.Identity;

                        if ((flags & RawPos) != 0)
                        {
                            int po = data + ((flags & RawRot) != 0 ? 6 : 0) + ((flags & RawRot2) != 0 ? 8 : 0);
                            v = new Vec3(Half(b, po), Half(b, po + 2), Half(b, po + 4));
                        }
                        else if ((flags & AnimPos) != 0)
                        {
                            int vp = data + ((flags & AnimRot) != 0 ? 6 : 0);
                            v = new Vec3(
                                ExtractValue(b, vp, 0, frame, sb.PosScale.X),
                                ExtractValue(b, vp, 1, frame, sb.PosScale.Y),
                                ExtractValue(b, vp, 2, frame, sb.PosScale.Z));
                            if (!delta) v = v + sb.Pos;
                        }
                        else if (delta) v = new Vec3(0, 0, 0);

                        int t = map[bone];
                        if (t >= 0) { pos[t] = v; rot[t] = q.Normalized; }
                    }
                    if (next == 0) break;
                    p += next;
                }
                d.Pos[f] = pos;
                d.Rot[f] = rot;
            }
            return d;
        }


        static float ExtractValue(byte[] b, int valuePtr, int axis, int frame, float scale)
        {
            int off = BitConverter.ToInt16(b, valuePtr + axis * 2);
            if (off == 0) return 0f;
            int p = valuePtr + off;
            int k = frame;
            for (int guard = 0; guard < 100000; guard++)
            {
                int valid = b[p], total = b[p + 1];
                if (total > k)
                {
                    int idx = valid > k ? k + 1 : valid;
                    return BitConverter.ToInt16(b, p + idx * 2) * scale;
                }
                k -= total;
                p += (valid + 1) * 2;
                if (b[p + 1] == 0) return 0f;
            }
            return 0f;
        }

        static Quat Quat48(byte[] b, int o)
        {
            int x = BitConverter.ToUInt16(b, o), y = BitConverter.ToUInt16(b, o + 2), zw = BitConverter.ToUInt16(b, o + 4);
            float fx = (x - 32768) * (1f / 32768f);
            float fy = (y - 32768) * (1f / 32768f);
            float fz = ((zw & 0x7FFF) - 16384) * (1f / 16384f);
            float w = (float)Math.Sqrt(Math.Max(0, 1 - fx * fx - fy * fy - fz * fz));
            if ((zw & 0x8000) != 0) w = -w;
            return new Quat(fx, fy, fz, w);
        }

        static Quat Quat64(byte[] b, int o)
        {
            ulong v = BitConverter.ToUInt64(b, o);
            long x = (long)(v & 0x1FFFFF), y = (long)((v >> 21) & 0x1FFFFF), z = (long)((v >> 42) & 0x1FFFFF);
            bool wneg = (v >> 63) != 0;
            float fx = (x - 1048576) * (1f / 1048576.5f);
            float fy = (y - 1048576) * (1f / 1048576.5f);
            float fz = (z - 1048576) * (1f / 1048576.5f);
            float w = (float)Math.Sqrt(Math.Max(0, 1 - fx * fx - fy * fy - fz * fz));
            return new Quat(fx, fy, fz, wneg ? -w : w);
        }

        static float Half(byte[] b, int o)
        {
            int h = BitConverter.ToUInt16(b, o);
            int sign = (h >> 15) & 1, exp = (h >> 10) & 0x1F, mant = h & 0x3FF;
            float val;
            if (exp == 0) val = (float)(mant / 1024.0 * Math.Pow(2, -14));
            else if (exp == 31) val = mant == 0 ? float.PositiveInfinity : float.NaN;
            else val = (float)((1 + mant / 1024.0) * Math.Pow(2, exp - 15));
            return sign != 0 ? -val : val;
        }

        /// <summary>Local pose at a time in seconds (interpolated).</summary>
        public void Sample(float time, Vec3[] pos, Quat[] rot)
        {
            float fr = time * Fps;
            if (Looping && NumFrames > 1) { fr %= (NumFrames - 1); if (fr < 0) fr += NumFrames - 1; }
            fr = Math.Max(0, Math.Min(NumFrames - 1, fr));
            int a = (int)Math.Floor(fr), c = Math.Min(NumFrames - 1, a + 1);
            float t = fr - a;
            for (int i = 0; i < pos.Length; i++)
            {
                var pa = Pos[a][i]; var pc = Pos[c][i];
                pos[i] = pa + (pc - pa) * t;
                rot[i] = Quat.Slerp(Rot[a][i], Rot[c][i], t);
            }
        }
    }

    /// <summary>Skeleton posing, bone-merge and CPU skinning.</summary>
    public static class Skinning
    {
        public static Mat34[] WorldFromLocal(StudioModel m, Vec3[] pos, Quat[] rot)
        {
            var w = new Mat34[m.Bones.Count];
            for (int i = 0; i < w.Length; i++)
            {
                var local = Mat34.FromQuatPos(rot[i], pos[i]);
                int p = m.Bones[i].Parent;
                w[i] = p >= 0 ? w[p] * local : local;
            }
            return w;
        }

        /// <summary>Bone-merge (TF2 "c_model" weapons): child bones named like a parent bone copy its world matrix.</summary>
        public static Mat34[] BoneMerge(StudioModel child, StudioModel parent, Mat34[] parentWorld)
        {
            var w = new Mat34[child.Bones.Count];
            for (int i = 0; i < w.Length; i++)
            {
                int pb = parent.FindBone(child.Bones[i].Name);
                if (pb >= 0) { w[i] = parentWorld[pb]; continue; }
                var b = child.Bones[i];
                var local = Mat34.FromQuatPos(b.Rot, b.Pos);
                w[i] = b.Parent >= 0 ? w[b.Parent] * local : local;
            }
            return w;
        }

        /// <summary>Skin matrices: world * poseToBone.</summary>
        public static Mat34[] SkinMatrices(StudioModel m, Mat34[] world)
        {
            var s = new Mat34[world.Length];
            for (int i = 0; i < s.Length; i++) s[i] = world[i] * m.Bones[i].PoseToBone;
            return s;
        }

        public static void Skin(StudioModel m, Mat34[] skin, Vec3[] outPos, Vec3[] outNrm)
        {
            for (int v = 0; v < m.Positions.Length; v++)
            {
                int n = Math.Max(1, (int)m.BoneCounts[v]);
                Vec3 p = new Vec3(), q = new Vec3();
                float wsum = 0;
                for (int k = 0; k < n && k < 3; k++)
                {
                    float wt = m.BoneWeights[v * 3 + k];
                    int bi = m.BoneIds[v * 3 + k];
                    if (wt <= 0 || bi >= skin.Length) continue;
                    p = p + skin[bi].TransformPoint(m.Positions[v]) * wt;
                    q = q + skin[bi].TransformDir(m.Normals[v]) * wt;
                    wsum += wt;
                }
                if (wsum <= 0) { p = m.Positions[v]; q = m.Normals[v]; }
                else if (Math.Abs(wsum - 1) > 1e-3f) { p = p * (1 / wsum); }
                outPos[v] = p;
                outNrm[v] = q.Normalized;
            }
        }
    }
}
