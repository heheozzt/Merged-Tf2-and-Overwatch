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
