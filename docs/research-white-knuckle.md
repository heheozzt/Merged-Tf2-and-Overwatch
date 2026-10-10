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
