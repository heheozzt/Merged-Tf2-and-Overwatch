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
