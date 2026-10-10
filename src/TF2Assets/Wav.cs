using System;
using System.IO;

namespace TF2Assets
{
    /// <summary>RIFF WAVE → float samples (interleaved). PCM 8/16/24/32-bit, IEEE float, MS-ADPCM and IMA-ADPCM.</summary>
    public sealed class WavSound
    {
        public int Channels;
        public int SampleRate;
        public float[] Samples;   // interleaved, length = frames * Channels
        public int Frames => Channels > 0 ? Samples.Length / Channels : 0;

        public static WavSound Decode(byte[] f)
        {
            if (f.Length < 12 || f[0] != 'R' || f[1] != 'I' || f[2] != 'F' || f[3] != 'F' || f[8] != 'W' || f[9] != 'A' || f[10] != 'V' || f[11] != 'E')
                throw new InvalidDataException("Not a RIFF WAVE file");
            int p = 12;
            int fmtTag = 0, channels = 0, rate = 0, blockAlign = 0, bits = 0, fmtOff = -1, fmtLen = 0;
            int dataOff = -1, dataLen = 0;
            while (p + 8 <= f.Length)
            {
                string id = new string(new[] { (char)f[p], (char)f[p + 1], (char)f[p + 2], (char)f[p + 3] });
                int len = BitConverter.ToInt32(f, p + 4);
                int body = p + 8;
                if (len < 0 || body + len > f.Length) len = f.Length - body;
                if (id == "fmt ")
                {
                    fmtTag = BitConverter.ToUInt16(f, body);
                    channels = BitConverter.ToUInt16(f, body + 2);
                    rate = BitConverter.ToInt32(f, body + 4);
                    blockAlign = BitConverter.ToUInt16(f, body + 12);
                    bits = BitConverter.ToUInt16(f, body + 14);
                    fmtOff = body; fmtLen = len;
                    if (fmtTag == 0xFFFE && len >= 26) fmtTag = BitConverter.ToUInt16(f, body + 24); // extensible: subformat GUID
                }
                else if (id == "data") { dataOff = body; dataLen = len; }
                p = body + len + (len & 1);
            }
            if (fmtOff < 0 || dataOff < 0) throw new InvalidDataException("WAVE file without fmt/data chunk");
            if (channels < 1) channels = 1;
            var w = new WavSound { Channels = channels, SampleRate = rate };
            switch (fmtTag)
            {
                case 1: w.Samples = Pcm(f, dataOff, dataLen, bits); break;
                case 3: w.Samples = Float(f, dataOff, dataLen, bits); break;
                case 2: w.Samples = MsAdpcm(f, dataOff, dataLen, channels, blockAlign, f, fmtOff, fmtLen); break;
                case 0x11: w.Samples = ImaAdpcm(f, dataOff, dataLen, channels, blockAlign); break;
                default: throw new NotSupportedException("WAVE format tag 0x" + fmtTag.ToString("X"));
            }
            return w;
        }

        static float[] Pcm(byte[] f, int o, int len, int bits)
        {
            int bps = bits / 8;
            int n = len / bps;
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                int q = o + i * bps;
                switch (bits)
                {
                    case 8: s[i] = (f[q] - 128) / 128f; break;
                    case 16: s[i] = BitConverter.ToInt16(f, q) / 32768f; break;
                    case 24: s[i] = ((f[q] | f[q + 1] << 8 | f[q + 2] << 16) << 8 >> 8) / 8388608f; break;
                    case 32: s[i] = BitConverter.ToInt32(f, q) / 2147483648f; break;
                    default: throw new NotSupportedException(bits + "-bit PCM");
                }
            }
            return s;
        }

        static float[] Float(byte[] f, int o, int len, int bits)
        {
            if (bits != 32) throw new NotSupportedException(bits + "-bit float");
            var s = new float[len / 4];
            for (int i = 0; i < s.Length; i++) s[i] = BitConverter.ToSingle(f, o + i * 4);
            return s;
        }

        static readonly int[] AdaptTable = { 230, 230, 230, 230, 307, 409, 512, 614, 768, 614, 512, 409, 307, 230, 230, 230 };
        static readonly int[,] DefaultCoef = { { 256, 0 }, { 512, -256 }, { 0, 0 }, { 192, 64 }, { 240, 0 }, { 460, -208 }, { 392, -232 } };

        static float[] MsAdpcm(byte[] f, int o, int len, int ch, int blockAlign, byte[] fmt, int fmtOff, int fmtLen)
        {
            int samplesPerBlock = fmtLen >= 20 ? BitConverter.ToUInt16(fmt, fmtOff + 18) : (blockAlign - 7 * ch) * 2 / ch + 2;
            int numCoef = fmtLen >= 22 ? BitConverter.ToUInt16(fmt, fmtOff + 20) : 7;
            var coef = new int[Math.Max(7, numCoef), 2];
            for (int i = 0; i < 7; i++) { coef[i, 0] = DefaultCoef[i, 0]; coef[i, 1] = DefaultCoef[i, 1]; }
            for (int i = 0; i < numCoef && fmtOff + 22 + i * 4 + 4 <= fmtOff + fmtLen; i++)
            {
                coef[i, 0] = BitConverter.ToInt16(fmt, fmtOff + 22 + i * 4);
                coef[i, 1] = BitConverter.ToInt16(fmt, fmtOff + 24 + i * 4);
            }
            int blocks = (len + blockAlign - 1) / blockAlign;
            var outS = new float[blocks * samplesPerBlock * ch];
            int w = 0;
            var pred = new int[ch]; var delta = new int[ch]; var s1 = new int[ch]; var s2 = new int[ch];
            for (int b = 0; b < blocks; b++)
            {
                int p = o + b * blockAlign;
                int end = Math.Min(o + len, p + blockAlign);
                if (p + 7 * ch > end) break;
                for (int c = 0; c < ch; c++) pred[c] = Math.Min(f[p++], coef.GetLength(0) - 1);
                for (int c = 0; c < ch; c++) { delta[c] = BitConverter.ToInt16(f, p); p += 2; }
                for (int c = 0; c < ch; c++) { s1[c] = BitConverter.ToInt16(f, p); p += 2; }
                for (int c = 0; c < ch; c++) { s2[c] = BitConverter.ToInt16(f, p); p += 2; }
                for (int c = 0; c < ch; c++) outS[w++] = s2[c] / 32768f;
                for (int c = 0; c < ch; c++) outS[w++] = s1[c] / 32768f;
                int produced = 2;
                int cc = 0;
                while (p < end && produced < samplesPerBlock)
                {
                    int bt = f[p++];
                    for (int nib = 0; nib < 2; nib++)
                    {
                        int code = nib == 0 ? bt >> 4 : bt & 15;
                        int signed = code >= 8 ? code - 16 : code;
                        int predicted = (s1[cc] * coef[pred[cc], 0] + s2[cc] * coef[pred[cc], 1]) / 256;
                        int sample = Clamp16(predicted + signed * delta[cc]);
                        s2[cc] = s1[cc]; s1[cc] = sample;
                        delta[cc] = Math.Max(16, AdaptTable[code] * delta[cc] / 256);
                        if (w < outS.Length) outS[w++] = sample / 32768f;
                        cc++;
                        if (cc == ch) { cc = 0; produced++; }
                    }
                }
            }
            Array.Resize(ref outS, w);
            return outS;
        }

        static readonly int[] ImaIndex = { -1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8 };
        static readonly int[] ImaStep = {
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
            130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060,
            1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
            7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767 };

        static float[] ImaAdpcm(byte[] f, int o, int len, int ch, int blockAlign)
        {
            int samplesPerBlock = (blockAlign - 4 * ch) * 8 / (4 * ch) + 1;
            int blocks = (len + blockAlign - 1) / blockAlign;
            var outS = new float[blocks * samplesPerBlock * ch];
            int frameBase = 0;
            var pred = new int[ch]; var idx = new int[ch];
            for (int b = 0; b < blocks; b++)
            {
                int p = o + b * blockAlign;
                int end = Math.Min(o + len, p + blockAlign);
                if (p + 4 * ch > end) break;
                for (int c = 0; c < ch; c++)
                {
                    pred[c] = BitConverter.ToInt16(f, p); idx[c] = Math.Min(88, Math.Max(0, (int)f[p + 2])); p += 4;
                    outS[frameBase * ch + c] = pred[c] / 32768f;
                }
                // data: per channel 4-byte chunks of 8 nibbles, interleaved by channel
                int frame = 1;
                while (p + 4 * ch <= end && frame < samplesPerBlock)
                {
                    for (int c = 0; c < ch; c++)
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            int bt = f[p + c * 4 + k / 2];
                            int code = (k & 1) == 0 ? bt & 15 : bt >> 4;
                            int step = ImaStep[idx[c]];
                            int diff = step >> 3;
                            if ((code & 1) != 0) diff += step >> 2;
                            if ((code & 2) != 0) diff += step >> 1;
                            if ((code & 4) != 0) diff += step;
                            if ((code & 8) != 0) diff = -diff;
                            pred[c] = Clamp16(pred[c] + diff);
                            idx[c] = Math.Min(88, Math.Max(0, idx[c] + ImaIndex[code]));
                            int fr = frame + k;
                            if (fr < samplesPerBlock) outS[(frameBase + fr) * ch + c] = pred[c] / 32768f;
                        }
                    }
                    p += 4 * ch;
                    frame += 8;
                }
                frameBase += samplesPerBlock;
            }
            return outS;
        }

        static int Clamp16(int v) => v < -32768 ? -32768 : v > 32767 ? 32767 : v;
    }
}
