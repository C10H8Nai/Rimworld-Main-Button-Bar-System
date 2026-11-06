#!/usr/bin/env python3
"""Copies only what the game loads into a clean folder for uploading to the Steam Workshop.

Usage (from the repository root):
    python3 tools/make_release.py <target folder> [--no-template]

The repository is also the mod folder, so it contains source code, tests, tools, design documents and test
screenshots that players do not need. This copies:
    About/  1.7/  Defs/  Languages/  Textures/  LoadFolders.xml
    ThemeKit/: blank theme files the Appearance tab's "New theme" copies (not loaded by the game)
    docs/API.md (as API.md) and docs/API.zh-TW.md (as API.zh-TW.md), for add-on authors
    ThemeTemplate/: docs/examples/ExampleTheme (a complete theme mod), tools/make_theme.py and its JSON, for theme
    authors (docs/workshop/theme-template-release.md explains the choice; --no-template leaves it out)
An existing About/PublishedFileId.txt in the target is kept, so uploads keep updating the same Workshop item.
The game reads Defs/, Textures/ and Languages/ only from the mod root (and LoadFolders.xml entries), so the copies
under ThemeTemplate/ are never loaded; the script checks that no loadable folder lands at the target's top level
by accident.
"""
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDERS = ["About", "1.7", "Defs", "Languages", "Textures", "ThemeKit"]
FILES = {
    "LoadFolders.xml": "LoadFolders.xml",
    os.path.join("docs", "API.md"): "API.md",
    os.path.join("docs", "API.zh-TW.md"): "API.zh-TW.md",
}
TEMPLATE_DIR = "ThemeTemplate"
TEMPLATE_FOLDERS = {os.path.join("docs", "examples", "ExampleTheme"): "ExampleTheme"}
TEMPLATE_FILES = {
    os.path.join("tools", "make_theme.py"): os.path.join("tools", "make_theme.py"),
    os.path.join("tools", "themes", "ExampleTheme.json"): os.path.join("tools", "ExampleTheme.json"),
}
KEEP = os.path.join("About", "PublishedFileId.txt")
LOADABLE = {"About", "Defs", "Textures", "Languages", "Assemblies", "Patches", "Sounds", "1.7", "ThemeTemplate", "ThemeKit"}


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    with_template = "--no-template" not in sys.argv
    if len(args) != 1:
        print(__doc__)
        return 1
    target = os.path.abspath(args[0])
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

    template = os.path.join(target, TEMPLATE_DIR)
    if os.path.exists(template):
        shutil.rmtree(template)
    if with_template:
        for src, dst in TEMPLATE_FOLDERS.items():
            shutil.copytree(os.path.join(ROOT, src), os.path.join(template, dst))
        for src, dst in TEMPLATE_FILES.items():
            os.makedirs(os.path.dirname(os.path.join(template, dst)), exist_ok=True)
            shutil.copy2(os.path.join(ROOT, src), os.path.join(template, dst))

    if kept is not None:
        with open(keep_path, "wb") as f:
            f.write(kept)

    unexpected = sorted(name for name in os.listdir(target) if os.path.isdir(os.path.join(target, name)) and name not in LOADABLE)
    if unexpected:
        print("Unexpected top-level folders in the release: " + ", ".join(unexpected))
        return 1

    count = sum(len(files) for _, _, files in os.walk(target))
    print(f"Release folder ready: {target} ({count} files{', with ThemeTemplate' if with_template else ''})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
