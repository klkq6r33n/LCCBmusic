import glob
import json
import re
import sys
import yaml

ROOT = r"D:\084json\_Jukebox_src\JukeboxAssets\Assets\Jukebox"
texts = set()


def load_docs(path):
    raw = open(path, encoding="utf-8").read()
    raw = re.sub(r"^%TAG.*$", "", raw, flags=re.M)
    raw = re.sub(r"!u!\d+", "", raw)
    raw = re.sub(r"^---\s*(&\d+)?\s*stripped\s*$", r"--- \1", raw, flags=re.M)
    raw = re.sub(r"^(\s*m_text:\s*)(<<|>>)\s*$", r"\1'\2'", raw, flags=re.M)
    return list(yaml.safe_load_all(raw))


def walk(node):
    if isinstance(node, dict):
        for key, value in node.items():
            if key in ("m_text", "label", "title", "sideNote", "description", "buttonLabel") and isinstance(value, str):
                if value.strip():
                    texts.add(value)
            elif key == "dropdownList" and isinstance(value, list):
                for item in value:
                    if isinstance(item, str) and item.strip():
                        texts.add(item)
            else:
                walk(value)
    elif isinstance(node, list):
        for item in node:
            walk(item)


files = glob.glob(ROOT + r"\**\*.prefab", recursive=True) + glob.glob(ROOT + r"\**\*.asset", recursive=True)
for path in files:
    if "TextMesh Pro" in path or "Fonts & Materials" in path or "Sprite Assets" in path:
        continue
    try:
        for doc in load_docs(path):
            walk(doc)
    except Exception as exc:
        print("ERR", path, exc, file=sys.stderr)

out = sorted(texts)
with open(r"D:\084json\Jukebox084\packaging\extracted_strings.json", "w", encoding="utf-8") as handle:
    json.dump(out, handle, ensure_ascii=False, indent=2)
print("count", len(out))
