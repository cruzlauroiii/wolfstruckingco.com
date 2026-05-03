import os, json, subprocess, time, tempfile, uuid, sys, glob, winsound, ctypes

REPO = r"C:\repo\public\wolfstruckingco.com"
FRAMES = r"C:\Users\user1\AppData\Local\Temp\wolfs-video\frames"
AUDIO = r"C:\Users\user1\AppData\Local\Temp\wolfs-video\audio-edge"
DOCS = r"C:\repo\public\wolfstruckingco.com\main\docs\videos"
SCENES = r"C:\repo\public\wolfstruckingco.com\main\docs\videos\scenes-final-v2.json"
SSO_WAIT = 90
SSO_HARD_TIMEOUT = 600
ACK_FILE = r"C:\Users\user1\AppData\Local\Temp\sso-ack.txt"
CDP_TIMEOUT = 60
CDP_RETRIES = 3
CDP_RETRY_SLEEP = 4
INTER_CDP_SLEEP = 0.5

os.makedirs(FRAMES, exist_ok=True)

CHAT_GROUPS = {
    'A': {'pads':['004','005','006','007','008','009','010'],'msgs':{
        '004':"I want to ship a BYD Han EV (2024 model, VIN LGXC76DH7L0123456) from China to the US. Shipper: Wei Zhang, 86-138-5555-0123, wei.zhang@hefei-export.com.",
        '005':"Pickup BYD factory, 8 Daoxiang Rd Hefei Anhui 230000 China. Drop 1428 Oak Street Wilmington NC 28401 USA. New, insured. Pickup 2026-06-15.",
        '006':"Factory invoice $18,000 SWIFT wire on pickup. Bill freight on RTP. Customs HTSUS 8703.80.00, Section 301 covered separately.",
        '007':"Buyer Sam Chen, 910-555-0123, sam.chen@wilmington-buyer.com, 1428 Oak Street Wilmington NC 28401. 45-day window. Sam pre-authorized COD.",
        '008':"Sale $48,500 COD via RTP at delivery. Badges: china-export, ocean-carrier, port-LA-drayage, cross-country-team, auto-handling-final. Multi-leg pay.",
        '009':"All fields confirmed. Publish this job to the marketplace and save to R2 listings collection. Reply with the listing id.",
        '010':"Saved. Listing id byd-han-ev-hefei-wilmington-2026 in R2 listings. Marketplace URL: https://cruzlauroiii.github.io/wolfstruckingco.com/Marketplace/",
    }},
    'B': {'pads':['014','015','017'],'msgs':{
        '014':"I am Wei Liu, driver from Hefei China. Ten years cross-border freight. Save to R2 applicants id wei_liu_china.",
        '015':"Documents: CDL LGZ-CN-2014-7782 expiry 2030-06-15, China export pass CE-2024-Hefei-991. Save to R2 documents.",
        '017':"Use db_get_blob to read my CDL from R2 documents. Confirm LGZ-CN-2014-7782 valid through 2030-06-15 with cross-border endorsements.",
    }},
    'C': {'pads':['021','022'],'msgs':{
        '021':"I am Marco Rivera, driver from San Pedro CA. Eight years Port of LA drayage. Save to R2 applicants id marco_rivera_la.",
        '022':"TWIC TWC-2022-LA-4471, California CDL-A. Save to R2 documents. Available for LA leg of byd-han-ev-hefei-wilmington-2026.",
    }},
    'D': {'pads':['027','028'],'msgs':{
        '027':"We are Diego Morales and Maria Santos, team drivers Phoenix AZ. Save to R2 applicants id diego_maria_phoenix.",
        '028':"Joint Visa Direct payouts. CDL-A both, team-driver HOS current. Available for cross-country team leg of byd-han-ev-hefei-wilmington-2026.",
    }},
    'E': {'pads':['033','034'],'msgs':{
        '033':"I am Sam Chen Jr, driver from Wilmington NC. Four years auto-handling. Save to R2 applicants id sam_chen_jr_wilmington.",
        '034':"Inspection cert current, CDL-A ready. Chase RTP. Available for final-mile leg of byd-han-ev-hefei-wilmington-2026.",
    }},
    'F': {'pads':['063','064','065'],'msgs':{
        '063':"Driver Wei Liu arrived BYD Hefei factory 2026-06-15 09:00. Update R2 itineraries id itin_d1 status arrived.",
        '064':"Confirm $18,000 factory cash cleared from COD escrow. Save to R2 charges id chg_factory_cash.",
        '065':"GPS device gps-byd-2026-001 installed. Update R2 itineraries itin_d1 gpsDevice gps-byd-2026-001.",
    }},
    'G': {'pads':['069'],'msgs':{
        '069':"Container loaded sealed CC-9912-WLM. Update R2 itineraries itin_d1 status loaded.",
    }},
    'H': {'pads':['074','079','081','083','086','087'],'msgs':{
        '074':"Ship arrived Los Angeles. Driver Marco Rivera assigned LA-Phoenix leg. Update R2 itineraries id itin_d2 status assigned.",
        '079':"Driver Marco Rivera picked up at LA terminal 401. Update R2 itineraries itin_d2 status pickup.",
        '081':"I-10 East mile 78 flip 47-min stoppage. Recompute. Update R2 schedules sch_byd recomputeReason 'I-10 flip'.",
        '083':"Buyer Sam Chen ETA +47 min. Notify R2 purchases pur_sam_byd with eta_change.",
        '086':"Driver Marco Rivera completed LA-Phoenix leg. Update R2 itineraries itin_d2 status delivered.",
        '087':"Team Diego/Maria starting Phoenix-Memphis leg. Update R2 itineraries id itin_d3 status start.",
    }},
    'I': {'pads':['093','094'],'msgs':{
        '093':"Team completed Phoenix-Memphis leg. Update R2 itineraries itin_d3 status delivered Memphis.",
        '094':"Driver Sam Chen Jr starting Memphis-Wilmington final leg. Update R2 itineraries id itin_d4 status start.",
    }},
    'J': {'pads':['100','101','102','103','104','105'],'msgs':{
        '100':"Arrived 1428 Oak Street Wilmington. Calling buyer. Update R2 itineraries itin_d4 status arrived.",
        '101':"Buyer at door. Update R2 itineraries itin_d4 status meeting.",
        '102':"Buyer inspected vehicle, accepted. Update R2 itineraries itin_d4 status inspected.",
        '103':"Buyer paid $48,500 COD via RTP. Update R2 purchases pur_sam_byd status paid.",
        '104':"Delivery photo captured. Update R2 itineraries itin_d4 status photo-captured.",
        '105':"Keys handed over. Update R2 itineraries itin_d4 status delivered. Update R2 purchases pur_sam_byd status complete.",
    }},
}

def alarm_burst():
    try:
        winsound.Beep(2200, 250)
        winsound.Beep(1500, 250)
        winsound.Beep(2800, 250)
        winsound.Beep(1500, 250)
        winsound.Beep(2200, 350)
    except Exception:
        pass

def clear_storage_and_reload():
    fn = "() => { try { localStorage.clear(); sessionStorage.clear(); document.cookie.split(';').forEach(c => { var n = c.split('=')[0].trim(); document.cookie = n + '=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/'; }); } catch(e){} location.reload(); return 'cleared'; }"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";')
    return cdp('clear-storage', body)

def click_logoff_link_and_wait():
    fn = "() => { var as_ = Array.from(document.querySelectorAll('a,button,[role=button]')); var b = as_.find(x => /(log\\s*off|sign\\s*out|log\\s*out)/i.test(x.textContent||x.getAttribute('aria-label')||'')); if (!b) return 'no-logoff-link'; if (b.tagName==='A' && b.href) { location.href = b.href; return 'navigating:' + b.href; } b.click(); return 'clicked'; }"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";')
    return cdp('logoff-link', body)

def goto_home():
    fn = "() => { location.href = 'https://cruzlauroiii.github.io/wolfstruckingco.com/?cb=001'; return 'navigating'; }"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";')
    return cdp('go-home', body)

def has_logoff_text():
    log = os.path.join(tempfile.gettempdir(), f"lo-{uuid.uuid4().hex}.log")
    fn = "() => /(log\\s*off|sign\\s*out|log\\s*out)/i.test(document.body ? document.body.innerText : '') ? 'yes' : 'no'"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";\n        '
            f'public const string OutputPath = "{log.replace(chr(92), chr(92)+chr(92))}";')
    cdp('logoff-check', body)
    try:
        with open(log,'r',encoding='utf-8') as f: c = f.read()
    except Exception: c = ''
    try: os.remove(log)
    except Exception: pass
    return 'yes' in c

def click_logout_first():
    fn = "() => { var btns = Array.from(document.querySelectorAll('button,a,[role=button]')); var b = btns.find(x => /(log\\s*out|sign\\s*out|log\\s*off)/i.test(x.textContent||x.getAttribute('aria-label')||'')); if (!b) return 'no-logout'; b.click(); return 'logged-out'; }"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";')
    return cdp('logout', body)

def click_sso_button(provider):
    fn = "() => { var btns = Array.from(document.querySelectorAll('button,a,[role=button]')); var b = btns.find(x => /__PROVIDER__/i.test(x.textContent||x.getAttribute('aria-label')||'')); if (!b) return 'no-btn'; b.click(); return 'clicked'; }".replace('__PROVIDER__', provider)
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";')
    return cdp('sso-click', body)

def fill_microsoft_email_if_needed(account):
    if not account: return False
    fn = "() => { if (!/login\\.microsoftonline\\.com|login\\.live\\.com/.test(location.host)) return 'not-ms'; var i = document.querySelector('input[type=email],input[name=loginfmt]'); if (!i) return 'no-input'; var nv = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set; nv.call(i, '__ACC__'); i.dispatchEvent(new Event('input',{bubbles:true})); i.dispatchEvent(new Event('change',{bubbles:true})); var btn = document.querySelector('#idSIButton9,input[type=submit],button[type=submit]'); if (!btn) return 'no-next'; btn.click(); return 'submitted'; }".replace('__ACC__', account)
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";')
    return cdp('msfill', body)

def current_url():
    log = os.path.join(tempfile.gettempdir(), f"url-{uuid.uuid4().hex}.log")
    fn = "() => location.href"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";\n        '
            f'public const string OutputPath = "{log.replace(chr(92), chr(92)+chr(92))}";')
    cdp('url', body)
    try:
        with open(log,'r',encoding='utf-8') as f: c = f.read()
    except Exception: c = ''
    try: os.remove(log)
    except Exception: pass
    return c.strip().strip('"').strip("'")

def has_password_input():
    log = os.path.join(tempfile.gettempdir(), f"hp-{uuid.uuid4().hex}.log")
    fn = "() => { return document.querySelector('input[type=password]') ? 'yes' : 'no'; }"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";\n        '
            f'public const string OutputPath = "{log.replace(chr(92), chr(92)+chr(92))}";')
    cdp('hp', body)
    try:
        with open(log,'r',encoding='utf-8') as f: c = f.read()
    except Exception: c = ''
    try: os.remove(log)
    except Exception: pass
    return 'yes' in c

def wait_for_sso(provider, account, pad):
    if provider:
        click_logout_first()
        time.sleep(3)
        click_sso_button(provider)
        time.sleep(10)
        if provider == 'microsoft':
            fill_microsoft_email_if_needed(account)
            time.sleep(8)
        url = current_url()
        if 'wolfstruckingco' in url:
            print(f"  *** SSO auto-completed scene {pad}: {provider} -> {url[:80]}", flush=True)
            return
        if not has_password_input():
            time.sleep(6)
            url = current_url()
            if 'wolfstruckingco' in url:
                print(f"  *** SSO auto-completed scene {pad}: {provider} -> {url[:80]}", flush=True)
                return
    try: os.remove(ACK_FILE)
    except Exception: pass
    msg = f"SSO PASSWORD REQUIRED scene {pad}: {provider} {account or ''}"
    print(f"  *** {msg} -- create {ACK_FILE} or wait {SSO_HARD_TIMEOUT}s", flush=True)
    try:
        ctypes.windll.user32.MessageBeep(0xFFFFFFFF)
    except Exception:
        pass
    deadline = time.time() + SSO_HARD_TIMEOUT
    while time.time() < deadline:
        if os.path.exists(ACK_FILE):
            try: os.remove(ACK_FILE)
            except Exception: pass
            print(f"  *** SSO ack received scene {pad}", flush=True)
            return
        alarm_burst()
        for _ in range(20):
            if os.path.exists(ACK_FILE):
                try: os.remove(ACK_FILE)
                except Exception: pass
                print(f"  *** SSO ack received scene {pad}", flush=True)
                return
            time.sleep(0.1)
    url = current_url()
    print(f"  *** SSO hard timeout scene {pad}, url={url[:80]} -- ABORT", flush=True)
    raise SystemExit(2)

def cdp(name, body):
    cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        " + body + "\n    }\n}\n"
    last_rc = -1
    for attempt in range(CDP_RETRIES):
        p = os.path.join(tempfile.gettempdir(), f"cdp-{name}-{uuid.uuid4().hex}.cs")
        with open(p,'w',encoding='utf-8') as f: f.write(cfg)
        try:
            r = subprocess.run(['dotnet','run','main/scripts/generic/chrome-devtools.cs',p], cwd=REPO, capture_output=True, text=True, timeout=CDP_TIMEOUT)
            try: os.remove(p)
            except Exception: pass
            time.sleep(INTER_CDP_SLEEP)
            return r.returncode
        except subprocess.TimeoutExpired:
            try: os.remove(p)
            except Exception: pass
            print(f"  cdp[{name}] timeout attempt {attempt+1}/{CDP_RETRIES}", flush=True)
            time.sleep(CDP_RETRY_SLEEP)
            last_rc = -2
    return last_rc

def list_pages_log():
    log = os.path.join(tempfile.gettempdir(), f"lp-{uuid.uuid4().hex}.log")
    body = ('public const string Command = "list_pages";\n        '
            f'public const string OutputPath = "{log.replace(chr(92), chr(92)+chr(92))}";')
    cdp('list', body)
    try:
        with open(log,'r',encoding='utf-8') as f: c = f.read()
    except Exception: c = ''
    try: os.remove(log)
    except Exception: pass
    return c

def close_page_at(pid):
    body = f'public const string Command = "close_page";\n        public const string PageId = "{pid}";'
    return cdp('close', body)

def close_all():
    for _ in range(40):
        c = list_pages_log()
        lines = []
        for l in c.splitlines():
            s = l.strip()
            head = s.split(':',1)[0].strip()
            if head.isdigit() and ('http' in s or 'chrome://' in s):
                lines.append(s)
        if not lines: return
        idx = lines[0].split(':',1)[0].strip()
        close_page_at(idx)
        time.sleep(0.4)

def new_page_at(url):
    esc = url.replace('"','\\"')
    body = ('public const string Command = "new_page";\n        '
            f'public const string Url = "{esc}";')
    return cdp('new', body)

def force_light():
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            "public const string Function = \"() => { document.documentElement.setAttribute('data-theme','light'); return 'ok'; }\";")
    return cdp('light', body)

def send_msg(msg):
    esc = msg.replace('\\','\\\\').replace("'","\\'")
    fn = f"() => {{ var i = document.querySelector('.ChatInputRow input[type=text]'); if (!i) return 'no-input'; var nv = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set; nv.call(i,'{esc}'); i.dispatchEvent(new Event('input',{{bubbles:true}})); var s = document.querySelector('.ChatBtnRound.Send'); if (!s) return 'no-send'; s.click(); return 'sent'; }}"
    body = ('public const string Command = "evaluate_script";\n        '
            'public const string PageId = "1";\n        '
            f'public const string Function = "{fn}";')
    return cdp('send', body)

def screenshot_to(pad):
    out_png = os.path.join(FRAMES, f"{pad}.png")
    body = ('public const string Command = "take_screenshot";\n        '
            'public const string PageId = "1";\n        '
            f'public const string FilePath = "{out_png.replace(chr(92), chr(92)+chr(92))}";')
    return cdp('shot', body), out_png

def encode_scene(pad):
    png = os.path.join(FRAMES, f"{pad}.png")
    wav = os.path.join(AUDIO, f"{pad}.wav")
    mp4 = os.path.join(DOCS, f"scene-{pad}.mp4")
    if not os.path.exists(png) or not os.path.exists(wav):
        return -1, mp4
    args = ['ffmpeg','-y','-loop','1','-i',png,'-i',wav,
            '-c:v','libx264','-tune','stillimage','-pix_fmt','yuv420p',
            '-vf','scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30',
            '-c:a','aac','-b:a','128k','-ar','44100','-shortest',mp4]
    try:
        r = subprocess.run(args, capture_output=True, text=True, timeout=120)
        return r.returncode, mp4
    except subprocess.TimeoutExpired:
        return -1, mp4

def pad_for(scene, idx):
    target = scene.get('target','')
    if 'cb=' in target:
        cb = target.split('cb=')[-1].replace('?','').replace('/','').strip()
        return cb[:3]
    return f"{idx:03d}"

def find_chat_group(pad):
    for k,g in CHAT_GROUPS.items():
        if pad in g['pads']: return k,g
    return None,None

def run_scene(idx, scene, current_group_in):
    pad = pad_for(scene, idx)
    target = scene.get('target','')
    is_chat = '/Chat/' in target
    is_sso = bool(scene.get('sso'))
    group_key, group = (find_chat_group(pad) if is_chat else (None,None))
    current_group = current_group_in
    try:
        if group_key is not None and group_key != current_group:
            close_all(); time.sleep(1)
            new_page_at(target); time.sleep(5)
            force_light()
            current_group = group_key
        elif group_key is not None and group_key == current_group:
            force_light()
        else:
            close_all(); time.sleep(0.5)
            new_page_at(target); time.sleep(4)
            if is_sso:
                wait_for_sso(scene.get('sso'), scene.get('account',''), pad)
            force_light()
            current_group = None
        if is_chat and group is not None:
            msg = group['msgs'].get(pad)
            if msg:
                send_msg(msg)
                time.sleep(15)
                force_light()
        if pad == '001':
            if has_logoff_text():
                click_logoff_link_and_wait()
                time.sleep(6)
                clear_storage_and_reload()
                time.sleep(5)
                goto_home()
                time.sleep(6)
                force_light()
            if has_logoff_text():
                url = current_url()
                print(f"  *** scene 001 STILL shows Log off after logoff+clear+home -- ABORT url={url[:80]}", flush=True)
                raise SystemExit(3)
            print(f"  *** scene 001 verified logged-out", flush=True)
        screenshot_to(pad)
        enc_rc, _ = encode_scene(pad)
        ok = (enc_rc == 0)
        print(f"  {pad} {'chat' if is_chat else 'nav'} grp={group_key} sso={is_sso} enc={enc_rc}", flush=True)
        return ok, current_group, pad
    except Exception as e:
        print(f"  {pad} EXC {e}", flush=True)
        return False, current_group, pad

with open(SCENES,'r',encoding='utf-8') as f:
    scenes = json.load(f)

print(f"closing all tabs (fresh start). total scenes={len(scenes)}", flush=True)
close_all()
time.sleep(2)

current_group = None
ok_count = 0
fail_pads = []

for idx, scene in enumerate(scenes, 1):
    ok, current_group, pad = run_scene(idx, scene, current_group)
    if ok: ok_count += 1
    else: fail_pads.append((idx, scene, pad))

print(f"PASS1 ok={ok_count} fail={len(fail_pads)}", flush=True)

if fail_pads:
    print(f"PASS2 retrying {len(fail_pads)} failed scenes...", flush=True)
    close_all(); time.sleep(2)
    current_group = None
    pass2_ok = 0
    still_fail = []
    for idx, scene, pad in fail_pads:
        ok, current_group, _ = run_scene(idx, scene, None)
        if ok: pass2_ok += 1
        else: still_fail.append(pad)
    print(f"PASS2 ok={pass2_ok} stillFail={len(still_fail)} pads={still_fail}", flush=True)

mp4s = sorted(glob.glob(os.path.join(DOCS,'scene-*.mp4')))
print(f"concat candidates: {len(mp4s)}", flush=True)
concat_txt = os.path.join(tempfile.gettempdir(),'concat-v2.txt')
with open(concat_txt,'w',encoding='ascii') as f:
    for m in mp4s: f.write(f"file '{m.replace(chr(92),'/')}'\n")
out_walk = os.path.join(DOCS,'walkthrough.mp4')
try: os.remove(out_walk)
except Exception: pass
try:
    r = subprocess.run(['ffmpeg','-y','-f','concat','-safe','0','-i',concat_txt,'-c','copy',out_walk], capture_output=True, text=True, timeout=600)
    print(f"concat rc={r.returncode} -> {out_walk}", flush=True)
except subprocess.TimeoutExpired:
    print("concat TIMEOUT", flush=True)
print("DONE", flush=True)
