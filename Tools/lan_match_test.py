#!/usr/bin/env python3
"""Run four to eight packaged players through three no-bots LAN or online Relay rounds."""
import argparse
from pathlib import Path
import subprocess
import time
from udp_test_link import ImpairedLink

parser = argparse.ArgumentParser()
parser.add_argument('--app', type=Path, required=True)
parser.add_argument('--players', type=int, choices=range(4, 9), default=4)
parser.add_argument('--online', action='store_true', help='Create a private Relay room instead of LAN')
parser.add_argument('--address', default='127.0.0.1')
parser.add_argument('--port', type=int, default=18130)
parser.add_argument('--logs', type=Path, required=True)
parser.add_argument('--impaired', action='store_true')
parser.add_argument('--blackout', type=float, default=0, help='Interrupt the first joined player during discussion (requires --impaired)')
args = parser.parse_args()
if args.online and args.impaired:
    parser.error('--impaired is available only for LAN')
if args.blackout < 0 or args.blackout >= 25 or (args.blackout and not args.impaired):
    parser.error('--blackout requires --impaired and a duration below the transport timeout (0–24 seconds)')
args.logs.mkdir(parents=True, exist_ok=True)
# Old failures must not end a rerun before every new player has opened its log.
for index in range(args.players):
    (args.logs / f'player-{index}.log').unlink(missing_ok=True)
code_file = (args.logs / 'room-code.txt').resolve()
if args.online:
    code_file.unlink(missing_ok=True)
processes, links = [], []
blackout_started = False
try:
    for index in range(args.players):
        port = args.port
        if index and args.impaired:
            port += index
            links.append(ImpairedLink(port, args.port))
        cmd = [str(args.app.resolve()), '-batchmode', '-nographics', '-saloonTest',
               'lan-match-host' if index == 0 else 'lan-match-client', '-saloonPlayers', str(args.players),
               '-saloonIndex', str(index), '-saloonPort', str(port), '-saloonAddress', args.address,
               '-saloonOutageSeconds', str(args.blackout),
               '-logFile', str((args.logs / f'player-{index}.log').resolve())]
        if args.online:
            cmd += ['-saloonOnline', '1']
            if index == 0:
                cmd += ['-saloonCodeFile', str(code_file)]
            else:
                cmd += ['-saloonCode', code_file.read_text().strip()]
        processes.append(subprocess.Popen(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
        if index == 0:
            if args.online:
                deadline = time.monotonic() + 70
                while not code_file.exists() and processes[0].poll() is None and time.monotonic() < deadline:
                    time.sleep(.25)
                if not code_file.exists():
                    raise RuntimeError('Relay host did not create a room; inspect player-0.log')
            else:
                time.sleep(2)
    deadline = time.monotonic() + 115 + args.blackout + (75 if args.online else 0)
    while any(p.poll() is None for p in processes) and time.monotonic() < deadline:
        failed = any(p.poll() not in (None, 0) for p in processes)
        if args.blackout and not blackout_started:
            path = args.logs / 'player-1.log'
            if path.exists() and 'SALOON_LAN_MATCH_PHASE index=1 round=1 Discussion' in path.read_text(errors='replace'):
                links[0].blackout(args.blackout)
                blackout_started = True
                print(f'Interrupting joined player 1 for {args.blackout:g} seconds', flush=True)
        for path in args.logs.glob('player-*.log'):
            with path.open('rb') as log:
                log.seek(max(0, path.stat().st_size - 8192))
                failed |= b'SALOON_LAN_MATCH FAIL' in log.read()
        if failed:
            break
        time.sleep(.25)
    passed = True
    passed &= not args.blackout or blackout_started
    roles = []
    for index, process in enumerate(processes):
        if process.poll() is None:
            process.terminate()
            process.wait(timeout=10)
        path = args.logs / f'player-{index}.log'
        log = path.read_text(errors='replace') if path.exists() else ''
        lines = [line for line in log.splitlines() if 'SALOON_LAN_MATCH' in line]
        print(f'Player {index} exit={process.returncode}\n' + '\n'.join(lines), flush=True)
        # Runtime exceptions fail in the game harness; handled service throttling
        # is allowed only if every process eventually completes the whole match.
        passed &= process.returncode == 0 and 'SALOON_LAN_MATCH PASS' in log and 'SALOON_LAN_MATCH FAIL' not in log
        result = next((line for line in lines if 'SALOON_LAN_MATCH PASS' in line), '')
        if 'role=' in result:
            roles.append(result.split('role=', 1)[1].split()[0])
    deputies, outlaws = {4: (1, 1), 5: (1, 2), 6: (2, 2), 7: (2, 3), 8: (3, 3)}[args.players]
    independents = [role for role in roles if role in ('Renegade', 'Swindler')]
    expected = sorted(['Sheriff'] + independents + ['Deputy'] * deputies + ['Outlaw'] * outlaws)
    passed &= len(independents) == 1 and sorted(roles) == expected
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
