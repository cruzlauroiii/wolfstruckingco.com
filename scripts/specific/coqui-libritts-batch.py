import sys, os, io, json, logging, pathlib
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
os.environ['TRANSFORMERS_VERBOSITY'] = 'error'
os.environ['PHONEMIZER_ESPEAK_LIBRARY'] = r'C:\Program Files\eSpeak NG\libespeak-ng.dll'
os.environ['PHONEMIZER_ESPEAK_PATH'] = r'C:\Program Files\eSpeak NG\espeak-ng.exe'
os.environ['ESPEAK_DATA_PATH'] = r'C:\Program Files\eSpeak NG\espeak-ng-data'
os.environ['PATH'] = r'C:\Program Files\eSpeak NG;' + os.environ.get('PATH', '')
logging.disable(logging.CRITICAL)

class _F:
    def __init__(s, r): s.r = r
    def write(s, x):
        if 'arning' in x or 'rror' in x or 'WARN' in x or 'ERR' in x: return
        try: s.r.write(x)
        except Exception: pass
    def flush(s):
        try: s.r.flush()
        except Exception: pass
    def __getattr__(s, n): return getattr(s.r, n)
sys.stderr = _F(sys.stderr); sys.stdout = _F(sys.stdout)

if len(sys.argv) < 4: sys.exit(1)
scenes_path, audio_dir, index_path = sys.argv[1], sys.argv[2], sys.argv[3]
pathlib.Path(audio_dir).mkdir(parents=True, exist_ok=True)

with open(scenes_path, 'r', encoding='utf-8') as f:
    data = json.load(f)
items = data if isinstance(data, list) else [{'pad': k, 'narration': v} for k, v in data.items()]

from TTS.api import TTS
tts = TTS(model_name='tts_models/en/multi-dataset/tortoise-v2', progress_bar=False)
speakers = list(getattr(tts, 'speakers', None) or [])
if not speakers:
    tts = TTS(model_name='tts_models/en/vctk/vits', progress_bar=False)
    speakers = list(tts.speakers or [])
print(f"loaded {len(speakers)} speakers", file=sys.__stdout__, flush=True)

index = []
for i, item in enumerate(items):
    pad = item.get('pad') or f"{i+1:03d}"
    text = item.get('narration', '')
    plain = ''.join(c if (c == ' ' or (ord(c) >= 32 and ord(c) < 128)) else ' ' for c in text)
    plain = ' '.join(plain.split()).strip() or f"scene {pad}"
    speaker = speakers[i % len(speakers)] if speakers else None
    out = os.path.join(audio_dir, f"{pad}.wav")
    try:
        if speaker:
            tts.tts_to_file(text=plain, speaker=speaker, file_path=out)
        else:
            tts.tts_to_file(text=plain, file_path=out)
        index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': f'coqui-{speaker}', 'ok': True})
    except Exception as e:
        index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': f'coqui-{speaker}', 'ok': False, 'error': str(e)})

with open(index_path, 'w', encoding='utf-8') as f:
    json.dump(index, f, indent=2)
