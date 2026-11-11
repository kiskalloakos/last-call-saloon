#!/usr/bin/env python3
"""Run actual Relay/Vivox integration checks with two independent game processes."""
import argparse
from pathlib import Path
import subprocess
import sys
import tempfile
import time

parser = argparse.ArgumentParser()
parser.add_argument('mode', choices=['voice', 'relay'])
parser.add_argument('--app', type=Path, default=Path(__file__).resolve().parents[1] / 'Builds/macOS/Last Call.app/Contents/MacOS/Last Call - Saloon Prototype')
parser.add_argument('--wave', type=Path, help='Mono 16-bit PCM WAV at 48000 Hz for the voice injection test')
parser.add_argument('--logs', type=Path, default=Path(tempfile.mkdtemp(prefix='saloon-cloud-')))
args = parser.parse_args()
args.app = args.app.resolve()
args.logs = args.logs.resolve()
if args.mode == 'voice' and not args.wave:
    parser.error('--wave is required for voice')
args.logs.mkdir(parents=True, exist_ok=True)
code_file = args.logs / 'room-code.txt'
stop_file = args.logs / 'audio-stopped.txt'
stop_file.unlink(missing_ok=True)
processes = []

def start(side, extra):
    log = args.logs / f'{side}.log'
    command = [str(args.app), '-batchmode', '-nographics', '-saloonCloudTest', f'{args.mode}-{side}',
               '-saloonPort', '17888', '-logFile', str(log), *extra]
    process = subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    processes.append((side, process, log))
    return process

try:
    extra = ['-saloonCodeFile', str(code_file)] if args.mode == 'relay' else ['-saloonWave', str(args.wave.resolve()), '-saloonStopFile', str(stop_file)]
    host = start('host', extra)
    client_extra = []
    if args.mode == 'relay':
        deadline = time.monotonic() + 55
        while not code_file.exists() and host.poll() is None and time.monotonic() < deadline:
            time.sleep(.25)
        if code_file.exists():
            client_extra = ['-saloonCode', code_file.read_text().strip()]
        else:
            raise RuntimeError('Relay host did not create a room; inspect host.log')
    else:
        time.sleep(2)
    if args.mode == 'voice':
        client_extra = ['-saloonStopFile', str(stop_file)]
    start('client', client_extra)
    deadline = time.monotonic() + 95
    while any(p.poll() is None for _, p, _ in processes) and time.monotonic() < deadline:
        time.sleep(.25)
    passed = True
    for side, process, log in processes:
        if process.poll() is None:
            process.terminate()
            process.wait(timeout=5)
        content = log.read_text(errors='replace')
        result_lines = [line for line in content.splitlines() if any(marker in line for marker in ['SALOON_RELAY ', 'SALOON_VOICE_JOIN ', 'SALOON_VOICE_AUDIO ', 'SALOON_VOICE_STOP ', 'SALOON_CLOUD FAIL'])]
        print(f'{side}: exit={process.returncode}; ' + '; '.join(result_lines))
        marker = 'SALOON_RELAY PASS' if args.mode == 'relay' else 'SALOON_VOICE_AUDIO PASS'
        passed &= process.returncode == 0 and marker in content and 'SALOON_CLOUD FAIL' not in content
        if args.mode == 'voice' and side == 'client':
            passed &= 'SALOON_VOICE_STOP PASS' in content
    print('Logs:', args.logs)
    sys.exit(0 if passed else 1)
finally:
    for _, process, _ in processes:
        if process.poll() is None:
            process.terminate()
