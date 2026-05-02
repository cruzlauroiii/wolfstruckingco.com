import os, json, subprocess, time, tempfile, uuid

REPO = r"C:\repo\public\wolfstruckingco.com"
FRAMES = r"C:\Users\user1\AppData\Local\Temp\wolfs-video\frames"
AUDIO = r"C:\Users\user1\AppData\Local\Temp\wolfs-video\audio-edge"
DOCS = r"C:\repo\public\wolfstruckingco.com\main\docs\videos"

USER_MSGS = {
    '004': "I want to ship a BYD Han EV (2024 model, VIN LGXC76DH7L0123456) from China to the US. Shipper of record: Wei Zhang, phone 86-138-5555-0123, email wei.zhang@hefei-export.com.",
    '005': "Pickup at BYD factory, 8 Daoxiang Rd, Hefei, Anhui 230000 China. Drop at 1428 Oak Street, Wilmington NC 28401 USA. Vehicle brand new operable, fully insured. Pickup date 2026-06-15.",
    '006': "Factory invoice $18,000 paid by SWIFT wire on pickup. Bill all Wolfs freight to me on RTP. Customs HTSUS 8703.80.00, Section 301 tariff covered separately.",
    '007': "Buyer Sam Chen, phone 910-555-0123, email sam.chen@wilmington-buyer.com, address 1428 Oak Street Wilmington NC 28401. Delivery window 45 days from pickup. Sam pre-authorized COD.",
    '008': "Sale price $48,500 COD via RTP at delivery. Required driver badges: china-export, ocean-carrier, port-LA-drayage, cross-country-team, auto-handling-final. Multi-leg pay configured. All fields are final.",
    '009': "Every required field is confirmed (VIN, shipper, buyer, addresses, prices, payments, dates, badges). Publish this job to the Wolfs marketplace now and save it to the R2 listings collection. Do not ask for any more details just publish and reply with the new listing id.",
}

def cdp(name, body):
    cfg = "return 0;\nnamespace Scripts\n{\n    internal static class CdpRun\n    {\n        " + body + "\n    }\n}\n"
    p = os.path.join(tempfile.gettempdir(), f"cdp-{name}-{uuid.uuid4().hex}.cs")
    with open(p, 'w', encoding='utf-8') as f:
        f.write(cfg)
    r = subprocess.run(['dotnet', 'run', 'main/scripts/generic/chrome-devtools.cs', p], cwd=REPO, capture_output=True, text=True, timeout=180)
    try: os.remove(p)
    except Exception: pass
    return r.returncode, r.stdout, r.stderr

def list_pages():
    log = os.path.join(tempfile.gettempdir(), "lp.log")
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
    body = (
        'public const string Command = "close_page";\n        '
        f'public const string PageId = "{pid}";'
    )
    return cdp('close', body)

def close_all_pages():
    for _ in range(30):
        content = list_pages()
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
        time.sleep(0.5)

def new_chat_tab():
    body = (
        'public const string Command = "new_page";\n        '
        'public const string Url = "https://cruzlauroiii.github.io/wolfstruckingco.com/Chat/?cb=004";'
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

def screenshot(pad):
    out_png = os.path.join(FRAMES, f"{pad}.png")
    body = (
        'public const string Command = "take_screenshot";\n        '
        'public const string PageId = "1";\n        '
        f'public const string FilePath = "{out_png.replace(chr(92), chr(92)+chr(92))}";'
    )
    return cdp('shot', body), out_png

def encode(pad):
    png = os.path.join(FRAMES, f"{pad}.png")
    wav = os.path.join(AUDIO, f"{pad}.wav")
    mp4 = os.path.join(DOCS, f"scene-{pad}.mp4")
    args = ['ffmpeg', '-y', '-loop', '1', '-i', png, '-i', wav,
            '-c:v', 'libx264', '-tune', 'stillimage', '-pix_fmt', 'yuv420p',
            '-vf', 'scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,fps=30',
            '-c:a', 'aac', '-b:a', '128k', '-ar', '44100', '-shortest', mp4]
    r = subprocess.run(args, capture_output=True, text=True, timeout=120)
    return r.returncode, mp4

print("closing all browser tabs (fresh state)...", flush=True)
close_all_pages()
time.sleep(2)
print("opening fresh chat tab at /Chat/?cb=004 ...", flush=True)
new_chat_tab()
time.sleep(6)
force_light()

for pad in ['004', '005', '006', '007', '008', '009']:
    msg = USER_MSGS[pad]
    rc, out, err = send_msg(msg)
    print(f"  {pad} send rc={rc}", flush=True)
    time.sleep(18)
    force_light()
    (rc, _, _), png = screenshot(pad)
    print(f"  {pad} shot rc={rc} -> {png}", flush=True)
    rc, mp4 = encode(pad)
    print(f"  {pad} encoded rc={rc} -> {mp4}", flush=True)

print("done")
