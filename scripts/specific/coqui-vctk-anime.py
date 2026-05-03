import sys, os, io, json, logging, pathlib, random
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

scenes_path = sys.argv[1]
audio_dir = sys.argv[2]
index_path = sys.argv[3]

pathlib.Path(audio_dir).mkdir(parents=True, exist_ok=True)

with open(scenes_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

if isinstance(data, dict):
    items = [{'pad': k, 'narration': v} for k, v in data.items()]
else:
    items = data

from TTS.api import TTS

vctk = TTS(model_name='tts_models/en/vctk/vits', progress_bar=False)
speakers = list(vctk.speakers) if vctk.speakers else []

import wave, struct

def speed_pitch(in_wav, out_wav, rate=1.08):
    with wave.open(in_wav, 'rb') as wi:
        params = wi.getparams()
        frames = wi.readframes(wi.getnframes())
    new_rate = int(params.framerate * rate)
    with wave.open(out_wav, 'wb') as wo:
        wo.setnchannels(params.nchannels)
        wo.setsampwidth(params.sampwidth)
        wo.setframerate(new_rate)
        wo.writeframes(frames)

index = []
for i, item in enumerate(items):
    pad = item.get('pad') or f"{i+1:03d}"
    text = item.get('narration', '')
    plain = ''.join(c if (c == ' ' or (ord(c) >= 32 and ord(c) < 128)) else ' ' for c in text)
    plain = ' '.join(plain.split()).strip() or f"scene {pad}"
    speaker = speakers[i % len(speakers)] if speakers else None
    raw_out = os.path.join(audio_dir, f"{pad}_raw.wav")
    out = os.path.join(audio_dir, f"{pad}.wav")
    try:
        if speaker:
            vctk.tts_to_file(text=plain, speaker=speaker, file_path=raw_out)
        else:
            vctk.tts_to_file(text=plain, file_path=raw_out)
        speed_pitch(raw_out, out, rate=1.08)
        try: os.remove(raw_out)
        except Exception: pass
        index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': f'coqui-vctk-{speaker}', 'ok': True})
    except Exception as e:
        index.append({'pad': pad, 'audio': out, 'audioPath': out, 'engine': f'coqui-vctk-{speaker}', 'ok': False, 'error': str(e)})

with open(index_path, 'w', encoding='utf-8') as f:
    json.dump(index, f, indent=2)
