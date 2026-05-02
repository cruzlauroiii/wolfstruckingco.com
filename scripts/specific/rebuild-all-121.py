import os, json, subprocess, time, tempfile, uuid, sys

REPO = r"C:\repo\public\wolfstruckingco.com"
FRAMES = r"C:\Users\user1\AppData\Local\Temp\wolfs-video\frames"
AUDIO = r"C:\Users\user1\AppData\Local\Temp\wolfs-video\audio-edge"
DOCS = r"C:\repo\public\wolfstruckingco.com\main\docs\videos"
SCENES = r"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-final.json"

os.makedirs(FRAMES, exist_ok=True)

CHAT_GROUPS = {
    'A': {
        'pads': ['004','005','006','007','008','009'],
        'msgs': {
            '004': "I want to ship a BYD Han EV (2024 model, VIN LGXC76DH7L0123456) from China to the US. Shipper of record: Wei Zhang, phone 86-138-5555-0123, email wei.zhang@hefei-export.com.",
            '005': "Pickup at BYD factory, 8 Daoxiang Rd, Hefei, Anhui 230000 China. Drop at 1428 Oak Street, Wilmington NC 28401 USA. Vehicle brand new operable, fully insured. Pickup date 2026-06-15.",
            '006': "Factory invoice $18,000 paid by SWIFT wire on pickup. Bill all Wolfs freight to me on RTP. Customs HTSUS 8703.80.00, Section 301 tariff covered separately.",
            '007': "Buyer Sam Chen, phone 910-555-0123, email sam.chen@wilmington-buyer.com, address 1428 Oak Street Wilmington NC 28401. Delivery window 45 days from pickup. Sam pre-authorized COD.",
            '008': "Sale price $48,500 COD via RTP at delivery. Required driver badges: china-export, ocean-carrier, port-LA-drayage, cross-country-team, auto-handling-final. Multi-leg pay configured. All fields are final.",
            '009': "Every required field is confirmed (VIN, shipper, buyer, addresses, prices, payments, dates, badges). Publish this job to the Wolfs marketplace now and save it to the R2 listings collection. Do not ask for any more details just publish and reply with the new listing id.",
        }
    },
    'B': {
        'pads': ['013','014','015'],
        'msgs': {
            '013': "I am Wei Liu, applying as a driver from Hefei, China. Ten years driving cross-border freight. Save my application to R2 applicants collection with id wei_liu_china.",
            '014': "Documents: Chinese commercial CDL (license LGZ-CN-2014-7782), China export pass (CE-2024-Hefei-991). Save to R2 documents collection.",
            '015': "DOT compliance current. HOS tracked daily. Available for the BYD China-export job. Match me to listing byd-han-ev-hefei-wilmington-2026.",
        }
    },
    'C': {
        'pads': ['020','021','022'],
        'msgs': {
            '020': "I am Marco Rivera, driver from San Pedro CA. Eight years on Port of LA drayage. Save my application to R2 applicants id marco_rivera_la.",
            '021': "TWIC card TWC-2022-LA-4471, California CDL Class A, both current. Save credentials to R2 documents.",
            '022': "Container handling and hazmat endorsements active. Available for LA leg of byd-han-ev-hefei-wilmington-2026.",
        }
    },
    'D': {
        'pads': ['027','028','029'],
        'msgs': {
            '027': "We are Diego Morales and Maria Santos, team drivers based in Phoenix AZ. Save team application to R2 applicants id diego_maria_phoenix.",
            '028': "Joint Visa Direct payouts on shared account. CDL-A both, team-driver HOS certs current.",
            '029': "Available for cross-country team leg of byd-han-ev-hefei-wilmington-2026.",
        }
    },
    'E': {
        'pads': ['034','035','036'],
        'msgs': {
            '034': "I am Sam Chen Jr, driver from Wilmington NC. Four years on auto-handling. Save application to R2 applicants id sam_chen_jr_wilmington.",
            '035': "Vehicle inspection cert current, work nights. Class A CDL ready.",
            '036': "Chase RTP for instant payout. Available for final-mile leg of byd-han-ev-hefei-wilmington-2026.",
        }
    },
    'F': {
        'pads': ['065','066','067'],
        'msgs': {
            '065': "I am Sam Chen, buyer in Wilmington NC. Show me the BYD Han EV listing byd-han-ev-hefei-wilmington-2026 from R2.",
            '066': "Confirm delivery window and total price. I will pay COD via RTP at delivery.",
            '067': "Buy the listing. Save purchase record to R2 purchases id pur_sam_byd with status purchased and link to listing byd-han-ev-hefei-wilmington-2026.",
        }
    },
    'G': {
        'pads': ['071'],
        'msgs': { '071': "Schedule the multi-leg job for byd-han-ev-hefei-wilmington-2026. Save schedule to R2 schedules id sch_byd with legs 1-5." }
    },
    'H': {
        'pads': ['076'],
        'msgs': { '076': "Confirm LA pickup and Wilmington final-mile windows. Update R2 schedules sch_byd with confirmed times." }
    },
    'I': {
        'pads': ['081','083'],
        'msgs': {
            '081': "Driver Wei Liu update: ETA at Hefei factory 2026-06-15 09:00. Update R2 itineraries id itin_d1 status arrived.",
            '083': "Container loaded ready for ocean transit. Update R2 itineraries itin_d1 status loaded.",
        }
    },
    'J': {
        'pads': ['087','088'],
        'msgs': {
            '087': "Customs hold released. Update R2 itineraries itin_d1 status customs-cleared.",
            '088': "Section 301 tariff $1,212.50 covered from COD escrow. Save to R2 charges id chg_tariff_301.",
        }
    },
    'K': {
        'pads': ['094','095'],
        'msgs': {
            '094': "Driver Marco Rivera picking up at LA terminal 401 now. Update R2 itineraries id itin_d2 status pickup.",
            '095': "On the road to Phoenix, ETA seven hours. Update R2 itineraries itin_d2 status in-transit.",
        }
    },
    'L': {
        'pads': ['101','102','103','104','105','106'],
        'msgs': {
            '101': "I-10 East mile 78 Boyle Heights flip 47-min stoppage. Recompute schedule. Update R2 schedules sch_byd with recomputeReason 'I-10 flip'.",
            '102': "Driver Diego Morales picking up the leg in Phoenix. Update R2 itineraries id itin_d3 status pickup.",
            '103': "Cross-country corridor I-10 to I-40 to Memphis. Update R2 itineraries itin_d3 status in-transit.",
            '104': "Team driver split on sleeper berth. Update R2 itineraries itin_d3 with note 'sleeper berth split'.",
            '105': "Memphis 24/7 yard handoff to Driver Sam Chen Jr. Update R2 itineraries itin_d3 status handoff.",
            '106': "Final mile to Wilmington, ETA 21:30. Update R2 itineraries id itin_d4 status final-mile.",
        }
    },
}

def cdp(name, body):
    cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        " + body + "\n    }\n}\n"
    p = os.path.join(tempfile.gettempdir(), f"cdp-{name}-{uuid.uuid4().hex}.cs")
    with open(p, 'w', encoding='utf-8') as f:
        f.write(cfg)
    r = subprocess.run(['dotnet', 'run', 'main/scripts/generic/chrome-devtools.cs', p], cwd=REPO, capture_output=True, text=True, timeout=180)
    try: os.remove(p)
    except Exception: pass
    return r.returncode

def list_pages_log():
    log = os.path.join(tempfile.gettempdir(), "lp-master.log")
    body = (
        'public const string Command = "list_pages";\n        '
        f'public const string OutputPath = "{log.replace(chr(92), chr(92)+chr(92))}";'
    )
    cdp('list', body)
    try:
        with open(log, 'r', encoding='utf-8') as f:
            return f.read()
    except Exception:
        return ''

def close_page_at(pid):
    body = f'public const string Command = "close_page";\n        public const string PageId = "{pid}";'
    return cdp('close', body)

def close_all():
    for _ in range(40):
        content = list_pages_log()
        lines = []
        for l in content.splitlines():
            s = l.strip()
            head = s.split(':', 1)[0].strip()
            if head.isdigit() and ('http' in s or 'chrome://' in s):
                lines.append(s)
        if not lines:
            return
        idx = lines[0].split(':', 1)[0].strip()
        close_page_at(idx)
        time.sleep(0.4)

def new_page_at(url):
    esc = url.replace('"', '\\"')
    body = (
        'public const string Command = "new_page";\n        '
        f'public const string Url = "{esc}";'
    )
    return cdp('new', body)

def force_light():
    body = (
        'public const string Command = "evaluate_script";\n        '
        'public const string PageId = "1";\n        '
        "public const string Function = \"() => { document.documentElement.setAttribute('data-theme', 'light'); return 'ok'; }\";"
    )
    return cdp('light', body)

def send_msg(msg):
    esc = msg.replace('\\', '\\\\').replace("'", "\\'")
    fn = f"() => {{ var i = document.querySelector('.ChatInputRow input[type=text]'); if (!i) return 'no-input'; var nv = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set; nv.call(i, '{esc}'); i.dispatchEvent(new Event('input', {{ bubbles: true }})); var s = document.querySelector('.ChatBtnRound.Send'); if (!s) return 'no-send'; s.click(); return 'sent'; }}"
    body = (
        'public const string Command = "evaluate_script";\n        '
        'public const string PageId = "1";\n        '
        f'public const string Function = "{fn}";'
    )
    return cdp('send', body)

def screenshot_to(pad):
    out_png = os.path.join(FRAMES, f"{pad}.png")
    body = (
        'public const string Command = "take_screenshot";\n        '
        'public const string PageId = "1";\n        '
        f'public const string FilePath = "{out_png.replace(chr(92), chr(92)+chr(92))}";'
    )
    return cdp('shot', body), out_png

def encode_scene(pad):
    png = os.path.join(FRAMES, f"{pad}.png")
    wav = os.path.join(AUDIO, f"{pad}.wav")
    mp4 = os.path.join(DOCS, f"scene-{pad}.mp4")
    if not os.path.exists(png) or not os.path.exists(wav):
        return -1, mp4
    args = ['ffmpeg', '-y', '-loop', '1', '-i', png, '-i', wav,
            '-c:v', 'libx264', '-tune', 'stillimage', '-pix_fmt', 'yuv420p',
            '-vf', 'scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30',
            '-c:a', 'aac', '-b:a', '128k', '-ar', '44100', '-shortest', mp4]
    r = subprocess.run(args, capture_output=True, text=True, timeout=120)
    return r.returncode, mp4

def pad_for(scene, idx):
    target = scene.get('target', '')
    if 'cb=' in target:
        cb = target.split('cb=')[-1].replace('?', '').replace('/', '').strip()
        return cb[:3]
    return f"{idx:03d}"

def find_chat_group(pad):
    for k, g in CHAT_GROUPS.items():
        if pad in g['pads']:
            return k, g
    return None, None

with open(SCENES, 'r', encoding='utf-8') as f:
    scenes = json.load(f)

print(f"closing all tabs (fresh start)...", flush=True)
close_all()
time.sleep(2)

current_group = None
encoded_ok = 0
encoded_fail = 0

for idx, scene in enumerate(scenes, 1):
    pad = pad_for(scene, idx)
    target = scene.get('target', '')
    is_chat = '/Chat/' in target
    group_key, group = (find_chat_group(pad) if is_chat else (None, None))

    if group_key is not None and group_key != current_group:
        # New chat thread phase
        close_all()
        time.sleep(1)
        new_page_at(target)
        time.sleep(5)
        force_light()
        current_group = group_key
    elif group_key is not None and group_key == current_group:
        # Continue same thread - just stay on tab
        force_light()
    else:
        # Non-chat or singleton chat - fresh nav
        close_all()
        time.sleep(0.5)
        new_page_at(target)
        time.sleep(4)
        force_light()
        current_group = None

    if is_chat and group is not None:
        msg = group['msgs'].get(pad)
        if msg:
            send_msg(msg)
            time.sleep(15)
            force_light()

    (rc, _, _) = (0, None, None)
    res = screenshot_to(pad)
    rc = res[0]
    enc_rc, mp4 = encode_scene(pad)
    if enc_rc == 0:
        encoded_ok += 1
    else:
        encoded_fail += 1
    print(f"  {pad} {'chat' if is_chat else 'nav'} grp={group_key} shot_rc={rc} enc_rc={enc_rc}", flush=True)

print(f"DONE encoded ok={encoded_ok} fail={encoded_fail}", flush=True)

# concat
import glob
mp4s = sorted(glob.glob(os.path.join(DOCS, 'scene-*.mp4')))
concat_txt = os.path.join(tempfile.gettempdir(), 'concat-all.txt')
with open(concat_txt, 'w', encoding='ascii') as f:
    for m in mp4s:
        f.write(f"file '{m.replace(chr(92), '/')}'\n")
out_walk = os.path.join(DOCS, 'walkthrough.mp4')
try: os.remove(out_walk)
except Exception: pass
r = subprocess.run(['ffmpeg','-y','-f','concat','-safe','0','-i',concat_txt,'-c','copy',out_walk], capture_output=True, text=True, timeout=600)
print(f"concat rc={r.returncode} -> {out_walk}", flush=True)
