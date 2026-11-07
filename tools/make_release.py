#!/usr/bin/env python3
"""Copies only what the game loads into a clean folder for uploading to the Steam Workshop.

Usage (from the repository root):
    python3 tools/make_release.py <target folder> [--clean] [--zip <folder>] [--no-template]

The repository is also the mod folder, so it contains source code, tests, tools, design documents and test
screenshots that players do not need. This copies:
    About/  1.7/  Defs/  Languages/  Textures/  LoadFolders.xml  LICENSE  LICENSES.md (which files use which license)
    ThemeKit/: blank theme files the Appearance tab's "New theme" copies (not loaded by the game)
    docs/API.md, docs/API.zh-TW.md and docs/API.zh-CN.md (same names), for add-on authors
    docs/ThemeGuide*.md and docs/ThemeGuide_NineSlice.png (same names): the step-by-step theme guide in English,
    Traditional and Simplified Chinese and Japanese
    ThemeTemplate/: docs/examples/ExampleTheme (a complete theme mod), tools/make_theme.py and its JSON, for theme
    authors (docs/workshop/theme-template-release.md explains the choice; --no-template leaves it out)
An existing About/PublishedFileId.txt in the target is kept, so uploads keep updating the same Workshop item.
The game reads Defs/, Textures/ and Languages/ only from the mod root (and LoadFolders.xml entries), so the copies
under ThemeTemplate/ are never loaded; the script checks that no loadable folder lands at the target's top level
by accident.

--clean   first removes everything else in the target (About/PublishedFileId.txt is kept), so the folder holds
          exactly the release, e.g. when the target is the copy in the game's Mods folder that the game uploads.
          Only done when the target is empty or is a copy of this mod (same packageId in About/About.xml).
--zip DIR also writes DIR/MainButtonBarSystem-<modVersion>.zip of the finished folder (one top folder
          MainButtonBarSystem/ inside, so it can be unpacked straight into Mods), for the GitHub release, and
          DIR/MainButtonBarSystem-<modVersion>-notes.md with that version's entry from CHANGELOG.md.
"""
import argparse
import os
import shutil
import sys
import xml.etree.ElementTree as ET
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDERS = ["About", "1.7", "Defs", "Languages", "Textures", "ThemeKit"]
FILES = {
    "LoadFolders.xml": "LoadFolders.xml",
    os.path.join("docs", "API.md"): "API.md",
    os.path.join("docs", "API.zh-TW.md"): "API.zh-TW.md",
    os.path.join("docs", "API.zh-CN.md"): "API.zh-CN.md",
    "LICENSE": "LICENSE",
    "LICENSES.md": "LICENSES.md",
}
for _guide in ("ThemeGuide.md", "ThemeGuide.zh-TW.md", "ThemeGuide.zh-CN.md", "ThemeGuide.ja.md",
               "ThemeGuide_NineSlice.png"):
    FILES[os.path.join("docs", _guide)] = _guide
TEMPLATE_DIR = "ThemeTemplate"
TEMPLATE_FOLDERS = {os.path.join("docs", "examples", "ExampleTheme"): "ExampleTheme"}
TEMPLATE_FILES = {
    os.path.join("tools", "make_theme.py"): os.path.join("tools", "make_theme.py"),
    os.path.join("tools", "themes", "ExampleTheme.json"): os.path.join("tools", "ExampleTheme.json"),
}
KEEP = os.path.join("About", "PublishedFileId.txt")
LOADABLE = {"About", "Defs", "Textures", "Languages", "Assemblies", "Patches", "Sounds", "1.7", "ThemeTemplate", "ThemeKit"}


def mod_info(about_xml):
    """(packageId, modVersion) from an About.xml; empty strings when missing or unreadable."""
    try:
        root = ET.parse(about_xml).getroot()
    except (ET.ParseError, OSError):
        return "", ""
    return (root.findtext("packageId") or "").strip(), (root.findtext("modVersion") or "").strip()


def is_link(path):
    """A symbolic link or (Windows) junction: never followed or deleted through."""
    return os.path.islink(path) or (hasattr(os.path, "isjunction") and os.path.isjunction(path))


def same_or_under(path, folder):
    """Whether path is folder or inside it (case-insensitive on Windows)."""
    path, folder = os.path.normcase(path), os.path.normcase(folder)
    return path == folder or path.startswith(folder + os.sep)


def clean_target(target, package_id):
    """
    Removes everything in the target except About/PublishedFileId.txt; returns an error message or None. Refuses
    unless the target is empty or holds a copy of this mod, so a wrong path (the whole Mods folder, another mod) is
    never wiped.
    """
    if not os.path.isdir(target) or not os.listdir(target):
        return None
    about = os.path.join(target, "About", "About.xml")
    if not os.path.isfile(about) or mod_info(about)[0].lower() != package_id.lower():
        return f"--clean refused: {target} is not empty and is not a copy of {package_id} (or its About.xml is unreadable)."
    about_dir = os.path.join(target, "About")
    links = [p for p in [target] + [os.path.join(target, e) for e in os.listdir(target)]
             + [os.path.join(about_dir, e) for e in os.listdir(about_dir)] if is_link(p)]
    if links:
        return "--clean refused: links or junctions in the target, remove them by hand first: " + ", ".join(links)
    for entry in os.listdir(target):
        path = os.path.join(target, entry)
        if entry == "About" and os.path.isdir(path):
            for name in os.listdir(path):
                inner = os.path.join(path, name)
                if os.path.join("About", name) == KEEP:
                    continue
                if os.path.isdir(inner):
                    shutil.rmtree(inner)
                else:
                    os.remove(inner)
        elif os.path.isdir(path):
            shutil.rmtree(path)
        else:
            os.remove(path)
    return None


def write_zip(target, zip_dir, version):
    """Zips the finished release folder as <zip_dir>/MainButtonBarSystem-<version>.zip; returns its path."""
    os.makedirs(zip_dir, exist_ok=True)
    path = os.path.join(zip_dir, f"MainButtonBarSystem-{version}.zip")
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for folder, dirs, files in os.walk(target):
            dirs.sort()
            for name in sorted(files):
                full = os.path.join(folder, name)
                archive.write(full, os.path.join("MainButtonBarSystem", os.path.relpath(full, target)))
    return path


def changelog_section(version):
    """The text under "## <version>" in CHANGELOG.md, up to the next "## " heading; None when there is no entry."""
    path = os.path.join(ROOT, "CHANGELOG.md")
    if not os.path.isfile(path):
        return None
    lines, inside = [], False
    with open(path, encoding="utf-8") as f:
        for line in f:
            if line.startswith("## "):
                if inside:
                    break
                words = line[3:].split()
                inside = bool(words) and words[0].lstrip("vV") == version   # "## 1.0.0", "## v1.0.0 (2026-11-08)"
                continue
            if inside:
                lines.append(line)
    text = "".join(lines).strip()
    return text + "\n" if text else None


def main():
    parser = argparse.ArgumentParser(description="Copy only what the game loads into a clean release folder.")
    parser.add_argument("target", help="the release folder (outside the repository)")
    parser.add_argument("--no-template", action="store_true", help="leave out ThemeTemplate/")
    parser.add_argument("--clean", action="store_true", help="first remove everything else in the target")
    parser.add_argument("--zip", metavar="DIR", help="also write MainButtonBarSystem-<modVersion>.zip into DIR")
    args = parser.parse_args()
    with_template = not args.no_template

    target = os.path.abspath(args.target)
    if same_or_under(target, ROOT):
        print("The target must be outside the repository.")
        return 1
    if os.path.isfile(target):
        print(f"The target is a file, not a folder: {target}")
        return 1
    zip_dir = os.path.abspath(args.zip) if args.zip else None
    if zip_dir and (same_or_under(zip_dir, target) or same_or_under(zip_dir, ROOT)):
        print("The zip must not be written inside the release folder or the repository.")
        return 1
    package_id, version = mod_info(os.path.join(ROOT, "About", "About.xml"))
    if zip_dir and not version:
        print("About/About.xml has no modVersion, so the zip cannot be named.")
        return 1

    if args.clean:
        problem = clean_target(target, package_id)
        if problem:
            print(problem)
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
    stray = sorted(name for name in os.listdir(target)
                   if os.path.isfile(os.path.join(target, name)) and name not in FILES.values())
    if stray:
        print("Note: files that are not part of the release are in the folder (use --clean): " + ", ".join(stray))

    count = sum(len(files) for _, _, files in os.walk(target))
    print(f"Release folder ready: {target} ({count} files{', with ThemeTemplate' if with_template else ''}), "
          f"version {version or '?'}" + ("" if kept is None else ", PublishedFileId.txt kept"))
    notes = changelog_section(version) if version else None
    if version and notes is None:
        print(f"Note: CHANGELOG.md has no '## {version}' entry yet.")
    if zip_dir:
        print("Zip: " + write_zip(target, zip_dir, version))
        if notes is not None:
            notes_path = os.path.join(zip_dir, f"MainButtonBarSystem-{version}-notes.md")
            with open(notes_path, "w", encoding="utf-8", newline="\n") as f:
                f.write(notes)
            print("Release notes: " + notes_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
