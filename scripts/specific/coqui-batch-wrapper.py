import sys, os, io, json, logging, pathlib
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
os.environ['TRANSFORMERS_VERBOSITY'] = 'error'
logging.disable(logging.CRITICAL)

class _Filter:
    def __init__(self, real):
        self.real = real
    def write(self, s):
        if 'arning' in s or 'rror' in s or 'WARN' in s or 'ERR' in s:
            return
        try:
            self.real.write(s)
        except Exception:
            pass
    def flush(self):
        try:
            self.real.flush()
        except Exception:
            pass
    def __getattr__(self, n):
        return getattr(self.real, n)

sys.stderr = _Filter(sys.stderr)
sys.stdout = _Filter(sys.stdout)

if len(sys.argv) < 4:
    sys.exit(1)

narrations_path = sys.argv[1]
audio_dir = sys.argv[2]
index_path = sys.argv[3]

models = [
    'tts_models/en/jenny/jenny',
    'tts_models/en/ljspeech/tacotron2-DDC',
    'tts_models/en/ljspeech/glow-tts',
]
fallback = 'tts_models/en/jenny/jenny'

pathlib.Path(audio_dir).mkdir(parents=True, exist_ok=True)

with open(narrations_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

if isinstance(data, dict):
    items = [{'pad': k, 'narration': v} for k, v in data.items()]
else:
    items = data

from TTS.api import TTS

cache = {}
def get_tts(model_name):
    if model_name not in cache:
        cache[model_name] = TTS(model_name=model_name, progress_bar=False)
    return cache[model_name]

index = []
for i, item in enumerate(items):
    pad = item.get('pad') or f"{i+1:03d}"
    text = item.get('narration', '')
    plain = ''.join(c if (c == ' ' or (ord(c) >= 32 and ord(c) < 128)) else ' ' for c in text)
    plain = ' '.join(plain.split()).strip() or f"scene {pad}"
    model_name = models[i % len(models)]
    out = os.path.join(audio_dir, f"{pad}.wav")
    try:
        tts = get_tts(model_name)
        tts.tts_to_file(text=plain, file_path=out)
        index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': model_name, 'ok': True})
    except Exception:
        try:
            tts = get_tts(fallback)
            tts.tts_to_file(text=plain, file_path=out)
            index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': fallback, 'ok': True})
        except Exception:
            index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': model_name, 'ok': False})

with open(index_path, 'w', encoding='utf-8') as f:
    json.dump(index, f, indent=2)
