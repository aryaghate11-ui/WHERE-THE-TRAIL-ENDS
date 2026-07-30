import sys
import subprocess

# Ensure NLTK is installed
try:
    import nltk
except ImportError:
    subprocess.check_call([sys.executable, '-m', 'pip', 'install', 'nltk'])
    import nltk

# Ensure the necessary NLTK resource is available
try:
    nltk.data.find('tokenizers/punkt_tab')
except LookupError:
    nltk.download('punkt_tab')

from vgpt import tts

# Read configuration variables
with open("Assets/VoiceGPT/Models/config.txt", "r") as file:
    for line in file:
        variable, value = line.split("=")
        variable = variable.strip()
        value = value.strip()
        exec(variable + " = " + value)

# Initialize TTS
_tts = tts.VGPT(Model, Config, ASRModel, ASRConfig, F0Model, BERTModel, BERTConfig)

# Run inference with or without embedding scaling
if _enableEScale:
    _tts.inference(
        _text,
        _targetVoice,
        _outputPath,
        embedding_scale=_eScale,
        alpha=_alpha,
        beta=_beta,
        diffusion_steps=_steps
    )
else:
    _tts.inference(
        _text,
        _targetVoice,
        _outputPath,
        alpha=_alpha,
        beta=_beta,
        diffusion_steps=_steps
    )
