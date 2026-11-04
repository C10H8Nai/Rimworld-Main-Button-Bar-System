#!/usr/bin/env python3
"""Generates Languages/<Language>/ from tools/i18n/<Language>.json.

Usage (from the repository root):
    python3 tools/gen_translations.py           # write all languages, report gaps
    python3 tools/gen_translations.py --check   # only check, write nothing; exit 1 on errors
    python3 tools/gen_translations.py --check French German   # check only these languages

tools/i18n/English.json is the source. Each language file has the same shape:
    {
      "Keyed":       { "MBBS_Key": "text", ... },
      "DefInjected": { "MainButtonBarSystem.BarDisplayModeDef": { "Vanilla.label": "text", ... }, ... }
    }

Rules checked for every language:
  - every placeholder ({0}, {1}, ...) of the English text appears in the translation, and no others;
  - keys that do not exist in English are errors;
  - keys missing from a translation are reported (the game falls back to English for them).

English DefInjected entries are only the source for translators; the English text lives in the Def XML,
so no English DefInjected files are written.
"""
import json
import os
import re
import sys
from xml.sax.saxutils import escape

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
I18N = os.path.join(ROOT, "tools", "i18n")
KEYED_FILE = "MainButtonBarSystem.xml"
PLACEHOLDER = re.compile(r"\{\d+\}")


def load(language):
    with open(os.path.join(I18N, language + ".json"), encoding="utf-8") as f:
        return json.load(f)


def placeholders(text):
    return sorted(set(PLACEHOLDER.findall(text)))


def check(language, data, english):
    errors, missing = [], []
    for key, text in data.get("Keyed", {}).items():
        if key not in english["Keyed"]:
            errors.append(f"{language}: Keyed {key} does not exist in English")
        elif placeholders(text) != placeholders(english["Keyed"][key]):
            errors.append(f"{language}: Keyed {key} placeholders {placeholders(text)} != English {placeholders(english['Keyed'][key])}")
        elif not text.strip():
            errors.append(f"{language}: Keyed {key} is empty")
    missing += [f"Keyed {k}" for k in english["Keyed"] if k not in data.get("Keyed", {})]

    for def_type, entries in english.get("DefInjected", {}).items():
        theirs = data.get("DefInjected", {}).get(def_type, {})
        for key in entries:
            if key not in theirs:
                missing.append(f"DefInjected {def_type} {key}")
        for key, text in theirs.items():
            if key not in entries:
                errors.append(f"{language}: DefInjected {def_type} {key} does not exist in English")
            elif placeholders(text) != placeholders(entries[key]):
                errors.append(f"{language}: DefInjected {def_type} {key} placeholders differ from English")
    for def_type in data.get("DefInjected", {}):
        if def_type not in english.get("DefInjected", {}):
            errors.append(f"{language}: DefInjected type {def_type} does not exist in English")
    return errors, missing


def write_xml(path, entries):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n<LanguageData>\n')
        for key, text in entries:
            f.write(f"  <{key}>{escape(text)}</{key}>\n")
        f.write("</LanguageData>\n")


def write(language, data, english):
    lang_dir = os.path.join(ROOT, "Languages", language)
    order = list(english["Keyed"])
    keyed = data.get("Keyed", {})
    write_xml(os.path.join(lang_dir, "Keyed", KEYED_FILE), [(k, keyed[k]) for k in order if k in keyed])

    if language == "English":
        return
    for def_type, entries in english.get("DefInjected", {}).items():
        theirs = data.get("DefInjected", {}).get(def_type, {})
        rows = [(k, theirs[k]) for k in entries if k in theirs]
        if not rows:
            continue
        file_name = def_type.split(".")[-1].replace("Def", "") + "s.xml"
        write_xml(os.path.join(lang_dir, "DefInjected", def_type, file_name), rows)


def main():
    only_check = "--check" in sys.argv
    wanted = [a for a in sys.argv[1:] if not a.startswith("--")]
    english = load("English")
    languages = wanted or sorted(f[:-5] for f in os.listdir(I18N) if f.endswith(".json"))
    all_errors = []
    for language in languages:
        data = english if language == "English" else load(language)
        errors, missing = ([], []) if language == "English" else check(language, data, english)
        all_errors += errors
        status = "ok" if not missing else f"{len(missing)} missing (falls back to English)"
        print(f"{language:22} {len(data.get('Keyed', {})):4} keys  {status}")
        for m in missing[:5]:
            print(f"    missing: {m}")
        if not only_check and not errors:
            write(language, data, english)
    for e in all_errors:
        print("ERROR " + e)
    return 1 if all_errors else 0


if __name__ == "__main__":
    sys.exit(main())
