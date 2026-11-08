#!/usr/bin/env python3
"""Rendered host/client regression: discussion/Ready UI, room commands and assignment."""
import argparse
from pathlib import Path
import subprocess
import time
from udp_test_link import ImpairedLink

p = argparse.ArgumentParser()
p.add_argument('--app', required=True)
p.add_argument('--port', type=int, default=17996)
p.add_argument('--logs', type=Path, default=Path('/tmp/saloon-room-test'))
p.add_argument('--limited', action='store_true', help='Also cap host-to-client traffic at 8,000 bytes/s')
p.add_argument('--impaired', action='store_true', help='80ms each way, jitter, 2%% loss and a 5s outage during room choice')
p.add_argument('--software-gpu', action='store_true', help='Render at 640x360 on Windows CI software graphics')
p.add_argument('--pair', action='store_true', help='Test mutual room invitation, coin choice and both actions')
p.add_argument('--voice', action='store_true', help='Connect real Vivox sessions with microphones muted')
args = p.parse_args()
args.logs = args.logs.resolve()
args.logs.mkdir(parents=True, exist_ok=True)
processes = []
link = ImpairedLink(args.port+1, args.port, bytes_per_second=8000 if args.limited else 0) if args.impaired or args.limited else None
outage_started = False
phase_seen = {}
phase_lags = []
try:
    for side in ('host', 'client'):
        mode = 'rooms-' + ('pair-' if args.pair else '') + ('voice-' if args.voice else '') + side
        command = [args.app, '-saloonTest', mode, '-saloonName', 'Room-' + side,
                   '-saloonPort', str(args.port+1) if link and side == 'client' else str(args.port), '-logFile', str(args.logs / (side + '.log'))]
        if args.software_gpu:
            command += ['-screen-width', '640', '-screen-height', '360', '-screen-fullscreen', '0', '-screen-quality', 'Very Low']
        processes.append(subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
        if side == 'host':
            time.sleep(2)
    deadline = time.monotonic() + 100
    while any(x.poll() is None for x in processes) and time.monotonic() < deadline:
        for side in ('host', 'client'):
            path = args.logs / (side + '.log')
            if path.exists():
                content = path.read_text(errors='replace')
                for phase in ('Discussion', 'Choosing', 'Turns', 'Aftermath'):
                    if (side, phase) not in phase_seen and 'SALOON_PHASE ' + phase in content:
                        phase_seen[side, phase] = time.monotonic()
                        if side == 'client' and ('host', phase) in phase_seen:
                            lag = phase_seen[side, phase] - phase_seen['host', phase]
                            phase_lags.append(lag)
                            print(f'SALOON_PHASE_LAG {phase} {lag:.2f}s', flush=True)
        if link and not outage_started:
            host_log = args.logs / 'host.log'
            if host_log.exists() and 'SALOON_PHASE Discussion' in host_log.read_text(errors='replace'):
                link.blackout(5)
                outage_started = True
                print('SALOON_LINK starting 5s packet outage during discussion/planning', flush=True)
        time.sleep(.25)
    passed = not link or (outage_started and link.error is None and link.forwarded > 0 and link.dropped > 0)
    if args.limited:
        passed &= bool(phase_lags) and max(phase_lags) < 7
    if link:
        print(f'SALOON_LINK forwarded={link.forwarded} dropped={link.dropped} rateDropped={link.rate_dropped} outage={outage_started} maxPhaseLag={max(phase_lags, default=0):.2f}s error={link.error}', flush=True)
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
    if link:
        link.close()
    for process in processes:
        if process.poll() is None:
            process.terminate()
