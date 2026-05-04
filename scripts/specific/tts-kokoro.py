import json
import os
import sys
import re

try:
    from kokoro_onnx import Kokoro
    import soundfile as sf
except ImportError:
    print("MISSING: pip install kokoro-onnx soundfile numpy", file=sys.stderr)
    sys.exit(2)

if len(sys.argv) < 4:
    print("usage: tts-kokoro.py <scenes-json> <model-onnx> <voices-bin> <out-dir> [voice]", file=sys.stderr)
    sys.exit(1)

scenes_path, model_path, voices_path, out_dir = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
voice = sys.argv[5] if len(sys.argv) > 5 else "af_bella"

os.makedirs(out_dir, exist_ok=True)
kokoro = Kokoro(model_path, voices_path)

with open(scenes_path, "r", encoding="utf-8") as f:
    scenes = json.load(f)

ok = 0
fail = []
for i, sc in enumerate(scenes, 1):
    target = sc.get("target", "")
    m = re.search(r"cb=(\d+)", target)
    pad = m.group(1).zfill(3) if m else f"{i:03d}"
    text = sc.get("narration", "").strip()
    if not text:
        fail.append((pad, "empty-narration"))
        continue
    out = os.path.join(out_dir, f"scene-{pad}.wav")
    try:
        samples, sr = kokoro.create(text, voice=voice, speed=1.0, lang="en-us")
        sf.write(out, samples, sr)
        ok += 1
        print(f"  {pad} OK ({len(text)}c)")
    except Exception as e:
        fail.append((pad, str(e)))
        print(f"  {pad} FAIL {e}", file=sys.stderr)

print(f"DONE ok={ok} fail={len(fail)}")
if fail:
    for p, msg in fail:
        print(f"  fail {p}: {msg}", file=sys.stderr)
    sys.exit(3)
