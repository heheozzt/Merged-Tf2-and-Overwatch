using System;

namespace TF2Assets
{
    public struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public Vec3 Normalized { get { float l = Length; return l > 1e-12f ? this * (1f / l) : this; } }
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }

    /// <summary>Quaternion with Source's (x, y, z, w) layout and standard rotation-matrix convention.</summary>
    public struct Quat
    {
        public float X, Y, Z, W;
        public Quat(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
        public static Quat Identity => new Quat(0, 0, 0, 1);

        /// <summary>Source's AngleQuaternion(RadianEuler): x = roll, y = pitch, z = yaw (radians).</summary>
        public static Quat FromRadianEuler(Vec3 a)
        {
            double sy = Math.Sin(a.Z * 0.5), cy = Math.Cos(a.Z * 0.5);
            double sp = Math.Sin(a.Y * 0.5), cp = Math.Cos(a.Y * 0.5);
            double sr = Math.Sin(a.X * 0.5), cr = Math.Cos(a.X * 0.5);
            double srXcp = sr * cp, crXsp = cr * sp, crXcp = cr * cp, srXsp = sr * sp;
            return new Quat((float)(srXcp * cy - crXsp * sy), (float)(crXsp * cy + srXcp * sy),
                            (float)(crXcp * sy - srXsp * cy), (float)(crXcp * cy + srXsp * sy));
        }

        public Quat Normalized
        {
            get
            {
                float l = (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
                return l > 1e-12f ? new Quat(X / l, Y / l, Z / l, W / l) : Identity;
            }
        }

        public static Quat Slerp(Quat a, Quat b, float t)
        {
            float d = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
            if (d < 0) { b = new Quat(-b.X, -b.Y, -b.Z, -b.W); d = -d; }
            if (d > 0.9995f)
                return new Quat(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t, a.W + (b.W - a.W) * t).Normalized;
            double th = Math.Acos(d), s = Math.Sin(th);
            float wa = (float)(Math.Sin((1 - t) * th) / s), wb = (float)(Math.Sin(t * th) / s);
            return new Quat(a.X * wa + b.X * wb, a.Y * wa + b.Y * wb, a.Z * wa + b.Z * wb, a.W * wa + b.W * wb);
        }
    }

    /// <summary>Row-major 3x4 matrix (rotation | translation), Source's matrix3x4_t.</summary>
    public struct Mat34
    {
        public float M00, M01, M02, M03, M10, M11, M12, M13, M20, M21, M22, M23;

        public float this[int r, int c]
        {
            get
            {
                switch (r * 4 + c)
                {
                    case 0: return M00; case 1: return M01; case 2: return M02; case 3: return M03;
                    case 4: return M10; case 5: return M11; case 6: return M12; case 7: return M13;
                    case 8: return M20; case 9: return M21; case 10: return M22; case 11: return M23;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                switch (r * 4 + c)
                {
                    case 0: M00 = value; break; case 1: M01 = value; break; case 2: M02 = value; break; case 3: M03 = value; break;
                    case 4: M10 = value; break; case 5: M11 = value; break; case 6: M12 = value; break; case 7: M13 = value; break;
                    case 8: M20 = value; break; case 9: M21 = value; break; case 10: M22 = value; break; case 11: M23 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public static Mat34 Identity => new Mat34 { M00 = 1, M11 = 1, M22 = 1 };

        public static Mat34 FromQuatPos(Quat q, Vec3 p)
        {
            return new Mat34
            {
                M00 = 1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z,
                M10 = 2 * q.X * q.Y + 2 * q.W * q.Z,
                M20 = 2 * q.X * q.Z - 2 * q.W * q.Y,
                M01 = 2 * q.X * q.Y - 2 * q.W * q.Z,
                M11 = 1 - 2 * q.X * q.X - 2 * q.Z * q.Z,
                M21 = 2 * q.Y * q.Z + 2 * q.W * q.X,
                M02 = 2 * q.X * q.Z + 2 * q.W * q.Y,
                M12 = 2 * q.Y * q.Z - 2 * q.W * q.X,
                M22 = 1 - 2 * q.X * q.X - 2 * q.Y * q.Y,
                M03 = p.X, M13 = p.Y, M23 = p.Z,
            };
        }

        public static Mat34 operator *(Mat34 a, Mat34 b)
        {
            var r = new Mat34();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float s = a[i, 0] * b[0, j] + a[i, 1] * b[1, j] + a[i, 2] * b[2, j];
                    if (j == 3) s += a[i, 3];
                    r[i, j] = s;
                }
            }
            return r;
        }

        public Vec3 TransformPoint(Vec3 v) => new Vec3(
            M00 * v.X + M01 * v.Y + M02 * v.Z + M03,
            M10 * v.X + M11 * v.Y + M12 * v.Z + M13,
            M20 * v.X + M21 * v.Y + M22 * v.Z + M23);

        public Vec3 TransformDir(Vec3 v) => new Vec3(
            M00 * v.X + M01 * v.Y + M02 * v.Z,
            M10 * v.X + M11 * v.Y + M12 * v.Z,
            M20 * v.X + M21 * v.Y + M22 * v.Z);

        public Vec3 Translation => new Vec3(M03, M13, M23);

        /// <summary>Inverse of a rigid (rotation + translation) matrix.</summary>
        public Mat34 InverseRigid()
        {
            var r = new Mat34
            {
                M00 = M00, M01 = M10, M02 = M20,
                M10 = M01, M11 = M11, M12 = M21,
                M20 = M02, M21 = M12, M22 = M22,
            };
            var t = r.TransformDir(Translation);
            r.M03 = -t.X; r.M13 = -t.Y; r.M23 = -t.Z;
            return r;
        }
    }

    /// <summary>Source (X forward, Y left, Z up, right-handed, inches) to Unity (X right, Y up, Z forward, left-handed).</summary>
    public static class SourceToUnity
    {
        public static Vec3 Point(Vec3 s, float scale) => new Vec3(-s.Y * scale, s.Z * scale, s.X * scale);
        public static Vec3 Dir(Vec3 s) => new Vec3(-s.Y, s.Z, s.X);

        /// <summary>Converts a Source rotation (as matrix rows) into a Unity-space rotation matrix (row-major 3x3).</summary>
        public static float[] Rotation(Mat34 m)
        {
            // U = P * R * P^T where P maps source axes to unity axes.
            // P rows: ux = -sy, uy = sz, uz = sx.
            int[] src = { 1, 2, 0 };
            float[] sign = { -1, 1, 1 };
            var r = new float[9];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    r[i * 3 + j] = sign[i] * sign[j] * m[src[i], src[j]];
            return r;
        }

        /// <summary>Quaternion (x,y,z,w) for a row-major 3x3 rotation matrix.</summary>
        public static Quat QuatFromMatrix(float[] m)
        {
            float trace = m[0] + m[4] + m[8];
            float x, y, z, w;
            if (trace > 0)
            {
                float s = (float)Math.Sqrt(trace + 1.0) * 2;
                w = 0.25f * s; x = (m[7] - m[5]) / s; y = (m[2] - m[6]) / s; z = (m[3] - m[1]) / s;
            }
            else if (m[0] > m[4] && m[0] > m[8])
            {
                float s = (float)Math.Sqrt(1.0 + m[0] - m[4] - m[8]) * 2;
                w = (m[7] - m[5]) / s; x = 0.25f * s; y = (m[1] + m[3]) / s; z = (m[2] + m[6]) / s;
            }
            else if (m[4] > m[8])
            {
                float s = (float)Math.Sqrt(1.0 + m[4] - m[0] - m[8]) * 2;
                w = (m[2] - m[6]) / s; x = (m[1] + m[3]) / s; y = 0.25f * s; z = (m[5] + m[7]) / s;
            }
            else
            {
                float s = (float)Math.Sqrt(1.0 + m[8] - m[0] - m[4]) * 2;
                w = (m[3] - m[1]) / s; x = (m[2] + m[6]) / s; y = (m[5] + m[7]) / s; z = 0.25f * s;
            }
            return new Quat(x, y, z, w).Normalized;
        }
    }
}
