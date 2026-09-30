#!/usr/bin/env python3
"""Keeps Tests/TestsUpdate.md up to date. Run by Tests/hooks/pre-commit on every commit.

What it rewrites (between the AUTO markers of TestsUpdate.md):
  NAMES  the object names and private members the tests look for, and whether they still exist in the project
  MAP    which tests to review for which changed files (from Tests/tools/tests-map.json)
  TESTS  the list of all tests
  LOG    a line per commit that touched files with tests attached (only with --staged or --files)

Everything outside the markers is hand-written guidance and is never touched.
It never blocks a commit: on any problem it prints a warning and leaves the file as it was.
"""
import fnmatch
import json
import os
import re
import subprocess
import sys
from datetime import date

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DOC = os.path.join(ROOT, "Tests", "TestsUpdate.md")
MAP = os.path.join(ROOT, "Tests", "tools", "tests-map.json")
TESTS_DIR = os.path.join(ROOT, "Tests", "unity")
SCRIPTS_DIR = os.path.join(ROOT, "Assets", "Scripts")
SCENES_DIR = os.path.join(ROOT, "Assets", "Scenes")
MAX_LOG = 15


def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()


def walk(directory, suffix):
    for folder, _, files in os.walk(directory):
        for name in sorted(files):
            if name.endswith(suffix):
                yield os.path.join(folder, name)


def rel(path):
    return os.path.relpath(path, ROOT).replace(os.sep, "/")


def staged_files():
    out = subprocess.run(["git", "diff", "--cached", "--name-only", "--diff-filter=ACMRD"], cwd=ROOT,
                         capture_output=True, text=True, check=True).stdout
    return [line.strip() for line in out.splitlines() if line.strip()]


def load_map():
    with open(MAP, encoding="utf-8") as f:
        return json.load(f)


def matching_rules(path, rules):
    return [rule for rule in rules if any(fnmatch.fnmatch(path, pattern) for pattern in rule["paths"])]


# ---------------------------------------------------------------- tests

TEST_ATTRIBUTE = re.compile(r"\[(?:Test|UnityTest)[^\]]*\]\s*(?:\[[^\]]*\]\s*)*public\s+\S+\s+(\w+)\s*\(")


def test_inventory():
    result = {}
    for path in walk(TESTS_DIR, "Tests.cs"):
        names = TEST_ATTRIBUTE.findall(read(path))
        if names:
            result[rel(path)] = names
    return result


# ---------------------------------------------------------------- names the tests depend on

MEMBER_PATTERNS = [
    r'GetPrivate<[^>]+>\([^,]+,\s*"(\w+)"',
    r'SetPrivate\([^,]+,\s*"(\w+)"',
    r'CallPrivate\([^,]+,\s*"(\w+)"',
    r'GetMethod\("(\w+)"',
    r'GetField\("(\w+)"',
    r'FindProperty\("(\w+)"',
]
OBJECT_PATTERNS = [
    r'\bFind<[^>]+>\("([^"]+)"\)',
    r'\bButton\("([^"]+)"\)',
    r'\.Find\("([^"]+)"\)',
    r'GameObject\.Find\("([^"]+)"\)',
    r'in new\[\]\s*\{([^}]*)\}',
]
# Names a test expects NOT to exist (removed buttons).
ABSENT_PATTERN = r'Assert\.IsNull\(GameObject\.Find\("([^"]+)"\)'


def collect_names():
    members, objects, absent = {}, {}, {}
    for path in walk(TESTS_DIR, ".cs"):
        text = read(path)
        for pattern in MEMBER_PATTERNS:
            for name in re.findall(pattern, text):
                members.setdefault(name, set()).add(os.path.basename(path))
        for name in re.findall(ABSENT_PATTERN, text):
            absent.setdefault(name, set()).add(os.path.basename(path))
        for pattern in OBJECT_PATTERNS:
            for match in re.findall(pattern, text):
                for name in re.findall(r'"([^"]+)"', match) if pattern.startswith(r"in new") else [match]:
                    # Only names that look like scene objects (no spaces, not messages).
                    for part in name.split("/"):
                        if re.fullmatch(r"[A-Za-z][A-Za-z0-9_ ()]*", part) and " " not in part:
                            objects.setdefault(part, set()).add(os.path.basename(path))
    for name in absent:
        objects.pop(name, None)
    return members, objects, absent


def all_text(directory, suffix):
    return "\n".join(read(p) for p in walk(directory, suffix))


def scene_has(scenes, name):
    # A plain object, or an object of a prefab instance whose name is overridden in the scene.
    return re.search(r"(m_Name: |propertyPath: m_Name\s+value: )" + re.escape(name) + r"\s*$", scenes, re.MULTILINE) is not None


def names_section():
    members, objects, absent = collect_names()
    code = all_text(SCRIPTS_DIR, ".cs")
    scenes = all_text(SCENES_DIR, ".unity")
    lines = ["Sprawdzane automatycznie przy każdym commicie: nazwa, w których testach jest użyta, i czy wciąż istnieje w projekcie.", ""]
    lines += ["**Składowe prywatne i metody (odczytywane przez refleksję):**", ""]
    missing = 0
    for name in sorted(members):
        found = re.search(r"\b" + re.escape(name) + r"\b", code) is not None
        missing += 0 if found else 1
        lines.append(f"- `{name}` ({', '.join(sorted(members[name]))}){'' if found else ' **⚠ nie znaleziono w kodzie**'}")
    lines += ["", "**Obiekty sceny (szukane po nazwie):**", ""]
    for name in sorted(objects):
        found = scene_has(scenes, name)
        missing += 0 if found else 1
        lines.append(f"- `{name}` ({', '.join(sorted(objects[name]))}){'' if found else ' **⚠ nie znaleziono w scenach**'}")
    if absent:
        lines += ["", "**Obiekty, których testy oczekują, że NIE istnieją:**", ""]
        for name in sorted(absent):
            present = scene_has(scenes, name)
            missing += 1 if present else 0
            lines.append(f"- `{name}` ({', '.join(sorted(absent[name]))}){' **⚠ znowu jest w scenie**' if present else ''}")
    if missing:
        lines += ["", f"**⚠ {missing} nazw z testów nie ma już w projekcie: zmień je w testach albo w kodzie/scenie.**"]
    return "\n".join(lines)


def map_section(rules):
    lines = ["| Zmieniasz | Sprawdź testy | Dlaczego |", "|---|---|---|"]
    for rule in rules:
        paths = "<br>".join(f"`{p}`" for p in rule["paths"])
        lines.append(f"| {paths} | {', '.join(rule['tests'])} | {rule['why']} |")
    return "\n".join(lines)


def tests_section(inventory):
    total = sum(len(v) for v in inventory.values())
    lines = [f"Razem: **{total}** testów w {len(inventory)} plikach.", ""]
    for path, names in inventory.items():
        lines.append(f"- `{path}` ({len(names)}): " + ", ".join(names))
    return "\n".join(lines)


# ---------------------------------------------------------------- log

def log_entry(files, rules):
    changed = [f for f in files if not f.startswith("Tests/")]
    tests, touched, uncovered = [], [], []
    for path in changed:
        matched = matching_rules(path, rules)
        if matched:
            touched.append(path)
            for rule in matched:
                for test in rule["tests"]:
                    if test not in tests:
                        tests.append(test)
        elif path.startswith("Assets/Scripts/") and path.endswith(".cs"):
            uncovered.append(path)
    if not touched and not uncovered:
        return None
    shown = touched[:5] + ([f"+{len(touched) - 5} więcej"] if len(touched) > 5 else [])
    text = f"- {date.today().isoformat()}: " + ", ".join(f"`{p}`" for p in shown)
    if tests:
        text += " → sprawdź: " + ", ".join(tests)
    if uncovered:
        text += " · **bez testów:** " + ", ".join(f"`{p}`" for p in uncovered[:5])
    return text


def replace_between(doc, name, body):
    start, end = f"<!-- AUTO:{name}:START -->", f"<!-- AUTO:{name}:END -->"
    a, b = doc.find(start), doc.find(end)
    if a < 0 or b < 0 or b < a:
        raise ValueError(f"Missing markers {name} in Tests/TestsUpdate.md")
    return doc[: a + len(start)] + "\n" + body + "\n" + doc[b:]


def existing_log(doc):
    start, end = "<!-- AUTO:LOG:START -->", "<!-- AUTO:LOG:END -->"
    a, b = doc.find(start), doc.find(end)
    if a < 0 or b < 0:
        return []
    return [line for line in doc[a + len(start): b].splitlines() if line.startswith("- ")]


def main(argv):
    files = None
    if "--staged" in argv:
        files = staged_files()
    elif "--files" in argv:
        files = argv[argv.index("--files") + 1].split(",")
    rules = load_map()
    doc = read(DOC)
    new = doc
    new = replace_between(new, "NAMES", names_section())
    new = replace_between(new, "MAP", map_section(rules))
    new = replace_between(new, "TESTS", tests_section(test_inventory()))
    if files:
        entry = log_entry(files, rules)
        if entry:
            lines = [entry] + [l for l in existing_log(new) if l != entry]
            new = replace_between(new, "LOG", "\n".join(lines[:MAX_LOG]))
    if new != doc:
        with open(DOC, "w", encoding="utf-8", newline="\n") as f:
            f.write(new)
        print("TestsUpdate.md updated")
    else:
        print("TestsUpdate.md is up to date")


if __name__ == "__main__":
    try:
        main(sys.argv[1:])
    except Exception as error:  # never block a commit
        print(f"TestsUpdate: skipped ({error})", file=sys.stderr)
    sys.exit(0)
