import sys, json
if len(sys.argv) < 3:
    sys.exit(1)
src = sys.argv[1]
dst = sys.argv[2]
with open(src, 'r', encoding='utf-8') as f:
    data = json.load(f)
out = []
for i, s in enumerate(data, 1):
    pad = f"{i:03d}"
    out.append({
        "index": i,
        "pad": pad,
        "url": s.get("target", ""),
        "narration": s.get("narration", ""),
        "selector": "",
        "typeText": "",
        "profileDir": "C:\\chrome-profiles\\car-seller-google"
    })
with open(dst, 'w', encoding='utf-8') as f:
    json.dump(out, f, indent=2)
print(f"wrote {len(out)} scenes")
