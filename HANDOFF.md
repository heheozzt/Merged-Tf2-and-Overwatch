# Handoff: TF2 explosive jumping inside White Knuckle (working title "Rocket Knuckle")

This is everything a new session needs to carry on, on the user's Windows PC where both games are installed.
It covers:

- the agreed design and every decision behind it;
- what is built and how it was checked;
- what is left, in order;
- the risks and open questions;
- the full contents of every file this work created (Appendix).

- **Repository:** https://github.com/heheozzt/Merged-Tf2-and-Overwatch
- **Branch:** `claude/peaceful-clarke-udidyg`

The repo name still says "Overwatch". The project is TF2 × White Knuckle; renaming the repo is up to the user.

> **Status in one line:** the TF2 file readers (VPK, models, animations, textures, materials, sounds) are
> written. The VPK, model, animation and material readers pass tests against real TF2 files; the VTF and WAV
> decoders pass only on synthetic and Valve platform files, so TF2's own textures and sounds still need testing
> on the PC (step 0.4). The design sheets, preflight and code generator are written.
> **The White Knuckle plugin itself (10 system files) is not written yet.** Nothing has been built for the
> game, tested in game, packaged or uploaded to Melty, and no Melty draft exists.

---

## 0. Quick start for the next session (on the PC)

1. Clone and check out the branch:
   ```
   git clone -c core.autocrlf=false -b claude/peaceful-clarke-udidyg https://github.com/heheozzt/Merged-Tf2-and-Overwatch
   cd Merged-Tf2-and-Overwatch
   ```
2. Tools the work needs. Install them from their official sources if missing.
   - .NET SDK 8: https://dotnet.microsoft.com/download/dotnet/8.0
   - Python 3.10+
   - git
3. Find the two game folders. They are usually under `<Steam library>\steamapps\common\`, and
   `Steam\steamapps\libraryfolders.vdf` lists every library.
   - **White Knuckle**: Steam app 3195790, folder `White Knuckle`, containing `White Knuckle.exe` and `White Knuckle_Data\Managed\`.
   - **Team Fortress 2**: Steam app 440, folder `Team Fortress 2`, containing `tf\tf2_misc_dir.vpk`, `tf\tf2_textures_dir.vpk` and `tf\tf2_sound_misc_dir.vpk`.
4. Re-run the TF2 reader tests against the **full** TF2 install. The cloud session could only test models and
   animations: the TF2 dedicated-server files it could download contain no sound or texture archives.
   ```
   dotnet run -c Release --project tests/TF2Assets.Tests -- "C:\...\Team Fortress 2\tf" out
   ```
   - Expect every model, skeleton and animation check to PASS.
   - This time the texture and sound checks must also PASS: rocket launcher, stickybomb launcher and arms
     textures, plus `rocket_shoot`, `stickybomblauncher_shoot`, `stickybomblauncher_det` and `explode1`.
   - The `.ppm` renders it writes to `out\` are the viewmodel oracle: TF2's arms and launcher as seen from the eye.
5. Run the sheet generator, then the preflight (section 3.1 gives the full preflight command line):
   ```
   python tools/gen_sheets.py
   python tools/preflight.py --tf2 "C:\...\Team Fortress 2\tf" --api api055_full.txt --mods refs --keycodes keycodes.txt
   ```
   Section 3.1 also shows how to make `api055_full.txt`, `keycodes.txt` and `refs\`.
6. Melty access. The user gives the token: a fresh "Publish" prompt from Melty Studio contains it.
   - Put it in the environment as `MELTY_TOKEN`, or register the MCP server with
     `claude mcp add --transport http melty https://melty.gg/api/mcp --header "Authorization: Bearer <token>"`.
   - **Never write the token into this repo, commits, screenshots or logs.**
   - `tools/dev/melty.py` calls one tool over HTTP and reads the token only from `MELTY_TOKEN`.

---

## 1. The mashup: what the user asked for and every answer they gave

### 1.1 Original request (verbatim)
> merge the explosion physics of soldier and demo so i can rocket and sticky jump inside of white knuckle.
> also add a way to toggle between the tf2 mode with explosives and white knuckles hands

The user picked **Team Fortress 2** and **White Knuckle** on Melty's Create page. No Melty project existed.

### 1.2 Interview answers (all agreed; do not re-ask)
| Question | User's choice | Meaning |
|---|---|---|
| How the launchers sit in your hands | **Swap like TF2** | One key (**Q**) swaps White Knuckle hands ↔ TF2 loadout. In TF2 mode **1** = Rocket Launcher, **2** = Stickybomb Launcher, **left click** fires, **right click** detonates stickies. Press Q again for the hands back. |
| How much TF2 you see and hear | **Full TF2 look** | The real Rocket Launcher and Stickybomb Launcher held on screen (with the Soldier and Demoman arms and their animations), real rocket and sticky projectile models, and TF2's fire, detonation and explosion sounds. All of it is read from the player's own TF2 install at runtime. |
| How punishing | **Unlimited** | No clips, no reloads, **no self-damage**. TF2's fire rates stay. Max 8 stickies stays (a TF2 rule, not ammo). |
| What blasts do to White Knuckle's world | **Shove loose objects** | Blasts launch you, and also fling White Knuckle's loose physics objects and dropped items. **Not creatures.** |
| Solo or with friends | **Solo** | White Knuckle is single-player. No `recipe.multiplayer`. |
| Jump feel | **TF2-true** | Same launch as TF2 relative to your body, with **TF2-style air control (air strafing) while flying from a blast**. |

### 1.3 Roles on Melty (fixed)
- **Host / primary: White Knuckle** (`custom-white-knuckle`). It is not in Melty's catalog, so it is an installed custom host. The player is in White Knuckle.
- **Companion: Team Fortress 2** (`team-fortress-2`). Melty checks the player has it and passes its folder as `{game:team-fortress-2}`. The mod reads TF2's real models, animations, textures and sounds from there. It ships none of them.
- TF2 does **not** run at the same time, so no `recipe.together` is needed.
- Never label TF2 as "secondary".

### 1.4 What Melty research showed (done in the cloud session)
- `search_mashups` (TF2 + White Knuckle, "white knuckle", "rocket jump"): **nothing similar exists.** The user was not told anything, since nothing close exists.
- The closest working recipes, kept as examples:
  - `half-life-climbed` (White Knuckle as the second game). Recipe: mode installed, `launch.kind "exe"` `{managed}/HLClimbed.exe --hl {game}`.
  - `isaac-s-brimstone-in-tf2` (TF2 host). It uses `recipe.setup` (a converter run once, waiting on a done-file).
  - Both list their other game as "secondary", which this project must **not** copy.
- `game_info team-fortress-2`:
  - Source engine. Mod folder `{game}/tf/custom`. **Melty installs no loader for TF2.**
  - Sell policy is tips-only (Valve's terms allow free mods only).
  - The console socket (`-netconport`) is restricted on VAC servers. Irrelevant here, because TF2 is only read from disk.
- `game_info custom-white-knuckle`:
  - Not in the catalog, so there is no loader config.
  - Use `{game}` for its folder. `launch.kind "game"` starts it through its store.
  - An `exe` launch can name `{game}/White Knuckle.exe`.
  - No account risk is flagged for either game, so per the user's instructions there are no ban or anti-cheat warnings.
- Because Melty installs no loader for White Knuckle, **BepInEx 5.4.23.5 (x64, LGPL-2.1) is bundled in the release**. Its licence allows redistribution; include its licence text.

### 1.5 Technical decisions already made (Claude's call; the user only decides game design)
1. **Route:** White Knuckle is Unity with Mono (Unity 2022.3 LTS, exact patch unknown). Its modding community
   uses **BepInEx 5** plus HarmonyX (Thunderstore `BepInEx-BepInExPack-5.4.2305`). The mod is a BepInEx 5
   plugin targeting `netstandard2.1`. There is no official White Knuckle code-mod API.
2. **No compile-time dependency on White Knuckle's code.** Every White Knuckle member is resolved **by name at
   runtime** (HarmonyLib `AccessTools`) from the `hooks` sheet, and each is logged found or missing at startup.
   - A required hook that is missing makes the mod switch itself off with a message.
   - An optional one falls back as its sheet row says.
   - Reason: the developer-approved reference DLLs (`WhiteKnuckle.GameLibs` 0.55.18 on nuget.org) are version
     0.55, while the live game is **Beta 0.61e** and names have drifted.
   - On the PC, prefer checking names against the user's real `White Knuckle_Data\Managed\Assembly-CSharp.dll`.
3. **TF2 content is read from the player's install by code written in this repo** (`src/TF2Assets`, no
   third-party code). Readers: VPK v1/v2 plus loose files; MDL v44–49 + VVD + DX90 VTX; bone-by-frame
   animations; VTF 7.0–7.5 (DXT1/3/5 and the uncompressed formats); VMT; WAV (PCM 8/16/24/32, float,
   MS-ADPCM, IMA-ADPCM).
4. **Viewmodel:** TF2's modern viewmodels are `c_<class>_arms.mdl`, animated by `c_<class>_animations.mdl`,
   with the `c_` weapon **bone-merged** onto the arms (weapon bones copy the arms' bones of the same name).
   - Soldier sequences: `dh_draw`, `dh_idle` (looping), `dh_fire`.
   - Demoman sequences: `sb_draw`, `sb_idle` (looping), `sb_fire`.
   - The plan is CPU skinning per frame (arms ~2–3k vertices, weapon ~8k).
5. **Viewmodel field of view:** TF2 draws viewmodels at `viewmodel_fov 54` (horizontal at 4:3), which is
   **41.81° vertical**. White Knuckle's camera FOV differs, so scale the viewmodel **along the camera's forward
   axis** by `t = tan(41.81°/2) / tan(vfovCam/2)` (a wider camera needs the model brought closer, t < 1). That makes its on-screen size match TF2 at any camera FOV
   (a uniform scale about the eye would not change the projection).
6. **Where to draw the viewmodel:** White Knuckle has `ENT_Player.inventoryCam`, a separate camera for the
   hands and held items, so the hands never clip into walls. Put the viewmodel on a layer that camera renders.
   - Fallback: the mod's own overlay camera with `clearFlags = Depth` and a higher depth, rendering a free layer.
   - White Knuckle's hands are 2D sprites (`Hand.handSprite` / `handForegroundSprite`).
7. **Shader availability is a runtime unknown.** `Shader.Find` only returns shaders included in the build.
   Use a fallback chain and log which one won:
   - viewmodel: `Standard` → `Legacy Shaders/Diffuse` → `Unlit/Texture` → the shader of an opaque
     `MeshRenderer` already in the scene → `Sprites/Default`;
   - world projectiles: prefer White Knuckle's own world shader so fog and palette match.
   - If only an unlit shader is found, bake TF2-style half-Lambert lighting into vertex colours.
8. **Coordinate conversion** (`SourceToUnity` in `MathTypes.cs`):
   - Source is X forward, Y left, Z up (right-handed). Unity is X right, Y up, Z forward (left-handed).
     `unity = (-y, z, x) * scale`.
   - Measured on real TF2 models: `cross(b-a, c-a)` points **against** the stored vertex normals for TF2
     triangles (0/6931 agree on the sticky launcher).
   - After the handedness flip, the same index order should face correctly in Unity. Keep TF2's index order,
     and **verify in game** (if faces look inside out, reverse every triangle).
   - Textures: VTF rows are decoded top row first. Loading them into a Unity `Texture2D` as-is flips the image
     vertically, which cancels Source's v-down UVs, so keep TF2 UVs unchanged. Verify in game.
9. **Units and "TF2-true" scaling:** keep TF2 trajectories the same shape relative to the body.
   - `s` = White Knuckle body height (m) / 82 HU, in metres per HU.
   - `k = sqrt(g_wk * s / 800)` converts HU/s to m/s.
   - Convert lengths ×s, speeds ×k, accelerations ×k²/s.
   - **Weapon timers stay in real seconds:** fire interval, charge time and arm time (TF2 feel).
   - White Knuckle's velocity field `ENT_Player.vel` is **not m/s**. The open-source BloodbugMode uses
     `vel * 175 = m/s` (unverified).
   - `g_wk` is unknown. The **Calibration** system measures it, the vel→m/s factor and the body height in
     flight; defaults until then come from the `movement` sheet.
10. **Fall damage and no self-damage:** a postfix on `ENT_Player.GetCurrentMaxFallDistance` adds the height
    gained from your own blasts (apex minus launch height) while blast-jumping. Falling back to where you
    launched is never worse than not jumping; falling below it counts normally. A blast also calls
    `ResetFallingHeight()` when it exists.
11. **Leaderboards:** a run where explosives were used must never post to White Knuckle's leaderboards.
    - Set `CL_GameManager.gamemode.allowLeaderboardScoring = false` for that run, and restore it on a new run.
    - Prefix `SteamManager.UploadGamemodeResults` to skip the upload (the same pattern as BloodbugMode and
      ParkourKnuckle).
12. **Keys:** read with Unity's legacy `Input.GetKey` (as other White Knuckle mods do) and rebindable in the
    BepInEx config. Defaults: Q, Alpha1, Alpha2, Mouse0, Mouse1.
    - In 0.61 the game uses **Rewired** (BloodbugMode looks bindings up with
      `Rewired.ReInput.players.GetPlayer(0)`). Use that through reflection to warn if a mod key collides with
      a game binding.
    - Ignore mod keys while input is locked or paused.
13. **How the plugin finds TF2:** in order,
    1. the command-line argument `--rocketknuckle-tf2 <path>`, which Melty fills from `{game:team-fortress-2}` in `launch.args`;
    2. the environment variable `ROCKETKNUCKLE_TF2`;
    3. the BepInEx config entry;
    4. a scan of Steam's `libraryfolders.vdf` for `steamapps/common/Team Fortress 2`.

    If TF2 is not found: an on-screen message, and TF2 mode is disabled. Never ask the player for a path.
14. **Threading:** parse TF2 files on a worker thread, and create Unity objects (Mesh, Texture2D, AudioClip,
    Material) on the main thread. Use `AudioClip.Create` + `SetData`, and `Texture2D` RGBA32 +
    `LoadRawTextureData`.

### 1.6 The TF2 rules being reproduced (all numbers are in the sheets)
- **Rocket Launcher** (Soldier):
  - 1100 HU/s, straight flight, fires every 0.8 s.
  - Spawns at eye + (23.5 forward, 12 right, −3 up) HU, aimed at what the crosshair trace (2000 HU) hits.
  - Explodes on impact.
- **Stickybomb Launcher** (Demoman):
  - Hold to charge over 4 s; release, or reach full charge, to fire. Speed is 900→2400 HU/s, +200 HU/s along
    view-up, ±10 HU/s jitter, gravity 800 HU/s².
  - Spawns at eye + (16, 8, −6). Fires every 0.6 s.
  - Sticks to surfaces and arms after 0.8 s.
  - Right click detonates the armed ones. Max 8: a 9th detonates the oldest.
  - Air-detonation radius is ×0.85, ramping back to ×1 over 2 s if it never touched a surface.
- **Self-blast knockback** (TF2 `DamageForce` / `ApplyPushFromDamage`):
  - `dist = min(|P − feet|, |P − centre|)` in HU.
  - If `dist > selfRadius` (× the air-det scale for stickies) there is no push; otherwise
    `damage = lerp(base, base·edgeDamageFraction, clamp01(dist/selfRadius))`. The same rule applies to loose objects with radiusHU.
  - Multiply damage by the self-damage scale: rocket 0.6 airborne / 1.0 grounded; sticky 0.75.
  - `force = min(1000, damage · (82 / hullZ) · scale)` HU/s, where `hullZ` = 82 standing or 55 crouched
    (the crouch bonus is ≈1.49×).
  - `scale`: rocket 10 airborne / 5 grounded; sticky 9.
  - Direction: `normalize(centre − (P − up·10 HU))`, which biases the push upward.
  - The result is added to velocity. Rocket self-radius 121 HU, sticky 146 HU, base damage 90 / 120.
- **Loose objects:**
  - Radius 146 HU, `Δv = min(1000, damage·6)` HU/s converted ×k, scaled by `min(1, 50 kg / mass)`.
  - Applied as `ForceMode.VelocityChange` to non-kinematic rigidbodies that are not the player and not part
    of a creature (`Denizen`).
- **Air control while flying from a blast** (Source `AirAccelerate`):
  - `wishspd = min(classSpeed, 30)`, `addspeed = wishspd − dot(v, wishdir)`; **if addspeed ≤ 0, add nothing**.
  - Otherwise `accel = min(10 · classSpeed · dt, addspeed)` and `v += accel · wishdir` (unit wishdir, horizontal relative to player up).
  - classSpeed is Soldier 240 or Demoman 280, following the launcher in hand.
  - While blast-jumping, set White Knuckle's `airControl` and `dragCoefficient` to 0, and restore them on
    landing or grabbing.
- Sources: Valve's Source SDK 2013 TF2 code (read for the numbers, **no code copied**) and the TF2 wiki.
  Details and URLs are in `docs/research-white-knuckle.md`.

---

## 2. What is built, and how it was checked

### 2.1 `src/TF2Assets` — TF2 content readers (done, tested)
These are plain C# (`netstandard2.0`) with no Unity dependency, so they are testable outside the game.
`tests/TF2Assets.Tests` is a console test runner.

| Check | Result |
|---|---|
| VPK directory read; 6 real models read from TF2 VPKs | PASS: `c_rocketlauncher` (v48), `c_stickybomb_launcher` (7914 verts, 6931 tris), `w_rocket` (722 v), `w_stickybomb` (1165 v), `c_soldier_arms` (2862 v, 3 submeshes), `c_demo_arms` (1979 v) |
| Skeleton: world bind × poseToBone = identity | PASS, worst error 2.5e-5 |
| Bind-pose CPU skinning reproduces raw vertices | PASS, worst error 1.3e-5 |
| Material resolution through skin table and texture dirs | PASS, e.g. `materials/models/player/soldier/soldier_hands.vmt` |
| Animations decoded from the `c_*_animations.mdl` libraries | PASS: `dh_idle` 41 frames looping, `dh_fire` 31, `dh_draw` 25, `sb_idle` 51 looping, `sb_fire` 19, `sb_draw` 31; posed bounds sane |
| Viewmodel render oracle (arms + bone-merged weapon from the eye at 54° hfov) | Looked right: the Soldier launcher on the right with its flip-up sight and the hand on the grip; the Demo sticky launcher drum on the right; the fire pose kicks; draw starts off-screen |
| Muzzle attachment in eye space (inches) | Rocket launcher in `dh_idle`: (24.2, −10.8, −5.3). Sticky launcher in `sb_idle`: (52.6, −8.6, −5.7) |
| VTF: synthetic DXT1 plus 4 real Valve VTFs | PASS: v7.1 BGR888, v7.2 DXT1, v7.2 BGRA8888, v7.3 DXT5 (images looked right) |
| VMT parse, including patch materials and nested proxy blocks | PASS |
| WAV vs ffmpeg's own decode | PASS: PCM u8/s16/s24/f32, MS-ADPCM mono and stereo, IMA-ADPCM mono and stereo, and 2 real Valve WAVs. Sample-exact except IMA, which is within 0.001 |
| TF2 **textures and sounds** from the real install | **NOT TESTED.** The free TF2 dedicated-server depot (app 232250, anonymous Steam) has their directory files but not the archives. Re-run on the PC (step 0.4) |

### 2.2 Design sheets, generator, preflight (done)
- `sheets/*.json`: 10 sheets, 82 rows, 646 cells. These are the **source of truth**; change a sheet before the code.
  - `weapons`, `projectiles`, `explosions`, `classes`, `movement`: the gameplay rules above.
  - `assets`: every TF2 file read at runtime.
  - `effects`: the blast visuals, built from TF2's own effect textures.
  - `bindings`: the controls.
  - `hooks`: 34 White Knuckle members.
  - `systems`: the 10 code files, each with the hooks and rows it uses.
- `tools/gen_sheets.py` writes `src/RocketKnuckle/Generated/Sheets.g.cs`. Each row becomes one `static readonly`
  definition built from its cells, and cross-sheet references become typed object references.
  It was run, and the output compiles (`netstandard2.1`).
- `tools/preflight.py` checks:
  - every cell is filled and every column declared;
  - every reference resolves;
  - every TF2 path exists in TF2's real VPK directories (all 21 assets PASS, including the companion
    `.vvd`/`.dx90.vtx` files);
  - every hook has evidence (all 34 found: in the 0.55 reference API and/or used by current 0.61 mods);
  - keys are real Unity KeyCodes;
  - numbers are in range;
  - the generated code matches the sheets;
  - every system's code file exists and every hook, binding and weapon row is used by code.
- **Current preflight result: 10 problems.** All of them are "`<System>.cs` not written yet":

  `Tf2Content`, `WkBridge`, `ModeToggle`, `ViewModel`, `Launchers`, `Projectiles`, `BlastPhysics`,
  `AirControl`, `Calibration`, `LeaderboardGuard`.

### 2.3 Research
`docs/research-white-knuckle.md` holds White Knuckle's engine and modding route, the API surface from the 0.55
reference DLL, the open-source mods and what they hook, TF2 paths and numbers, and the VPK/MDL/VTF format notes,
all with source URLs. White Knuckle hooks confirmed in current (0.61) open-source mods:

| Mod | Licence | What it confirms |
|---|---|---|
| senkodev/WKBloodbugMode | MIT | `ENT_Player.Movement` prefix/postfix, `vel` via `AccessTools.FieldRefAccess`, `GetCurrentMaxFallDistance`, `ResetFallingHeight`, `GetPlayerUpVector`, `IsInputLocked`, `inventoryCam`, `SteamManager.UploadGamemodeResults`, `CL_Leaderboard.WK_Leaderboard_Core.disableLeaderboards` |
| NimiusTheMans/WKParkourKnuckle | no licence: read only, don't copy | `SetDirectionalForce`, `hands`, `DropHand`, `IsCrouching`, `gamemode.allowLeaderboardScoring` |
| monksilly/ResourcefulHands | MPL-2.0 | `Hand.handModel`, hand textures and sounds swapped at runtime |
| shishyando/WK_MoreCommands | Apache-2.0 | `playerObject`, `vel`, `Movement`, `hands`, `ENT_Player.Start` |

### 2.4 Helper tools (in `tools/dev/`)
- `melty.py`: one Melty MCP call over HTTP, with the token from `MELTY_TOKEN`.
- `vpkls.py` and `vpkcat.py`: list and extract files from a VPK.
- `apidump/`: a .NET tool that dumps types and members of an assembly (it made the 0.55 API dump used by preflight).

### 2.5 Melty state
- Connected with the user's token. `list_my_mods` returned **no mods**.
- **No draft was created. Nothing was uploaded.** No `melty.json` was committed.

---

## 3. What is left, in order

### 3.1 Recreate the local evidence files for preflight (not committed: they derive from the game DLL or from other people's repos)
```
# 0.55 reference API (developer-approved package) + Unity modules + BepInEx core into one folder:
#   https://api.nuget.org/v3-flatcontainer/whiteknuckle.gamelibs/0.55.18/whiteknuckle.gamelibs.0.55.18.nupkg   (lib/netstandard2.1/*.dll)
#   https://nuget.bepinex.dev/v3/package/unityengine.modules/2022.3.62/unityengine.modules.2022.3.62.nupkg   (lib/netstandard2.0/*.dll)
#   BepInEx_win_x64_5.4.23.5.zip from https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5 (BepInEx/core/*.dll)
dotnet run -c Release --project tools/dev/apidump -- <refs>\Assembly-CSharp.dll <refs> ".*" > api055_full.txt
dotnet run -c Release --project tools/dev/apidump -- <refs>\UnityEngine.CoreModule.dll <refs> "^UnityEngine.KeyCode$"   # keep the field names (4th column of " F static" lines) one per line → keycodes.txt
# Better on the PC: also dump the REAL game's White Knuckle_Data\Managed\Assembly-CSharp.dll (0.61e) and pass that with --api.
# Current-version mods used as evidence (clone into refs\):
git clone --depth 1 https://github.com/senkodev/WKBloodbugMode
git clone --depth 1 https://github.com/NimiusTheMans/WKParkourKnuckle
git clone --depth 1 https://github.com/monksilly/ResourcefulHands
git clone --depth 1 https://github.com/shishyando/WK_MoreCommands
```

### 3.2 Create the plugin project `src/RocketKnuckle/RocketKnuckle.csproj`
- `netstandard2.1`, `LangVersion 9`.
- Compile `../TF2Assets/*.cs` into the same DLL, so there is one plugin DLL.
- `PackageReference`s, all `PrivateAssets=all`, compile only:
  - `BepInEx.Core` 5.4.21 from `https://nuget.bepinex.dev/v3/index.json`;
  - `UnityEngine.Modules`, matching the game's Unity version (read it from `UnityPlayer.dll` file properties or
    from the first lines of `BepInEx\LogOutput.log`; 2022.3.62 was the guess);
  - HarmonyX comes with BepInEx.Core.
- **Do not reference White Knuckle's Assembly-CSharp at compile time** (decision 1.5.2).
- Plugin GUID suggestion: `melty.rocketknuckle`. Config file: `BepInEx/config/melty.rocketknuckle.cfg`.

### 3.3 Write the 10 system files (`src/RocketKnuckle/*.cs`), each driven by the generated sheet definitions

| File | What it does |
|---|---|
| `WkBridge.cs` | Resolves every `Hooks.*` row with AccessTools by type, member and args, and logs `hook <id> OK/MISSING (required?)`. Exposes typed accessors (`ref Vector3 Vel(player)`, `IsHanging`, `IsCrouching`, `UpVector`, `InputLocked`, `DropHand`, `SetLocked`, `HandModel`, `GetMoveVector`, …). Installs the Harmony prefixes and postfixes named in the sheet. A missing required hook disables the mod with an on-screen and log message. |
| `Tf2Content.cs` | Finds TF2 (decision 13) and loads every `Assets.*` row. Builds Unity meshes: Source→Unity conversion, submeshes per material, skin family 0 (RED). Materials come from VMT `$basetexture` → VTF, using the shader chain (decision 7). Builds audio clips from the WAVs and effect textures. Keeps per-weapon decoded animations (draw/idle/fire). Parses off the main thread. |
| `ModeToggle.cs` | State Hands ↔ TF2. **Into TF2 mode:** `DropHand(true)` on both hands, `SetLocked(true)`, `SetCanInteract(false)`, disable every Renderer under `handModel` (remember the states), show the viewmodel with its draw sequence and the `draw_primary` sound. **Back:** restore everything. Prefixes `GrabHold`, `GrabTarget`, `HandItem.TryUse` and `HandItem.Use` return false while in TF2 mode. Keys 1 and 2 switch launcher (with its draw sequence). On a new run (`ENT_Player.Start` postfix): back to hands, clear projectiles. |
| `ViewModel.cs` | Arms + bone-merged weapon per class. CPU skinning every frame from the current sequence time (draw → idle loop; fire plays `fireSeq`, then idle). Drawn under the inventory camera or the mod's overlay camera. Depth-scaled for TF2's 54° viewmodel FOV (decision 5). Gives the muzzle attachment's world position for visual spawn. |
| `Launchers.cs` | Fire intervals; rocket aim trace; sticky charge with charge sound and auto-fire at 4 s; right click detonates armed stickies (det sound); max 8 with oldest detonated; fire sound and fire sequence. Spawn positions use the sheet offsets × s from the eye. |
| `Projectiles.cs` | Per-frame sim with `SphereCast` from the previous to the next position (ignore the player and triggers). Rocket: straight, explodes at the hit (nudged along the normal). Sticky: ballistic with gravity converted; sticks (parented to the hit transform) with impact sound; armed after `armTime`; tracks whether it touched a surface (air-det radius). Lifetime cleanup. |
| `BlastPhysics.cs` | On explosion: random sound from the row, TF2 effect cards (fireball, fire cloud, smoke from TF2 textures, a short light). Self knockback (section 1.6) queued as an impulse and applied in the next `Movement` prefix (`vel += impulse_mps / velToMetres`). Enters blast state: launch height, `ResetFallingHeight`, air control on. Shoves loose rigidbodies. Fall-grace postfix. Crouch from `IsCrouching()`, then the `crouching` field, then the game action `Crouch`. |
| `AirControl.cs` | While in blast state (from the blast until grounded or hanging, at least `blastStateMinTime`): TF2 AirAccelerate in TF2 units (convert vel → HU/s with velToMetres and k; dt in TF2 time = dt·k/s), using the `Move` game action relative to camera yaw on the plane ⟂ player up. Zeroes `airControl` and `dragCoefficient`, then restores them. |
| `Calibration.cs` | In the `Movement` postfix while airborne, not hanging and not just blasted: sample world position and `vel` each physics step. Measure the vel→m/s factor (Δpos/Δt ÷ vel), gravity (−Δv_world/Δt along up) and body height (`cCon.height × lossyScale`). Use the median after `calibrationSamples`. Log the result and save it to config for the next launch. |
| `LeaderboardGuard.cs` | The first shot in a run marks the run: turns off `allowLeaderboardScoring` (restored on a new run) and skips `UploadGamemodeResults`. Logs it and shows a one-line HUD note ("Leaderboards are off for this run: explosives used"). |

Then run `python tools/gen_sheets.py`, then preflight. **Build only when the preflight is clean.** Commit each working step.

### 3.4 Test in the real game (needs the PC)
- Back up White Knuckle's saves and config first. Saves are under `%USERPROFILE%\AppData\LocalLow\<company>\White Knuckle`: find the exact folder.
- Copy BepInEx 5.4.23.5 x64 into the White Knuckle folder: `winhttp.dll`, `doorstop_config.ini`,
  `.doorstop_version`, `BepInEx\core\`. Put the plugin in `BepInEx\plugins\RocketKnuckle\`.
- Launch through Steam with `--rocketknuckle-tf2 "<TF2 folder>"` as a launch option.
- Read `BepInEx\LogOutput.log`: the hook report, the TF2 folder found, the shader chosen and the calibration values.
- Test checklist:
  - Q swaps the hands for the launcher: the hands disappear, let go, and can't grab.
  - 1 and 2 switch launchers, each playing its draw animation.
  - A rocket jump while crouched in the air gives a big launch; a standing rocket on the ground gives a small push.
  - Stickies charge, arm, detonate with right click, max 8.
  - Air strafing works after a blast; landing restores White Knuckle's control.
  - Props get shoved, creatures don't.
  - Falling back to the launch point does no damage.
  - Q back restores the hands fully.
  - The leaderboard guard works.
  - No errors appear in the log.
- Tune the sheets, never the code constants.
- **White Knuckle's devs asked players to uninstall mods after the 0.61 update caused crashes.** Watch for launch crashes and check the version (0.61e as of 2026-09-29).

### 3.5 Gameplay media
Capture a real screenshot, or ideally a 20–45 s clip, of this build in White Knuckle showing a rocket jump and a
sticky jump with the TF2 viewmodel. Capture only the game window: no Melty token, no private windows.

### 3.6 Package and publish on Melty (follow the user's original Melty prompt step by step)
**Release zip** (proposed name `RocketKnuckle-0.1.0.zip`):
```
winhttp.dll, doorstop_config.ini, .doorstop_version      (BepInEx 5.4.23.5 x64 doorstop)
BepInEx/core/*                                            (BepInEx 5.4.23.5 x64 core, unmodified)
BepInEx/plugins/RocketKnuckle/RocketKnuckle.dll
BepInEx/plugins/RocketKnuckle/README.md
licenses/BepInEx-LGPL-2.1.txt, licenses/HarmonyX-MIT.txt, licenses/MonoMod-MIT.txt, licenses/Mono.Cecil-MIT.txt
```
It contains no TF2 or White Knuckle files.

**Draft recipe.** It has NOT been validated yet. Run `inspect_package` → `validate_recipe` → `one_click_check`
on it and fix their findings:
```json
{
  "schemaVersion": 1,
  "mode": "installed",
  "games": [
    { "role": "primary", "slug": "custom-white-knuckle" },
    { "role": "companion", "slug": "team-fortress-2" }
  ],
  "components": [
    { "id": "main", "kind": "main", "label": "Rocket Knuckle", "fileName": "RocketKnuckle-0.1.0.zip", "required": true }
  ],
  "mappings": [
    { "component": "main", "from": "BepInEx/", "to": "{game}/BepInEx" },
    { "component": "main", "from": "winhttp.dll", "to": "{game}" },
    { "component": "main", "from": "doorstop_config.ini", "to": "{game}" },
    { "component": "main", "from": ".doorstop_version", "to": "{game}" }
  ],
  "launch": { "kind": "game", "args": ["--rocketknuckle-tf2", "{game:team-fortress-2}"] },
  "notes": {
    "firstLaunch": "Start a run, then press Q to swap your hands for the Rocket Launcher (1) or Stickybomb Launcher (2). Left click fires, right click detonates stickies, Q gives your hands back."
  },
  "requirements": []
}
```

Open recipe questions to settle with `validate_recipe` and `one_click_check`:
- Whether `launch.kind "game"` passes `args` through Steam. If not, use
  `{"kind":"exe","path":"{game}/White Knuckle.exe", ...}`; the plugin's Steam-library scan is the backup.
- Whether a mapping can target a file at the game root like this.
- Whether a player with BepInEx already installed by r2modman is affected. r2modman uses profiles, so it
  normally is not.

**Melty steps** (from the user's prompt):
1. `list_my_mods`, then `create_mod` (pass `githubRepo: "heheozzt/Merged-Tf2-and-Overwatch"`; if it says the repo
   is already published, tell the user and offer a remix).
2. `start_upload` (byte size + SHA-256), PUT the bytes, then `finish_upload`.
3. `submit_release` with the validated recipe.
4. `add_screenshot`, PUT, then `finish_screenshot`.
5. Show the user a summary and **ask before `publish`**.
6. After publishing, `mod_status`.
7. Save the validated recipe as `melty.json` at the repo root, and commit it **only once the user agrees**
   (a component's `fileName` may use `*` for the version).

**Listing.** These are provisional words; the user can change them. Rewrite them against the actual build.
- Title: **Rocket Knuckle**
- Tagline: *Swap your climbing hands for TF2's Rocket Launcher and Stickybomb Launcher and blast-jump up White Knuckle's tower.*
- Description: *Press Q in White Knuckle and your hands become the Soldier's Rocket Launcher or the Demoman's
  Stickybomb Launcher, with the real models, animations and sounds loaded from your own copy of Team
  Fortress 2. Rocket jumps and sticky jumps use TF2's own knockback rules (crouch for a bigger launch, air-strafe
  while you fly), and blasts fling the tower's loose objects. Unlimited ammo, no self-damage. Press Q again to
  climb with your hands. Single player. Runs that use explosives don't post to the leaderboards.*

**Still to ask the user, because none of these are decided:**
- **credits** (how they want to be credited);
- **content licence**;
- **whether others may remix it on Melty**.

Do not guess any of them.

---

## 4. Risks and open questions
1. **Version drift.**
   - The reference DLL is 0.55; the game is 0.61e. `ENT_Player.GetPlayerUpVector`, `ResetFallingHeight` and
     `ENT_Player.GetVelocity` exist only in 0.61; input moved to Rewired. Every hook has a fallback in the sheet.
   - Check them against the real `Assembly-CSharp.dll` on the PC and adjust the `hooks` sheet first.
2. **White Knuckle units are unknown.** `vel` ×175 = m/s comes from another mod's author and is unverified,
   and gravity is unknown. Hence the Calibration system; until it has measured, the defaults in the
   `movement` sheet apply.
3. **Shaders.** Which shaders exist in the build is unknown. See the fallback chain (decision 7).
4. **Look.** White Knuckle hands are palette-shaded sprites while TF2 viewmodels are 3D, so they may look out
   of place. This was accepted with the "Full TF2 look" choice.
5. **Sticky arm time.** The code constant (0.8 s) is in the sheet; the TF2 wiki says 0.7 s. Uncharged sticky
   speed: the code says 900 HU/s; the wiki measured 805.
6. **TF2 sound encodings** are unverified. The WAV reader handles PCM and both ADPCM kinds; a few TF2 sounds are
   MP3, but none the mod uses.
7. **Missing TF2:** if a player has no TF2, Melty's companion check should stop the install. The plugin
   still guards it.
8. **Mod conflicts** with White Knuckle mods that also patch `ENT_Player.Movement` (BloodbugMode, ParkourKnuckle).
9. The repo name says "Overwatch", which is confusing. Ask the user before renaming anything.
10. Not done yet: a universal-modder field note (`share-field-notes`) at the end, which needs the user's OK
    before any PR to that toolkit.

---

## 5. Environment notes

- **Do this on the PC:** clone https://github.com/rehan-remade/universal-modder and follow
  `skills/mod-any-game/SKILL.md` for the rest of the work (the user's prompt requires it). from the cloud session
- The toolkit used is universal-modder (https://github.com/rehan-remade/universal-modder),
  `skills/mod-any-game/SKILL.md`. Its knowledge note
  `knowledge/techniques/reading-source-engine-models-and-animations.md` matched TF2's formats.
- TF2 test files came from the **TF2 dedicated server (Steam app 232250), downloaded anonymously** with
  DepotDownloader (https://github.com/SteamRE/DepotDownloader) `-filelist`.
  - Used: `tf/*_dir.vpk`, `tf2_misc_000/001/020/023.vpk`, and `platform/` VPKs and WAVs.
  - It contains no sound or texture archives. These files were only for local tests and are never shipped
    or committed.
- On the PC, use the user's real TF2 install instead.
- Not committed (regenerate when needed):
  - the API dumps;
  - the rendered `.ppm` test images (renders of TF2 models);
  - the downloaded BepInEx and NuGet packages;
  - the cloned reference mods.


---

## 6. Independent review findings (binding: these refine or override sections 0–5)

Four independent reviewers checked this handoff: the built claims (by re-running the tests and preflight), the design maths, Windows usability and completeness.
The FOV ratio, air control, blast-radius clamp, clone command and status line are already fixed above.
`.gitignore` and `.gitattributes` are already updated. Apply everything else listed here.

### 6.1 [error] Status line (built-claims)

**Problem:** Re-running the tests shows the texture and sound readers have never run on real TF2 files. The run against tf2srv/root/tf prints "(texture not in this install: ...)" 9 times and "(sound not in this install: ...)" 4 times. The real-VTF and real-WAV checks used Valve platform files (platform_misc_dir.vpk VTFs, 2 Valve WAVs), not TF2 weapon or arms assets. Section 2.1 itself says TF2 textures and sounds are NOT TESTED, so the status line contradicts it.

**Do:** Replace with: "the TF2 file readers (VPK, models, animations, textures, materials, sounds) are written. The VPK, model, animation and material readers pass tests against real TF2 files. The VTF and WAV decoders pass on synthetic files and on Valve platform files only; TF2's own textures and sounds still need testing on the PC (step 0.4)."

### 6.2 [gap] 0 step 4 / 2.1 (TF2 textures and sounds NOT TESTED) (built-claims)

**Problem:** In tests/TF2Assets.Tests/Program.cs, a texture or sound missing from the install is only printed as "(texture not in this install: ...)" or "(sound not in this install: ...)". It does not count as a failure, and the run still ends with "ALL PASSED" and exit code 0 (confirmed on the server-depot run). The next session could see ALL PASSED and wrongly conclude that textures and sounds were verified.

**Do:** Add: "The runner does NOT fail when a texture or sound is missing; it prints '(texture not in this install: …)' and still says ALL PASSED. Success here means: no '(… not in this install' lines, 9 'PASS texture materials/…vtf WxH' lines (one per material: rl, sl, rocket, sticky, 3 soldier arms, 2 demo arms) and 4 'PASS sound/weapons/….wav: …' lines."

### 6.3 [gap] 2.1 VTF and WAV rows (built-claims)

**Problem:** The handoff never says how to reproduce the real-VTF and WAV checks. The real-VTF check runs only when the env var VTF_VPK points at a VPK; it was platform/platform_misc_dir.vpk, giving materials/vgui/white.vtf, vgui/vtfnotloaded.vtf, debug/defaultlightmap.vtf and engine/box.vtf. The WAV check runs only with a third argument: a folder of .wav files, each with a <name>.f32 ffmpeg decode beside it. Neither the folder nor its files are in the repo. The step 0.4 command passes neither, so on the PC these rows silently do not run.

**Do:** Add under the table: "To re-run the real-VTF rows: set VTF_VPK=<Team Fortress 2>\platform\platform_misc_dir.vpk (first arg may be \"\"). To re-run the WAV rows: pass a third arg, a folder of test .wav files, each with a reference made by `ffmpeg -i X.wav -f f32le -acodec pcm_f32le X.f32` (test files: ffmpeg-generated PCM u8/s16le/s24le/f32le, adpcm_ms mono/stereo, adpcm_ima_wav mono/stereo, plus 2 Valve WAVs). This folder was not committed."

### 6.4 [gap] 0 step 4 (built-claims)

**Problem:** Run from the repo root, this writes `out\*.ppm` into the repo: renders of TF2 meshes and, on the PC, decoded TF2 textures (tex_*.ppm). .gitignore does not ignore `out/` or `*.ppm`, so a later `git add .` would commit content derived from TF2, against the repo's own "never commit game files or extracted content" rule.

**Do:** Use an output folder outside the repo (e.g. `%TEMP%\tf2out`), or add `/out/` and `*.ppm` to .gitignore before running.

### 6.5 [nit] 2.1 table, VPK/models row (built-claims)

**Problem:** Every other model lists its counts, but the rocket launcher has none. The actual output is "v48 6783 verts, 6916 tris, 1 submeshes", which the next session needs to compare its own PC run against.

**Do:** `c_rocketlauncher` (v48, 6783 verts, 6916 tris), `c_stickybomb_launcher` (7914 verts, 6931 tris), ... `c_demo_arms` (1979 v, 2 submeshes)

### 6.6 [error] 1.5 decision 5 (Viewmodel field of view) (design-math)

**Problem:** The ratio is inverted. On-screen size ∝ size / (depth · tan(vfov/2)). Matching TF2 needs depth' · tan(vfovCam/2) = depth · tan(41.81°/2), so depth' = depth · tan(41.81°/2) / tan(vfovCam/2). A larger camera FOV needs the model pulled CLOSER, not pushed further. Multiplying depth by the stated t pushes it further, so at e.g. a 90° vertical FOV the viewmodel shrinks to about 0.15× its intended size instead of filling the screen as in TF2. The ViewModel.cs row in 3.3 inherits the error through 'decision 5'.

**Do:** Replace with: "scale the viewmodel along the camera's forward axis (multiply view-space depth) by `t = tan(41.81°/2) / tan(vfovCam/2)`, where vfovCam is the vertical `fieldOfView` of the camera that actually draws the viewmodel (the inventory camera or the mod's overlay camera). A larger camera FOV gives t < 1 and brings the model closer. On the mod's own overlay camera it is simpler to set `fieldOfView = 41.81` and use no scale."

### 6.7 [error] 1.6 Air control (design-math)

**Problem:** Source's AirAccelerate returns early when addspeed <= 0. As written, min(...) gives a negative accel whenever the player already moves faster than 30 HU/s along wishdir (true for almost every blast jump), so v += accel·wishdir would brake the player whenever they hold a key in the direction of travel. That would break blast jumps.

**Do:** Write: "`addspeed = wishspd − dot(v, wishdir)`; **if addspeed ≤ 0, add nothing**; otherwise `accel = min(10 · classSpeed · dt, addspeed)` and `v += accel · wishdir` (wishdir has unit length, and only horizontal relative to player up)."

### 6.8 [gap] 1.6 Self-blast knockback (design-math)

**Problem:** It does not say what happens when dist > selfRadius. A plain lerp extrapolates, so the push drops below half and then goes negative (a pull) instead of stopping. TF2 uses RemapValClamped and only affects entities inside the radius.

**Do:** Write: "If dist > selfRadius × (air-det radius scale, for stickies), no push. Otherwise `damage = lerp(base, base·edgeDamageFraction, clamp01(dist/selfRadius))`." Apply the same rule to loose objects with radiusHU.

### 6.9 [gap] 1.5 decision 6 / 3.3 ViewModel.cs (design-math)

**Problem:** TF2 viewmodel vertices sit only a few HU in front of the eye. At s ≈ 1.8/82 ≈ 0.022 m/HU that is about 2–20 cm, and less again once the depth scale for a wide camera FOV is applied. Unity's default near clip plane is 0.3 m. The next session will likely see the arms and launcher clipped away or missing and not know why.

**Do:** Add: "The viewmodel is scaled by s (metres per HU) and sits about 0.02–0.5 m from the eye. Check the drawing camera's `nearClipPlane` and log it. On the mod's overlay camera set it to about 0.01. If the inventory camera's near plane is larger, use the overlay camera."

### 6.10 [gap] 1.6 / 3.3 (AirControl, BlastPhysics) (design-math)

**Problem:** The movement sheet has `maxVelocityHU` 3500 (sv_maxvelocity per axis), but no handoff section and no system row in 3.3 says to apply it. Repeated rocket or sticky stacking can then exceed TF2's limit, and the sheet value is never used.

**Do:** Add to 1.6 and to the BlastPhysics/AirControl rows: "After adding knockback or air acceleration, clamp each component of velocity in TF2 units (converted with k) to ±maxVelocityHU (3500), as Source's CheckVelocity does."

### 6.11 [nit] 1.6 Stickybomb Launcher (design-math)

**Problem:** The explosions sheet column `airDetRampTime` says the 2 s are "seconds after arming", so the ramp starts at 0.8 s. The handoff leaves out the start point, so the ramp could be coded from launch. It also does not say that the scale applies to selfRadiusHU, which is what matters for sticky jumps.

**Do:** Write: "Air-detonation radius is ×airDetRadiusScale (0.85) if the sticky never touched a surface, ramping linearly to ×1 from arming (0.8 s) to arming + airDetRampTime (2.8 s after launch). It scales both radiusHU and selfRadiusHU."

### 6.12 [error] 3.1 (windows-continuity)

**Problem:** As written, the four `git clone` commands run from the repo root, so the mods land next to `src/` and not in `refs\`. `preflight --mods refs` then finds no `.cs` files, and the hooks that have no 0.55 evidence fail. preflight.py's grep_mods walks the `--mods` folder and names each hit by its first path part, so it expects a folder that holds the cloned repos (the old session used /home/user/refs/<Repo>/).

**Do:** Put the target folder in each clone: `git clone --depth 1 https://github.com/senkodev/WKBloodbugMode refs/WKBloodbugMode` (and the same for refs/WKParkourKnuckle, refs/ResourcefulHands, refs/WK_MoreCommands). Add: "`--mods` takes the folder that contains these four clones."

### 6.13 [error] 3.1 (windows-continuity)

**Problem:** In Windows PowerShell 5.1 (the Windows 11 default), `>` writes UTF-16LE. preflight.parse_api reads the dump as UTF-8 and finds no `== ` lines, so it sees zero types and every hook loses its 0.55 evidence. check_keys opens keycodes.txt with the locale encoding (cp1252), so every modkey binding is reported as not a KeyCode. Separately, `<refs>` is undefined, and it is easy to confuse with the different `refs\` folder that holds the mod clones (the old session used two folders: deps/refs for DLLs and /home/user/refs for mods). Unquoted backslash paths also break in Git Bash, the shell Claude Code's Bash tool uses on Windows: `wkrefs\Assembly-CSharp.dll` becomes `wkrefsAssembly-CSharp.dll`.

**Do:** Name the DLL folder separately, e.g. `wkrefs/`, and use forward slashes. Give the command as: `dotnet run -c Release --project tools/dev/apidump -- wkrefs/Assembly-CSharp.dll wkrefs ".*" | Out-File -Encoding utf8 api055_full.txt` (PowerShell 5.1), or with plain `>` in cmd, Git Bash or PowerShell 7. Add: "the dump and keycodes.txt must be UTF-8/ASCII; PowerShell 5.1 `>` output is UTF-16 and preflight will misread it."

### 6.14 [gap] 3.1 (windows-continuity)

**Problem:** No runnable command is given for keycodes.txt; the next session has to work out the filtering. The recipe is right: apidump prints `  F Int32 value__` and then `  F static KeyCode None` and so on, and splitting on whitespace gives F/static/KeyCode/None. But the step is prose inside a code block. In cmd, the `#` lines are not comments: pasting gives "'#' is not recognized", and the inline `# keep ...` text becomes extra arguments.

**Do:** Replace it with a runnable command and move the comments out of the code block. PowerShell: `dotnet run -c Release --project tools/dev/apidump -- wkrefs/UnityEngine.CoreModule.dll wkrefs "^UnityEngine.KeyCode$" | Select-String '^  F static ' | ForEach-Object { ($_.Line.Trim() -split ' ')[3] } | Set-Content -Encoding ascii keycodes.txt`. Git Bash: `... | awk '/^  F static /{print $4}' > keycodes.txt`. Expected: 337 lines, starting None, Backspace, Delete.

### 6.15 [gap] 3.1 (windows-continuity)

**Problem:** There are no download or extract commands. On Windows, PowerShell 5.1's `Expand-Archive` refuses a `.nupkg` (it requires a .zip extension), so the obvious approach fails. All three URLs return 200 to a HEAD request, which shows the format is right. The direct BepInEx asset URL is https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip.

**Do:** Add commands: `mkdir wkrefs, dl` (PowerShell) or `mkdir wkrefs dl` (cmd); `curl.exe -L -o dl/gl.nupkg <gamelibs url>`; `curl.exe -L -o dl/um.nupkg <unityengine.modules url>`; `curl.exe -L -o dl/bpx.zip https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip`. Then extract each into its own folder under dl/ with `tar -xf` (Windows' bundled bsdtar reads zip/nupkg), and copy gl `lib/netstandard2.1/*.dll`, um `lib/netstandard2.0/*.dll` and bpx `BepInEx/core/*.dll` into wkrefs/.

### 6.16 [error] 3.1 / 0.4 (windows-continuity)

**Problem:** .gitignore covers only bin/, obj/, /dist/, /build/ and TF2 file extensions. It does not cover refs/, out/, *.ppm, api055_full.txt, keycodes.txt, wkrefs/ or dl/. Section 3.3 says "Commit each working step", so a `git add -A` would commit four other people's repos as embedded gitlinks (ParkourKnuckle has no licence) and the .ppm renders of TF2 models. Section 5 says these must never be committed.

**Do:** Add a step before 0.4: append `/refs/`, `/wkrefs/`, `/dl/`, `/out/`, `*.ppm`, `api*.txt`, `keycodes.txt` to .gitignore and commit that. Or tell the session to keep all of them outside the repo (e.g. `%USERPROFILE%\rk-work\`) and give the paths to preflight and the tests.

### 6.17 [error] 0.1 (windows-continuity)

**Problem:** Git for Windows defaults to core.autocrlf=true, which checks the sheets/*.json files out with CRLF. preflight.py and gen_sheets.py hash the raw sheet bytes. I checked: LF gives 385c9a01989827c7 (the value committed in Sheets.g.cs) and CRLF gives 94fba11bf7337375. On the PC, preflight reports "Sheets.g.cs is out of date". After regenerating, a Sheets.g.cs is committed that fails on any LF checkout, and the hash line flips with every machine.

**Do:** Clone with `git clone -c core.autocrlf=false -b claude/peaceful-clarke-udidyg https://github.com/heheozzt/Merged-Tf2-and-Overwatch`. Also have the session add a `.gitattributes` with `* text=auto eol=lf` as its first commit.

### 6.18 [gap] 0.4 (windows-continuity)

**Problem:** tests/TF2Assets.Tests/Program.cs does not fail when a texture or sound is missing. It prints `(texture not in this install: ...)` or `(sound not in this install: ...)`, continues, and can still end with `ALL PASSED`. A session that checks only the last line would wrongly conclude the textures and sounds were verified.

**Do:** Add: "`ALL PASSED` is not enough. Confirm there are four `PASS sound/weapons/...wav` lines and `PASS texture materials/...` lines for rl, sl, rocket, sticky, sarms and darms. Any `not in this install` line counts as a failure."

### 6.19 [nit] 0.5 (windows-continuity)

**Problem:** Step 5 does not say what result to expect. With the code unwritten, preflight exits 1 with exactly 10 "<System>.cs not written yet" problems; I reproduced this with the old session's inputs. A new session could take that as breakage. Note too that `--tf2` must be the `tf` subfolder: check_assets joins it directly with tf2_misc_dir.vpk. The plugin and the tests accept either folder.

**Do:** Add: "Expected now: `PREFLIGHT: 10 problem(s)`, all `systems.<X>.codeFile: <X>.cs not written yet`. Anything else is a real problem. `--tf2` must point at `...\Team Fortress 2\tf`, not the game folder."

### 6.20 [gap] 3.1 (windows-continuity)

**Problem:** No command is given, and "pass that" could be read as passing the DLL itself to --api; preflight needs the text dump. The path contains spaces, so it must be quoted, and the dependency directory should be the Managed folder.

**Do:** Give the command: `dotnet run -c Release --project tools/dev/apidump -- "<WK>/White Knuckle_Data/Managed/Assembly-CSharp.dll" "<WK>/White Knuckle_Data/Managed" ".*" > api061.txt` (redirect with UTF-8 as above). Then run preflight with `--api api061.txt`. Any hook that fails only against 0.61 means the `hooks` sheet must be fixed first (risk 4.1).

### 6.21 [gap] 3.2 / 3.3 (windows-continuity)

**Problem:** No file is named for the plugin entry point. Neither the systems sheet nor the handoff has the `[BepInPlugin]` class deriving from `BaseUnityPlugin`, the place that creates the Harmony instance, binds the config, initializes the systems in order and drives the per-frame Update. The next session has to invent where it goes and the order the systems start in. preflight's check_code allows extra .cs files, so adding one is safe.

**Do:** Add an 11th, non-sheet file to 3.3: `Plugin.cs`, with `[BepInPlugin("melty.rocketknuckle", "Rocket Knuckle", "0.1.0")]` on a class deriving from `BaseUnityPlugin`. Awake: bind config (keys from `Bindings.*`, TF2 path, saved calibration); `new Harmony(guid)`; WkBridge.Init (abort if a required hook is missing); start the Tf2Content load; register the ModeToggle, Launchers, Projectiles, BlastPhysics, AirControl, Calibration and LeaderboardGuard patches. Update: mode keys and launchers. Also: "Sheets.g.cs is in src/RocketKnuckle/Generated and is picked up by the SDK's default glob."

### 6.22 [gap] 3.2 (windows-continuity)

**Problem:** Nothing tells the session to register the feed. Without a NuGet.Config, `dotnet build` searches only nuget.org and fails to find BepInEx.Core and UnityEngine.Modules. Both packages exist on the feed (bepinex.core 5.4.21 and unityengine.modules 2022.3.62 return 200). No build command or output path is given either.

**Do:** Add: "Create `NuGet.Config` at the repo root with `<add key="BepInEx" value="https://nuget.bepinex.dev/v3/index.json" />` alongside nuget.org. Build with `dotnet build -c Release src/RocketKnuckle`; the DLL is at `src/RocketKnuckle/bin/Release/netstandard2.1/RocketKnuckle.dll`."

### 6.23 [gap] 0.6 (windows-continuity)

**Problem:** On Windows, when Claude Code runs the script its stdout is a pipe, so Python encodes output as cp1252. Any non-cp1252 character in a Melty response (an arrow, emoji or ×) raises UnicodeEncodeError after the call has already run. A create_mod or finish_upload could succeed with its output lost, and a retry would duplicate it. The JSON-argument example in the script's docstring is cmd-only: PowerShell 5.1 strips the embedded quotes when calling a native exe. No PowerShell way to set the variable is given.

**Do:** Add: "On Windows run it as `python -X utf8 tools/dev/melty.py ...` (or set `PYTHONUTF8=1`). Pass arguments with `@args.json` files, not inline JSON. Set the token in PowerShell with `$env:MELTY_TOKEN = '...'` (cmd: `set MELTY_TOKEN=...`) before starting `claude`. After `claude mcp add`, restart the session so the melty tools load."

### 6.24 [gap] 3.6 (windows-continuity)

**Problem:** There is no packaging command, and the Windows defaults go wrong. Windows PowerShell 5.1's `Compress-Archive` can write entry names with backslashes, and without -Force it skips dotfiles, so `.doorstop_version` or the `BepInEx/` mapping may not match. In PowerShell 5.1, `curl` is an alias for Invoke-WebRequest, so a pasted `curl -T` PUT fails. `Get-FileHash` prints the SHA-256 in uppercase hex.

**Do:** Add: "Build the zip with Python `zipfile` (forward-slash arcnames) or with `tar -a -c -f RocketKnuckle-0.1.0.zip <entries>` from the staging folder. Check the entries with `tar -tf`. Upload with `curl.exe -T RocketKnuckle-0.1.0.zip "<put url>"`. Get the size and lowercase hash with `python -c "import hashlib,os,sys;p=sys.argv[1];print(os.path.getsize(p),hashlib.sha256(open(p,'rb').read()).hexdigest())" RocketKnuckle-0.1.0.zip`."

### 6.25 [error] 3.6 Package and publish on Melty (completeness-critic)

**Problem:** The new session on the PC does not have the user's original Melty prompt, so it cannot follow it. Only some of its rules are spread through the handoff. Several are missing or never stated as rules: ask the user only about credits, licence, remix and sign-ins (everything else is Claude's call); use universal-modder; work from the sheets and run preflight before every build; a real gameplay screenshot or clip is a hard gate before publish; never upload game content anywhere (zip, screenshots, GitHub).

**Do:** Add a subsection '1.7 Melty rules carried over from the user's prompt (binding)' with this checklist: (1) White Knuckle is primary/host and TF2 is companion; never call TF2 'secondary'. (2) Bundle what Melty doesn't install (BepInEx 5.4.23.5 plus licences). (3) Never upload or commit game content from either game. (4) Never put the Melty token in files, commits, logs or screenshots. (5) Ask the user only about credits, content licence, remix permission and any sign-ins (Steam/Melty/GitHub); decide everything else yourself. (6) Design sheets are the source of truth; change a sheet before code; run preflight before every build. (7) Use universal-modder (skills/mod-any-game/SKILL.md). (8) A real in-game screenshot or clip of this build is required before publish; never use the .ppm oracle renders or a mockup. (9) Run inspect_package, validate_recipe and one_click_check before submit_release. (10) Call create_mod with githubRepo. (11) Ask before publish. (12) Save melty.json only once the user agrees. (13) Leaderboard-safe. (14) Solo: no recipe.multiplayer. Then change the 3.6 heading to 'follow the rules in 1.7'.

### 6.26 [gap] 3.3 ModeToggle.cs (completeness-critic)

**Problem:** It does not say what happens to items the player is holding (pitons, the game's guns and other HandItems). DropHand's bool argument is undocumented, so DropHand(true) may throw the held item into the world. That would conflict with the hooks sheet, whose HandModel purpose ('hide the White Knuckle hands (and held items)') means items are hidden and kept, not dropped. The next session could ship a toggle that destroys the player's inventory.

**Do:** Add: 'Held items are kept, not dropped. TF2 mode only lets go of holds; any HandItem in a hand stays in that hand, is hidden with handModel's renderers and is blocked by the TryUse/Use prefixes, then comes back unchanged on Q. Before relying on DropHand(true), read DropHand(bool) in the real 0.61e Assembly-CSharp.dll to learn what the bool does. If it drops the held item, use the variant that releases only the handhold (or call handhold release directly) and record this in the hooks sheet. Pressing Q while hanging lets go, so the player falls (accepted consequence of Swap like TF2).'

### 6.27 [gap] 3.3 Launchers.cs / 1.6 (completeness-critic)

**Problem:** It does not say how stickies and firing behave across launcher and mode switches. Unanswered: can right click detonate stickies while the Rocket Launcher is out, or after Q back to hands? Do stickies persist through Q? Is a sticky charge cancelled on switch? Does holding left click auto-refire rockets every 0.8 s? Can you fire during the draw animation? Each session would guess differently, and lifetime 0 (never expires) makes it matter.

**Do:** Add a 'Weapon-state rules' list (TF2 behaviour unless the sheets say otherwise): placed stickies persist through launcher switches and Q toggles and are cleared only on a new run or death. Right click detonates armed stickies while either launcher is out (TF2: any Demoman weapon), but not in hands mode, where right click belongs to White Knuckle's hand. Switching launcher or toggling mode cancels a sticky charge without firing. Holding left click refires the rocket at fireInterval. Firing is blocked until drawSeq finishes; if TF2-accurate deploy time is wanted, add a deployTime column to the weapons sheet. The fire-interval timer keeps running through switches.

### 6.28 [gap] 3.3 (HUD / player feedback) (completeness-critic)

**Problem:** The handoff never specifies what the player sees on screen: the weapon-name hint the weapons sheet expects, a sticky charge meter, the active sticky count (out of 8), the message when TF2 is missing or a required asset or hook fails, or how text is drawn (IMGUI OnGUI vs White Knuckle's UI). No system file owns the HUD, so it may be left out or done inconsistently.

**Do:** Add a 'HUD and messages' paragraph and give each part an owner. ModeToggle shows weapons.displayName for ~2 s on Q, 1 or 2. Launchers draws a small TF2-style charge bar while charging and a sticky counter 'n/8' while the Stickybomb Launcher is out. All messages go through one helper drawn with IMGUI (OnGUI), bottom-centre, auto-hide after ~5 s. Fixed texts: 'Rocket Knuckle: Team Fortress 2 not found, TF2 mode disabled'; 'Rocket Knuckle: <asset> missing from TF2, TF2 mode disabled' (required asset) / log-only for optional assets; 'Rocket Knuckle: game update not supported (<hook> missing), mod disabled'; the leaderboard note. Mark the charge bar and counter as Claude's call, consistent with 'Full TF2 look', not a user decision.

### 6.29 [gap] 1.5 decision 13 / 3.3 Tf2Content.cs (completeness-critic)

**Problem:** It does not cover TF2 being found but some assets failing to load or decode. The assets sheet has a required true/false column, but the handoff never says what to do for each case (for example, missing explode2/3 vs a missing rocket launcher model), or what Q does when TF2 mode is disabled.

**Do:** Add: 'A required asset that fails to load disables TF2 mode (message plus log naming the asset). An optional asset (charge, det, land and draw sounds, explode2/3, effect textures) is skipped silently: random sound picks only among loaded rows, and blasts with no effect textures show only the light. While TF2 mode is disabled, Q does nothing except show the message again, and hands mode is untouched.'

### 6.30 [gap] 3.2 / 1.5 decision 12-13 (completeness-critic)

**Problem:** The handoff never lists the config entries. The text refers to keys (rebindable), a TF2 path entry (decision 13.3) and calibration results 'saved to config for the next launch', but gives no section or key names, defaults, or rules for when saved calibration overrides the movement sheet defaults.

**Do:** List the config schema: [Controls] ToggleMode=Q, SelectRocketLauncher=Alpha1, SelectStickybombLauncher=Alpha2, Fire=Mouse0, AltFire=Mouse1 (from the bindings sheet's configSection). [Paths] Tf2Folder='' (empty = auto). [Calibration] Measured=false, WkGravity, WkVelocityToMetres, WkBodyHeight (initial values from the movement sheet; used instead of the sheet defaults once Measured=true; re-measured each launch and overwritten). State that config holds only player settings and measured values, never tuning numbers (those stay in the sheets).

### 6.31 [gap] 3.3 BlastPhysics.cs (completeness-critic)

**Problem:** The explosion visual spec is only gestured at. The effects sheet has no card count or duration for the fire-cloud texture, no light colour, no blend mode, and no rule for scaling HU sizes. The handoff also does not say that TF2 sounds must go through White Knuckle's audio mixer (the research doc lists AudioManager.PlaySound and gameMixer), so the game's volume settings may not control them.

**Do:** Add: 'Effects use the effects.TF2Explosion row. Sizes and spread are in HU × s. Fire and fire-cloud cards are additive camera-facing quads: fireCards fireball cards plus one fire_cloud2 card at endSize, both over fireDuration. smokeCards smokelit cards are alpha-blended over smokeDuration. The light is warm orange (≈1.0, 0.6, 0.25), range lightRangeHU × s, over lightDuration. If any of these needs a number, add a column to the effects sheet first. Play every TF2 sound through an AudioSource routed to White Knuckle's SFX AudioMixerGroup (AudioManager.instance / gameMixer, resolved by name and added as an optional hook row, with plain AudioSource as fallback), so the game's volume slider applies.'

### 6.32 [nit] 1.5 decision 2 (completeness-critic)

**Problem:** The handoff doesn't list which hooks are required, and the sheet's PlayerObject row contradicts this rule: it is marked required with the fallback 'mod stays idle until a player exists'. The next session needs to know that 6 hooks are hard requirements, and that PlayerObject being null at runtime (menu) is different from the field being missing.

**Do:** Add: 'Required hooks: PlayerObject, PlayerVel, PlayerMovementPrefix, PlayerHands, HandDropHand, HandModel. Missing at startup → mod disabled. PlayerObject existing but null (main menu, loading) → idle, not disabled.'

### 6.33 [gap] 5. Environment notes (completeness-critic)

**Problem:** It is written as a past fact, not an instruction. The user's prompt requires using universal-modder, but the handoff doesn't tell the next session to install or load it on the PC, so the requirement may be dropped. The field note in risk 10 depends on it too.

**Do:** Add to section 0 a step: 'Clone https://github.com/rehan-remade/universal-modder and follow skills/mod-any-game/SKILL.md for the rest of the work (required by the user's prompt). At the end, offer a share-field-notes note and open a PR to the toolkit only with the user's OK.'

---

## Appendix: full contents of every file created in this work

Snapshot of the branch at the time of this handoff. Each file is shown in full. `README.md` came with the repository. Not included: `HANDOFF.md` itself, and the Melty token (never stored in the repo).

File list (31 files):

- `.gitattributes`
- `.gitignore`
- `README.md`
- `docs/research-white-knuckle.md`
- `sheets/assets.json`
- `sheets/bindings.json`
- `sheets/classes.json`
- `sheets/effects.json`
- `sheets/explosions.json`
- `sheets/hooks.json`
- `sheets/movement.json`
- `sheets/projectiles.json`
- `sheets/systems.json`
- `sheets/weapons.json`
- `src/RocketKnuckle/Generated/Sheets.g.cs`
- `src/TF2Assets/MathTypes.cs`
- `src/TF2Assets/StudioAnimation.cs`
- `src/TF2Assets/StudioModel.cs`
- `src/TF2Assets/TF2Assets.csproj`
- `src/TF2Assets/Vpk.cs`
- `src/TF2Assets/Vtf.cs`
- `src/TF2Assets/Wav.cs`
- `tests/TF2Assets.Tests/Program.cs`
- `tests/TF2Assets.Tests/TF2Assets.Tests.csproj`
- `tools/dev/apidump/Program.cs`
- `tools/dev/apidump/apidump.csproj`
- `tools/dev/melty.py`
- `tools/dev/vpkcat.py`
- `tools/dev/vpkls.py`
- `tools/gen_sheets.py`
- `tools/preflight.py`

### `.gitattributes`

```
* text=auto eol=lf
*.dll binary
*.zip binary
```

### `.gitignore`

```gitignore
bin/
obj/
/dist/
/build/
*.user
.vs/
# never commit game files or extracted content
/tf2cache/
*.vpk
*.mdl
*.vvd
*.vtx
*.vtf
*.wav

# local working files (never commit: other people's repos, game-derived renders and dumps)
/refs/
/wkrefs/
/dl/
/out/
*.ppm
api*.txt
keycodes.txt
```

### `README.md`

```markdown
# Merged-Tf2-and-Overwatch
```

### `docs/research-white-knuckle.md`

````markdown
# White Knuckle × TF2 rocket and sticky jumping: research notes

Researched 2026-10-10. **[V]** means I verified it from a primary source or by inspecting the file myself. **[I]** means it is inferred. **[U]** means I could not verify it.

---

## 1. White Knuckle: engine and build facts

| Item | Finding | Status |
|---|---|---|
| Engine | Unity | [V] |
| Unity version | **2022.3 LTS**. The exact patch is unknown. Mod projects compile against `UnityEngine.Modules` 2022.3.29, 2022.3.32 and 2022.3.62 (ResourcefulHands uses 2022.3.62). In Beta 0.50l (Oct 2025) the developers also patched "the Unity Security Vulnerability", and 2022.3.62f2 is one of the patched 2022.3 builds. | [I] |
| Scripting backend | **Mono**. Mods reference `White Knuckle_Data\Managed\Assembly-CSharp.dll`, and the Thunderstore pack is "BepInEx pack for Mono Unity games". | [V] |
| Exe / data folder | `White Knuckle.exe`, `White Knuckle_Data\` (with `Managed\`), and `MonoBleedingEdge\` / `UnityPlayer.dll` next to the exe | [V] |
| Managed assemblies | `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `ALINE`, `DarkMachineUI`, `DOTween`, `AstarPathfindingProject`, `Unity.InputSystem` 1.7, `Unity.TextMeshPro`, `Unity.Postprocessing.Runtime`, `Facepunch.Steamworks.Win64`, `Newtonsoft.Json` 13 | [V] (AssemblyRef table of the reference DLL) |
| Render pipeline | Probably the built-in pipeline, because it references PostProcessing Stack v2. This matters for which shaders you can `Shader.Find` at runtime. | [I] |
| Players | **Single-player only.** The Steam features are Single-player, Achievements and Family Sharing. Multiplayer exists only as a community mod. The game has in-game leaderboards (Thunderstore has a "Leaderboard Legal" tag), so a movement mod should turn off scoring. ParkourKnuckle does this with `CL_GameManager.gamemode.allowLeaderboardScoring = false`. | [V] |
| Current version | **Beta 0.61e (2026-09-29)**. In the 0.61c notes the developers asked users to uninstall mods, including BepInEx, because incompatible mods caused crashes after the update. | [V] |
| Steam app / dev | 3195790, Dark Machine Games, published by DreadXP, Early Access since 2025-04-17 | [V] |

Sources:
- https://store.steampowered.com/app/3195790/White_Knuckle/
- https://store.steampowered.com/news/app/3195790/view/1844751498235547 (0.61e)
- https://store.steampowered.com/news/app/3195790/view/1844751498226797 (0.61c crash notice)
- https://store.steampowered.com/news/app/3195790/view/1811772772546782 (0.50l Unity patch)
- csproj HintPaths and `UnityEngine.Modules` versions:
  - https://github.com/NimiusTheMans/WKParkourKnuckle/blob/HEAD/KnuckleTesting/KnuckleTesting.csproj
  - https://github.com/monksilly/ResourcefulHands/blob/HEAD/ResourcefulHands.csproj
  - https://github.com/Bryan-000/MoreFOV (README: "next to the White Knuckle.exe")

To pin down the exact Unity version, check the first lines of `BepInEx/LogOutput.log` ("Running under Unity v…") or the file version of `UnityPlayer.dll`.

## 2. Modding route

- **Thunderstore community:** https://thunderstore.io/c/white-knuckle/
  - About 400 packages in the API as of 2026-10-10.
  - API endpoint: https://thunderstore.io/c/white-knuckle/api/v1/package/
  - Mod managers: r2modman, Gale, Thunderstore Mod Manager.
- **Loader:** the loader is **BepInEx 5**, via `BepInEx-BepInExPack` **5.4.2305**, which is BepInEx 5.4.23.5.
  - Package page: https://thunderstore.io/c/white-knuckle/p/BepInEx/BepInExPack/
  - Mods declare the dependency `BepInEx-BepInExPack-5.4.2305`; older ones use `-5.4.2100`.
  - Patching is done with HarmonyX. Nobody uses BepInEx 6 or MelonLoader, which you would only need for IL2CPP.
- **Official support from the developers:**
  - **Data mods only, for hand cosmetics.** The Chimney update (Dec 2025) added a cosmetics system that "Supports user made mods for hands. Item reskin mod support will come in a later patch." Users drop folders into `White Knuckle_Data/Mods/Cosmetics/Hands`. Sources: https://store.steampowered.com/news/app/3195790/view/1818752592136791 and https://thunderstore.io/c/white-knuckle/p/WitherTheGoat/Goat_Hooves/
  - The game also has a `CL_CustomRadioLoader` class and an in-game `CommandConsole`.
  - There is **no official code-modding API.** The developers did grant permission for the publicized reference assemblies (section 3).
- **Popular mods** (total downloads, 2026-10-10):

| Mod | Downloads | Notes |
|---|---|---|
| Simple Multi Player | 29k | |
| MonkSilly-WKLib | 23k | Library |
| ResourcefulHands | 22k + 19k | Two separate listings; resource packs for textures and sounds |
| Stamina Display | 11k | |
| MoreCommands | 9.5k | |
| CustomGamemodes | 9k | |
| Parkour Knuckle | 6.4k | |
| Chaos Mod | 6.2k | |
| ModMenu | 5.9k | |
| CiCi's Trinket & Binding Framework | 12.7k | Library |
| Grappling Hook | New | Two listings, DoggoMods and DoggoMods998 |
| GibberishDevelopment Scout_TF2_Hands | | Already a TF2-themed hand pack |

### Open-source mods and the game classes they hook

1. **senkodev/WKBloodbugMode** (MIT, Oct 2026, so current for 0.61; the most relevant reference for movement), https://github.com/senkodev/WKBloodbugMode
   - Harmony patches:
     - `ENT_Player.Movement` (private): Prefix and Postfix
     - `ENT_Player.AddForce(Vector3, string)`: Prefix that rescales the force
     - `ENT_Player.Start`, `ENT_Player.OnControllerColliderHit`, `ENT_Player.GetCurrentMaxFallDistance`, `ENT_Player.GrabPropUpdate`, `ENT_Player.SetCameraFov`
     - `ENT_Player.Hand.GrabHold`
     - `UT_ForceZone.FixedUpdate`
     - `Cosmetic_HandItem.SpriteSwap`
   - Fields it reads through `AccessTools.FieldRefAccess`: `ENT_Player.vel` (Vector3), `camSway`, `infiniteStamina`, `GameEntity.grappled`.
   - It defines `VelocityToMetres = 175f` and multiplies `vel` by it to get m/s. That suggests **`vel` is not in m/s**. This is the author's constant [U].
   - It also calls `player.GetVelocity()` and `GetPlayerUpVector()`. Neither exists in the 0.55 reference DLL, so the API has changed since then.
2. **NimiusTheMans/WKParkourKnuckle** (no license file), https://github.com/NimiusTheMans/WKParkourKnuckle
   - Patches: Postfix on `ENT_Player.Update`; `ENT_Player.Damage`; `CL_GameManager.ChangeState`; `EventSystem.Update`.
   - What it uses:
     - `player.GetComponent<CharacterController>().isGrounded`
     - `player.hands` (`ENT_Player.Hand[]`), with `hand.handhold.GetHolding()`, `hand.IsHolding()`, `hand.DropHand(true)`, `hand.gripStrength` and `hand.ShakeHand()`
     - `player.cam.transform.forward`
     - **`player.SetDirectionalForce(Vector3)`** for leaps and wall-kicks
     - `player.IsCrouching()`
     - Legacy `UnityEngine.Input.GetKey` (it works alongside the Input System)
3. **nachariah/WK_ChaosMod** (no license file), https://github.com/nachariah/WK_ChaosMod. It uses `ENT_Player.playerObject.AddForce(Vector3)` and `ENT_Player.GetPlayer()`.
4. **monksilly/ResourcefulHands** (MPL-2.0), https://github.com/monksilly/ResourcefulHands
   - It swaps textures and sounds at runtime, which is a useful pattern for loading external assets.
   - Patches: `CL_CosmeticManager.ScanForCosmetics` / `CreateHandCosmetics`; `ENT_Player.HandAnimation` / `Awake`; `ViewSway.Start` / `Update`; `ENT_Player.Hand.Initialize` / `SetScale` / `ScaleUpdate`.
   - Its csproj uses Krafs.Publicizer 2.3.0.
5. **Shen-X-L/WKMultiMod** (MIT), https://github.com/Shen-X-L/WKMultiMod
   - Patches: `ENT_Player.Kill`, `GameEntity.Damage`, `Item_Object.Pickup`, `WorldLoader.Initialize`, `HandItem_Piton.PitonHit`, `Projectile`, `CL_GameManager.GetPlayerTravelDistance`, `UI_GamemodeScreen.Initialize`.
   - Don't copy its Steam appid-480 "pirated mode" patch.
6. **BarackOBusiness/WKWideAngleCamera**, https://github.com/BarackOBusiness/WKWideAngleCamera. It puts a MonoMod `ILHook` on the private `ENT_Player.SetCameraFov` and uses `ENT_Player.GetPlayer().camRoot` (`CL_CameraControl`).
7. Others:
   - **overmet15/WKStaminaDisplay**: `FindObjectOfType<ENT_Player>()`, then iterates `hands`.
   - **shishyando/WK_MoreCommands** (Apache-2.0): console commands, and a clean SDK-style csproj.
   - **monksilly/WKLib** (MIT): library for config, UI, keybinds and asset bundles.
   - **kyanite-rock/White_Knuckle_VR**: declares GPL-3.0, but the repo contains only README, LICENSE and `external/`, so its source is not published there.

### Game API surface

This is dumped from the WhiteKnuckle.GameLibs 0.55.18 reference DLL. **It is a 0.55 build, while the live game is 0.61e, so names may have drifted.**

**`ENT_Player : GameEntity`**
- Statics: `ENT_Player.playerObject` and `ENT_Player.GetPlayer()`.
- Movement fields:
  - `CharacterController cCon`
  - `Vector3 vel, curVel, lastVel, correctedVelocity, moveAxis, jumpDir`
  - `Vector2 inputAxis`
  - `float speed, sprintSpeed, crouchSpeed, jumpHeight, gravity, gravityMult, dragCoefficient, frictionMultiplier, airControl, slideFriction`
  - `bool isGrounded, wasGrounded, crouching, hasGravity, moveLocked, hasJumped`
  - `PlayerState playerState`, an enum of `{grounded, airborne, hanging}`
- Camera: `Camera cam`, `CL_CameraControl camRoot`, `Transform camTransform`.
- Hands: `ENT_Player.Hand[] hands`.
- Events: `_OnJump`, `_OnLand`, `_OnHandGrab`, `_OnHandRelease`, `_OnKill`.
- Fall damage: `curFallDistance`, `fallStartHeight`, `AddIgnoreFallDamageTime(float)`, `FallingDamage()`, `GetCurrentMaxFallDistance()`.
- Methods:
  - Forces: `AddForce(Vector3, string)`, `AddForce(Vector3, float, string, float)`, `AddForceAtPosition(Vector3, Vector3, string)`, `SetDirectionalForce(Vector3)`
  - Velocity: `ScaleVelocity(Vector3)`, `ClampVelocityToAxis(Vector3)`, `SetGravityMult(float)`
  - State: `IsGrounded(bool)`, `IsCrouching()`, `IsCrouchHeld()`, `SetCrouched(bool)`, `IsHanging()`, `DropHang(...)`, `Jump(bool, float)`, `ForceJump()`
  - Damage and position: `bool Damage(Damageable.DamageInfo)`, `Kill(string, DamageInfo)`, `Teleport(...)`
  - Private: `Movement()`
  - Console handlers: `AddForceCommand(string[])` and others

**`ENT_Player.Hand`** (nested)
- Fields: `id`, `handModel`, `handBase` (`Hand_Base`), `string fireButton` / `throwButton` (input action names), `gripStrength`, `handhold` (`CL_Handhold`), `holdVelocity`.
- Methods: `GetHandItem()`, `IsHolding()`, `IsHanging()`, `IsFree()`, `DropHand(bool)`, `GrabHold(...)`, `ShakeHand(float)`, `SetLocked(bool)`.

**Items**
- `HandItem : Hand_Base`: `Initialize(Item, ENT_Player.Hand)`, `TryUse()`, `Use()`, `StopUse()`.
- **`HandItem_Shoot`** is the game's own gun item and the closest template for a launcher. It has `GameObject projectile`, `shootSpeed`, `recoil`, `maxAmmo`, `ammo`, `canReload`, `useChargeShot` / `chargeShots`, `recoilJumpStat`, `Shoot()`, `StartReload()` and `OnProjectileHit(...)`.
- There is also a `Projectile` class.

**Input**
- `InputManager.instance`. `GetButton(string)` returns a `ButtonInput` with `Pressed`, `Down` and `Up`. There are also `GetVector(string)` and `MoveInput`.
- It is built on Unity Input System 1.7 `PlayerInput`.

**Other useful classes**
- `UT_Explosion` (`hitMask`, `damageMult`, `maxDistance`, `Explode()`), `UT_AddForceToPlayer`, `UT_ForceZone`.
- `AudioManager.instance.PlaySound(AudioClip, Vector3, …, AudioMixerGroup, string)` and `gameMixer`.
- `CommandConsole.instance.AddCommand` / `RegisterCommand`.
- `CL_GameManager.gMan` (static) and `CL_GameManager.gamemode` (static `M_Gamemode`).

## 3. Reference assemblies and templates

**WhiteKnuckle.GameLibs** on nuget.org is the reference package. [V]
- Version **0.55.18**, published 2026-05-28.
- netstandard2.1, containing `Assembly-CSharp.dll` and `GameRefs.dll`.
- Method bodies are replaced with `throw null`, and all members are publicized.
- The README says Dark Machine Games explicitly permitted it, with a condition: no trainers, cheats or other tools that break game balance.
- Version mapping: 0.55a = .1, 0.55b = .2, and so on, so .18 is 0.55r.
- **It is stale relative to 0.61.**
- Links:
  - https://www.nuget.org/packages/WhiteKnuckle.GameLibs
  - https://github.com/monksilly/White-Knuckle-References
- To use it: `<PackageReference Include="WhiteKnuckle.GameLibs" Version="0.55.*"/>`.

**Alternative that most mods use:** reference your local `White Knuckle_Data/Managed/*.dll` and publicize it.
- Krafs.Publicizer or BepInEx.AssemblyPublicizer.MSBuild.
- Add `BepInEx.Core 5.*`, `BepInEx.Analyzers`, `BepInEx.PluginInfoProps` and `UnityEngine.Modules 2022.3.x` (with `IncludeAssets="compile"`) from https://nuget.bepinex.dev/v3/index.json.
- Target `netstandard2.1`.
- WK_MoreCommands and d0tlake/Wuckle-CheatManager have a `PullGameLibs` MSBuild target that copies the game's DLLs into `lib/`.

**No White Knuckle–specific `dotnet new` template was found** on GitHub or NuGet. [V for "none found"]
- Use the generic BepInEx one:
  ```
  dotnet new install BepInEx.Templates::2.0.0-be.4 --nuget-source https://nuget.bepinex.dev/v3/index.json
  ```
- The template is `bepinex5plugin`.
- Source: https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/1_setup.html

## 4. TF2 data

### VPK file listings

The listings come from SteamDatabase GameTracking-TF2 at PatchVersion 11087207. [V]
- https://github.com/SteamDatabase/GameTracking-TF2/blob/master/tf/tf2_misc_dir.txt
- https://github.com/SteamDatabase/GameTracking-TF2/blob/master/tf/tf2_sound_misc_dir.txt
- https://github.com/SteamDatabase/GameTracking-TF2/blob/master/tf/tf2_textures_dir.txt

**Which VPK holds what** [V]:

| VPK | Contents |
|---|---|
| `tf/tf2_misc_dir.vpk` | Models (`.mdl/.vvd/.dx90.vtx/.dx80.vtx/.sw.vtx/.phy`), materials (`.vmt`), scripts (`game_sounds_weapons.txt`, encrypted `tf_weapon_*.ctx`). It contains **no** `sound/` files. |
| `tf/tf2_textures_dir.vpk` | `.vtf` textures |
| `tf/tf2_sound_misc_dir.vpk` | `sound/…` (2,757 `.wav` plus some `.mp3`) |
| `tf2_sound_vo_*` | Voice lines |

Shared Half-Life 2 content is in `hl2/hl2_*_dir.vpk`.

**Models**

Stock weapon paths come from `items_game.txt` `model_player`: https://github.com/SteamDatabase/GameTracking-TF2/blob/master/tf/scripts/items/items_game.txt

| What | Path(s) | Material (`.vmt` in misc / `.vtf` in textures) |
|---|---|---|
| Rocket Launcher (defindex 18/205, `c_` model, bonemerged to hands) | `models/weapons/c_models/c_rocketlauncher/c_rocketlauncher.mdl`, plus `.vvd`, `.dx90.vtx`, `.phy` | probably `materials/models/weapons/c_items/c_rocketlauncher.vmt/.vtf` [I] |
| Soldier arms and viewmodel animations | `models/weapons/c_models/c_soldier_arms.mdl` and `c_soldier_animations.mdl` | `materials/models/player/soldier/soldier_hands` |
| Stickybomb Launcher (20/207) | `models/weapons/c_models/c_stickybomb_launcher/c_stickybomb_launcher.mdl` | `materials/models/weapons/c_items/c_stickybomb_launcher` [I] |
| Demoman arms and animations | `models/weapons/c_models/c_demo_arms.mdl` and `c_demo_animations.mdl` | `materials/models/player/demo/demoman_hands` |
| Legacy self-animated viewmodels | `models/weapons/v_models/v_rocketlauncher_soldier.mdl`, `v_stickybomb_launcher_demo.mdl` | |
| Rocket projectile | `models/weapons/w_models/w_rocket.mdl` | `materials/models/weapons/w_rocketlauncher/w_rocket01` |
| Stickybomb projectile | `models/weapons/w_models/w_stickybomb.mdl` (`w_stickybomb2.mdl` and `w_stickybomb_d.mdl` are variants; see `tf_weapon_grenade_pipebomb.cpp`) | `materials/models/weapons/w_stickybomb/w_stickybomb_red`/`_blue` |
| World models | `models/weapons/w_models/w_rocketlauncher.mdl`, `w_stickybomb_launcher.mdl` | |

Notes on the models:
- `c_` weapons carry no animations of their own. The `*_animations.mdl` files are presumably pulled in by the arms model via `$includemodel` [I].
- `v_models` animate by themselves, which makes them simpler to port.

**Sounds**

Sound-script names come from the weapon scripts and their wav files from `game_sounds_weapons.txt`. I used third-party copies of both [V]. All are in `tf2_sound_misc_dir.vpk` and prefixed with `sound/`. Strip the `)` / `^` sound-chars that appear in script entries.

| Event | Script name | File(s) |
|---|---|---|
| Rocket fire | `Weapon_RPG.Single` (crit `.SingleCrit`) | `sound/weapons/rocket_shoot.wav` (`rocket_shoot_crit.wav`) |
| Rocket reload | `Weapon_RPG.Reload` | `sound/weapons/rocket_reload.wav` |
| Rocket explosion | `ExplosionSound` = `BaseExplosionEffect.Sound` | `sound/weapons/explode1.wav`, `explode2.wav`, `explode3.wav` (random) |
| Sticky fire | `Weapon_StickyBombLauncher.Single` | `sound/weapons/stickybomblauncher_shoot.wav` (`_shoot_crit.wav`) |
| Sticky charge | `Weapon_StickyBombLauncher.ChargeUp` | `sound/weapons/stickybomblauncher_charge_up.wav` |
| Sticky detonate (click) | `Weapon_StickyBombLauncher.ModeSwitch` (weapon "special3") | `sound/weapons/stickybomblauncher_det.wav` |
| Sticky reload | `.WorldReload` / `.BoltBack` / `.BoltForward` | `stickybomblauncher_worldreload.wav` / `_boltback.wav` / `_boltforward.wav` |
| Sticky explosion | `ExplosionSound` = `Weapon_Grenade_Pipebomb.Explode` | `sound/weapons/pipe_bomb1.wav`, `pipe_bomb2.wav`, `pipe_bomb3.wav` (all three have the same CRC and size, so they are identical files) |
| Sticky bounce | `Weapon_Grenade_Pipebomb.Bounce` | `sound/weapons/grenade_impact.wav` |
| Jumper variants | | `rocket_jumper_shoot.wav`, `rocket_jumper_explode1.wav`, `sticky_jumper_shoot.wav`, `sticky_jumper_explode1.wav` |

Sound sources:
- https://github.com/misterslin/SlinFireHUD/blob/HEAD/SlinHud/scripts/tf_weapon_rocketlauncher.txt (and `…pipebomblauncher.txt`)
- https://github.com/cueki/casual-pre-loader/blob/HEAD/backup/scripts/game_sounds_weapons.txt

The WAV encoding (PCM vs. ADPCM) is not verified [U]. Parse the RIFF header defensively, then use `AudioClip.Create` and `SetData`.

### Gameplay numbers

**Rocket Launcher (stock)**

| Stat | Value | Source |
|---|---|---|
| Speed | **1100 HU/s** (about 20.96 m/s) | `tf_weaponbase_rocket.cpp` `flLaunchSpeed = 1100.0f` [V] |
| Spawn offset from eye | fwd 23.5, right 12, up −3 HU (z = 8 when ducking) | `tf_weaponbase_gun.cpp` `FireRocket` [V] |
| Damage | 90 base; ramp-up 125% (112) down to fall-off 52.8% (48) | wiki |
| Blast radius | **146 HU** (`TF_ROCKET_RADIUS`, about 2.78 m) | code [V] |
| Radius on the shooter | **121 HU** (`TF_ROCKET_RADIUS_FOR_RJS` = 110×1.1), using the base Rocket Launcher damage | `tf_weaponbase_rocket.h` / `tf_gamerules.cpp RadiusDamage` [V] |
| Edge splash | 50% of damage | wiki "Minimum splash 50%, 2.8 m" |
| Splash falloff | `lerp(D, D·falloff, dist/radius)`; for players, dist = min(distance to origin, distance to center) | `tf_gamerules.cpp` [V] |
| Clip / reserve | 4 / 20 | wiki |
| Fire interval | 0.8 s | wiki |
| Reload | first 0.92 s, then 0.8 s each | wiki |
| Weapon script | TimeFireDelay 0.8, TimeReloadStart 0.1, TimeReload 0.83, Damage 90 | |
| Self-damage | ×0.60 when the Soldier is airborne (`tf_damagescale_self_soldier`); wiki: self-damage 27–89, rocket jump 27–46 | code [V], wiki |

**Stickybomb Launcher (stock)**

| Stat | Value | Source |
|---|---|---|
| Launch speed | forward = remap(charge time 0→**4.0 s**, **900→2400 HU/s**), plus **200 HU/s** along view-up, plus ±10 HU/s random right/up | `TF_PIPEBOMB_MIN/MAX_CHARGE_VEL`, `TF_PIPEBOMB_MAX_CHARGE_TIME`, `FirePipeBomb` [V] |
| Measured speed | 805 HU/s uncharged; about 2.3× that at full charge. This conflicts with the code constants. | wiki |
| Spawn offset | shoot position + 16 fwd + 8 right − 6 up | code [V] |
| Arm time | `tf_grenadelauncher_livetime` = **0.8 s**. The wiki table's "activation 0.7 s" conflicts with this. | code [V] |
| Air-detonation radius | ×0.85 (`tf_sticky_airdet_radius`), ramping to ×1.0 over **2.0 s** after arming (`tf_sticky_radius_ramp_time`) if the sticky hasn't touched a surface | code [V] |
| Max deployed | **8** (`TF_WEAPON_PIPEBOMB_COUNT`); a ninth detonates the oldest | code [V], wiki |
| Damage | 120 base | wiki |
| Radius | **146 HU** (script `DamageRadius 146`; the 2014-12-22 patch changed it from 159 to 146) | script, wiki |
| Self-jump radius | 146 (`TF_GRENADE_JUMP_RADIUS`) | code [V] |
| Clip / reserve | **8** / 24 | wiki |
| Fire interval | 0.6 s | wiki |
| Reload | first 1.09 s, then 0.67 s each (script TimeReload 0.67) | wiki |
| Self-damage | ×0.75 for Pipebomb/Grenade launchers, applied in `RadiusDamage` | code [V] |

**Self-blast knockback**

This is in `src/game/server/tf/tf_player.cpp` (`DamageForce`, `ApplyPushFromDamage`) [V]:

```
force = min(1000, dmg * (48*48*82) / (hull.x*hull.y*hull.z) * scale)   // HU/s, added as a velocity impulse
standing hull 48x48x82; crouched hull is 48x48x62, but self-blast substitutes z=55  =>  crouch bonus = 82/55 ≈ 1.49x
scale: Soldier DMG_BLAST self, airborne = 10 (tf_damageforcescale_self_soldier_rj); on ground = 5 (…_badrj)
       non-Soldier self (stickies) = 9 (DAMAGE_FORCE_SCALE_SELF); Pyro detonator = 8.5; hit by others = 6
dir   = normalize((inflictor.WorldSpaceCenter - (0,0,10)) - player.WorldSpaceCenter); impulse = -dir * force
        (the explosion is treated as 10 HU lower, which biases the push upward)
```

- `dmg` is the falloff-adjusted damage, captured as `DamageForForceCalc` in `ApplyOnDamageModifyRules`.
- Reading the code, Soldier's airborne ×0.6 is applied *before* that capture, and the Demoman's ×0.75 inside `RadiusDamage`. My reading of the call order is [I].
- Rough examples, ignoring ramp-up and crits:

| Case | Calculation | Result |
|---|---|---|
| Airborne, crouched Soldier, rocket at feet | 90 × 0.6 × 1.49 × 10 | ≈ 805 HU/s (≈ 15.3 m/s) |
| Airborne, crouched Demoman, sticky at feet | 120 × 0.75 × 1.49 × 9 ≈ 1208 | capped at **1000 HU/s** (≈ 19 m/s) |

**Units**

- TF2 wiki "Hammer unit": 16 HU = 1 ft, so **1 HU ≈ 1.905 cm** for maps and props. Character models use 12 HU/ft. https://wiki.teamfortress.com/wiki/Hammer_unit
- The wiki's weapon pages use 1.905 cm (146 HU ≈ 2.8 m).
- sv_gravity is 800 HU/s² (about 15.24 m/s²). This is the standard Source default; I did not re-check it this session [U].

Sources:
- https://wiki.teamfortress.com/wiki/Rocket_Launcher
- https://wiki.teamfortress.com/wiki/Stickybomb_Launcher
- https://wiki.teamfortress.com/wiki/Rocket_jump (crouching boosts knockback; Soldier gets 40% self-resistance airborne; Demoman takes 25% less self-damage)
- https://github.com/ValveSoftware/source-sdk-2013/tree/master/src/game/shared/tf (`tf_weaponbase_rocket.{h,cpp}`, `tf_weapon_pipebomblauncher.{h,cpp}`, `tf_weapon_grenade_pipebomb.cpp`, `tf_weaponbase_gun.cpp`, `tf_gamerules.cpp`, `tf_shareddefs.h`)
- https://github.com/ValveSoftware/source-sdk-2013/blob/master/src/game/server/tf/tf_player.cpp

The SDK is under the Source SDK license. Read it for the numbers; don't copy its code into the mod.

## 5. File formats and C# libraries

### VPK

Sources: https://developer.valvesoftware.com/wiki/VPK_(file_format), which lists TF2 as a v2 game, and the ValvePak source, read directly [V].

**Header**
- Fields: `uint32 sig 0x55AA1234`, `uint32 version`, `uint32 treeSize`.
- v2 adds `fileDataSectionSize`, `archiveMD5SectionSize`, `otherMD5SectionSize` (48) and `signatureSectionSize`.
- The v1 header is 12 bytes; the v2 header is 28 bytes.

**Tree** (three nested lists of null-terminated strings, each list ended by an empty string)
- `extension` → `path` → `filename`.
- `" "` (a single space) means an empty path or extension.
- Paths are lowercase with forward slashes.

**Entry**
- `uint32 CRC32`
- `uint16 PreloadBytes`
- `uint16 ArchiveIndex`
- `uint32 EntryOffset`
- `uint32 EntryLength`
- `uint16 0xFFFF` terminator
- then `PreloadBytes` bytes inline in the tree

**Data location**
- The file content is the preload bytes followed by `EntryLength` bytes, read from `<name>_<NNN>.vpk`. `NNN` is `ArchiveIndex` formatted as 3 digits.
- **`ArchiveIndex == 0x7FFF`** means the data sits in the `_dir.vpk` itself, at `headerSize + treeSize + EntryOffset`.
- After the data come the v2 sections: archive MD5 entries (`ushort archive`, `ushort hashType`, `uint offset`, `uint length`, `byte[16]`), the "other MD5" block, and the signature.

### Models and textures

**Source model formats** (Source SDK 2013 headers) [V]

| Format | Version / magic | Contents |
|---|---|---|
| MDL | `IDST`, **v48** (`STUDIO_VERSION 48` in `public/studio.h`) | Bones, body parts, textures plus cdmaterials, sequences |
| VVD | `IDSV`, **v4** | Vertices, bone weights, tangents, LOD fixups |
| VTX | `.dx90.vtx` / `.dx80.vtx` / `.sw.vtx`, **v7** (`OPTIMIZED_MODEL_FILE_VERSION 7`) | Strip groups and indices per body part/model/LOD/mesh |
| PHY | | Collision |

**VTF** [V for the header layout]
- Header: `"VTF\0"`, `version[2]`, `headerSize`, `width`, `height`, `flags`, `frames`, `startFrame`, `reflectivity`, `bumpScale`, `highResFormat`, `mipCount`, `lowResFormat` (usually a 16×16 DXT1 thumbnail), `lowResW/H`.
- 7.2 adds `depth`; 7.3 adds a resource dictionary.
- Mips are stored smallest first. Unity's `LoadRawTextureData` wants largest first, so reorder them.
- DXT1/DXT5 can be uploaded directly as `TextureFormat.DXT1`/`DXT5`.
- The SDK 2013 engine's `VTF_MAJOR/MINOR_VERSION` is **7.4**, so TF2 files are 7.x with x ≤ 4 [I].
- `c_rocketlauncher.vtf` is 1,398,360 B, which is roughly 1024² DXT5 with mips [I].

### C# libraries

| Library | License | Formats | Notes |
|---|---|---|---|
| **Sledge.Formats.Packages** + **Sledge.Formats.Texture** (LogicAndTrick) | MIT | VPK (`VpkPackage`) + VTF (`VtfFile`; "all formats VtfLib supports") | **netstandard2.0**, so it drops straight into Unity Mono. Source MDL is explicitly *unsupported*. https://github.com/LogicAndTrick/sledge-formats, NuGet 1.1.0 |
| **ValvePak** (ValveResourceFormat) | MIT | VPK v1/v2, preload, MD5 verify | Current 6.x targets **net10.0**, which is unusable in Unity. **v1.8.0.93** is the last netstandard2.1 build (deps System.IO.Hashing 8.0.0, Cryptography.Algorithms). https://github.com/ValveResourceFormat/ValvePak |
| **SourceUtils** (Metapyziks) | MIT | VPK (`ValvePackage`), MDL (`StudioModelFile`), VVD (`ValveVertexFile`, asserts v4), VTX (`ValveTriangleFile`, asserts v7), VTF (`ValveTextureFile`, DXT), VMT, BSP | .NET Framework 4.8 project; copy the sources in. Old (2016–2017). https://github.com/Metapyziks/SourceUtils |
| REDxEYE/SourceIO | MIT | Thorough MDL v44–49 / VVD / VTX / VTF / VPK | Python (Blender add-on). Good porting reference. https://github.com/REDxEYE/SourceIO |
| jackyyang09/TF2Ls-for-Unity | MIT | Editor-only; uses VTFLib (LGPL) | TF2 paint shader for the built-in pipeline. https://github.com/jackyyang09/TF2Ls-for-Unity |

Not permissively licensed:
- DeadZoneLuna/uSource (Unity MDL/BSP/VTF/VPK importer, no license file)
- lewa-j/Unity-Source-Tools (GPL-2.0)
- ZeqMacaw/Crowbar (CC BY-SA 3.0, VB.NET)

## Practical notes

- **Finding the TF2 install:**
  - Windows: registry `HKCU\Software\Valve\Steam\SteamPath`.
  - Then read `steamapps/libraryfolders.vdf` and look for app 440.
  - Install path: `<lib>/steamapps/common/Team Fortress 2/tf/`.
  - Sledge.Formats can parse VDF (`SerialisedObject`).
- **Applying knockback in WK:** use `ENT_Player.AddForce`, `SetDirectionalForce`, or write `vel` directly through `AccessTools.FieldRefAccess`. Calibrate the HU→WK unit mapping empirically: Bloodbug's `VelocityToMetres = 175` suggests `vel` is in units per fixed step [U]. Also call `AddIgnoreFallDamageTime` and release the hands with `DropHand`.
- **Leaderboards:** disable scoring (`CL_GameManager.gamemode.allowLeaderboardScoring = false`).
- **TF2 assets:** they are Valve IP. Load them only from the user's own install at runtime and never redistribute them.
- **Particles:** TF2 particles (`.pcf`, e.g. `ExplosionCore_wall` / `ExplosionCore_MidAir`) are impractical to port. Recreate them with Unity particles.
````

### `sheets/assets.json`

```json
{
  "sheet": "assets",
  "about": "Every file the mod reads from the player's own Team Fortress 2 install at runtime. Nothing here ships with the mod. A model row also needs its .vvd and .dx90.vtx next to the .mdl (except animation libraries).",
  "columns": {
    "id": "row id",
    "path": "path inside TF2's tf/ content (VPK or loose)",
    "package": "which tf2_*_dir.vpk holds it: misc, sound or textures",
    "kind": "model, animations, sound or texture",
    "required": "true: the feature it belongs to cannot work without it; false: cosmetic",
    "usedBy": "the sheet rows that use it"
  },
  "rows": [
    { "id": "mdl_c_rocketlauncher", "path": "models/weapons/c_models/c_rocketlauncher/c_rocketlauncher.mdl", "package": "misc", "kind": "model", "required": true, "usedBy": "weapons.RocketLauncher" },
    { "id": "mdl_c_stickybomb_launcher", "path": "models/weapons/c_models/c_stickybomb_launcher/c_stickybomb_launcher.mdl", "package": "misc", "kind": "model", "required": true, "usedBy": "weapons.StickybombLauncher" },
    { "id": "mdl_c_soldier_arms", "path": "models/weapons/c_models/c_soldier_arms.mdl", "package": "misc", "kind": "model", "required": true, "usedBy": "classes.Soldier" },
    { "id": "mdl_c_soldier_animations", "path": "models/weapons/c_models/c_soldier_animations.mdl", "package": "misc", "kind": "animations", "required": true, "usedBy": "classes.Soldier" },
    { "id": "mdl_c_demo_arms", "path": "models/weapons/c_models/c_demo_arms.mdl", "package": "misc", "kind": "model", "required": true, "usedBy": "classes.Demoman" },
    { "id": "mdl_c_demo_animations", "path": "models/weapons/c_models/c_demo_animations.mdl", "package": "misc", "kind": "animations", "required": true, "usedBy": "classes.Demoman" },
    { "id": "mdl_w_rocket", "path": "models/weapons/w_models/w_rocket.mdl", "package": "misc", "kind": "model", "required": true, "usedBy": "projectiles.Rocket" },
    { "id": "mdl_w_stickybomb", "path": "models/weapons/w_models/w_stickybomb.mdl", "package": "misc", "kind": "model", "required": true, "usedBy": "projectiles.Stickybomb" },
    { "id": "snd_rocket_shoot", "path": "sound/weapons/rocket_shoot.wav", "package": "sound", "kind": "sound", "required": true, "usedBy": "weapons.RocketLauncher" },
    { "id": "snd_sticky_shoot", "path": "sound/weapons/stickybomblauncher_shoot.wav", "package": "sound", "kind": "sound", "required": true, "usedBy": "weapons.StickybombLauncher" },
    { "id": "snd_sticky_charge", "path": "sound/weapons/stickybomblauncher_charge_up.wav", "package": "sound", "kind": "sound", "required": false, "usedBy": "weapons.StickybombLauncher" },
    { "id": "snd_sticky_det", "path": "sound/weapons/stickybomblauncher_det.wav", "package": "sound", "kind": "sound", "required": false, "usedBy": "weapons.StickybombLauncher" },
    { "id": "snd_sticky_land", "path": "sound/weapons/grenade_impact.wav", "package": "sound", "kind": "sound", "required": false, "usedBy": "projectiles.Stickybomb" },
    { "id": "snd_explode1", "path": "sound/weapons/explode1.wav", "package": "sound", "kind": "sound", "required": true, "usedBy": "explosions.RocketBlast" },
    { "id": "snd_explode2", "path": "sound/weapons/explode2.wav", "package": "sound", "kind": "sound", "required": false, "usedBy": "explosions.RocketBlast" },
    { "id": "snd_explode3", "path": "sound/weapons/explode3.wav", "package": "sound", "kind": "sound", "required": false, "usedBy": "explosions.RocketBlast" },
    { "id": "snd_pipe_bomb1", "path": "sound/weapons/pipe_bomb1.wav", "package": "sound", "kind": "sound", "required": true, "usedBy": "explosions.StickyBlast" },
    { "id": "snd_draw_primary", "path": "sound/weapons/draw_primary.wav", "package": "sound", "kind": "sound", "required": false, "usedBy": "systems.ModeToggle" },
    { "id": "tex_fireball", "path": "materials/effects/fireball.vtf", "package": "textures", "kind": "texture", "required": false, "usedBy": "effects.TF2Explosion" },
    { "id": "tex_fire_cloud", "path": "materials/effects/fire_cloud2.vtf", "package": "textures", "kind": "texture", "required": false, "usedBy": "effects.TF2Explosion" },
    { "id": "tex_smoke", "path": "materials/effects/smoke/smokelit.vtf", "package": "textures", "kind": "texture", "required": false, "usedBy": "effects.TF2Explosion" }
  ]
}
```

### `sheets/bindings.json`

```json
{
  "sheet": "bindings",
  "about": "Controls. Mod keys are read with Unity's legacy input (as other White Knuckle mods do) and are rebindable in BepInEx/config/melty.rocketknuckle.cfg; game actions are White Knuckle's own InputManager names.",
  "columns": {
    "id": "row id",
    "source": "modkey (mod's own, rebindable) or gameaction (White Knuckle's InputManager action)",
    "defaultKey": "Unity KeyCode name for modkey rows; action name for gameaction rows",
    "when": "when it does something",
    "action": "what it does",
    "configSection": "BepInEx config section (modkey rows) or none"
  },
  "rows": [
    { "id": "ToggleMode", "source": "modkey", "defaultKey": "Q", "when": "always in a run", "action": "swap White Knuckle hands <-> TF2 launchers", "configSection": "Controls" },
    { "id": "SelectRocketLauncher", "source": "modkey", "defaultKey": "Alpha1", "when": "TF2 mode", "action": "switch to the Rocket Launcher", "configSection": "Controls" },
    { "id": "SelectStickybombLauncher", "source": "modkey", "defaultKey": "Alpha2", "when": "TF2 mode", "action": "switch to the Stickybomb Launcher", "configSection": "Controls" },
    { "id": "Fire", "source": "modkey", "defaultKey": "Mouse0", "when": "TF2 mode", "action": "fire (stickies: hold to charge, release to launch)", "configSection": "Controls" },
    { "id": "AltFire", "source": "modkey", "defaultKey": "Mouse1", "when": "TF2 mode", "action": "detonate stickybombs", "configSection": "Controls" },
    { "id": "Move", "source": "gameaction", "defaultKey": "Move", "when": "TF2 mode, airborne after a blast", "action": "TF2 air strafing input", "configSection": "none" },
    { "id": "Crouch", "source": "gameaction", "defaultKey": "Crouch", "when": "TF2 mode", "action": "crouched blasts push harder, as in TF2", "configSection": "none" }
  ]
}
```

### `sheets/classes.json`

```json
{
  "sheet": "classes",
  "about": "The TF2 class whose arms hold each launcher and whose air control applies while it is out.",
  "columns": {
    "id": "row id",
    "armsModel": "assets row: c_<class>_arms model",
    "animationsModel": "assets row: the arms' animation library",
    "armsSkin": "skin family (0 = RED sleeves)",
    "maxSpeedHU": "class run speed; the wish speed that TF2 air acceleration uses (HU/s)",
    "source": "where the numbers come from"
  },
  "rows": [
    { "id": "Soldier", "armsModel": "mdl_c_soldier_arms", "animationsModel": "mdl_c_soldier_animations", "armsSkin": 0, "maxSpeedHU": 240.0, "source": "TF2 class speed: Soldier 240 HU/s" },
    { "id": "Demoman", "armsModel": "mdl_c_demo_arms", "animationsModel": "mdl_c_demo_animations", "armsSkin": 0, "maxSpeedHU": 280.0, "source": "TF2 class speed: Demoman 280 HU/s" }
  ]
}
```

### `sheets/effects.json`

```json
{
  "sheet": "effects",
  "about": "Blast visuals built from TF2's own effect textures (camera-facing cards that grow and fade).",
  "columns": {
    "id": "row id",
    "fireTexture": "assets row for the fireball cards",
    "fireCloudTexture": "assets row for the outer fire cloud",
    "smokeTexture": "assets row for the smoke cards",
    "fireCards": "number of fireball cards",
    "smokeCards": "number of smoke cards",
    "startSizeHU": "card size at the start",
    "endSizeHU": "card size at the end",
    "fireDuration": "seconds the fire lasts",
    "smokeDuration": "seconds the smoke lasts",
    "spreadHU": "how far cards drift from the centre",
    "lightRangeHU": "range of the blast flash light (0 = no light)",
    "lightDuration": "seconds the flash light lasts",
    "source": "where it comes from"
  },
  "rows": [
    {
      "id": "TF2Explosion",
      "fireTexture": "tex_fireball",
      "fireCloudTexture": "tex_fire_cloud",
      "smokeTexture": "tex_smoke",
      "fireCards": 6,
      "smokeCards": 5,
      "startSizeHU": 40.0,
      "endSizeHU": 150.0,
      "fireDuration": 0.45,
      "smokeDuration": 1.6,
      "spreadHU": 40.0,
      "lightRangeHU": 300.0,
      "lightDuration": 0.25,
      "source": "Textures from TF2's ExplosionCore particle materials (effects/fireball, effects/fire_cloud2, effects/smoke/smokelit); timings tuned by this mod to approximate it"
    }
  ]
}
```

### `sheets/explosions.json`

```json
{
  "sheet": "explosions",
  "about": "Blasts and the TF2 knockback they give you. Self knockback: force = min(forceCapHU, damage * selfDamageScale * (82 / hullHeight) * forceScale), damage = lerp(baseDamage, baseDamage*edgeDamageFraction, distance/selfRadius). No damage is ever applied (unlimited mode).",
  "columns": {
    "id": "row id",
    "baseDamage": "damage at the centre (used only to size the push)",
    "radiusHU": "blast radius for loose objects",
    "selfRadiusHU": "blast radius that reaches the shooter",
    "edgeDamageFraction": "damage fraction at the edge of the radius",
    "selfDamageScaleAirborne": "self-damage multiplier when airborne, applied before the push is sized",
    "selfDamageScaleGrounded": "self-damage multiplier when on the ground",
    "forceScaleAirborne": "TF2 self push scale when airborne",
    "forceScaleGrounded": "TF2 self push scale when on the ground",
    "forceCapHU": "largest push from one blast (HU/s)",
    "downBiasHU": "the blast is treated as this much lower, biasing the push upward",
    "airDetRadiusScale": "radius scale for a projectile that never touched a surface (1 = none)",
    "airDetRampTime": "seconds after arming for that scale to ramp back to 1",
    "propForceScale": "push scale for loose objects (TF2's scale for others)",
    "propReferenceMassKg": "objects heavier than this are pushed proportionally less",
    "sounds": "assets rows; one is picked at random",
    "flash": "effects row drawn at the blast",
    "source": "where the numbers come from"
  },
  "rows": [
    {
      "id": "RocketBlast",
      "baseDamage": 90.0,
      "radiusHU": 146.0,
      "selfRadiusHU": 121.0,
      "edgeDamageFraction": 0.5,
      "selfDamageScaleAirborne": 0.6,
      "selfDamageScaleGrounded": 1.0,
      "forceScaleAirborne": 10.0,
      "forceScaleGrounded": 5.0,
      "forceCapHU": 1000.0,
      "downBiasHU": 10.0,
      "airDetRadiusScale": 1.0,
      "airDetRampTime": 0.0,
      "propForceScale": 6.0,
      "propReferenceMassKg": 50.0,
      "sounds": ["snd_explode1", "snd_explode2", "snd_explode3"],
      "flash": "TF2Explosion",
      "source": "TF2 SDK TF_ROCKET_RADIUS 146, TF_ROCKET_RADIUS_FOR_RJS 121, tf_damagescale_self_soldier 0.6, tf_damageforcescale_self_soldier_rj 10 / _badrj 5, DamageForce cap 1000, ApplyPushFromDamage 10 HU down bias; wiki 50% edge splash"
    },
    {
      "id": "StickyBlast",
      "baseDamage": 120.0,
      "radiusHU": 146.0,
      "selfRadiusHU": 146.0,
      "edgeDamageFraction": 0.5,
      "selfDamageScaleAirborne": 0.75,
      "selfDamageScaleGrounded": 0.75,
      "forceScaleAirborne": 9.0,
      "forceScaleGrounded": 9.0,
      "forceCapHU": 1000.0,
      "downBiasHU": 10.0,
      "airDetRadiusScale": 0.85,
      "airDetRampTime": 2.0,
      "propForceScale": 6.0,
      "propReferenceMassKg": 50.0,
      "sounds": ["snd_pipe_bomb1"],
      "flash": "TF2Explosion",
      "source": "TF2 SDK DamageRadius 146, TF_GRENADE_JUMP_RADIUS 146, pipebomb self-damage 0.75, DAMAGE_FORCE_SCALE_SELF 9, tf_sticky_airdet_radius 0.85, tf_sticky_radius_ramp_time 2.0"
    }
  ]
}
```

### `sheets/hooks.json`

```json
{
  "sheet": "hooks",
  "about": "Every place the mod touches White Knuckle. All are resolved by name at runtime (no compile-time dependency on a game version) and logged as found/missing at startup, so a game update degrades a feature instead of crashing. Preflight checks each against the developer-approved 0.55 reference assembly (WhiteKnuckle.GameLibs) and against open-source mods built for the current 0.61 game.",
  "columns": {
    "id": "row id, also the generated accessor name",
    "kind": "field, staticfield, method, staticmethod, prefix, postfix, or type",
    "type": "White Knuckle type (nested types use +)",
    "member": "field or method name (empty for kind type: use '-')",
    "args": "method parameter types, comma separated, or '-'",
    "required": "true: the mod turns itself off if missing; false: the listed fallback is used",
    "fallback": "what happens when it is missing",
    "purpose": "why the mod needs it"
  },
  "rows": [
    { "id": "PlayerObject", "kind": "staticfield", "type": "ENT_Player", "member": "playerObject", "args": "-", "required": true, "fallback": "mod stays idle until a player exists", "purpose": "find the climber" },
    { "id": "PlayerVel", "kind": "field", "type": "ENT_Player", "member": "vel", "args": "-", "required": true, "fallback": "none: blasts cannot move you", "purpose": "add blast knockback and air control to the climber's velocity" },
    { "id": "PlayerMovementPrefix", "kind": "prefix", "type": "ENT_Player", "member": "Movement", "args": "-", "required": true, "fallback": "none", "purpose": "apply pending blast impulses and TF2 air acceleration right before White Knuckle moves the climber" },
    { "id": "PlayerMovementPostfix", "kind": "postfix", "type": "ENT_Player", "member": "Movement", "args": "-", "required": false, "fallback": "calibration uses Update timing instead", "purpose": "measure White Knuckle's gravity and velocity units, detect landing" },
    { "id": "PlayerCharacterController", "kind": "field", "type": "ENT_Player", "member": "cCon", "args": "-", "required": false, "fallback": "GetComponent<CharacterController>()", "purpose": "body height, grounded state, centre of the climber" },
    { "id": "PlayerCam", "kind": "field", "type": "ENT_Player", "member": "cam", "args": "-", "required": false, "fallback": "Camera.main", "purpose": "aim and eye position" },
    { "id": "PlayerInventoryCam", "kind": "field", "type": "ENT_Player", "member": "inventoryCam", "args": "-", "required": false, "fallback": "the mod's own overlay camera", "purpose": "draw the TF2 viewmodel the way White Knuckle draws its hands (never clips into walls)" },
    { "id": "PlayerHands", "kind": "field", "type": "ENT_Player", "member": "hands", "args": "-", "required": true, "fallback": "none: the toggle cannot put the hands away", "purpose": "let go, hide and lock both hands in TF2 mode" },
    { "id": "PlayerIsHanging", "kind": "method", "type": "ENT_Player", "member": "IsHanging", "args": "-", "required": false, "fallback": "treated as not hanging", "purpose": "ground/air state for TF2 push scales" },
    { "id": "PlayerIsCrouching", "kind": "method", "type": "ENT_Player", "member": "IsCrouching", "args": "-", "required": false, "fallback": "PlayerCrouchingField", "purpose": "crouched blasts push harder (TF2 crouch hull)" },
    { "id": "PlayerCrouchingField", "kind": "field", "type": "ENT_Player", "member": "crouching", "args": "-", "required": false, "fallback": "game action Crouch held", "purpose": "crouch state when IsCrouching is missing" },
    { "id": "PlayerUpVector", "kind": "method", "type": "ENT_Player", "member": "GetPlayerUpVector", "args": "-", "required": false, "fallback": "Vector3.up", "purpose": "which way is up for the blast's downward bias and air control" },
    { "id": "PlayerInputLocked", "kind": "method", "type": "ENT_Player", "member": "IsInputLocked", "args": "-", "required": false, "fallback": "PlayerPaused field and Time.timeScale", "purpose": "ignore mod keys in menus, cutscenes and the console" },
    { "id": "PlayerPaused", "kind": "field", "type": "ENT_Player", "member": "paused", "args": "-", "required": false, "fallback": "Time.timeScale == 0", "purpose": "ignore mod keys while paused" },
    { "id": "PlayerMaxFallPostfix", "kind": "postfix", "type": "ENT_Player", "member": "GetCurrentMaxFallDistance", "args": "-", "required": false, "fallback": "White Knuckle fall damage counts blast height", "purpose": "height gained from your own blasts never counts as fall damage (no self-damage)" },
    { "id": "PlayerResetFallingHeight", "kind": "method", "type": "ENT_Player", "member": "ResetFallingHeight", "args": "-", "required": false, "fallback": "only the max-fall postfix is used", "purpose": "a blast starts a fresh fall measurement" },
    { "id": "PlayerAirControl", "kind": "field", "type": "ENT_Player", "member": "airControl", "args": "-", "required": false, "fallback": "White Knuckle air control stays on alongside TF2's", "purpose": "swap White Knuckle air control for TF2 air strafing while blast-jumping" },
    { "id": "PlayerDrag", "kind": "field", "type": "ENT_Player", "member": "dragCoefficient", "args": "-", "required": false, "fallback": "White Knuckle air drag stays on", "purpose": "TF2 has no air drag; removed while blast-jumping" },
    { "id": "PlayerStartPostfix", "kind": "postfix", "type": "ENT_Player", "member": "Start", "args": "-", "required": false, "fallback": "new run detected when the player object changes", "purpose": "reset per-run state (back to hands, clear projectiles)" },
    { "id": "HandDropHand", "kind": "method", "type": "ENT_Player+Hand", "member": "DropHand", "args": "Boolean", "required": true, "fallback": "none", "purpose": "let go of holds when switching to TF2 mode" },
    { "id": "HandSetLocked", "kind": "method", "type": "ENT_Player+Hand", "member": "SetLocked", "args": "Boolean", "required": false, "fallback": "grab prefixes alone block the hands", "purpose": "stop the hands acting in TF2 mode" },
    { "id": "HandSetCanInteract", "kind": "method", "type": "ENT_Player+Hand", "member": "SetCanInteract", "args": "Boolean", "required": false, "fallback": "grab prefixes alone block the hands", "purpose": "stop the hands interacting in TF2 mode" },
    { "id": "HandModel", "kind": "field", "type": "ENT_Player+Hand", "member": "handModel", "args": "-", "required": true, "fallback": "none: hands would stay on screen", "purpose": "hide the White Knuckle hands (and held items) in TF2 mode" },
    { "id": "HandGrabHoldPrefix", "kind": "prefix", "type": "ENT_Player+Hand", "member": "GrabHold", "args": "Transform,Vector3,InteractionInfo", "required": false, "fallback": "SetLocked/SetCanInteract alone", "purpose": "no grabbing holds in TF2 mode" },
    { "id": "HandGrabTargetPrefix", "kind": "prefix", "type": "ENT_Player+Hand", "member": "GrabTarget", "args": "Transform,Vector3", "required": false, "fallback": "SetLocked/SetCanInteract alone", "purpose": "no grabbing props in TF2 mode" },
    { "id": "HandItemTryUsePrefix", "kind": "prefix", "type": "HandItem", "member": "TryUse", "args": "-", "required": false, "fallback": "held items could still be used by left/right click", "purpose": "left/right click fire the launchers, not the hand items" },
    { "id": "HandItemUsePrefix", "kind": "prefix", "type": "HandItem", "member": "Use", "args": "-", "required": false, "fallback": "held items could still be used by left/right click", "purpose": "left/right click fire the launchers, not the hand items" },
    { "id": "InputGetVector", "kind": "staticmethod", "type": "InputManager", "member": "GetVector", "args": "String", "required": false, "fallback": "WASD via legacy input", "purpose": "movement input for TF2 air strafing (respects the player's bindings)" },
    { "id": "InputGetButton", "kind": "staticmethod", "type": "InputManager", "member": "GetButton", "args": "String", "required": false, "fallback": "LeftControl via legacy input", "purpose": "crouch input when the crouch state can't be read" },
    { "id": "GamemodeStatic", "kind": "staticfield", "type": "CL_GameManager", "member": "gamemode", "args": "-", "required": false, "fallback": "LeaderboardUploadPrefix alone", "purpose": "turn off leaderboard scoring for runs that used explosives" },
    { "id": "GamemodeAllowScoring", "kind": "field", "type": "M_Gamemode", "member": "allowLeaderboardScoring", "args": "-", "required": false, "fallback": "LeaderboardUploadPrefix alone", "purpose": "turn off leaderboard scoring for runs that used explosives" },
    { "id": "LeaderboardUploadPrefix", "kind": "prefix", "type": "SteamManager", "member": "UploadGamemodeResults", "args": "*", "required": false, "fallback": "GamemodeAllowScoring alone", "purpose": "never upload a score from a run that used explosives" },
    { "id": "DenizenType", "kind": "type", "type": "Denizen", "member": "-", "args": "-", "required": false, "fallback": "creatures are recognised by having a GameEntity that is not a prop", "purpose": "blasts shove loose objects but not creatures" },
    { "id": "PropType", "kind": "type", "type": "CL_Prop", "member": "-", "args": "-", "required": false, "fallback": "any non-kinematic rigidbody that is not a creature", "purpose": "recognise White Knuckle's loose objects" }
  ]
}
```

### `sheets/movement.json`

```json
{
  "sheet": "movement",
  "about": "How TF2's world maps onto White Knuckle's. One row. The mod measures White Knuckle's gravity and velocity units in flight and keeps TF2 trajectories the same relative to your body: speed scale k = sqrt(g_wk * s / gravityHU), s = WK body height / hullHeightHU (metres per HU).",
  "columns": {
    "id": "row id",
    "gravityHU": "TF2 sv_gravity (HU/s^2)",
    "hullHeightHU": "TF2 standing hull height",
    "crouchForceHeightHU": "hull height TF2 uses for blast push while crouched",
    "hullWidthHU": "TF2 hull width",
    "airAccelerate": "TF2 sv_airaccelerate",
    "airWishSpeedCapHU": "TF2 air wish-speed cap (HU/s)",
    "maxVelocityHU": "TF2 sv_maxvelocity per axis (HU/s)",
    "defaultWkGravity": "White Knuckle gravity (m/s^2) used until measured in flight",
    "defaultWkVelocityToMetres": "White Knuckle vel field to m/s, used until measured",
    "defaultWkBodyHeight": "White Knuckle body height (m) used if the character controller can't be read",
    "calibrationSamples": "airborne physics steps to measure before trusting the measurement",
    "blastStateMinTime": "seconds after a blast before landing ends TF2 air control",
    "source": "where the numbers come from"
  },
  "rows": [
    {
      "id": "Tf2InWhiteKnuckle",
      "gravityHU": 800.0,
      "hullHeightHU": 82.0,
      "crouchForceHeightHU": 55.0,
      "hullWidthHU": 48.0,
      "airAccelerate": 10.0,
      "airWishSpeedCapHU": 30.0,
      "maxVelocityHU": 3500.0,
      "defaultWkGravity": 20.0,
      "defaultWkVelocityToMetres": 175.0,
      "defaultWkBodyHeight": 1.8,
      "calibrationSamples": 20,
      "blastStateMinTime": 0.1,
      "source": "Source sv_gravity 800, sv_airaccelerate 10, air wishspeed cap 30, sv_maxvelocity 3500; TF2 hull 48x48x82, self-blast crouch hull 55 (tf_player.cpp); WK vel x175 = m/s from senkodev/WKBloodbugMode (MIT); WK gravity default is a guess replaced by in-flight measurement"
    }
  ]
}
```

### `sheets/projectiles.json`

```json
{
  "sheet": "projectiles",
  "about": "What the launchers fire. HU and seconds.",
  "columns": {
    "id": "row id",
    "model": "assets row: the TF2 projectile model",
    "skin": "skin family (0 = RED)",
    "motion": "straight (no gravity) or ballistic (falls under gravityHU)",
    "gravityHU": "downward acceleration while flying (HU/s^2)",
    "collisionRadiusHU": "sphere radius used to detect hits",
    "armTime": "seconds before it can be detonated (0 = always)",
    "sticksToSurfaces": "true: stops and sticks where it lands",
    "detonateOn": "impact (explodes on first hit) or command (right click)",
    "lifetime": "seconds before it removes itself (0 = never)",
    "spinDegPerSec": "visual spin around its flight axis",
    "explosion": "explosions row",
    "impactSound": "assets row played when it lands without exploding, or none",
    "source": "where the numbers come from"
  },
  "rows": [
    {
      "id": "Rocket",
      "model": "mdl_w_rocket",
      "skin": 0,
      "motion": "straight",
      "gravityHU": 0.0,
      "collisionRadiusHU": 2.0,
      "armTime": 0.0,
      "sticksToSurfaces": false,
      "detonateOn": "impact",
      "lifetime": 10.0,
      "spinDegPerSec": 0.0,
      "explosion": "RocketBlast",
      "impactSound": "none",
      "source": "TF2 rockets fly straight with no gravity; 10 s cap is this mod's cleanup"
    },
    {
      "id": "Stickybomb",
      "model": "mdl_w_stickybomb",
      "skin": 0,
      "motion": "ballistic",
      "gravityHU": 800.0,
      "collisionRadiusHU": 4.0,
      "armTime": 0.8,
      "sticksToSurfaces": true,
      "detonateOn": "command",
      "lifetime": 0.0,
      "spinDegPerSec": 360.0,
      "explosion": "StickyBlast",
      "impactSound": "snd_sticky_land",
      "source": "TF2 SDK tf_grenadelauncher_livetime 0.8; sv_gravity 800"
    }
  ]
}
```

### `sheets/systems.json`

```json
{
  "sheet": "systems",
  "about": "The mod's systems. Each lists the sheet rows it is built from and the hooks it needs; each is one C# file.",
  "columns": {
    "id": "row id",
    "codeFile": "source file under src/RocketKnuckle",
    "hooks": "hooks rows it uses",
    "uses": "other sheet rows it reads (sheet.row)",
    "does": "what it does in game"
  },
  "rows": [
    {
      "id": "Tf2Content",
      "codeFile": "Tf2Content.cs",
      "hooks": [],
      "uses": [
        "assets.*",
        "classes.*",
        "weapons.*",
        "projectiles.*",
        "effects.*"
      ],
      "does": "finds the player's TF2 folder (passed by Melty), reads models, animations, textures and sounds from it, builds Unity meshes, materials and audio clips"
    },
    {
      "id": "WkBridge",
      "codeFile": "WkBridge.cs",
      "hooks": [
        "*"
      ],
      "uses": [
        "hooks.*"
      ],
      "does": "resolves every hooks row by name, logs found/missing, exposes typed accessors and installs the Harmony patches"
    },
    {
      "id": "ModeToggle",
      "codeFile": "ModeToggle.cs",
      "hooks": [
        "PlayerHands",
        "HandDropHand",
        "HandSetLocked",
        "HandSetCanInteract",
        "HandModel",
        "HandGrabHoldPrefix",
        "HandGrabTargetPrefix",
        "HandItemTryUsePrefix",
        "HandItemUsePrefix",
        "PlayerInputLocked",
        "PlayerPaused",
        "PlayerStartPostfix"
      ],
      "uses": [
        "bindings.ToggleMode",
        "bindings.SelectRocketLauncher",
        "bindings.SelectStickybombLauncher",
        "assets.snd_draw_primary"
      ],
      "does": "Q swaps White Knuckle hands for the TF2 launchers and back; 1/2 pick the launcher"
    },
    {
      "id": "ViewModel",
      "codeFile": "ViewModel.cs",
      "hooks": [
        "PlayerCam",
        "PlayerInventoryCam"
      ],
      "uses": [
        "weapons.*",
        "classes.*"
      ],
      "does": "draws the real TF2 arms and launcher with TF2's draw/idle/fire animations, at TF2's viewmodel field of view"
    },
    {
      "id": "Launchers",
      "codeFile": "Launchers.cs",
      "hooks": [
        "PlayerCam"
      ],
      "uses": [
        "weapons.*",
        "bindings.Fire",
        "bindings.AltFire"
      ],
      "does": "fire rates, sticky charge and auto-fire, detonation, max 8 stickies, aiming from TF2's muzzle offsets"
    },
    {
      "id": "Projectiles",
      "codeFile": "Projectiles.cs",
      "hooks": [
        "PlayerCharacterController"
      ],
      "uses": [
        "projectiles.*"
      ],
      "does": "flies rockets straight and stickies on a gravity arc, sticks stickies to surfaces, arms them, explodes rockets on impact"
    },
    {
      "id": "BlastPhysics",
      "codeFile": "BlastPhysics.cs",
      "hooks": [
        "PlayerVel",
        "PlayerMovementPrefix",
        "PlayerIsHanging",
        "PlayerIsCrouching",
        "PlayerCrouchingField",
        "PlayerUpVector",
        "PlayerResetFallingHeight",
        "PlayerMaxFallPostfix",
        "DenizenType",
        "PropType"
      ],
      "uses": [
        "explosions.*",
        "movement.Tf2InWhiteKnuckle",
        "effects.TF2Explosion",
        "bindings.Crouch"
      ],
      "does": "TF2 self-blast knockback (crouch bonus, airborne/grounded scales, 1000 HU/s cap, down bias), shoves loose objects, blast sounds and visuals, keeps blast height out of fall damage"
    },
    {
      "id": "AirControl",
      "codeFile": "AirControl.cs",
      "hooks": [
        "PlayerVel",
        "PlayerMovementPrefix",
        "PlayerAirControl",
        "PlayerDrag",
        "InputGetVector"
      ],
      "uses": [
        "movement.Tf2InWhiteKnuckle",
        "classes.*",
        "bindings.Move"
      ],
      "does": "while flying from a blast: TF2 air acceleration (air strafing) replaces White Knuckle's air control and drag, until you land or grab"
    },
    {
      "id": "Calibration",
      "codeFile": "Calibration.cs",
      "hooks": [
        "PlayerVel",
        "PlayerMovementPostfix",
        "PlayerCharacterController"
      ],
      "uses": [
        "movement.Tf2InWhiteKnuckle"
      ],
      "does": "measures White Knuckle's gravity, velocity units and body height in flight so TF2 trajectories keep their shape relative to your body"
    },
    {
      "id": "LeaderboardGuard",
      "codeFile": "LeaderboardGuard.cs",
      "hooks": [
        "GamemodeStatic",
        "GamemodeAllowScoring",
        "LeaderboardUploadPrefix",
        "PlayerStartPostfix"
      ],
      "uses": [],
      "does": "a run where explosives were used never posts to White Knuckle's leaderboards"
    }
  ]
}
```

### `sheets/weapons.json`

```json
{
  "sheet": "weapons",
  "about": "The two TF2 launchers you can hold in TF2 mode. Distances in Hammer units (HU), times in seconds. Unlimited mode: no clip, no reserve, no reload; fire rates stay TF2's.",
  "columns": {
    "id": "row id, also the generated C# name",
    "displayName": "name shown in the HUD hint",
    "slot": "binding row that selects it",
    "tf2Class": "classes row whose arms hold it",
    "weaponModel": "assets row: the c_model weapon, bone-merged onto the arms",
    "weaponSkin": "skin family of the weapon model (0 = stock RED)",
    "drawSeq": "arms sequence played when switching to it",
    "idleSeq": "arms sequence looped while held",
    "fireSeq": "arms sequence played on each shot",
    "fireInterval": "seconds between shots (TF2 TimeFireDelay)",
    "projectile": "projectiles row it fires",
    "spawnForwardHU": "projectile spawn offset from the eye, forward",
    "spawnRightHU": "projectile spawn offset from the eye, right",
    "spawnUpHU": "projectile spawn offset from the eye, up",
    "aimTraceHU": "how far the crosshair trace reaches when aiming the projectile at what you look at",
    "launchSpeedMinHU": "launch speed with no charge (HU/s)",
    "launchSpeedMaxHU": "launch speed at full charge (HU/s); same as min when the weapon has no charge",
    "chargeTime": "seconds to full charge (0 = no charge, fires on press)",
    "launchUpHU": "extra launch speed along view-up (HU/s)",
    "launchJitterHU": "random launch speed added along view right/up (HU/s)",
    "maxActive": "most projectiles alive at once; firing past it detonates the oldest (0 = no limit)",
    "fireSound": "assets row played on each shot",
    "chargeSound": "assets row played while charging, or none",
    "altFire": "what right click does: detonate or none",
    "altFireSound": "assets row played on right click, or none",
    "source": "where the numbers come from"
  },
  "rows": [
    {
      "id": "RocketLauncher",
      "displayName": "Rocket Launcher",
      "slot": "SelectRocketLauncher",
      "tf2Class": "Soldier",
      "weaponModel": "mdl_c_rocketlauncher",
      "weaponSkin": 0,
      "drawSeq": "dh_draw",
      "idleSeq": "dh_idle",
      "fireSeq": "dh_fire",
      "fireInterval": 0.8,
      "projectile": "Rocket",
      "spawnForwardHU": 23.5,
      "spawnRightHU": 12.0,
      "spawnUpHU": -3.0,
      "aimTraceHU": 2000.0,
      "launchSpeedMinHU": 1100.0,
      "launchSpeedMaxHU": 1100.0,
      "chargeTime": 0.0,
      "launchUpHU": 0.0,
      "launchJitterHU": 0.0,
      "maxActive": 0,
      "fireSound": "snd_rocket_shoot",
      "chargeSound": "none",
      "altFire": "none",
      "altFireSound": "none",
      "source": "TF2 SDK tf_weaponbase_rocket.cpp (1100 HU/s), tf_weaponbase_gun.cpp FireRocket offset (23.5, 12, -3); weapon script TimeFireDelay 0.8"
    },
    {
      "id": "StickybombLauncher",
      "displayName": "Stickybomb Launcher",
      "slot": "SelectStickybombLauncher",
      "tf2Class": "Demoman",
      "weaponModel": "mdl_c_stickybomb_launcher",
      "weaponSkin": 0,
      "drawSeq": "sb_draw",
      "idleSeq": "sb_idle",
      "fireSeq": "sb_fire",
      "fireInterval": 0.6,
      "projectile": "Stickybomb",
      "spawnForwardHU": 16.0,
      "spawnRightHU": 8.0,
      "spawnUpHU": -6.0,
      "aimTraceHU": 0.0,
      "launchSpeedMinHU": 900.0,
      "launchSpeedMaxHU": 2400.0,
      "chargeTime": 4.0,
      "launchUpHU": 200.0,
      "launchJitterHU": 10.0,
      "maxActive": 8,
      "fireSound": "snd_sticky_shoot",
      "chargeSound": "snd_sticky_charge",
      "altFire": "detonate",
      "altFireSound": "snd_sticky_det",
      "source": "TF2 SDK tf_weapon_pipebomblauncher.h (TF_PIPEBOMB_MIN/MAX_CHARGE_VEL 900/2400, MAX_CHARGE_TIME 4.0, TF_WEAPON_PIPEBOMB_COUNT 8), FirePipeBomb (+200 up, +-10 jitter, offset 16/8/-6); weapon script TimeFireDelay 0.6"
    }
  ]
}
```

### `src/RocketKnuckle/Generated/Sheets.g.cs`

```csharp
// <auto-generated> by tools/gen_sheets.py from sheets/*.json. Do not edit: change the sheet and regenerate. </auto-generated>
// Every row below is one sheet row; every property is one of its cells.
namespace RocketKnuckle.Data
{
    public static class SheetInfo
    {
        public const string SheetsHash = "385c9a01989827c7";
    }

    /// <summary>Every file the mod reads from the player's own Team Fortress 2 install at runtime. Nothing here ships with the mod. A model row also needs its .vvd and .dx90.vtx next to the .mdl (except animation libraries).</summary>
    public sealed class AssetDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>path inside TF2's tf/ content (VPK or loose)</summary>
        public string Path;
        /// <summary>which tf2_*_dir.vpk holds it: misc, sound or textures</summary>
        public string Package;
        /// <summary>model, animations, sound or texture</summary>
        public string Kind;
        /// <summary>true: the feature it belongs to cannot work without it; false: cosmetic</summary>
        public bool Required;
        /// <summary>the sheet rows that use it</summary>
        public string UsedBy;
        public override string ToString() => Id;
    }

    public static class Assets
    {
        public static readonly AssetDef mdl_c_rocketlauncher = new AssetDef { Id = "mdl_c_rocketlauncher", Path = "models/weapons/c_models/c_rocketlauncher/c_rocketlauncher.mdl", Package = "misc", Kind = "model", Required = true, UsedBy = "weapons.RocketLauncher" };
        public static readonly AssetDef mdl_c_stickybomb_launcher = new AssetDef { Id = "mdl_c_stickybomb_launcher", Path = "models/weapons/c_models/c_stickybomb_launcher/c_stickybomb_launcher.mdl", Package = "misc", Kind = "model", Required = true, UsedBy = "weapons.StickybombLauncher" };
        public static readonly AssetDef mdl_c_soldier_arms = new AssetDef { Id = "mdl_c_soldier_arms", Path = "models/weapons/c_models/c_soldier_arms.mdl", Package = "misc", Kind = "model", Required = true, UsedBy = "classes.Soldier" };
        public static readonly AssetDef mdl_c_soldier_animations = new AssetDef { Id = "mdl_c_soldier_animations", Path = "models/weapons/c_models/c_soldier_animations.mdl", Package = "misc", Kind = "animations", Required = true, UsedBy = "classes.Soldier" };
        public static readonly AssetDef mdl_c_demo_arms = new AssetDef { Id = "mdl_c_demo_arms", Path = "models/weapons/c_models/c_demo_arms.mdl", Package = "misc", Kind = "model", Required = true, UsedBy = "classes.Demoman" };
        public static readonly AssetDef mdl_c_demo_animations = new AssetDef { Id = "mdl_c_demo_animations", Path = "models/weapons/c_models/c_demo_animations.mdl", Package = "misc", Kind = "animations", Required = true, UsedBy = "classes.Demoman" };
        public static readonly AssetDef mdl_w_rocket = new AssetDef { Id = "mdl_w_rocket", Path = "models/weapons/w_models/w_rocket.mdl", Package = "misc", Kind = "model", Required = true, UsedBy = "projectiles.Rocket" };
        public static readonly AssetDef mdl_w_stickybomb = new AssetDef { Id = "mdl_w_stickybomb", Path = "models/weapons/w_models/w_stickybomb.mdl", Package = "misc", Kind = "model", Required = true, UsedBy = "projectiles.Stickybomb" };
        public static readonly AssetDef snd_rocket_shoot = new AssetDef { Id = "snd_rocket_shoot", Path = "sound/weapons/rocket_shoot.wav", Package = "sound", Kind = "sound", Required = true, UsedBy = "weapons.RocketLauncher" };
        public static readonly AssetDef snd_sticky_shoot = new AssetDef { Id = "snd_sticky_shoot", Path = "sound/weapons/stickybomblauncher_shoot.wav", Package = "sound", Kind = "sound", Required = true, UsedBy = "weapons.StickybombLauncher" };
        public static readonly AssetDef snd_sticky_charge = new AssetDef { Id = "snd_sticky_charge", Path = "sound/weapons/stickybomblauncher_charge_up.wav", Package = "sound", Kind = "sound", Required = false, UsedBy = "weapons.StickybombLauncher" };
        public static readonly AssetDef snd_sticky_det = new AssetDef { Id = "snd_sticky_det", Path = "sound/weapons/stickybomblauncher_det.wav", Package = "sound", Kind = "sound", Required = false, UsedBy = "weapons.StickybombLauncher" };
        public static readonly AssetDef snd_sticky_land = new AssetDef { Id = "snd_sticky_land", Path = "sound/weapons/grenade_impact.wav", Package = "sound", Kind = "sound", Required = false, UsedBy = "projectiles.Stickybomb" };
        public static readonly AssetDef snd_explode1 = new AssetDef { Id = "snd_explode1", Path = "sound/weapons/explode1.wav", Package = "sound", Kind = "sound", Required = true, UsedBy = "explosions.RocketBlast" };
        public static readonly AssetDef snd_explode2 = new AssetDef { Id = "snd_explode2", Path = "sound/weapons/explode2.wav", Package = "sound", Kind = "sound", Required = false, UsedBy = "explosions.RocketBlast" };
        public static readonly AssetDef snd_explode3 = new AssetDef { Id = "snd_explode3", Path = "sound/weapons/explode3.wav", Package = "sound", Kind = "sound", Required = false, UsedBy = "explosions.RocketBlast" };
        public static readonly AssetDef snd_pipe_bomb1 = new AssetDef { Id = "snd_pipe_bomb1", Path = "sound/weapons/pipe_bomb1.wav", Package = "sound", Kind = "sound", Required = true, UsedBy = "explosions.StickyBlast" };
        public static readonly AssetDef snd_draw_primary = new AssetDef { Id = "snd_draw_primary", Path = "sound/weapons/draw_primary.wav", Package = "sound", Kind = "sound", Required = false, UsedBy = "systems.ModeToggle" };
        public static readonly AssetDef tex_fireball = new AssetDef { Id = "tex_fireball", Path = "materials/effects/fireball.vtf", Package = "textures", Kind = "texture", Required = false, UsedBy = "effects.TF2Explosion" };
        public static readonly AssetDef tex_fire_cloud = new AssetDef { Id = "tex_fire_cloud", Path = "materials/effects/fire_cloud2.vtf", Package = "textures", Kind = "texture", Required = false, UsedBy = "effects.TF2Explosion" };
        public static readonly AssetDef tex_smoke = new AssetDef { Id = "tex_smoke", Path = "materials/effects/smoke/smokelit.vtf", Package = "textures", Kind = "texture", Required = false, UsedBy = "effects.TF2Explosion" };
        public static readonly AssetDef[] All = { mdl_c_rocketlauncher, mdl_c_stickybomb_launcher, mdl_c_soldier_arms, mdl_c_soldier_animations, mdl_c_demo_arms, mdl_c_demo_animations, mdl_w_rocket, mdl_w_stickybomb, snd_rocket_shoot, snd_sticky_shoot, snd_sticky_charge, snd_sticky_det, snd_sticky_land, snd_explode1, snd_explode2, snd_explode3, snd_pipe_bomb1, snd_draw_primary, tex_fireball, tex_fire_cloud, tex_smoke };
    }

    /// <summary>Controls. Mod keys are read with Unity's legacy input (as other White Knuckle mods do) and are rebindable in BepInEx/config/melty.rocketknuckle.cfg; game actions are White Knuckle's own InputManager names.</summary>
    public sealed class BindingDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>modkey (mod's own, rebindable) or gameaction (White Knuckle's InputManager action)</summary>
        public string Source;
        /// <summary>Unity KeyCode name for modkey rows; action name for gameaction rows</summary>
        public string DefaultKey;
        /// <summary>when it does something</summary>
        public string When;
        /// <summary>what it does</summary>
        public string Action;
        /// <summary>BepInEx config section (modkey rows) or none</summary>
        public string ConfigSection;
        public override string ToString() => Id;
    }

    public static class Bindings
    {
        public static readonly BindingDef ToggleMode = new BindingDef { Id = "ToggleMode", Source = "modkey", DefaultKey = "Q", When = "always in a run", Action = "swap White Knuckle hands <-> TF2 launchers", ConfigSection = "Controls" };
        public static readonly BindingDef SelectRocketLauncher = new BindingDef { Id = "SelectRocketLauncher", Source = "modkey", DefaultKey = "Alpha1", When = "TF2 mode", Action = "switch to the Rocket Launcher", ConfigSection = "Controls" };
        public static readonly BindingDef SelectStickybombLauncher = new BindingDef { Id = "SelectStickybombLauncher", Source = "modkey", DefaultKey = "Alpha2", When = "TF2 mode", Action = "switch to the Stickybomb Launcher", ConfigSection = "Controls" };
        public static readonly BindingDef Fire = new BindingDef { Id = "Fire", Source = "modkey", DefaultKey = "Mouse0", When = "TF2 mode", Action = "fire (stickies: hold to charge, release to launch)", ConfigSection = "Controls" };
        public static readonly BindingDef AltFire = new BindingDef { Id = "AltFire", Source = "modkey", DefaultKey = "Mouse1", When = "TF2 mode", Action = "detonate stickybombs", ConfigSection = "Controls" };
        public static readonly BindingDef Move = new BindingDef { Id = "Move", Source = "gameaction", DefaultKey = "Move", When = "TF2 mode, airborne after a blast", Action = "TF2 air strafing input", ConfigSection = "none" };
        public static readonly BindingDef Crouch = new BindingDef { Id = "Crouch", Source = "gameaction", DefaultKey = "Crouch", When = "TF2 mode", Action = "crouched blasts push harder, as in TF2", ConfigSection = "none" };
        public static readonly BindingDef[] All = { ToggleMode, SelectRocketLauncher, SelectStickybombLauncher, Fire, AltFire, Move, Crouch };
    }

    /// <summary>Every place the mod touches White Knuckle. All are resolved by name at runtime (no compile-time dependency on a game version) and logged as found/missing at startup, so a game update degrades a feature instead of crashing. Preflight checks each against the developer-approved 0.55 reference assembly (WhiteKnuckle.GameLibs) and against open-source mods built for the current 0.61 game.</summary>
    public sealed class HookDef
    {
        /// <summary>row id, also the generated accessor name</summary>
        public string Id;
        /// <summary>field, staticfield, method, staticmethod, prefix, postfix, or type</summary>
        public string Kind;
        /// <summary>White Knuckle type (nested types use +)</summary>
        public string Type;
        /// <summary>field or method name (empty for kind type: use '-')</summary>
        public string Member;
        /// <summary>method parameter types, comma separated, or '-'</summary>
        public string Args;
        /// <summary>true: the mod turns itself off if missing; false: the listed fallback is used</summary>
        public bool Required;
        /// <summary>what happens when it is missing</summary>
        public string Fallback;
        /// <summary>why the mod needs it</summary>
        public string Purpose;
        public override string ToString() => Id;
    }

    public static class Hooks
    {
        public static readonly HookDef PlayerObject = new HookDef { Id = "PlayerObject", Kind = "staticfield", Type = "ENT_Player", Member = "playerObject", Args = "-", Required = true, Fallback = "mod stays idle until a player exists", Purpose = "find the climber" };
        public static readonly HookDef PlayerVel = new HookDef { Id = "PlayerVel", Kind = "field", Type = "ENT_Player", Member = "vel", Args = "-", Required = true, Fallback = "none: blasts cannot move you", Purpose = "add blast knockback and air control to the climber's velocity" };
        public static readonly HookDef PlayerMovementPrefix = new HookDef { Id = "PlayerMovementPrefix", Kind = "prefix", Type = "ENT_Player", Member = "Movement", Args = "-", Required = true, Fallback = "none", Purpose = "apply pending blast impulses and TF2 air acceleration right before White Knuckle moves the climber" };
        public static readonly HookDef PlayerMovementPostfix = new HookDef { Id = "PlayerMovementPostfix", Kind = "postfix", Type = "ENT_Player", Member = "Movement", Args = "-", Required = false, Fallback = "calibration uses Update timing instead", Purpose = "measure White Knuckle's gravity and velocity units, detect landing" };
        public static readonly HookDef PlayerCharacterController = new HookDef { Id = "PlayerCharacterController", Kind = "field", Type = "ENT_Player", Member = "cCon", Args = "-", Required = false, Fallback = "GetComponent<CharacterController>()", Purpose = "body height, grounded state, centre of the climber" };
        public static readonly HookDef PlayerCam = new HookDef { Id = "PlayerCam", Kind = "field", Type = "ENT_Player", Member = "cam", Args = "-", Required = false, Fallback = "Camera.main", Purpose = "aim and eye position" };
        public static readonly HookDef PlayerInventoryCam = new HookDef { Id = "PlayerInventoryCam", Kind = "field", Type = "ENT_Player", Member = "inventoryCam", Args = "-", Required = false, Fallback = "the mod's own overlay camera", Purpose = "draw the TF2 viewmodel the way White Knuckle draws its hands (never clips into walls)" };
        public static readonly HookDef PlayerHands = new HookDef { Id = "PlayerHands", Kind = "field", Type = "ENT_Player", Member = "hands", Args = "-", Required = true, Fallback = "none: the toggle cannot put the hands away", Purpose = "let go, hide and lock both hands in TF2 mode" };
        public static readonly HookDef PlayerIsHanging = new HookDef { Id = "PlayerIsHanging", Kind = "method", Type = "ENT_Player", Member = "IsHanging", Args = "-", Required = false, Fallback = "treated as not hanging", Purpose = "ground/air state for TF2 push scales" };
        public static readonly HookDef PlayerIsCrouching = new HookDef { Id = "PlayerIsCrouching", Kind = "method", Type = "ENT_Player", Member = "IsCrouching", Args = "-", Required = false, Fallback = "PlayerCrouchingField", Purpose = "crouched blasts push harder (TF2 crouch hull)" };
        public static readonly HookDef PlayerCrouchingField = new HookDef { Id = "PlayerCrouchingField", Kind = "field", Type = "ENT_Player", Member = "crouching", Args = "-", Required = false, Fallback = "game action Crouch held", Purpose = "crouch state when IsCrouching is missing" };
        public static readonly HookDef PlayerUpVector = new HookDef { Id = "PlayerUpVector", Kind = "method", Type = "ENT_Player", Member = "GetPlayerUpVector", Args = "-", Required = false, Fallback = "Vector3.up", Purpose = "which way is up for the blast's downward bias and air control" };
        public static readonly HookDef PlayerInputLocked = new HookDef { Id = "PlayerInputLocked", Kind = "method", Type = "ENT_Player", Member = "IsInputLocked", Args = "-", Required = false, Fallback = "PlayerPaused field and Time.timeScale", Purpose = "ignore mod keys in menus, cutscenes and the console" };
        public static readonly HookDef PlayerPaused = new HookDef { Id = "PlayerPaused", Kind = "field", Type = "ENT_Player", Member = "paused", Args = "-", Required = false, Fallback = "Time.timeScale == 0", Purpose = "ignore mod keys while paused" };
        public static readonly HookDef PlayerMaxFallPostfix = new HookDef { Id = "PlayerMaxFallPostfix", Kind = "postfix", Type = "ENT_Player", Member = "GetCurrentMaxFallDistance", Args = "-", Required = false, Fallback = "White Knuckle fall damage counts blast height", Purpose = "height gained from your own blasts never counts as fall damage (no self-damage)" };
        public static readonly HookDef PlayerResetFallingHeight = new HookDef { Id = "PlayerResetFallingHeight", Kind = "method", Type = "ENT_Player", Member = "ResetFallingHeight", Args = "-", Required = false, Fallback = "only the max-fall postfix is used", Purpose = "a blast starts a fresh fall measurement" };
        public static readonly HookDef PlayerAirControl = new HookDef { Id = "PlayerAirControl", Kind = "field", Type = "ENT_Player", Member = "airControl", Args = "-", Required = false, Fallback = "White Knuckle air control stays on alongside TF2's", Purpose = "swap White Knuckle air control for TF2 air strafing while blast-jumping" };
        public static readonly HookDef PlayerDrag = new HookDef { Id = "PlayerDrag", Kind = "field", Type = "ENT_Player", Member = "dragCoefficient", Args = "-", Required = false, Fallback = "White Knuckle air drag stays on", Purpose = "TF2 has no air drag; removed while blast-jumping" };
        public static readonly HookDef PlayerStartPostfix = new HookDef { Id = "PlayerStartPostfix", Kind = "postfix", Type = "ENT_Player", Member = "Start", Args = "-", Required = false, Fallback = "new run detected when the player object changes", Purpose = "reset per-run state (back to hands, clear projectiles)" };
        public static readonly HookDef HandDropHand = new HookDef { Id = "HandDropHand", Kind = "method", Type = "ENT_Player+Hand", Member = "DropHand", Args = "Boolean", Required = true, Fallback = "none", Purpose = "let go of holds when switching to TF2 mode" };
        public static readonly HookDef HandSetLocked = new HookDef { Id = "HandSetLocked", Kind = "method", Type = "ENT_Player+Hand", Member = "SetLocked", Args = "Boolean", Required = false, Fallback = "grab prefixes alone block the hands", Purpose = "stop the hands acting in TF2 mode" };
        public static readonly HookDef HandSetCanInteract = new HookDef { Id = "HandSetCanInteract", Kind = "method", Type = "ENT_Player+Hand", Member = "SetCanInteract", Args = "Boolean", Required = false, Fallback = "grab prefixes alone block the hands", Purpose = "stop the hands interacting in TF2 mode" };
        public static readonly HookDef HandModel = new HookDef { Id = "HandModel", Kind = "field", Type = "ENT_Player+Hand", Member = "handModel", Args = "-", Required = true, Fallback = "none: hands would stay on screen", Purpose = "hide the White Knuckle hands (and held items) in TF2 mode" };
        public static readonly HookDef HandGrabHoldPrefix = new HookDef { Id = "HandGrabHoldPrefix", Kind = "prefix", Type = "ENT_Player+Hand", Member = "GrabHold", Args = "Transform,Vector3,InteractionInfo", Required = false, Fallback = "SetLocked/SetCanInteract alone", Purpose = "no grabbing holds in TF2 mode" };
        public static readonly HookDef HandGrabTargetPrefix = new HookDef { Id = "HandGrabTargetPrefix", Kind = "prefix", Type = "ENT_Player+Hand", Member = "GrabTarget", Args = "Transform,Vector3", Required = false, Fallback = "SetLocked/SetCanInteract alone", Purpose = "no grabbing props in TF2 mode" };
        public static readonly HookDef HandItemTryUsePrefix = new HookDef { Id = "HandItemTryUsePrefix", Kind = "prefix", Type = "HandItem", Member = "TryUse", Args = "-", Required = false, Fallback = "held items could still be used by left/right click", Purpose = "left/right click fire the launchers, not the hand items" };
        public static readonly HookDef HandItemUsePrefix = new HookDef { Id = "HandItemUsePrefix", Kind = "prefix", Type = "HandItem", Member = "Use", Args = "-", Required = false, Fallback = "held items could still be used by left/right click", Purpose = "left/right click fire the launchers, not the hand items" };
        public static readonly HookDef InputGetVector = new HookDef { Id = "InputGetVector", Kind = "staticmethod", Type = "InputManager", Member = "GetVector", Args = "String", Required = false, Fallback = "WASD via legacy input", Purpose = "movement input for TF2 air strafing (respects the player's bindings)" };
        public static readonly HookDef InputGetButton = new HookDef { Id = "InputGetButton", Kind = "staticmethod", Type = "InputManager", Member = "GetButton", Args = "String", Required = false, Fallback = "LeftControl via legacy input", Purpose = "crouch input when the crouch state can't be read" };
        public static readonly HookDef GamemodeStatic = new HookDef { Id = "GamemodeStatic", Kind = "staticfield", Type = "CL_GameManager", Member = "gamemode", Args = "-", Required = false, Fallback = "LeaderboardUploadPrefix alone", Purpose = "turn off leaderboard scoring for runs that used explosives" };
        public static readonly HookDef GamemodeAllowScoring = new HookDef { Id = "GamemodeAllowScoring", Kind = "field", Type = "M_Gamemode", Member = "allowLeaderboardScoring", Args = "-", Required = false, Fallback = "LeaderboardUploadPrefix alone", Purpose = "turn off leaderboard scoring for runs that used explosives" };
        public static readonly HookDef LeaderboardUploadPrefix = new HookDef { Id = "LeaderboardUploadPrefix", Kind = "prefix", Type = "SteamManager", Member = "UploadGamemodeResults", Args = "*", Required = false, Fallback = "GamemodeAllowScoring alone", Purpose = "never upload a score from a run that used explosives" };
        public static readonly HookDef DenizenType = new HookDef { Id = "DenizenType", Kind = "type", Type = "Denizen", Member = "-", Args = "-", Required = false, Fallback = "creatures are recognised by having a GameEntity that is not a prop", Purpose = "blasts shove loose objects but not creatures" };
        public static readonly HookDef PropType = new HookDef { Id = "PropType", Kind = "type", Type = "CL_Prop", Member = "-", Args = "-", Required = false, Fallback = "any non-kinematic rigidbody that is not a creature", Purpose = "recognise White Knuckle's loose objects" };
        public static readonly HookDef[] All = { PlayerObject, PlayerVel, PlayerMovementPrefix, PlayerMovementPostfix, PlayerCharacterController, PlayerCam, PlayerInventoryCam, PlayerHands, PlayerIsHanging, PlayerIsCrouching, PlayerCrouchingField, PlayerUpVector, PlayerInputLocked, PlayerPaused, PlayerMaxFallPostfix, PlayerResetFallingHeight, PlayerAirControl, PlayerDrag, PlayerStartPostfix, HandDropHand, HandSetLocked, HandSetCanInteract, HandModel, HandGrabHoldPrefix, HandGrabTargetPrefix, HandItemTryUsePrefix, HandItemUsePrefix, InputGetVector, InputGetButton, GamemodeStatic, GamemodeAllowScoring, LeaderboardUploadPrefix, DenizenType, PropType };
    }

    /// <summary>Blast visuals built from TF2's own effect textures (camera-facing cards that grow and fade).</summary>
    public sealed class EffectDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>assets row for the fireball cards</summary>
        public AssetDef FireTexture;
        /// <summary>assets row for the outer fire cloud</summary>
        public AssetDef FireCloudTexture;
        /// <summary>assets row for the smoke cards</summary>
        public AssetDef SmokeTexture;
        /// <summary>number of fireball cards</summary>
        public int FireCards;
        /// <summary>number of smoke cards</summary>
        public int SmokeCards;
        /// <summary>card size at the start</summary>
        public float StartSizeHU;
        /// <summary>card size at the end</summary>
        public float EndSizeHU;
        /// <summary>seconds the fire lasts</summary>
        public float FireDuration;
        /// <summary>seconds the smoke lasts</summary>
        public float SmokeDuration;
        /// <summary>how far cards drift from the centre</summary>
        public float SpreadHU;
        /// <summary>range of the blast flash light (0 = no light)</summary>
        public float LightRangeHU;
        /// <summary>seconds the flash light lasts</summary>
        public float LightDuration;
        /// <summary>where it comes from</summary>
        public string Source;
        public override string ToString() => Id;
    }

    public static class Effects
    {
        public static readonly EffectDef TF2Explosion = new EffectDef { Id = "TF2Explosion", FireTexture = Assets.tex_fireball, FireCloudTexture = Assets.tex_fire_cloud, SmokeTexture = Assets.tex_smoke, FireCards = 6, SmokeCards = 5, StartSizeHU = 40.0f, EndSizeHU = 150.0f, FireDuration = 0.45f, SmokeDuration = 1.6f, SpreadHU = 40.0f, LightRangeHU = 300.0f, LightDuration = 0.25f, Source = "Textures from TF2's ExplosionCore particle materials (effects/fireball, effects/fire_cloud2, effects/smoke/smokelit); timings tuned by this mod to approximate it" };
        public static readonly EffectDef[] All = { TF2Explosion };
    }

    /// <summary>Blasts and the TF2 knockback they give you. Self knockback: force = min(forceCapHU, damage * selfDamageScale * (82 / hullHeight) * forceScale), damage = lerp(baseDamage, baseDamage*edgeDamageFraction, distance/selfRadius). No damage is ever applied (unlimited mode).</summary>
    public sealed class ExplosionDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>damage at the centre (used only to size the push)</summary>
        public float BaseDamage;
        /// <summary>blast radius for loose objects</summary>
        public float RadiusHU;
        /// <summary>blast radius that reaches the shooter</summary>
        public float SelfRadiusHU;
        /// <summary>damage fraction at the edge of the radius</summary>
        public float EdgeDamageFraction;
        /// <summary>self-damage multiplier when airborne, applied before the push is sized</summary>
        public float SelfDamageScaleAirborne;
        /// <summary>self-damage multiplier when on the ground</summary>
        public float SelfDamageScaleGrounded;
        /// <summary>TF2 self push scale when airborne</summary>
        public float ForceScaleAirborne;
        /// <summary>TF2 self push scale when on the ground</summary>
        public float ForceScaleGrounded;
        /// <summary>largest push from one blast (HU/s)</summary>
        public float ForceCapHU;
        /// <summary>the blast is treated as this much lower, biasing the push upward</summary>
        public float DownBiasHU;
        /// <summary>radius scale for a projectile that never touched a surface (1 = none)</summary>
        public float AirDetRadiusScale;
        /// <summary>seconds after arming for that scale to ramp back to 1</summary>
        public float AirDetRampTime;
        /// <summary>push scale for loose objects (TF2's scale for others)</summary>
        public float PropForceScale;
        /// <summary>objects heavier than this are pushed proportionally less</summary>
        public float PropReferenceMassKg;
        /// <summary>assets rows; one is picked at random</summary>
        public AssetDef[] Sounds;
        /// <summary>effects row drawn at the blast</summary>
        public EffectDef Flash;
        /// <summary>where the numbers come from</summary>
        public string Source;
        public override string ToString() => Id;
    }

    public static class Explosions
    {
        public static readonly ExplosionDef RocketBlast = new ExplosionDef { Id = "RocketBlast", BaseDamage = 90.0f, RadiusHU = 146.0f, SelfRadiusHU = 121.0f, EdgeDamageFraction = 0.5f, SelfDamageScaleAirborne = 0.6f, SelfDamageScaleGrounded = 1.0f, ForceScaleAirborne = 10.0f, ForceScaleGrounded = 5.0f, ForceCapHU = 1000.0f, DownBiasHU = 10.0f, AirDetRadiusScale = 1.0f, AirDetRampTime = 0.0f, PropForceScale = 6.0f, PropReferenceMassKg = 50.0f, Sounds = new[] { Assets.snd_explode1, Assets.snd_explode2, Assets.snd_explode3 }, Flash = Effects.TF2Explosion, Source = "TF2 SDK TF_ROCKET_RADIUS 146, TF_ROCKET_RADIUS_FOR_RJS 121, tf_damagescale_self_soldier 0.6, tf_damageforcescale_self_soldier_rj 10 / _badrj 5, DamageForce cap 1000, ApplyPushFromDamage 10 HU down bias; wiki 50% edge splash" };
        public static readonly ExplosionDef StickyBlast = new ExplosionDef { Id = "StickyBlast", BaseDamage = 120.0f, RadiusHU = 146.0f, SelfRadiusHU = 146.0f, EdgeDamageFraction = 0.5f, SelfDamageScaleAirborne = 0.75f, SelfDamageScaleGrounded = 0.75f, ForceScaleAirborne = 9.0f, ForceScaleGrounded = 9.0f, ForceCapHU = 1000.0f, DownBiasHU = 10.0f, AirDetRadiusScale = 0.85f, AirDetRampTime = 2.0f, PropForceScale = 6.0f, PropReferenceMassKg = 50.0f, Sounds = new[] { Assets.snd_pipe_bomb1 }, Flash = Effects.TF2Explosion, Source = "TF2 SDK DamageRadius 146, TF_GRENADE_JUMP_RADIUS 146, pipebomb self-damage 0.75, DAMAGE_FORCE_SCALE_SELF 9, tf_sticky_airdet_radius 0.85, tf_sticky_radius_ramp_time 2.0" };
        public static readonly ExplosionDef[] All = { RocketBlast, StickyBlast };
    }

    /// <summary>What the launchers fire. HU and seconds.</summary>
    public sealed class ProjectileDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>assets row: the TF2 projectile model</summary>
        public AssetDef Model;
        /// <summary>skin family (0 = RED)</summary>
        public int Skin;
        /// <summary>straight (no gravity) or ballistic (falls under gravityHU)</summary>
        public string Motion;
        /// <summary>downward acceleration while flying (HU/s^2)</summary>
        public float GravityHU;
        /// <summary>sphere radius used to detect hits</summary>
        public float CollisionRadiusHU;
        /// <summary>seconds before it can be detonated (0 = always)</summary>
        public float ArmTime;
        /// <summary>true: stops and sticks where it lands</summary>
        public bool SticksToSurfaces;
        /// <summary>impact (explodes on first hit) or command (right click)</summary>
        public string DetonateOn;
        /// <summary>seconds before it removes itself (0 = never)</summary>
        public float Lifetime;
        /// <summary>visual spin around its flight axis</summary>
        public float SpinDegPerSec;
        /// <summary>explosions row</summary>
        public ExplosionDef Explosion;
        /// <summary>assets row played when it lands without exploding, or none</summary>
        public AssetDef ImpactSound;
        /// <summary>where the numbers come from</summary>
        public string Source;
        public override string ToString() => Id;
    }

    public static class Projectiles
    {
        public static readonly ProjectileDef Rocket = new ProjectileDef { Id = "Rocket", Model = Assets.mdl_w_rocket, Skin = 0, Motion = "straight", GravityHU = 0.0f, CollisionRadiusHU = 2.0f, ArmTime = 0.0f, SticksToSurfaces = false, DetonateOn = "impact", Lifetime = 10.0f, SpinDegPerSec = 0.0f, Explosion = Explosions.RocketBlast, ImpactSound = null, Source = "TF2 rockets fly straight with no gravity; 10 s cap is this mod's cleanup" };
        public static readonly ProjectileDef Stickybomb = new ProjectileDef { Id = "Stickybomb", Model = Assets.mdl_w_stickybomb, Skin = 0, Motion = "ballistic", GravityHU = 800.0f, CollisionRadiusHU = 4.0f, ArmTime = 0.8f, SticksToSurfaces = true, DetonateOn = "command", Lifetime = 0.0f, SpinDegPerSec = 360.0f, Explosion = Explosions.StickyBlast, ImpactSound = Assets.snd_sticky_land, Source = "TF2 SDK tf_grenadelauncher_livetime 0.8; sv_gravity 800" };
        public static readonly ProjectileDef[] All = { Rocket, Stickybomb };
    }

    /// <summary>The TF2 class whose arms hold each launcher and whose air control applies while it is out.</summary>
    public sealed class ClassDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>assets row: c_<class>_arms model</summary>
        public AssetDef ArmsModel;
        /// <summary>assets row: the arms' animation library</summary>
        public AssetDef AnimationsModel;
        /// <summary>skin family (0 = RED sleeves)</summary>
        public int ArmsSkin;
        /// <summary>class run speed; the wish speed that TF2 air acceleration uses (HU/s)</summary>
        public float MaxSpeedHU;
        /// <summary>where the numbers come from</summary>
        public string Source;
        public override string ToString() => Id;
    }

    public static class Classes
    {
        public static readonly ClassDef Soldier = new ClassDef { Id = "Soldier", ArmsModel = Assets.mdl_c_soldier_arms, AnimationsModel = Assets.mdl_c_soldier_animations, ArmsSkin = 0, MaxSpeedHU = 240.0f, Source = "TF2 class speed: Soldier 240 HU/s" };
        public static readonly ClassDef Demoman = new ClassDef { Id = "Demoman", ArmsModel = Assets.mdl_c_demo_arms, AnimationsModel = Assets.mdl_c_demo_animations, ArmsSkin = 0, MaxSpeedHU = 280.0f, Source = "TF2 class speed: Demoman 280 HU/s" };
        public static readonly ClassDef[] All = { Soldier, Demoman };
    }

    /// <summary>The two TF2 launchers you can hold in TF2 mode. Distances in Hammer units (HU), times in seconds. Unlimited mode: no clip, no reserve, no reload; fire rates stay TF2's.</summary>
    public sealed class WeaponDef
    {
        /// <summary>row id, also the generated C# name</summary>
        public string Id;
        /// <summary>name shown in the HUD hint</summary>
        public string DisplayName;
        /// <summary>binding row that selects it</summary>
        public BindingDef Slot;
        /// <summary>classes row whose arms hold it</summary>
        public ClassDef Tf2Class;
        /// <summary>assets row: the c_model weapon, bone-merged onto the arms</summary>
        public AssetDef WeaponModel;
        /// <summary>skin family of the weapon model (0 = stock RED)</summary>
        public int WeaponSkin;
        /// <summary>arms sequence played when switching to it</summary>
        public string DrawSeq;
        /// <summary>arms sequence looped while held</summary>
        public string IdleSeq;
        /// <summary>arms sequence played on each shot</summary>
        public string FireSeq;
        /// <summary>seconds between shots (TF2 TimeFireDelay)</summary>
        public float FireInterval;
        /// <summary>projectiles row it fires</summary>
        public ProjectileDef Projectile;
        /// <summary>projectile spawn offset from the eye, forward</summary>
        public float SpawnForwardHU;
        /// <summary>projectile spawn offset from the eye, right</summary>
        public float SpawnRightHU;
        /// <summary>projectile spawn offset from the eye, up</summary>
        public float SpawnUpHU;
        /// <summary>how far the crosshair trace reaches when aiming the projectile at what you look at</summary>
        public float AimTraceHU;
        /// <summary>launch speed with no charge (HU/s)</summary>
        public float LaunchSpeedMinHU;
        /// <summary>launch speed at full charge (HU/s); same as min when the weapon has no charge</summary>
        public float LaunchSpeedMaxHU;
        /// <summary>seconds to full charge (0 = no charge, fires on press)</summary>
        public float ChargeTime;
        /// <summary>extra launch speed along view-up (HU/s)</summary>
        public float LaunchUpHU;
        /// <summary>random launch speed added along view right/up (HU/s)</summary>
        public float LaunchJitterHU;
        /// <summary>most projectiles alive at once; firing past it detonates the oldest (0 = no limit)</summary>
        public int MaxActive;
        /// <summary>assets row played on each shot</summary>
        public AssetDef FireSound;
        /// <summary>assets row played while charging, or none</summary>
        public AssetDef ChargeSound;
        /// <summary>what right click does: detonate or none</summary>
        public string AltFire;
        /// <summary>assets row played on right click, or none</summary>
        public AssetDef AltFireSound;
        /// <summary>where the numbers come from</summary>
        public string Source;
        public override string ToString() => Id;
    }

    public static class Weapons
    {
        public static readonly WeaponDef RocketLauncher = new WeaponDef { Id = "RocketLauncher", DisplayName = "Rocket Launcher", Slot = Bindings.SelectRocketLauncher, Tf2Class = Classes.Soldier, WeaponModel = Assets.mdl_c_rocketlauncher, WeaponSkin = 0, DrawSeq = "dh_draw", IdleSeq = "dh_idle", FireSeq = "dh_fire", FireInterval = 0.8f, Projectile = Projectiles.Rocket, SpawnForwardHU = 23.5f, SpawnRightHU = 12.0f, SpawnUpHU = -3.0f, AimTraceHU = 2000.0f, LaunchSpeedMinHU = 1100.0f, LaunchSpeedMaxHU = 1100.0f, ChargeTime = 0.0f, LaunchUpHU = 0.0f, LaunchJitterHU = 0.0f, MaxActive = 0, FireSound = Assets.snd_rocket_shoot, ChargeSound = null, AltFire = "none", AltFireSound = null, Source = "TF2 SDK tf_weaponbase_rocket.cpp (1100 HU/s), tf_weaponbase_gun.cpp FireRocket offset (23.5, 12, -3); weapon script TimeFireDelay 0.8" };
        public static readonly WeaponDef StickybombLauncher = new WeaponDef { Id = "StickybombLauncher", DisplayName = "Stickybomb Launcher", Slot = Bindings.SelectStickybombLauncher, Tf2Class = Classes.Demoman, WeaponModel = Assets.mdl_c_stickybomb_launcher, WeaponSkin = 0, DrawSeq = "sb_draw", IdleSeq = "sb_idle", FireSeq = "sb_fire", FireInterval = 0.6f, Projectile = Projectiles.Stickybomb, SpawnForwardHU = 16.0f, SpawnRightHU = 8.0f, SpawnUpHU = -6.0f, AimTraceHU = 0.0f, LaunchSpeedMinHU = 900.0f, LaunchSpeedMaxHU = 2400.0f, ChargeTime = 4.0f, LaunchUpHU = 200.0f, LaunchJitterHU = 10.0f, MaxActive = 8, FireSound = Assets.snd_sticky_shoot, ChargeSound = Assets.snd_sticky_charge, AltFire = "detonate", AltFireSound = Assets.snd_sticky_det, Source = "TF2 SDK tf_weapon_pipebomblauncher.h (TF_PIPEBOMB_MIN/MAX_CHARGE_VEL 900/2400, MAX_CHARGE_TIME 4.0, TF_WEAPON_PIPEBOMB_COUNT 8), FirePipeBomb (+200 up, +-10 jitter, offset 16/8/-6); weapon script TimeFireDelay 0.6" };
        public static readonly WeaponDef[] All = { RocketLauncher, StickybombLauncher };
    }

    /// <summary>How TF2's world maps onto White Knuckle's. One row. The mod measures White Knuckle's gravity and velocity units in flight and keeps TF2 trajectories the same relative to your body: speed scale k = sqrt(g_wk * s / gravityHU), s = WK body height / hullHeightHU (metres per HU).</summary>
    public sealed class MovementDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>TF2 sv_gravity (HU/s^2)</summary>
        public float GravityHU;
        /// <summary>TF2 standing hull height</summary>
        public float HullHeightHU;
        /// <summary>hull height TF2 uses for blast push while crouched</summary>
        public float CrouchForceHeightHU;
        /// <summary>TF2 hull width</summary>
        public float HullWidthHU;
        /// <summary>TF2 sv_airaccelerate</summary>
        public float AirAccelerate;
        /// <summary>TF2 air wish-speed cap (HU/s)</summary>
        public float AirWishSpeedCapHU;
        /// <summary>TF2 sv_maxvelocity per axis (HU/s)</summary>
        public float MaxVelocityHU;
        /// <summary>White Knuckle gravity (m/s^2) used until measured in flight</summary>
        public float DefaultWkGravity;
        /// <summary>White Knuckle vel field to m/s, used until measured</summary>
        public float DefaultWkVelocityToMetres;
        /// <summary>White Knuckle body height (m) used if the character controller can't be read</summary>
        public float DefaultWkBodyHeight;
        /// <summary>airborne physics steps to measure before trusting the measurement</summary>
        public int CalibrationSamples;
        /// <summary>seconds after a blast before landing ends TF2 air control</summary>
        public float BlastStateMinTime;
        /// <summary>where the numbers come from</summary>
        public string Source;
        public override string ToString() => Id;
    }

    public static class Movement
    {
        public static readonly MovementDef Tf2InWhiteKnuckle = new MovementDef { Id = "Tf2InWhiteKnuckle", GravityHU = 800.0f, HullHeightHU = 82.0f, CrouchForceHeightHU = 55.0f, HullWidthHU = 48.0f, AirAccelerate = 10.0f, AirWishSpeedCapHU = 30.0f, MaxVelocityHU = 3500.0f, DefaultWkGravity = 20.0f, DefaultWkVelocityToMetres = 175.0f, DefaultWkBodyHeight = 1.8f, CalibrationSamples = 20, BlastStateMinTime = 0.1f, Source = "Source sv_gravity 800, sv_airaccelerate 10, air wishspeed cap 30, sv_maxvelocity 3500; TF2 hull 48x48x82, self-blast crouch hull 55 (tf_player.cpp); WK vel x175 = m/s from senkodev/WKBloodbugMode (MIT); WK gravity default is a guess replaced by in-flight measurement" };
        public static readonly MovementDef[] All = { Tf2InWhiteKnuckle };
    }

    /// <summary>The mod's systems. Each lists the sheet rows it is built from and the hooks it needs; each is one C# file.</summary>
    public sealed class SystemDef
    {
        /// <summary>row id</summary>
        public string Id;
        /// <summary>source file under src/RocketKnuckle</summary>
        public string CodeFile;
        /// <summary>hooks rows it uses</summary>
        public HookDef[] Hooks;
        /// <summary>other sheet rows it reads (sheet.row)</summary>
        public string[] Uses;
        /// <summary>what it does in game</summary>
        public string Does;
        public override string ToString() => Id;
    }

    public static class Systems
    {
        public static readonly SystemDef Tf2Content = new SystemDef { Id = "Tf2Content", CodeFile = "Tf2Content.cs", Hooks = new HookDef[0], Uses = new[] { "assets.*", "classes.*", "weapons.*", "projectiles.*", "effects.*" }, Does = "finds the player's TF2 folder (passed by Melty), reads models, animations, textures and sounds from it, builds Unity meshes, materials and audio clips" };
        public static readonly SystemDef WkBridge = new SystemDef { Id = "WkBridge", CodeFile = "WkBridge.cs", Hooks = Hooks.All, Uses = new[] { "hooks.*" }, Does = "resolves every hooks row by name, logs found/missing, exposes typed accessors and installs the Harmony patches" };
        public static readonly SystemDef ModeToggle = new SystemDef { Id = "ModeToggle", CodeFile = "ModeToggle.cs", Hooks = new[] { Hooks.PlayerHands, Hooks.HandDropHand, Hooks.HandSetLocked, Hooks.HandSetCanInteract, Hooks.HandModel, Hooks.HandGrabHoldPrefix, Hooks.HandGrabTargetPrefix, Hooks.HandItemTryUsePrefix, Hooks.HandItemUsePrefix, Hooks.PlayerInputLocked, Hooks.PlayerPaused, Hooks.PlayerStartPostfix }, Uses = new[] { "bindings.ToggleMode", "bindings.SelectRocketLauncher", "bindings.SelectStickybombLauncher", "assets.snd_draw_primary" }, Does = "Q swaps White Knuckle hands for the TF2 launchers and back; 1/2 pick the launcher" };
        public static readonly SystemDef ViewModel = new SystemDef { Id = "ViewModel", CodeFile = "ViewModel.cs", Hooks = new[] { Hooks.PlayerCam, Hooks.PlayerInventoryCam }, Uses = new[] { "weapons.*", "classes.*" }, Does = "draws the real TF2 arms and launcher with TF2's draw/idle/fire animations, at TF2's viewmodel field of view" };
        public static readonly SystemDef Launchers = new SystemDef { Id = "Launchers", CodeFile = "Launchers.cs", Hooks = new[] { Hooks.PlayerCam }, Uses = new[] { "weapons.*", "bindings.Fire", "bindings.AltFire" }, Does = "fire rates, sticky charge and auto-fire, detonation, max 8 stickies, aiming from TF2's muzzle offsets" };
        public static readonly SystemDef Projectiles = new SystemDef { Id = "Projectiles", CodeFile = "Projectiles.cs", Hooks = new[] { Hooks.PlayerCharacterController }, Uses = new[] { "projectiles.*" }, Does = "flies rockets straight and stickies on a gravity arc, sticks stickies to surfaces, arms them, explodes rockets on impact" };
        public static readonly SystemDef BlastPhysics = new SystemDef { Id = "BlastPhysics", CodeFile = "BlastPhysics.cs", Hooks = new[] { Hooks.PlayerVel, Hooks.PlayerMovementPrefix, Hooks.PlayerIsHanging, Hooks.PlayerIsCrouching, Hooks.PlayerCrouchingField, Hooks.PlayerUpVector, Hooks.PlayerResetFallingHeight, Hooks.PlayerMaxFallPostfix, Hooks.DenizenType, Hooks.PropType }, Uses = new[] { "explosions.*", "movement.Tf2InWhiteKnuckle", "effects.TF2Explosion", "bindings.Crouch" }, Does = "TF2 self-blast knockback (crouch bonus, airborne/grounded scales, 1000 HU/s cap, down bias), shoves loose objects, blast sounds and visuals, keeps blast height out of fall damage" };
        public static readonly SystemDef AirControl = new SystemDef { Id = "AirControl", CodeFile = "AirControl.cs", Hooks = new[] { Hooks.PlayerVel, Hooks.PlayerMovementPrefix, Hooks.PlayerAirControl, Hooks.PlayerDrag, Hooks.InputGetVector }, Uses = new[] { "movement.Tf2InWhiteKnuckle", "classes.*", "bindings.Move" }, Does = "while flying from a blast: TF2 air acceleration (air strafing) replaces White Knuckle's air control and drag, until you land or grab" };
        public static readonly SystemDef Calibration = new SystemDef { Id = "Calibration", CodeFile = "Calibration.cs", Hooks = new[] { Hooks.PlayerVel, Hooks.PlayerMovementPostfix, Hooks.PlayerCharacterController }, Uses = new[] { "movement.Tf2InWhiteKnuckle" }, Does = "measures White Knuckle's gravity, velocity units and body height in flight so TF2 trajectories keep their shape relative to your body" };
        public static readonly SystemDef LeaderboardGuard = new SystemDef { Id = "LeaderboardGuard", CodeFile = "LeaderboardGuard.cs", Hooks = new[] { Hooks.GamemodeStatic, Hooks.GamemodeAllowScoring, Hooks.LeaderboardUploadPrefix, Hooks.PlayerStartPostfix }, Uses = new string[0], Does = "a run where explosives were used never posts to White Knuckle's leaderboards" };
        public static readonly SystemDef[] All = { Tf2Content, WkBridge, ModeToggle, ViewModel, Launchers, Projectiles, BlastPhysics, AirControl, Calibration, LeaderboardGuard };
    }
}
```

### `src/TF2Assets/MathTypes.cs`

```csharp
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
```

### `src/TF2Assets/StudioAnimation.cs`

```csharp
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
```

### `src/TF2Assets/StudioModel.cs`

```csharp
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
```

### `src/TF2Assets/TF2Assets.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <AssemblyName>TF2Assets</AssemblyName>
    <RootNamespace>TF2Assets</RootNamespace>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

### `src/TF2Assets/Vpk.cs`

```csharp
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
```

### `src/TF2Assets/Vtf.cs`

```csharp
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
```

### `src/TF2Assets/Wav.cs`

```csharp
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
```

### `tests/TF2Assets.Tests/Program.cs`

```csharp
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
```

### `tests/TF2Assets.Tests/TF2Assets.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/TF2Assets/TF2Assets.csproj" />
  </ItemGroup>
</Project>
```

### `tools/dev/apidump/Program.cs`

```csharp
using System.Reflection;
// usage: apidump <dll> <dir-of-deps> <type-regex>
var dll = args[0]; var depDir = args[1]; var rx = new System.Text.RegularExpressions.Regex(args[2]);
var paths = Directory.GetFiles(depDir, "*.dll").Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")).Append(dll).Distinct();
var ctx = new MetadataLoadContext(new PathAssemblyResolver(paths));
var asm = ctx.LoadFromAssemblyPath(dll);
Type[] types; try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
string N(Type t) { try { return t.Name; } catch { return "?"; } }
foreach (var t in types.Where(t => rx.IsMatch(t.FullName ?? "")).OrderBy(t => t.FullName))
{
    string bt = ""; try { bt = t.BaseType?.Name; } catch {}
    Console.WriteLine($"== {t.FullName} : {bt}");
    var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    try { foreach (var f in t.GetFields(bf)) { string ft; try { ft = N(f.FieldType); } catch { ft = "?"; } Console.WriteLine($"  F {(f.IsStatic ? "static " : "")}{ft} {f.Name}"); } } catch (Exception e) { Console.WriteLine("  fields err " + e.Message); }
    try { foreach (var p in t.GetProperties(bf)) { string pt; try { pt = N(p.PropertyType); } catch { pt = "?"; } Console.WriteLine($"  P {pt} {p.Name}"); } } catch {}
    try { foreach (var m in t.GetMethods(bf)) { string ps; try { ps = string.Join(", ", m.GetParameters().Select(p => N(p.ParameterType) + " " + p.Name)); } catch { ps = "?"; } string rt; try { rt = N(m.ReturnType); } catch { rt = "?"; } Console.WriteLine($"  M {(m.IsStatic ? "static " : "")}{rt} {m.Name}({ps})"); } } catch {}
}
```

### `tools/dev/apidump/apidump.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup>
  <ItemGroup><PackageReference Include="System.Reflection.MetadataLoadContext" Version="8.0.0" /></ItemGroup>
</Project>
```

### `tools/dev/melty.py`

```python
#!/usr/bin/env python3
"""Call one Melty MCP tool over HTTP (JSON-RPC), for sessions where the melty MCP server isn't loaded.

The token is read from the MELTY_TOKEN environment variable and sent only in Melty's Authorization header.
Never put the token in a file inside this repo.

  set MELTY_TOKEN=llc_...            (Windows cmd)   |   export MELTY_TOKEN=llc_...   (bash)
  python tools/dev/melty.py list_my_mods
  python tools/dev/melty.py game_info "{\"slug\": \"team-fortress-2\"}"
  python tools/dev/melty.py validate_recipe @recipe-args.json
  python tools/dev/melty.py __list                       (tools/list)
"""
import json, os, sys, urllib.request, urllib.error

URL = "https://melty.gg/api/mcp"


def main():
    token = os.environ.get("MELTY_TOKEN")
    if not token:
        sys.exit("Set MELTY_TOKEN first (copy it from a fresh Publish prompt in Melty Studio).")
    name = sys.argv[1]
    a = sys.argv[2] if len(sys.argv) > 2 else "{}"
    args = json.load(open(a[1:], encoding="utf-8")) if a.startswith("@") else json.loads(a)
    if name == "__list":
        req = {"jsonrpc": "2.0", "id": 1, "method": "tools/list", "params": {}}
    else:
        req = {"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": name, "arguments": args}}
    r = urllib.request.Request(URL, data=json.dumps(req).encode("utf-8"), method="POST", headers={
        "Authorization": "Bearer " + token,
        "content-type": "application/json",
        "accept": "application/json, text/event-stream",
    })
    try:
        body = urllib.request.urlopen(r, timeout=120).read().decode("utf-8")
    except urllib.error.HTTPError as e:
        sys.exit(f"HTTP {e.code}: {e.read().decode('utf-8', 'replace')}")
    lines = [l[5:].strip() for l in body.splitlines() if l.startswith("data:")]
    j = json.loads(lines[-1] if lines else body)
    if "error" in j:
        sys.exit("JSON-RPC error: " + json.dumps(j["error"], indent=1))
    res = j["result"]
    if name == "__list":
        print(json.dumps(res, indent=1))
        return
    if res.get("isError"):
        print("TOOL ISERROR")
    for c in res.get("content", []):
        print(c["text"] if c.get("type") == "text" else json.dumps(c))


if __name__ == "__main__":
    main()
```

### `tools/dev/vpkcat.py`

```python
#!/usr/bin/env python3
"""Print one file from a VPK to stdout: python tools/dev/vpkcat.py <x_dir.vpk> <path-inside> > out.bin (preload bytes not handled)."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from vpkls import read_dir
import struct
def get(dirpath, name):
    d = read_dir(dirpath); arc, off, ln, pre = d[name]
    f = open(dirpath, 'rb').read()
    # preload bytes: not handled here (0 for our files)
    if arc == 0x7fff:
        sig, ver, tree = struct.unpack_from('<III', f, 0); hdr = 12 if ver == 1 else 28
        return f[hdr+tree+off: hdr+tree+off+ln]
    a = dirpath.replace('_dir.vpk', '_%03d.vpk' % arc)
    with open(a, 'rb') as fh: fh.seek(off); return fh.read(ln)
if __name__ == '__main__':
    sys.stdout.buffer.write(get(sys.argv[1], sys.argv[2]))
```

### `tools/dev/vpkls.py`

```python
#!/usr/bin/env python3
"""List files in a Valve VPK directory: python tools/dev/vpkls.py <x_dir.vpk> <regex>. Prints path (archive, offset, length, preload)."""
import struct, sys, re
def read_dir(path):
    f = open(path, 'rb').read()
    sig, ver, tree = struct.unpack_from('<III', f, 0)
    assert sig == 0x55aa1234, hex(sig)
    hdr = 12 if ver == 1 else 28
    p = hdr; end = hdr + tree
    def cstr():
        nonlocal p
        e = f.index(b'\0', p); s = f[p:e].decode('latin1'); p = e + 1; return s
    out = {}
    while True:
        ext = cstr()
        if not ext: break
        while True:
            d = cstr()
            if not d: break
            while True:
                n = cstr()
                if not n: break
                crc, pre, arc, off, ln, term = struct.unpack_from('<IHHIIH', f, p); p += 18
                p += pre
                full = (d + '/' if d != ' ' else '') + n + '.' + ext
                out[full] = (arc, off, ln, pre)
    return out
if __name__ == '__main__':
    d = read_dir(sys.argv[1]); pat = re.compile(sys.argv[2])
    for k, v in sorted(d.items()):
        if pat.search(k): print(k, v)
```

### `tools/gen_sheets.py`

```python
#!/usr/bin/env python3
"""Generates src/RocketKnuckle/Generated/Sheets.g.cs from sheets/*.json.

Every sheet becomes a Def class (one property per column) and a static table class with one
static readonly field per row (named after the row id), built from that row's cells.
Cross-sheet references become typed object references ("none" becomes null).
"""
import json, os, sys, hashlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHEETS = os.path.join(ROOT, "sheets")
OUT = os.path.join(ROOT, "src", "RocketKnuckle", "Generated", "Sheets.g.cs")

# sheet -> (def class, table class)
NAMES = {
    "assets": ("AssetDef", "Assets"),
    "bindings": ("BindingDef", "Bindings"),
    "hooks": ("HookDef", "Hooks"),
    "effects": ("EffectDef", "Effects"),
    "explosions": ("ExplosionDef", "Explosions"),
    "projectiles": ("ProjectileDef", "Projectiles"),
    "classes": ("ClassDef", "Classes"),
    "weapons": ("WeaponDef", "Weapons"),
    "movement": ("MovementDef", "Movement"),
    "systems": ("SystemDef", "Systems"),
}
ORDER = ["assets", "bindings", "hooks", "effects", "explosions", "projectiles", "classes", "weapons", "movement", "systems"]

# (sheet, column) -> referenced sheet
REFS = {
    ("weapons", "slot"): "bindings",
    ("weapons", "tf2Class"): "classes",
    ("weapons", "weaponModel"): "assets",
    ("weapons", "projectile"): "projectiles",
    ("weapons", "fireSound"): "assets",
    ("weapons", "chargeSound"): "assets",
    ("weapons", "altFireSound"): "assets",
    ("projectiles", "model"): "assets",
    ("projectiles", "explosion"): "explosions",
    ("projectiles", "impactSound"): "assets",
    ("explosions", "sounds"): "assets",
    ("explosions", "flash"): "effects",
    ("classes", "armsModel"): "assets",
    ("classes", "animationsModel"): "assets",
    ("effects", "fireTexture"): "assets",
    ("effects", "fireCloudTexture"): "assets",
    ("effects", "smokeTexture"): "assets",
    ("systems", "hooks"): "hooks",
}


def sheets_hash():
    h = hashlib.sha256()
    for fn in sorted(os.listdir(SHEETS)):
        if fn.endswith(".json"):
            h.update(fn.encode())
            h.update(open(os.path.join(SHEETS, fn), "rb").read())
    return h.hexdigest()[:16]


def pascal(s):
    return s[0].upper() + s[1:]


def cs_type(sheet, col, values):
    ref = REFS.get((sheet, col))
    if ref:
        base = NAMES[ref][0]
        return base + "[]" if any(isinstance(v, list) for v in values) else base
    if all(isinstance(v, bool) for v in values):
        return "bool"
    if all(isinstance(v, int) and not isinstance(v, bool) for v in values):
        return "int"
    if all(isinstance(v, (int, float)) and not isinstance(v, bool) for v in values):
        return "float"
    if all(isinstance(v, list) for v in values):
        return "string[]"
    return "string"


def lit(v, t, sheet, col):
    ref = REFS.get((sheet, col))
    if ref:
        table = NAMES[ref][1]
        if isinstance(v, list):
            if v == ["*"]:
                return f"{table}.All"
            return "new[] { " + ", ".join(f"{table}.{x}" for x in v) + " }" if v else f"new {NAMES[ref][0]}[0]"
        return "null" if v == "none" else f"{table}.{v}"
    if t == "bool":
        return "true" if v else "false"
    if t == "int":
        return str(v)
    if t == "float":
        return repr(float(v)) + "f"
    if t == "string[]":
        return "new[] { " + ", ".join(json.dumps(x) for x in v) + " }" if v else "new string[0]"
    return json.dumps(v)


def main():
    sheets = {}
    for fn in os.listdir(SHEETS):
        if fn.endswith(".json"):
            d = json.load(open(os.path.join(SHEETS, fn), encoding="utf-8"))
            sheets[d["sheet"]] = d
    out = []
    w = out.append
    w("// <auto-generated> by tools/gen_sheets.py from sheets/*.json. Do not edit: change the sheet and regenerate. </auto-generated>")
    w("// Every row below is one sheet row; every property is one of its cells.")
    w("namespace RocketKnuckle.Data")
    w("{")
    w("    public static class SheetInfo")
    w("    {")
    w(f"        public const string SheetsHash = \"{sheets_hash()}\";")
    w("    }")
    for name in ORDER:
        d = sheets[name]
        dcls, tcls = NAMES[name]
        cols = list(d["columns"].keys())
        types = {c: cs_type(name, c, [r[c] for r in d["rows"]]) for c in cols}
        w("")
        w(f"    /// <summary>{d['about']}</summary>")
        w(f"    public sealed class {dcls}")
        w("    {")
        for c in cols:
            w(f"        /// <summary>{d['columns'][c]}</summary>")
            w(f"        public {types[c]} {pascal(c)};")
        w("        public override string ToString() => Id;")
        w("    }")
        w("")
        w(f"    public static class {tcls}")
        w("    {")
        for r in d["rows"]:
            inits = ", ".join(f"{pascal(c)} = {lit(r[c], types[c], name, c)}" for c in cols)
            w(f"        public static readonly {dcls} {r['id']} = new {dcls} {{ {inits} }};")
        w(f"        public static readonly {dcls}[] All = {{ " + ", ".join(r["id"] for r in d["rows"]) + " };")
        w("    }")
    w("}")
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    # systems.hooks "*" refers to Hooks.All, which is fine since Hooks is declared earlier
    open(OUT, "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
    print("wrote", os.path.relpath(OUT, ROOT))


if __name__ == "__main__":
    main()
```

### `tools/preflight.py`

```python
#!/usr/bin/env python3
"""Preflight: lay every sheet over the others and list what will fail or is unimplemented.

Checks
  1. every row has every declared column, filled (no null / empty), and no undeclared columns
  2. every reference between sheets resolves (weapons -> projectiles -> explosions -> effects, assets, hooks, ...)
  3. every TF2 file in the assets sheet exists in the TF2 install given with --tf2 (VPK directories or loose files)
  4. every White Knuckle hook exists in the 0.55 reference API dump (--api) or is used by a mod built for
     the current game (--mods); kind/args match
  5. every modkey binding is a real Unity KeyCode (--keycodes)
  6. numbers are in sane ranges
  7. every systems row has its code file, and the generated code is in sync with the sheets

Exit code 0 only when clean.  Usage:
  python3 tools/preflight.py [--tf2 <tf folder>] [--api <api dump>] [--mods <dir>] [--keycodes <file>]
"""
import argparse, json, os, re, struct, sys, hashlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHEETS = os.path.join(ROOT, "sheets")
CODE = os.path.join(ROOT, "src", "RocketKnuckle")

problems, notes = [], []


def bad(where, msg):
    problems.append(f"{where}: {msg}")


def load_sheets():
    out = {}
    for fn in sorted(os.listdir(SHEETS)):
        if fn.endswith(".json"):
            with open(os.path.join(SHEETS, fn), encoding="utf-8") as f:
                d = json.load(f)
            out[d["sheet"]] = d
    return out


def check_cells(sheets):
    for name, d in sheets.items():
        cols = d["columns"]
        ids = set()
        for i, row in enumerate(d["rows"]):
            rid = row.get("id", f"#{i}")
            if rid in ids:
                bad(f"{name}.{rid}", "duplicate id")
            ids.add(rid)
            for c in cols:
                if c not in row:
                    bad(f"{name}.{rid}.{c}", "missing cell")
                elif row[c] is None or (isinstance(row[c], str) and row[c].strip() == ""):
                    bad(f"{name}.{rid}.{c}", "empty cell")
                elif isinstance(row[c], list) and len(row[c]) == 0 and c not in ("hooks", "uses"):
                    bad(f"{name}.{rid}.{c}", "empty list")
            for c in row:
                if c not in cols:
                    bad(f"{name}.{rid}.{c}", "column not declared in the sheet header")


def index(sheets):
    return {n: {r["id"]: r for r in d["rows"]} for n, d in sheets.items()}


def check_refs(sheets, ix):
    def ref(where, sheet, val, allow_none=False, kind=None):
        if allow_none and val == "none":
            return
        row = ix.get(sheet, {}).get(val)
        if row is None:
            bad(where, f"'{val}' is not a row of {sheet}")
        elif kind and row.get("kind") not in (kind if isinstance(kind, tuple) else (kind,)):
            bad(where, f"'{val}' is a {row.get('kind')}, expected {kind}")

    for w in ix["weapons"].values():
        p = f"weapons.{w['id']}"
        ref(p + ".slot", "bindings", w["slot"])
        ref(p + ".tf2Class", "classes", w["tf2Class"])
        ref(p + ".weaponModel", "assets", w["weaponModel"], kind="model")
        ref(p + ".projectile", "projectiles", w["projectile"])
        ref(p + ".fireSound", "assets", w["fireSound"], kind="sound")
        ref(p + ".chargeSound", "assets", w["chargeSound"], allow_none=True, kind="sound")
        ref(p + ".altFireSound", "assets", w["altFireSound"], allow_none=True, kind="sound")
        if w["altFire"] not in ("detonate", "none"):
            bad(p + ".altFire", "must be detonate or none")
        if w["altFire"] == "detonate" and ix["projectiles"].get(w["projectile"], {}).get("detonateOn") != "command":
            bad(p + ".altFire", "detonate needs a projectile with detonateOn=command")
        if w["chargeTime"] > 0 and w["launchSpeedMaxHU"] < w["launchSpeedMinHU"]:
            bad(p + ".launchSpeedMaxHU", "smaller than min")
        cls = ix["classes"].get(w["tf2Class"])
        if cls:
            notes.append(f"{p}: arms sequences {w['drawSeq']}/{w['idleSeq']}/{w['fireSeq']} come from {cls['animationsModel']}")
    for r in ix["projectiles"].values():
        p = f"projectiles.{r['id']}"
        ref(p + ".model", "assets", r["model"], kind="model")
        ref(p + ".explosion", "explosions", r["explosion"])
        ref(p + ".impactSound", "assets", r["impactSound"], allow_none=True, kind="sound")
        if r["motion"] not in ("straight", "ballistic"):
            bad(p + ".motion", "must be straight or ballistic")
        if r["detonateOn"] not in ("impact", "command"):
            bad(p + ".detonateOn", "must be impact or command")
    for r in ix["explosions"].values():
        p = f"explosions.{r['id']}"
        for s in r["sounds"]:
            ref(p + ".sounds", "assets", s, kind="sound")
        ref(p + ".flash", "effects", r["flash"])
    for r in ix["classes"].values():
        p = f"classes.{r['id']}"
        ref(p + ".armsModel", "assets", r["armsModel"], kind="model")
        ref(p + ".animationsModel", "assets", r["animationsModel"], kind="animations")
    for r in ix["effects"].values():
        p = f"effects.{r['id']}"
        for c in ("fireTexture", "fireCloudTexture", "smokeTexture"):
            ref(p + "." + c, "assets", r[c], kind="texture")
    for r in ix["assets"].values():
        p = f"assets.{r['id']}"
        for u in str(r["usedBy"]).split(","):
            u = u.strip()
            sh, _, rid = u.partition(".")
            if sh not in ix or rid not in ix[sh]:
                bad(p + ".usedBy", f"'{u}' does not resolve")
        if r["package"] not in ("misc", "sound", "textures"):
            bad(p + ".package", "must be misc, sound or textures")
        if r["kind"] not in ("model", "animations", "sound", "texture"):
            bad(p + ".kind", "unknown kind")
    for r in ix["bindings"].values():
        p = f"bindings.{r['id']}"
        if r["source"] not in ("modkey", "gameaction"):
            bad(p + ".source", "must be modkey or gameaction")
    for r in ix["systems"].values():
        p = f"systems.{r['id']}"
        for h in r["hooks"]:
            if h != "*" and h not in ix["hooks"]:
                bad(p + ".hooks", f"'{h}' is not a hooks row")
        for u in r["uses"]:
            sh, _, rid = u.partition(".")
            if sh not in ix:
                bad(p + ".uses", f"'{u}': no sheet {sh}")
            elif rid != "*" and rid not in ix[sh]:
                bad(p + ".uses", f"'{u}' does not resolve")
    # every hook is used by some system
    used = {h for s in ix["systems"].values() for h in s["hooks"]}
    for h in ix["hooks"]:
        if h not in used and "*" not in used:
            bad(f"hooks.{h}", "no system uses it")
    # every binding / weapon / projectile / explosion / asset is used
    for sheet in ("bindings", "assets"):
        for rid in ix[sheet]:
            hit = any(u in (f"{sheet}.{rid}", f"{sheet}.*") for s in ix["systems"].values() for u in s["uses"])
            if sheet == "bindings":
                hit = hit or any(w["slot"] == rid for w in ix["weapons"].values())
            if sheet == "assets":
                hit = hit or any(rid in json.dumps(r) for n in ("weapons", "projectiles", "explosions", "classes", "effects") for r in ix[n].values())
            if not hit:
                bad(f"{sheet}.{rid}", "nothing uses this row")


def check_ranges(ix):
    def pos(where, v, allow_zero=False):
        if not isinstance(v, (int, float)) or v < 0 or (v == 0 and not allow_zero):
            bad(where, f"expected a {'non-negative' if allow_zero else 'positive'} number, got {v!r}")
    for w in ix["weapons"].values():
        p = f"weapons.{w['id']}"
        pos(p + ".fireInterval", w["fireInterval"])
        pos(p + ".launchSpeedMinHU", w["launchSpeedMinHU"])
        pos(p + ".chargeTime", w["chargeTime"], True)
        pos(p + ".maxActive", w["maxActive"], True)
        pos(p + ".aimTraceHU", w["aimTraceHU"], True)
    for r in ix["explosions"].values():
        p = f"explosions.{r['id']}"
        for c in ("baseDamage", "radiusHU", "selfRadiusHU", "forceCapHU", "forceScaleAirborne", "forceScaleGrounded", "propReferenceMassKg"):
            pos(p + "." + c, r[c])
        if not 0 < r["edgeDamageFraction"] <= 1:
            bad(p + ".edgeDamageFraction", "must be in (0, 1]")
        if not 0 < r["airDetRadiusScale"] <= 1:
            bad(p + ".airDetRadiusScale", "must be in (0, 1]")
    for r in ix["movement"].values():
        p = f"movement.{r['id']}"
        for c in ("gravityHU", "hullHeightHU", "crouchForceHeightHU", "airAccelerate", "airWishSpeedCapHU", "defaultWkGravity", "defaultWkVelocityToMetres", "defaultWkBodyHeight"):
            pos(p + "." + c, r[c])
        if r["crouchForceHeightHU"] >= r["hullHeightHU"]:
            bad(p + ".crouchForceHeightHU", "should be smaller than hullHeightHU (crouch pushes harder)")


def vpk_listing(path):
    f = open(path, "rb").read()
    sig, ver, tree = struct.unpack_from("<III", f, 0)
    p = 12 if ver == 1 else 28
    names = set()

    def cstr():
        nonlocal p
        e = f.index(b"\0", p)
        s = f[p:e].decode("latin1")
        p = e + 1
        return s
    while True:
        ext = cstr()
        if not ext:
            break
        while True:
            d = cstr()
            if not d:
                break
            while True:
                n = cstr()
                if not n:
                    break
                pre = struct.unpack_from("<H", f, p + 4)[0]
                p += 18 + pre
                names.add(((d + "/") if d != " " else "") + n + "." + ext)
    return names


def check_assets(ix, tf):
    if not tf:
        bad("assets", "not checked: pass --tf2 <Team Fortress 2/tf folder>")
        return
    pk = {"misc": "tf2_misc_dir.vpk", "sound": "tf2_sound_misc_dir.vpk", "textures": "tf2_textures_dir.vpk"}
    lists = {k: vpk_listing(os.path.join(tf, v)) for k, v in pk.items() if os.path.exists(os.path.join(tf, v))}
    for r in ix["assets"].values():
        p = f"assets.{r['id']}.path"
        need = [r["path"]]
        if r["kind"] == "model":
            stem = r["path"][:-4]
            need += [stem + ".vvd", stem + ".dx90.vtx"]
        for n in need:
            listing = lists.get(r["package"])
            if listing is None:
                bad(p, f"{pk[r['package']]} not found in {tf}")
            elif n.lower() not in listing and not os.path.exists(os.path.join(tf, n)):
                bad(p, f"{n} is not in {pk[r['package']]}")


def parse_api(path):
    types = {}
    cur = None
    for line in open(path, encoding="utf-8", errors="replace"):
        line = line.rstrip("\n")
        if line.startswith("== "):
            name = line[3:].split(" : ")[0]
            cur = types.setdefault(name, {"F": {}, "M": {}, "P": {}})
        elif cur is not None and line.startswith("  ") and len(line) > 4 and line[2] in "FMP":
            body = line[4:]
            static = body.startswith("static ")
            if static:
                body = body[7:]
            if line[2] == "M":
                m = re.match(r"(\S+) ([^(]+)\((.*)\)$", body)
                if m:
                    args = [a.strip().split(" ")[0] for a in m.group(3).split(",") if a.strip()]
                    cur["M"].setdefault(m.group(2), []).append((static, args))
            else:
                parts = body.split(" ")
                if len(parts) >= 2:
                    cur[line[2]][parts[-1]] = (static, parts[0])
    return types


def grep_mods(mods, member, typ):
    if not mods:
        return []
    hits = []
    pat = re.compile(r"\b" + re.escape(member) + r"\b")
    for dp, _, fns in os.walk(mods):
        for fn in fns:
            if fn.endswith(".cs"):
                fp = os.path.join(dp, fn)
                try:
                    if pat.search(open(fp, encoding="utf-8", errors="replace").read()):
                        hits.append(os.path.relpath(fp, mods).split(os.sep)[0])
                except OSError:
                    pass
    return sorted(set(hits))


def check_hooks(ix, api_path, mods):
    api = parse_api(api_path) if api_path else None
    if api is None:
        bad("hooks", "not checked against the 0.55 reference API: pass --api <dump>")
    for h in ix["hooks"].values():
        p = f"hooks.{h['id']}"
        t, m, kind = h["type"], h["member"], h["kind"]
        if kind not in ("field", "staticfield", "method", "staticmethod", "prefix", "postfix", "type"):
            bad(p + ".kind", f"unknown kind {kind}")
            continue
        in055 = False
        if api is not None:
            ty = api.get(t)
            if ty is None:
                in055 = False
            elif kind == "type":
                in055 = True
            elif kind in ("field", "staticfield"):
                f = ty["F"].get(m) or ty["P"].get(m)
                in055 = f is not None and (f[0] == (kind == "staticfield") or ty["P"].get(m) is not None)
            else:
                ov = ty["M"].get(m, [])
                want = None if h["args"] in ("-", "*") else [a.strip() for a in h["args"].split(",")]
                for st, args in ov:
                    if kind == "staticmethod" and not st:
                        continue
                    if kind == "method" and st:
                        continue
                    if h["args"] == "*" or (want is None and not args) or want == args:
                        in055 = True
        used = grep_mods(mods, m if kind != "type" else t, t)
        ev = []
        if in055:
            ev.append("0.55 refs")
        if used:
            ev.append("used by " + ", ".join(used))
        if not ev:
            bad(p, f"no evidence that {t}.{m} exists (not in 0.55 refs, not used by a current mod)")
        else:
            notes.append(f"{p}: {t}.{m} ({kind}) evidence: {'; '.join(ev)}; in-game: pending")


def check_keys(ix, keycodes):
    if not keycodes:
        bad("bindings", "not checked: pass --keycodes <file>")
        return
    keys = {k.strip() for k in open(keycodes) if k.strip()}
    for r in ix["bindings"].values():
        if r["source"] == "modkey" and r["defaultKey"] not in keys:
            bad(f"bindings.{r['id']}.defaultKey", f"{r['defaultKey']} is not a Unity KeyCode")
    mk = [r["defaultKey"] for r in ix["bindings"].values() if r["source"] == "modkey"]
    for k in set(mk):
        if mk.count(k) > 1:
            bad("bindings", f"key {k} bound twice")


def sheets_hash():
    h = hashlib.sha256()
    for fn in sorted(os.listdir(SHEETS)):
        if fn.endswith(".json"):
            h.update(fn.encode())
            h.update(open(os.path.join(SHEETS, fn), "rb").read())
    return h.hexdigest()[:16]


def check_code(ix):
    for r in ix["systems"].values():
        fp = os.path.join(CODE, r["codeFile"])
        if not os.path.exists(fp):
            bad(f"systems.{r['id']}.codeFile", f"{r['codeFile']} not written yet")
    gen = os.path.join(CODE, "Generated", "Sheets.g.cs")
    if not os.path.exists(gen):
        bad("generated", "src/RocketKnuckle/Generated/Sheets.g.cs missing: run tools/gen_sheets.py")
    else:
        txt = open(gen, encoding="utf-8").read()
        m = re.search(r"SheetsHash = \"([0-9a-f]+)\"", txt)
        if not m or m.group(1) != sheets_hash():
            bad("generated", "Sheets.g.cs is out of date: run tools/gen_sheets.py")
        for sheet in ("weapons", "projectiles", "explosions", "classes", "movement", "assets", "effects", "bindings", "hooks", "systems"):
            for rid in ix[sheet]:
                if f" {rid} = new " not in txt:
                    bad(f"{sheet}.{rid}", "no generated definition")
    # every hook id must be referenced by code (beyond the generated table)
    code_txt = ""
    for dp, _, fns in os.walk(CODE):
        if "Generated" in dp or os.sep + "obj" in dp or os.sep + "bin" in dp:
            continue
        for fn in fns:
            if fn.endswith(".cs"):
                code_txt += open(os.path.join(dp, fn), encoding="utf-8").read()
    if code_txt:
        for sheet, prefix in (("hooks", "Hooks."), ("weapons", "Weapons."), ("bindings", "Bindings."), ("assets", "Assets.")):
            for rid in ix[sheet]:
                used_directly = (prefix + rid) in code_txt
                used_via_table = sheet in ("weapons", "assets") and ("Weapons.All" in code_txt or "Assets.All" in code_txt)
                if not used_directly and not used_via_table:
                    bad(f"{sheet}.{rid}", "not used by any code")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tf2")
    ap.add_argument("--api")
    ap.add_argument("--mods")
    ap.add_argument("--keycodes")
    ap.add_argument("--quiet", action="store_true")
    a = ap.parse_args()
    sheets = load_sheets()
    for need in ("weapons", "projectiles", "explosions", "classes", "movement", "assets", "effects", "bindings", "hooks", "systems"):
        if need not in sheets:
            bad("sheets", f"missing sheet {need}")
    if problems:
        print("\n".join(problems))
        return 1
    check_cells(sheets)
    ix = index(sheets)
    check_refs(sheets, ix)
    check_ranges(ix)
    check_assets(ix, a.tf2)
    check_hooks(ix, a.api, a.mods)
    check_keys(ix, a.keycodes)
    check_code(ix)
    cells = sum(len(d["columns"]) * len(d["rows"]) for d in sheets.values())
    rows = sum(len(d["rows"]) for d in sheets.values())
    if not a.quiet:
        for n in notes:
            print("note  " + n)
    print(f"\n{len(sheets)} sheets, {rows} rows, {cells} cells")
    if problems:
        print(f"PREFLIGHT: {len(problems)} problem(s)")
        for p in problems:
            print("  FIX  " + p)
        return 1
    print("PREFLIGHT CLEAN")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

