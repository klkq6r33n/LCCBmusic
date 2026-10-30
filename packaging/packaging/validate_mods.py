import yaml

p = r"C:\Users\A\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\zhibo\mods.yml"
raw = open(p, encoding="utf-8-sig").read()
try:
    data = yaml.safe_load(raw)
    print("entries", len(data))
    for entry in data:
        if entry.get("name") == "Claude-JukeboxLimbusCompany":
            print("displayName:", entry.get("displayName"))
            print("description lines:", len(entry.get("description", "").splitlines()))
            print("first:", entry["description"].splitlines()[0])
            print("last:", entry["description"].splitlines()[-1])
            print("enabled:", entry.get("enabled"))
    for entry in data:
        if entry.get("name") == "Flazhik-CybergrindMusicExplorer":
            print("CGME enabled:", entry.get("enabled"))
except Exception as exc:
    print("PARSE ERROR:", exc)
