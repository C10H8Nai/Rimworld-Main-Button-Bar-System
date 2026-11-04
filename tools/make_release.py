#!/usr/bin/env python3
"""Copies only what the game loads into a clean folder for uploading to the Steam Workshop.

Usage (from the repository root):
    python3 tools/make_release.py <target folder>

The repository is also the mod folder, so it contains source code, tests, tools, design documents and test
screenshots that players do not need. This copies:
    About/  1.7/  Defs/  Languages/  Textures/  LoadFolders.xml  docs/API.md (as API.md, for add-on authors)
An existing About/PublishedFileId.txt in the target is kept, so uploads keep updating the same Workshop item.
"""
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDERS = ["About", "1.7", "Defs", "Languages", "Textures"]
FILES = {"LoadFolders.xml": "LoadFolders.xml", os.path.join("docs", "API.md"): "API.md"}
KEEP = os.path.join("About", "PublishedFileId.txt")


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        return 1
    target = os.path.abspath(sys.argv[1])
    if target == ROOT or target.startswith(ROOT + os.sep):
        print("The target must be outside the repository.")
        return 1

    kept = None
    keep_path = os.path.join(target, KEEP)
    if os.path.exists(keep_path):
        with open(keep_path, "rb") as f:
            kept = f.read()

    for folder in FOLDERS:
        dst = os.path.join(target, folder)
        if os.path.exists(dst):
            shutil.rmtree(dst)
        shutil.copytree(os.path.join(ROOT, folder), dst)
    for src, dst in FILES.items():
        shutil.copy2(os.path.join(ROOT, src), os.path.join(target, dst))

    if kept is not None:
        with open(keep_path, "wb") as f:
            f.write(kept)

    count = sum(len(files) for _, _, files in os.walk(target))
    print(f"Release folder ready: {target} ({count} files)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
