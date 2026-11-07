#!/usr/bin/env python3
"""Launch five real builds over localhost and check each process's match result."""
import argparse
from pathlib import Path
import subprocess
import sys
import tempfile
import time

parser = argparse.ArgumentParser()
parser.add_argument('--app', type=Path, default=Path(__file__).resolve().parents[1] / 'Builds/macOS/Last Call.app/Contents/MacOS/Last Call - Saloon Prototype')
parser.add_argument('--logs', type=Path, default=Path(tempfile.mkdtemp(prefix='saloon-smoke-')))
parser.add_argument('--port', default='17877')
parser.add_argument('--practice', action='store_true', help='Use two real clients and three active practice opponents')
args = parser.parse_args()
args.logs.mkdir(parents=True, exist_ok=True)
processes = []
try:
    for i in range(2 if args.practice else 5):
        log = args.logs / f'player-{i}.log'
        mode=('practice-host' if args.practice else 'host') if i==0 else 'client'
        command = [str(args.app), '-batchmode', '-nographics', '-saloonTest', mode,
                   '-saloonName', f'Test-{i}', '-saloonPort', args.port, '-logFile', str(log)]
        processes.append(subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
        if i == 0:
            time.sleep(2)
    deadline = time.monotonic() + 65
    while any(p.poll() is None for p in processes) and time.monotonic() < deadline:
        time.sleep(.25)
    passed = True
    for i, p in enumerate(processes):
        if p.poll() is None:
            p.terminate()
            p.wait(timeout=5)
        text = (args.logs / f'player-{i}.log').read_text(errors='replace')
        results = [line for line in text.splitlines() if 'SALOON_SMOKE ' in line or 'SALOON_PRIVACY ' in line]
        print(f'Player {i}: exit={p.returncode}; ' + '; '.join(results))
        passed &= p.returncode == 0 and 'SALOON_SMOKE PASS' in text and 'Exception:' not in text
    print(f'Logs: {args.logs}')
    sys.exit(0 if passed else 1)
finally:
    for p in processes:
        if p.poll() is None:
            p.terminate()
