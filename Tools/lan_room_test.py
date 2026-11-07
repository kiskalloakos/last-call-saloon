#!/usr/bin/env python3
"""Rendered host/client regression: real 30s discussion, UI, room commands and assignment."""
import argparse
from pathlib import Path
import subprocess
import time

p = argparse.ArgumentParser()
p.add_argument('--app', required=True)
p.add_argument('--logs', type=Path, default=Path('/tmp/saloon-room-test'))
p.add_argument('--voice', action='store_true', help='Connect real Vivox sessions with microphones muted')
args = p.parse_args()
args.logs.mkdir(parents=True, exist_ok=True)
processes = []
try:
    for side in ('host', 'client'):
        mode = 'rooms-' + ('voice-' if args.voice else '') + side
        command = [args.app, '-saloonTest', mode, '-saloonName', 'Room-' + side,
                   '-saloonPort', '17996', '-logFile', str(args.logs / (side + '.log'))]
        processes.append(subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
        if side == 'host':
            time.sleep(2)
    deadline = time.monotonic() + 100
    while any(x.poll() is None for x in processes) and time.monotonic() < deadline:
        time.sleep(.25)
    passed = True
    for side, process in zip(('host', 'client'), processes):
        if process.poll() is None:
            process.terminate()
            process.wait(timeout=10)
        log = (args.logs / (side + '.log')).read_text(errors='replace')
        for line in log.splitlines():
            if 'SALOON_ROOMS ' in line or 'SALOON_DISCONNECTED' in line or 'Exception:' in line:
                print(side, line)
        passed &= process.returncode == 0 and 'SALOON_ROOMS PASS' in log and 'Exception:' not in log
    raise SystemExit(0 if passed else 1)
finally:
    for process in processes:
        if process.poll() is None:
            process.terminate()
