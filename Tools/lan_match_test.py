#!/usr/bin/env python3
"""Run unchanged packaged players: four/five processes, three complete no-bots LAN rounds."""
import argparse
from pathlib import Path
import subprocess
import time
from udp_test_link import ImpairedLink

parser = argparse.ArgumentParser()
parser.add_argument('--app', type=Path, required=True)
parser.add_argument('--players', type=int, choices=(4, 5), default=4)
parser.add_argument('--address', default='127.0.0.1')
parser.add_argument('--port', type=int, default=18130)
parser.add_argument('--logs', type=Path, required=True)
parser.add_argument('--impaired', action='store_true')
args = parser.parse_args()
args.logs.mkdir(parents=True, exist_ok=True)
processes, links = [], []
try:
    for index in range(args.players):
        port = args.port
        if index and args.impaired:
            port += index
            links.append(ImpairedLink(port, args.port))
        cmd = [str(args.app.resolve()), '-batchmode', '-nographics', '-saloonTest',
               'lan-match-host' if index == 0 else 'lan-match-client', '-saloonPlayers', str(args.players),
               '-saloonIndex', str(index), '-saloonPort', str(port), '-saloonAddress', args.address,
               '-logFile', str((args.logs / f'player-{index}.log').resolve())]
        processes.append(subprocess.Popen(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
        if index == 0:
            time.sleep(2)
    deadline = time.monotonic() + 115
    while any(p.poll() is None for p in processes) and time.monotonic() < deadline:
        failed = False
        for path in args.logs.glob('player-*.log'):
            with path.open('rb') as log:
                log.seek(max(0, path.stat().st_size - 8192))
                failed |= b'SALOON_LAN_MATCH FAIL' in log.read()
        if failed:
            break
        time.sleep(.25)
    passed = True
    roles = []
    for index, process in enumerate(processes):
        if process.poll() is None:
            process.terminate()
            process.wait(timeout=10)
        path = args.logs / f'player-{index}.log'
        log = path.read_text(errors='replace') if path.exists() else ''
        lines = [line for line in log.splitlines() if 'SALOON_LAN_MATCH' in line]
        print(f'Player {index} exit={process.returncode}\n' + '\n'.join(lines), flush=True)
        passed &= process.returncode == 0 and 'SALOON_LAN_MATCH PASS' in log and 'Exception:' not in log
        result = next((line for line in lines if 'SALOON_LAN_MATCH PASS' in line), '')
        if 'role=' in result:
            roles.append(result.split('role=', 1)[1].split()[0])
    expected = sorted(['Sheriff', 'Deputy', 'Renegade'] + ['Outlaw'] * (args.players - 3))
    passed &= sorted(roles) == expected
    print(f'Exact role deck: {roles}; expected: {expected}', flush=True)
    for link in links:
        print(f'Impaired link forwarded={link.forwarded} dropped={link.dropped} error={link.error}', flush=True)
        passed &= link.error is None and link.forwarded > 0 and link.dropped > 0
    print('SALOON_LAN_MATCH_PROCESSES ' + ('PASS' if passed else 'FAIL'), flush=True)
    raise SystemExit(0 if passed else 1)
finally:
    for link in links:
        link.close()
    for process in processes:
        if process.poll() is None:
            process.terminate()
