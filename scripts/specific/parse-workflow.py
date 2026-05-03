import sys, re, json

if len(sys.argv) < 3:
    sys.exit(1)

workflow_md = sys.argv[1]
output_json = sys.argv[2]
base = "https://cruzlauroiii.github.io/wolfstruckingco.com"

phase_routes = {
    1: "/Login/",
    2: "/Apply/",
    3: "/HiringHall/",
    4: "/Marketplace/",
    5: "/Marketplace/",
    6: "/Map/",
    7: "/Track/",
    8: "/Map/",
    9: "/Track/",
    10: "/Map/",
    11: "/Map/",
    12: "/Track/",
    13: "/HiringHall/",
    14: "/Dispatcher/",
}

phase_titles = {
    1: "SSO Login",
    2: "Driver onboarding",
    3: "Admin hiring",
    4: "Marketplace + employer setup",
    5: "Buyer purchase + schedule",
    6: "Driver 1 China leg",
    7: "Ocean transit + customs",
    8: "Driver 2 LA leg",
    9: "Realtime delay + recompute",
    10: "Driver 3 cross-country",
    11: "Driver 4 final mile",
    12: "Delivery + COD",
    13: "Settlement + KPIs",
    14: "Dispatcher control on behalf",
}

with open(workflow_md, 'r', encoding='utf-8') as f:
    text = f.read()

phase_re = re.compile(r'^### Phase (\d+) — (.+?) \((\d+)\)$', re.M)
scenes = []
i = 1
for m in phase_re.finditer(text):
    phase_num = int(m.group(1))
    phase_name = m.group(2)
    count = int(m.group(3))
    route = phase_routes.get(phase_num, "/")
    title = phase_titles.get(phase_num, phase_name)
    for n in range(count):
        cb = f"{phase_num:02d}{n+1:02d}"
        url = f"{base}{route}?cb={cb}"
        narration = f"Phase {phase_num} {title} step {n+1} of {count}"
        scenes.append({
            "index": i,
            "pad": f"{i:03d}",
            "url": url,
            "narration": narration,
            "selector": "",
            "typeText": "",
            "profileDir": "C:\\chrome-profiles\\car-seller-google"
        })
        i += 1

with open(output_json, 'w', encoding='utf-8') as f:
    json.dump(scenes, f, indent=2)

print(f"wrote {len(scenes)} scenes")
