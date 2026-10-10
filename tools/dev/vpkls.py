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
