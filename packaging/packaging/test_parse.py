import re
import yaml

p = r"D:\084json\_Jukebox_src\JukeboxAssets\Assets\Jukebox\UI\JukeboxMenu.prefab"
raw = open(p, encoding="utf-8").read()
raw = re.sub(r"^%TAG.*$", "", raw, flags=re.M)
raw = re.sub(r"!u!\d+", "", raw)
raw = re.sub(r"^---\s*(&\d+)?\s*stripped\s*$", r"--- \1", raw, flags=re.M)
docs = list(yaml.safe_load_all(raw))
print("docs", len(docs))

acc = []
def walk(n):
    if isinstance(n, dict):
        for k, v in n.items():
            if k == "m_text" and isinstance(v, str):
                acc.append(v)
            else:
                walk(v)
    elif isinstance(n, list):
        for i in n:
            walk(i)

for d in docs:
    walk(d)

print("texts", len(acc))
for i in (0, 3, 30):
    if i < len(acc):
        print(i, repr(acc[i])[:400])
