import json

p = r"D:\084json\Jukebox084\Jukebox\LimbusCompany\localization.json"
d = json.load(open(p, encoding="utf-8"))
print("entries", len(d))
print("custom", any(k.startswith("<size=24>CUSTOM TRACKS") for k in d))
print("downloader", any(k.startswith("Now you can download") for k in d))
print("themes", any(k.startswith("You know how in original") for k in d))
print("cookies", any(k.startswith("YouTube's issued") for k in d))
print("radiant", d.get("RADIANT"))
print("calm upper", d.get("CALM & BATTLE THEMES"))
