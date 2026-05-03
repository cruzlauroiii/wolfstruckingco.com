import sys, os, io, logging
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

text = sys.argv[1]
out = sys.argv[2]
model_name = sys.argv[3]

from TTS.api import TTS
tts = TTS(model_name=model_name, progress_bar=False)
tts.tts_to_file(text=text, file_path=out)
