"""Prepare the bundled CC0 recordings; requires ffmpeg, no audio synthesis."""
from pathlib import Path
import json
import hashlib
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Audio/Recorded'
OUTPUT = ROOT / 'Assets/Resources/Audio/Recorded'
OUTPUT.mkdir(parents=True, exist_ok=True)

def convert(name, filters):
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y',
                    '-i', str(SOURCE / (name + '.mp3')), '-ac', '1',
                    '-af', filters, '-ar', '44100', '-c:a', 'pcm_s16le',
                    str(OUTPUT / (name + '.wav'))], check=True)

convert('Reload', 'loudnorm=I=-20:TP=-3:LRA=7,afade=t=in:d=0.005,afade=t=out:st=3.36:d=0.032')
# Fit the full drinking performance to the existing 2.5-second action without raising pitch.
convert('Drinking', 'atempo=1.5204,loudnorm=I=-23:TP=-3:LRA=7,afade=t=in:d=0.015,apad,atrim=duration=2.5,afade=t=out:st=2.47:d=0.03')
convert('RevolverShot', 'loudnorm=I=-18:TP=-3:LRA=7,afade=t=out:st=1.897:d=0.02')

manifest = [item for item in json.loads((SOURCE / 'sources.json').read_text())
            if item['asset'] in {'Reload', 'Drinking', 'RevolverShot'}]
for item in manifest:
    item['runtime_file'] = item['asset'] + '.wav'
    item['runtime_sha256'] = hashlib.sha256((OUTPUT / item['runtime_file']).read_bytes()).hexdigest()
(OUTPUT / 'Sources.json').write_text(json.dumps(manifest, indent=2) + '\n')
