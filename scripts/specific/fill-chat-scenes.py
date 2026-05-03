import sys, os, json, subprocess, time, tempfile, pathlib, uuid

if len(sys.argv) < 4:
    sys.exit(1)
scenes_path = sys.argv[1]
frames_dir = sys.argv[2]
repo = sys.argv[3]
pathlib.Path(frames_dir).mkdir(parents=True, exist_ok=True)

with open(scenes_path, 'r', encoding='utf-8') as f:
    scenes = json.load(f)

USER_MESSAGES = {
    '004': "I want to ship a car. It's a BYD Han EV from a factory in Hefei, China to a buyer in Wilmington, North Carolina.",
    '005': "Pickup is at the BYD factory in Hefei. Drop is the buyer's home in Wilmington, NC.",
    '006': "$18,000 cash for the factory, paid by SWIFT wire on pickup.",
    '007': "Buyer is Sam in Wilmington, North Carolina.",
    '008': "Sale price is $48,500, COD on delivery via RTP.",
    '009': "Looks good, please publish the job.",
    '013': "I'm Wei, ten years driving cross-border freight in China.",
    '014': "I'll upload my Chinese commercial CDL and China export pass now.",
    '015': "DOT compliance is fine, hours of service tracked daily.",
    '020': "I'm Marco, eight years on Port of LA drayage.",
    '021': "TWIC card and California CDL Class A, both current.",
    '022': "I have container handling and hazmat endorsements.",
    '027': "I'm Diego and Maria, team drivers based in Phoenix.",
    '028': "We share Visa Direct payouts on a joint account.",
    '029': "Both have current CDL-A and team-driver HOS certs.",
    '034': "I'm Sam, four years on auto-handling in Wilmington.",
    '035': "I have a vehicle inspection cert and I work nights.",
    '036': "Chase RTP for instant payout, Class A CDL ready.",
    '065': "Show me cars under $60,000 with COD.",
    '066': "The BYD Han EV looks good, what's the delivery window?",
    '067': "I'll buy it. Pre-authorize my RTP for $48,500.",
    '071': "Schedule the China leg first, then the ocean carrier.",
    '076': "Confirm the LA pickup and Wilmington final mile.",
    '081': "What's the ETA at the Hefei factory?",
    '083': "Container loaded, ready for ocean transit.",
    '085': "Confirm ISF and ocean freight bookings.",
    '087': "Customs hold released, when does it arrive at LA?",
    '088': "Section 301 tariff covered from COD escrow.",
    '094': "Driver 2 picking up at LA terminal 401 now.",
    '095': "On the road to Phoenix, ETA seven hours.",
    '101': "Recompute schedule, I-10 East flip causing delay.",
    '102': "Driver 3 picking up the leg in Phoenix.",
    '103': "Cross-country corridor I-10 to I-40 to Memphis.",
    '104': "Team driver split on sleeper berth.",
    '105': "Memphis 24/7 yard handoff to Driver 4.",
    '106': "Final mile to Wilmington, ETA 21:30.",
}

def run_cdp(command, **kw):
    body_lines = [f'public const string Command = "{command}";']
    for k, v in kw.items():
        if isinstance(v, int):
            body_lines.append(f'public const int {k} = {v};')
        else:
            esc = str(v).replace('\\', '\\\\').replace('"', '\\"')
            body_lines.append(f'public const string {k} = "{esc}";')
    body = '\n        '.join(body_lines)
    cfg = f"""return 0;
namespace Scripts
{{
    internal static class CdpRun
    {{
        {body}
    }}
}}
"""
    cfg_path = os.path.join(tempfile.gettempdir(), f"cdp-fill-{uuid.uuid4().hex}.cs")
    with open(cfg_path, 'w', encoding='utf-8') as f:
        f.write(cfg)
    try:
        p = subprocess.run(
            ['dotnet', 'run', 'main/scripts/generic/chrome-devtools.cs', cfg_path],
            cwd=repo,
            capture_output=True,
            text=True,
            timeout=120,
        )
        return p.returncode
    finally:
        try: os.remove(cfg_path)
        except Exception: pass

count = 0
for scene in scenes:
    target = scene.get('target', '')
    if '/Chat/' not in target:
        continue
    cb = target.split('cb=')[-1].rstrip('/').rstrip('?')
    cb = cb[:3]
    pad = cb
    user_msg = USER_MESSAGES.get(cb)
    out_png = os.path.join(frames_dir, f"{pad}.png")

    run_cdp('navigate_page', Url=target)
    time.sleep(3)

    if user_msg:
        js = f"""
(() => {{
  const sel = document.querySelector('textarea, input[type=text], [contenteditable=true]');
  if (!sel) return 'no-input';
  const setter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, 'value') || Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value');
  if (setter && setter.set) setter.set.call(sel, {json.dumps(user_msg)});
  else sel.value = {json.dumps(user_msg)};
  sel.dispatchEvent(new Event('input', {{bubbles: true}}));
  const send = [...document.querySelectorAll('button')].find(b => /send|submit/i.test(b.textContent || b.getAttribute('aria-label') || ''));
  if (send) send.click();
  return 'sent';
}})()
"""
        run_cdp('evaluate_script', Script=js)
        time.sleep(10)

    run_cdp('take_screenshot', FilePath=out_png)
    count += 1
    print(f"scene {pad}: ok", flush=True)

print(f"done {count} chat scenes")
