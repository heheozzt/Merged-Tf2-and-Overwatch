// Offline oracle for the TF2 asset readers: runs against a real TF2 "tf" folder (TF2_DIR) and synthetic files.
// Usage: dotnet run -- <tf2 folder> <output folder> [sample-wav-folder]
using TF2Assets;

int failures = 0;
void Check(bool ok, string what)
{
    Console.WriteLine((ok ? "PASS " : "FAIL ") + what);
    if (!ok) failures++;
}

string tfDir = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("TF2_DIR");
string outDir = args.Length > 1 ? args[1] : "out";
string wavDir = args.Length > 2 ? args[2] : null;
Directory.CreateDirectory(outDir);

// ---------- synthetic VTF ----------
{
    // 4x4 DXT1 block: c0 = pure red (0xF800), c1 = pure blue (0x001F), indices alternate 0/1 per pixel
    var vtf = new List<byte>();
    vtf.AddRange("VTF\0"u8.ToArray());
    vtf.AddRange(BitConverter.GetBytes(7)); vtf.AddRange(BitConverter.GetBytes(2)); vtf.AddRange(BitConverter.GetBytes(80));
    vtf.AddRange(BitConverter.GetBytes((ushort)4)); vtf.AddRange(BitConverter.GetBytes((ushort)4));
    vtf.AddRange(BitConverter.GetBytes(0)); // flags
    vtf.AddRange(BitConverter.GetBytes((ushort)1)); vtf.AddRange(BitConverter.GetBytes((ushort)0)); // frames, first
    vtf.AddRange(new byte[4]); vtf.AddRange(new byte[12]); vtf.AddRange(new byte[4]); // pad, reflectivity, pad
    vtf.AddRange(BitConverter.GetBytes(1f)); // bumpscale
    vtf.AddRange(BitConverter.GetBytes(13)); // DXT1
    vtf.Add(1); // mips
    vtf.AddRange(BitConverter.GetBytes(-1)); vtf.Add(0); vtf.Add(0); // no low-res
    vtf.AddRange(BitConverter.GetBytes((ushort)1)); // depth
    while (vtf.Count < 80) vtf.Add(0);
    vtf.AddRange(BitConverter.GetBytes((ushort)0xF800)); vtf.AddRange(BitConverter.GetBytes((ushort)0x001F));
    vtf.AddRange(BitConverter.GetBytes(0x44444444u)); // pixel i: index = (i odd) ? 1 : 0  -> 0b01 per odd 2-bit slot
    var img = VtfImage.Decode(vtf.ToArray());
    Check(img.Width == 4 && img.Height == 4, "VTF DXT1 size");
    Check(img.Rgba[0] == 255 && img.Rgba[2] == 0, "VTF DXT1 pixel 0 is red");
    Check(img.Rgba[4 * 1 + 0] == 0 && img.Rgba[4 * 1 + 2] == 255, "VTF DXT1 pixel 1 is blue");
}

// ---------- VMT ----------
{
    var m = VmtMaterial.Parse("\"VertexLitGeneric\"\n{\n\t\"$basetexture\" \"models/x/y\" // c\n\t\"Proxies\" { \"Sine\" { \"resultVar\" \"$alpha\" } }\n\t$translucent 1\n}");
    Check(m.Shader == "VertexLitGeneric" && m.BaseTexture == "models/x/y" && m.Translucent && m.Get("resultVar") == null, "VMT parse");
    var p = VmtMaterial.Parse("patch { include \"materials/a.vmt\" replace { \"$basetexture\" \"b\" } }");
    Check(p.Shader == "patch" && p.Get("include") == "materials/a.vmt" && p.BaseTexture == "b", "VMT patch parse");
}

// ---------- WAV: decoded samples must match ffmpeg's decode of the same file (<name>.f32 next to it) ----------
if (wavDir != null && Directory.Exists(wavDir))
{
    foreach (var f in Directory.GetFiles(wavDir, "*.wav"))
    {
        try
        {
            var w = WavSound.Decode(File.ReadAllBytes(f));
            var refBytes = File.ReadAllBytes(Path.ChangeExtension(f, ".f32"));
            var reference = new float[refBytes.Length / 4];
            Buffer.BlockCopy(refBytes, 0, reference, 0, refBytes.Length);
            int n = Math.Min(reference.Length, w.Samples.Length);
            float worst = 0;
            for (int i = 0; i < n; i++) worst = Math.Max(worst, Math.Abs(reference[i] - w.Samples[i]));
            Check(n > 0 && Math.Abs(reference.Length - w.Samples.Length) <= w.Channels * 2048 && worst < 0.002f,
                $"WAV {Path.GetFileName(f)}: {w.Channels}ch {w.SampleRate}Hz, {w.Samples.Length} vs ffmpeg {reference.Length} samples, worst diff {worst:0.0000}");
        }
        catch (Exception ex) { Check(false, $"WAV {Path.GetFileName(f)}: {ex.Message}"); }
    }
}

// ---------- real Valve VTFs from a package given in VTF_VPK ----------
var vtfVpk = Environment.GetEnvironmentVariable("VTF_VPK");
if (!string.IsNullOrEmpty(vtfVpk) && File.Exists(vtfVpk))
{
    var pkg = new VpkPackage(vtfVpk);
    foreach (var path in pkg.Paths.Where(x => x.EndsWith(".vtf")))
    {
        try
        {
            pkg.TryRead(path, out var data);
            var img = VtfImage.Decode(data, 256);
            long sum = 0; for (int i = 0; i < img.Rgba.Length; i++) sum += img.Rgba[i];
            Check(img.Width > 0 && img.Height > 0, $"real VTF {path}: v7.{BitConverter.ToInt32(data, 8)} fmt {BitConverter.ToInt32(data, 52)} -> {img.Width}x{img.Height}, mean {sum / (double)img.Rgba.Length:0.0}");
            Raster.WritePpm(Path.Combine(outDir, "vtf_" + Path.GetFileNameWithoutExtension(path) + ".ppm"), img.Width, img.Height, img.Rgba);
        }
        catch (Exception ex) { Check(false, $"real VTF {path}: {ex.Message}"); }
    }
}

if (string.IsNullOrEmpty(tfDir) || !Directory.Exists(tfDir))
{
    Console.WriteLine("No TF2 folder given; skipping real-file tests.");
    return failures == 0 ? 0 : 1;
}

var fs = new Tf2FileSystem(tfDir, Console.WriteLine);
Console.WriteLine($"TF2 packages: {fs.Packages.Count}");

StudioModel Load(string path)
{
    var m = StudioModel.Load(fs, path);
    int tris = m.SubMeshes.Sum(s => s.Indices.Count) / 3;
    bool inRange = m.SubMeshes.All(s => s.Indices.All(i => i >= 0 && i < m.Positions.Length));
    Check(m.Positions.Length > 0 && tris > 0 && inRange, $"{path}: v{m.Version} {m.Positions.Length} verts, {tris} tris, {m.SubMeshes.Count} submeshes, indices in range");

    // Skeleton self-check: world bind * poseToBone == identity
    var bind = m.BindPose();
    float worst = 0;
    for (int i = 0; i < bind.Length; i++)
    {
        var id = bind[i] * m.Bones[i].PoseToBone;
        for (int r = 0; r < 3; r++) for (int c = 0; c < 4; c++)
            worst = Math.Max(worst, Math.Abs(id[r, c] - (r == c ? 1 : 0)));
    }
    Check(worst < 1e-3f, $"{path}: bind*poseToBone identity (worst {worst:E1})");

    // Skinning at bind pose reproduces raw positions
    var pos = new Vec3[m.Positions.Length]; var nrm = new Vec3[m.Positions.Length];
    Skinning.Skin(m, Skinning.SkinMatrices(m, bind), pos, nrm);
    float worstV = 0;
    for (int i = 0; i < pos.Length; i++) worstV = Math.Max(worstV, (pos[i] - m.Positions[i]).Length);
    Check(worstV < 1e-2f, $"{path}: bind-pose skinning matches raw verts (worst {worstV:E1})");

    // Winding: in Source, which way do triangle normals face relative to vertex normals?
    int agree = 0, total = 0;
    foreach (var s in m.SubMeshes)
        for (int k = 0; k + 2 < s.Indices.Count; k += 3)
        {
            var a = m.Positions[s.Indices[k]]; var b = m.Positions[s.Indices[k + 1]]; var c = m.Positions[s.Indices[k + 2]];
            var n = Vec3.Cross(b - a, c - a);
            var vn = m.Normals[s.Indices[k]] + m.Normals[s.Indices[k + 1]] + m.Normals[s.Indices[k + 2]];
            if (n.Length < 1e-9f) continue;
            total++;
            if (Vec3.Dot(n, vn) > 0) agree++;
        }
    Console.WriteLine($"     winding: cross(b-a,c-a) agrees with vertex normals on {agree}/{total} tris");
    foreach (var s in m.SubMeshes)
        Console.WriteLine($"     material {s.MaterialIndex}: {m.ResolveMaterial(fs, s.MaterialIndex) ?? "(unresolved)"}");
    return m;
}

var rl = Load("models/weapons/c_models/c_rocketlauncher/c_rocketlauncher.mdl");
var sl = Load("models/weapons/c_models/c_stickybomb_launcher/c_stickybomb_launcher.mdl");
var rocket = Load("models/weapons/w_models/w_rocket.mdl");
var sticky = Load("models/weapons/w_models/w_stickybomb.mdl");
var sarms = Load("models/weapons/c_models/c_soldier_arms.mdl");
var darms = Load("models/weapons/c_models/c_demo_arms.mdl");

Console.WriteLine($"rocket bounds {Bounds(rocket.Positions)}  sticky bounds {Bounds(sticky.Positions)}");
var muzzle = rl.FindAttachment("muzzle");
Check(muzzle != null, "rocket launcher has a muzzle attachment");

var sAnim = StudioModel.Load(fs, sarms.IncludeModels[0], geometry: false);
var dAnim = StudioModel.Load(fs, darms.IncludeModels[0], geometry: false);

void RenderSeq(StudioModel arms, StudioModel anims, StudioModel weapon, string seq, string tag)
{
    var anim = DecodedAnimation.Decode(anims, seq, arms);
    Check(anim.NumFrames > 0, $"{seq}: {anim.NumFrames} frames @ {anim.Fps} fps, looping={anim.Looping}");
    var lp = new Vec3[arms.Bones.Count]; var lr = new Quat[arms.Bones.Count];
    foreach (var t in new[] { 0f, anim.Duration * 0.25f, anim.Duration * 0.5f })
    {
        anim.Sample(t, lp, lr);
        var world = Skinning.WorldFromLocal(arms, lp, lr);
        var ap = new Vec3[arms.Positions.Length]; var an = new Vec3[ap.Length];
        Skinning.Skin(arms, Skinning.SkinMatrices(arms, world), ap, an);
        var ww = Skinning.BoneMerge(weapon, arms, world);
        var wp = new Vec3[weapon.Positions.Length]; var wn = new Vec3[wp.Length];
        Skinning.Skin(weapon, Skinning.SkinMatrices(weapon, ww), wp, wn);
        var b = Bounds(ap.Concat(wp));
        bool sane = b.max.Length < 200 && b.min.Length < 200;
        Check(sane, $"{seq} t={t:0.00}s posed bounds {b.min} .. {b.max}");
        string file = Path.Combine(outDir, $"{tag}_{seq}_{t:0.00}.ppm");
        Raster.ViewModel(file, 640, 480, 54f, new[] { (arms, ap, an), (weapon, wp, wn) });
        if (t == 0)
        {
            int mb = weapon.FindAttachment("muzzle")?.Bone ?? -1;
            if (mb >= 0)
            {
                var m = ww[mb] * weapon.FindAttachment("muzzle").Local;
                Console.WriteLine($"     muzzle at {m.Translation} (eye space, inches)");
            }
        }
    }
}
RenderSeq(sarms, sAnim, rl, "dh_idle", "soldier");
RenderSeq(sarms, sAnim, rl, "dh_fire", "soldier");
RenderSeq(sarms, sAnim, rl, "dh_draw", "soldier");
RenderSeq(darms, dAnim, sl, "sb_idle", "demo");
RenderSeq(darms, dAnim, sl, "sb_fire", "demo");
RenderSeq(darms, dAnim, sl, "sb_draw", "demo");

// Textures and sounds if this install has them
foreach (var (m, name) in new[] { (rl, "rl"), (sl, "sl"), (rocket, "rocket"), (sticky, "sticky"), (sarms, "sarms"), (darms, "darms") })
{
    foreach (var s in m.SubMeshes)
    {
        var vmtPath = m.ResolveMaterial(fs, s.MaterialIndex);
        if (vmtPath == null) continue;
        try
        {
            var vmt = VmtMaterial.Load(fs, vmtPath);
            var vtfPath = "materials/" + vmt.BaseTexture + ".vtf";
            if (!fs.TryRead(vtfPath, out var tex)) { Console.WriteLine($"     (texture not in this install: {vtfPath})"); continue; }
            var img = VtfImage.Decode(tex, 512);
            Check(img.Width > 0, $"texture {vtfPath} {img.Width}x{img.Height}");
            Raster.WritePpm(Path.Combine(outDir, $"tex_{name}_{s.MaterialIndex}.ppm"), img.Width, img.Height, img.Rgba);
        }
        catch (Exception ex) { Check(false, $"material {vmtPath}: {ex.Message}"); }
    }
}
foreach (var snd in new[] { "sound/weapons/rocket_shoot.wav", "sound/weapons/stickybomblauncher_shoot.wav", "sound/weapons/stickybomblauncher_det.wav", "sound/weapons/explode1.wav" })
{
    if (!fs.TryRead(snd, out var data)) { Console.WriteLine($"     (sound not in this install: {snd})"); continue; }
    var w = WavSound.Decode(data);
    Check(w.Frames > 0, $"{snd}: {w.Channels}ch {w.SampleRate}Hz {w.Frames / (double)w.SampleRate:0.00}s");
}

Console.WriteLine(failures == 0 ? "ALL PASSED" : $"{failures} FAILED");
return failures == 0 ? 0 : 1;

static (Vec3 min, Vec3 max) Bounds(IEnumerable<Vec3> pts)
{
    var mn = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue); var mx = new Vec3(float.MinValue, float.MinValue, float.MinValue);
    foreach (var p in pts)
    {
        mn = new Vec3(Math.Min(mn.X, p.X), Math.Min(mn.Y, p.Y), Math.Min(mn.Z, p.Z));
        mx = new Vec3(Math.Max(mx.X, p.X), Math.Max(mx.Y, p.Y), Math.Max(mx.Z, p.Z));
    }
    return (mn, mx);
}

static class Raster
{
    /// Renders posed meshes as seen from the TF2 eye (origin, looking down +X, Z up) with a horizontal FOV at 4:3.
    public static void ViewModel(string path, int w, int h, float hfov, (StudioModel m, Vec3[] p, Vec3[] n)[] parts)
    {
        var rgb = new byte[w * h * 4];
        var z = new float[w * h];
        Array.Fill(z, float.MaxValue);
        for (int i = 0; i < w * h; i++) { rgb[i * 4] = 40; rgb[i * 4 + 1] = 44; rgb[i * 4 + 2] = 52; rgb[i * 4 + 3] = 255; }
        float f = (float)(w / 2 / Math.Tan(hfov * Math.PI / 360));
        var light = new Vec3(0.3f, 0.5f, 0.8f).Normalized;
        int partIx = 0;
        foreach (var (m, p, n) in parts)
        {
            foreach (var s in m.SubMeshes)
            {
                for (int k = 0; k + 2 < s.Indices.Count; k += 3)
                {
                    var tri = new[] { s.Indices[k], s.Indices[k + 1], s.Indices[k + 2] };
                    var sp = new (float x, float y, float d)[3];
                    bool behind = false;
                    for (int j = 0; j < 3; j++)
                    {
                        var v = p[tri[j]];
                        if (v.X < 1f) { behind = true; break; }
                        sp[j] = (w / 2f - v.Y / v.X * f, h / 2f - v.Z / v.X * f, v.X);
                    }
                    if (behind) continue;
                    var nn = (n[tri[0]] + n[tri[1]] + n[tri[2]]).Normalized;
                    float shade = 0.35f + 0.65f * Math.Max(0, Vec3.Dot(nn, light));
                    byte cr = (byte)(shade * (partIx == 0 ? 220 : 160)), cg = (byte)(shade * (partIx == 0 ? 170 : 160)), cb = (byte)(shade * (partIx == 0 ? 140 : 175));
                    int x0 = (int)Math.Max(0, Math.Floor(Math.Min(sp[0].x, Math.Min(sp[1].x, sp[2].x))));
                    int x1 = (int)Math.Min(w - 1, Math.Ceiling(Math.Max(sp[0].x, Math.Max(sp[1].x, sp[2].x))));
                    int y0 = (int)Math.Max(0, Math.Floor(Math.Min(sp[0].y, Math.Min(sp[1].y, sp[2].y))));
                    int y1 = (int)Math.Min(h - 1, Math.Ceiling(Math.Max(sp[0].y, Math.Max(sp[1].y, sp[2].y))));
                    float area = (sp[1].x - sp[0].x) * (sp[2].y - sp[0].y) - (sp[2].x - sp[0].x) * (sp[1].y - sp[0].y);
                    if (Math.Abs(area) < 1e-6f) continue;
                    for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                        {
                            float px = x + 0.5f, py = y + 0.5f;
                            float w0 = ((sp[1].x - px) * (sp[2].y - py) - (sp[2].x - px) * (sp[1].y - py)) / area;
                            float w1 = ((sp[2].x - px) * (sp[0].y - py) - (sp[0].x - px) * (sp[2].y - py)) / area;
                            float w2 = 1 - w0 - w1;
                            if (w0 < 0 || w1 < 0 || w2 < 0) continue;
                            float d = w0 * sp[0].d + w1 * sp[1].d + w2 * sp[2].d;
                            int idx = y * w + x;
                            if (d >= z[idx]) continue;
                            z[idx] = d;
                            rgb[idx * 4] = cr; rgb[idx * 4 + 1] = cg; rgb[idx * 4 + 2] = cb;
                        }
                }
            }
            partIx++;
        }
        WritePpm(path, w, h, rgb);
    }

    public static void WritePpm(string path, int w, int h, byte[] rgba)
    {
        using var fs = File.Create(path);
        var hdr = System.Text.Encoding.ASCII.GetBytes($"P6\n{w} {h}\n255\n");
        fs.Write(hdr);
        var row = new byte[w * 3];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++) { int i = (y * w + x) * 4; row[x * 3] = rgba[i]; row[x * 3 + 1] = rgba[i + 1]; row[x * 3 + 2] = rgba[i + 2]; }
            fs.Write(row);
        }
    }
}
