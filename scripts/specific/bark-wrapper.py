import sys
import os
os.environ.setdefault('SUNO_OFFLOAD_CPU', 'False')
os.environ['HF_TOKEN'] = 'placeholder'
os.environ['HF_HUB_DISABLE_PROGRESS_BARS'] = '1'
os.environ['HF_HUB_DISABLE_TELEMETRY'] = '1'
os.environ['TRANSFORMERS_VERBOSITY'] = 'error'
_real_stderr = sys.stderr
class _Filter:
    def write(self, s):
        if 'arning' in s or 'rror' in s or 'WARNING' in s or 'ERROR' in s: return
        _real_stderr.write(s)
    def flush(self): _real_stderr.flush()
    def __getattr__(self, n): return getattr(_real_stderr, n)
sys.stderr = _Filter()
import torch
_orig_load = torch.load
def _patched_load(*args, **kwargs):
    kwargs['weights_only'] = False
    return _orig_load(*args, **kwargs)
torch.load = _patched_load
from bark import generate_audio, SAMPLE_RATE, preload_models
from scipy.io.wavfile import write as write_wav

if len(sys.argv) < 3:
    sys.stderr.write('usage: bark-wrapper.py <text> <output.wav>\n')
    sys.exit(1)

text = sys.argv[1]
out = sys.argv[2]
preload_models()
audio = generate_audio(text)
write_wav(out, SAMPLE_RATE, audio)
print(f'wrote {out}')
