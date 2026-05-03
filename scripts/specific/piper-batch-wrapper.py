import sys, os, io, json, logging, pathlib, urllib.request, wave
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
logging.disable(logging.CRITICAL)

class _Filter:
    def __init__(self, real):
        self.real = real
    def write(self, s):
        if 'arning' in s or 'rror' in s or 'WARN' in s or 'ERR' in s:
            return
        try: self.real.write(s)
        except Exception: pass
    def flush(self):
        try: self.real.flush()
        except Exception: pass
    def __getattr__(self, n): return getattr(self.real, n)

sys.stderr = _Filter(sys.stderr)
sys.stdout = _Filter(sys.stdout)

if len(sys.argv) < 4:
    sys.exit(1)

scenes_path = sys.argv[1]
audio_dir = sys.argv[2]
index_path = sys.argv[3]

VOICES = [
    'en_US-amy-medium',
    'en_US-arctic-medium',
    'en_US-bryce-medium',
    'en_US-danny-low',
    'en_US-hfc_female-medium',
    'en_US-hfc_male-medium',
    'en_US-joe-medium',
    'en_US-john-medium',
    'en_US-kathleen-low',
    'en_US-kristin-medium',
    'en_US-kusal-medium',
    'en_US-l2arctic-medium',
    'en_US-lessac-medium',
    'en_US-libritts-high',
    'en_US-libritts_r-medium',
    'en_US-ljspeech-medium',
    'en_US-norman-medium',
    'en_US-ryan-medium',
    'en_GB-alan-medium',
    'en_GB-alba-medium',
    'en_GB-aru-medium',
    'en_GB-cori-medium',
    'en_GB-jenny_dioco-medium',
    'en_GB-northern_english_male-medium',
    'en_GB-semaine-medium',
    'en_GB-southern_english_female-low',
    'en_GB-vctk-medium',
]

cache = pathlib.Path(os.environ.get('LOCALAPPDATA', os.path.expanduser('~'))) / 'piper-voices'
cache.mkdir(parents=True, exist_ok=True)

def download_voice(name):
    parts = name.split('-')
    lang = parts[0]
    voice = parts[1]
    qual = parts[2]
    onnx_path = cache / f'{name}.onnx'
    cfg_path = cache / f'{name}.onnx.json'
    base = f'https://huggingface.co/rhasspy/piper-voices/resolve/main/{lang[:2]}/{lang}/{voice}/{qual}/{name}'
    if not onnx_path.exists():
        try:
            urllib.request.urlretrieve(base + '.onnx', onnx_path)
        except Exception:
            return None
    if not cfg_path.exists():
        try:
            urllib.request.urlretrieve(base + '.onnx.json', cfg_path)
        except Exception:
            return None
    return onnx_path

pathlib.Path(audio_dir).mkdir(parents=True, exist_ok=True)

with open(scenes_path, 'r', encoding='utf-8') as f:
    data = json.load(f)
items = data if isinstance(data, list) else [{'pad': k, 'narration': v} for k, v in data.items()]

from piper import PiperVoice

voice_cache = {}
def get_voice(name):
    if name in voice_cache:
        return voice_cache[name]
    p = download_voice(name)
    if p is None:
        return None
    v = PiperVoice.load(str(p))
    voice_cache[name] = v
    return v

index = []
for i, item in enumerate(items):
    pad = item.get('pad') or f"{i+1:03d}"
    text = item.get('narration', '')
    plain = ''.join(c if (c == ' ' or (ord(c) >= 32 and ord(c) < 128)) else ' ' for c in text)
    plain = ' '.join(plain.split()).strip() or f"scene {pad}"
    voice_name = VOICES[i % len(VOICES)]
    out = os.path.join(audio_dir, f"{pad}.wav")
    try:
        v = get_voice(voice_name)
        if v is None:
            index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': f'piper-{voice_name}', 'ok': False})
            continue
        with wave.open(out, 'wb') as wo:
            v.synthesize_wav(plain, wo)
        index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': f'piper-{voice_name}', 'ok': True})
    except Exception as e:
        index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': f'piper-{voice_name}', 'ok': False, 'error': str(e)})

with open(index_path, 'w', encoding='utf-8') as f:
    json.dump(index, f, indent=2)
