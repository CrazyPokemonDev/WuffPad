#!/usr/bin/env python3
"""Rebuild the files that WuffPad used to download at startup from its server.

WuffPad was shut down and its server (crazypokemondev.de) is gone.  At startup
the application tried to fetch a handful of helper files into ``Resources/``:

    language.xml       English "blueprint" used by File > New
    descriptions.dict  JSON map key -> human readable description
    standardKeys.db    one key per line, all keys a langfile should contain
    emojis.txt         one emoji per line, fills the emoji keyboard

This script re-creates them from the upstream Werewolf repository
(https://github.com/GreyWolfDev/Werewolf, GPL-3.0), which still ships the
English langfile and the canonical emoji langfile.

Usage:

    python3 tools/make-builtin-resources.py --english English.xml \
                                            --emoji Emoji.xml \
                                            [-o Resources/Builtin]

``English.xml`` and ``Emoji.xml`` can be downloaded with

    gh api "repos/GreyWolfDev/Werewolf/contents/Werewolf%20for%20Telegram/Languages/English.xml" \
        -H "Accept: application/vnd.github.raw" > English.xml
    gh api "repos/GreyWolfDev/Werewolf/contents/Werewolf%20for%20Telegram/Languages/Emoji.xml" \
        -H "Accept: application/vnd.github.raw" > Emoji.xml

The output directory is the build action's input; see the .csproj for how it is
embedded into the assembly.
"""

import argparse
import json
import os
import re
import sys
import unicodedata
import xml.etree.ElementTree as ET

# Emoji are everything outside the "normal" script/punctuation/whitespace blocks
# plus the handful of ASCII symbols that are presented as emoji in the langfile.
KEEP_ASCII = set("©®™‼⁉")


def parse_langfile(path):
    """Return (root element, ordered list of (key, [values]), comments per key)."""
    with open(path, "r", encoding="utf-8-sig") as fh:
        text = fh.read()
    root = ET.fromstring(text)

    comments = {}
    # Collect <!-- ... --> blocks that appear inside a <string> element and use
    # them to describe the string: usually "{0} - player name" style hints.
    for match in re.finditer(r"<string\b[^>]*\bkey=\"([^\"]*)\"[^>]*>(.*?)</string>", text, re.S):
        key, body = match.group(1), match.group(2)
        found = []
        for comment in re.findall(r"<!--(.*?)-->", body, re.S):
            comment = " ".join(comment.split())
            if comment and comment not in found:
                found.append(comment)
        if found:
            comments[key] = found
    return root, comments


PLACEHOLDER = re.compile(r"^\{?(\d)\}?\s*[-=]\s*(.+)$")


def normalise_hint(hint):
    """Turn ``0 - player name`` into ``{0} - player name`` and tidy punctuation."""
    hint = " ".join(hint.split())
    match = PLACEHOLDER.match(hint)
    if match:
        hint = "{%s} - %s" % (match.group(1), match.group(2))
    elif hint[:1].islower():
        hint = hint[:1].upper() + hint[1:]
    if not hint.endswith((".", "!", "?")):
        hint += "."
    return hint


def describe(key, hints):
    """Human readable description, mirroring the wording WuffPad used to show."""
    if not hints:
        return "No description yet."
    normalised = []
    for hint in hints:
        hint = normalise_hint(hint)
        if hint not in normalised:
            normalised.append(hint)
    return " ".join(normalised)


def is_emoji_run(chunk):
    for ch in chunk:
        if ch in ("\u200d", "\ufe0f", "\u20e3", "\u200b"):
            continue
        if ch in KEEP_ASCII:
            continue
        cat = unicodedata.category(ch)
        cp = ord(ch)
        if cp < 0x2000:  # ASCII and basic latin-1 letters/digits/punctuation
            return False
        if cat.startswith("L") and cp < 0x1F000:
            return False
        if cat in ("Zl", "Zp"):
            return False
    return True


def collect_emojis(values):
    """Split values into emoji runs, keeping order and dropping duplicates."""
    # Split on whitespace, then break remaining runs into grapheme-ish clusters
    # separated by spaces or by ASCII text.
    chunks = []
    for value in values:
        for part in re.split(r"\s+|(?<=[^\u200d\ufe0f\u20e3])_(?=.)", value):
            part = part.strip()
            if part and is_emoji_run(part):
                chunks.append(part)
    return chunks


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--english", required=True, help="English.xml from GreyWolfDev/Werewolf")
    parser.add_argument("--emoji", required=True, help="Emoji.xml from GreyWolfDev/Werewolf")
    parser.add_argument("-o", "--output", default=os.path.join("Resources", "Builtin"),
                        help="output directory (default: Resources/Builtin)")
    args = parser.parse_args(argv)

    english_root, english_comments = parse_langfile(args.english)
    emoji_root, _ = parse_langfile(args.emoji)

    strings = english_root.findall("string")
    if not strings:
        parser.error("%s does not contain any <string> elements" % args.english)

    out = args.output
    if not os.path.isdir(out):
        os.makedirs(out)

    # language.xml -- verbatim copy of the English blueprint.
    with open(os.path.join(out, "language.xml"), "wb") as fh:
        with open(args.english, "rb") as src:
            fh.write(src.read())

    # descriptions.dict -- JSON object, same shape WuffPad deserialized.
    descriptions = {}
    for element in strings:
        key = element.get("key")
        if key is None:
            continue
        descriptions[key] = describe(key, english_comments.get(key))
    with open(os.path.join(out, "descriptions.dict"), "w", encoding="utf-8", newline="\n") as fh:
        json.dump(descriptions, fh, ensure_ascii=False, indent=1, sort_keys=True)
        fh.write("\n")

    # standardKeys.db -- one key per line, in the canonical order of the blueprint.
    keys = []
    for element in strings:
        key = element.get("key")
        if key is not None and key not in keys:
            keys.append(key)
    with open(os.path.join(out, "standardKeys.db"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(keys) + "\n")

    # emojis.txt -- one emoji per line for the emoji keyboard.
    emoji_values = []
    for element in emoji_root.findall("string"):
        for value in element.findall("value"):
            emoji_values.append(value.text or "")
    seen = set()
    emojis = []
    for chunk in collect_emojis(emoji_values):
        if chunk not in seen:
            seen.add(chunk)
            emojis.append(chunk)
    with open(os.path.join(out, "emojis.txt"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(emojis) + "\n")

    print("wrote %d strings, %d descriptions, %d keys and %d emojis to %s"
          % (len(strings), len(descriptions), len(keys), len(emojis), out))
    return 0


if __name__ == "__main__":
    sys.exit(main())
