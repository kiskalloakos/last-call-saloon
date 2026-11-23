"""Prepare the three bundled CC0 toilet recordings with ffmpeg (no synthesis)."""
from pathlib import Path
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Audio/Toilet'
OUTPUT = ROOT / 'Assets/Resources/Audio/Toilet'
OUTPUT.mkdir(parents=True, exist_ok=True)
FILTERS = {
    'Plunger': 'atempo=1.75,loudnorm=I=-20:TP=-3:LRA=7,afade=t=in:d=0.005,afade=t=out:st=0.34:d=0.04',
    'Gurgle': 'atrim=start=0.5:end=1.15,asetpts=PTS-STARTPTS,loudnorm=I=-24:TP=-3:LRA=7,afade=t=in:d=0.02,afade=t=out:st=0.55:d=0.10',
    'Flush': 'atrim=start=2.12:end=7.52,asetpts=PTS-STARTPTS,atempo=1.2,loudnorm=I=-21:TP=-3:LRA=7,afade=t=in:d=0.02,afade=t=out:st=3.8:d=0.7',
}
manifest = json.loads((SOURCE / 'Sources.json').read_text())
for item in manifest:
    name = item['asset']
    subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y',
                    '-i', str(SOURCE / (name + '.mp3')), '-ac', '1', '-af', FILTERS[name],
                    '-ar', '44100', '-c:a', 'pcm_s16le', str(OUTPUT / (name + '.wav'))], check=True)
    item['runtime_file'] = name + '.wav'
    item['filters'] = FILTERS[name]
    item['runtime_sha256'] = hashlib.sha256((OUTPUT / item['runtime_file']).read_bytes()).hexdigest()
(OUTPUT / 'Sources.json').write_text(json.dumps(manifest, indent=2) + '\n')
(OUTPUT / 'License.txt').write_text('''Toilet Foley recordings — CC0 1.0 Universal
https://creativecommons.org/publicdomain/zero/1.0/

Plunger: Breviceps — Cartoon - Smoochy Squeak + Plop
https://freesound.org/people/Breviceps/sounds/444494/
Gurgle: harrisonlace — toilet bubbles
https://freesound.org/people/harrisonlace/sounds/830332/
Flush: buzzatsea — sound toilet flushing - homemade.wav
https://freesound.org/people/buzzatsea/sounds/427906/

Public HQ preview recordings were trimmed, converted to mono, normalized,
and faded for gameplay. Source downloads and hashes: ArtSource/Audio/Toilet.
Reproduce with python3 Tools/prepare_toilet_audio.py (requires ffmpeg).
''')
