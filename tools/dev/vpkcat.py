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
