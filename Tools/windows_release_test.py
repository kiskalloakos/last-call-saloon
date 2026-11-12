#!/usr/bin/env python3
"""Execute an unchanged published Windows player, keeping all logs on any failure."""
import argparse
import json
import os
from pathlib import Path
import platform
import subprocess
import sys

p = argparse.ArgumentParser()
p.add_argument('--app', type=Path, required=True)
p.add_argument('--logs', type=Path, required=True)
p.add_argument('--suite', choices=['standard', 'rooms', 'stress', 'limited', 'smoke', 'lan'], default='standard')
args = p.parse_args()
if sys.platform != 'win32':
    p.error('This test must execute on Windows, not a cross-build host')
app = args.app.resolve()
logs = args.logs.resolve()
logs.mkdir(parents=True, exist_ok=True)
tools = Path(__file__).resolve().parent
results = []


def run(name, command, timeout):
    print(f'Running {name} on {platform.platform()}', flush=True)
    output_path = logs / (name + '-runner.log')
    result = {'name': name, 'command': command, 'timed_out': False}
    try:
        with output_path.open('w', encoding='utf-8') as output:
            completed = subprocess.run(command, stdout=output, stderr=subprocess.STDOUT,
                                       timeout=timeout, check=False)
        result['exit_code'] = completed.returncode
        result['passed'] = completed.returncode == 0
    except subprocess.TimeoutExpired:
        result.update(timed_out=True, passed=False)
    except OSError as e:
        result.update(error=str(e), passed=False)
    finally:
        # A timed-out harness must not leave game processes to affect later tests.
        subprocess.run(['taskkill', '/F', '/IM', app.name, '/T'],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False)
    if name == 'functional':
        log = (logs / 'functional.log').read_text(encoding='utf-8', errors='replace') if (logs / 'functional.log').exists() else ''
        result['passed'] &= 'SALOON_GAMEPLAY PASS' in log and 'Exception:' not in log
    print(output_path.read_text(encoding='utf-8', errors='replace')[-8000:], flush=True)
    print(json.dumps(result), flush=True)
    results.append(result)
    (logs / 'results.json').write_text(json.dumps(results, indent=2), encoding='utf-8')


if args.suite == 'standard':
    run('voice-controls', [str(app), '-batchmode', '-nographics', '-saloonTest', 'voice-controls',
                           '-saloonPort', '17986', '-logFile', str(logs / 'voice-controls.log')], 150)
    run('functional', [str(app), '-batchmode', '-nographics', '-saloonTest', 'functional',
                       '-saloonPort', '17987', '-logFile', str(logs / 'functional.log')], 120)
    run('five-player', [sys.executable, str(tools / 'smoke_test.py'), '--app', str(app),
                        '--logs', str(logs / 'five-player'), '--port', '17992'], 100)
if args.suite in ('standard', 'rooms'):
    for name, options in [('separate-rooms', []), ('paired-room', ['--pair']),
                          ('paired-room-voice', ['--pair', '--voice'])]:
        run(name, [sys.executable, str(tools / 'lan_room_test.py'), '--software-gpu', '--app', str(app),
                   '--logs', str(logs / name), *options], 120)
elif args.suite == 'lan':
    for name, options in [('four-player', ['--players', '4']), ('five-player', ['--players', '5']),
                          ('four-player-impaired', ['--players', '4', '--impaired']),
                          ('four-player-blackout', ['--players', '4', '--impaired', '--blackout', '13'])]:
        run(name, [sys.executable, str(tools / 'lan_match_test.py'), '--app', str(app),
                   '--logs', str(logs / name), *options], 150)
elif args.suite == 'smoke':
    for i in range(3):
        run('five-player-' + str(i+1), [sys.executable, str(tools / 'smoke_test.py'), '--app', str(app),
                                      '--logs', str(logs / ('five-player-' + str(i+1))), '--port', str(17992+i)], 100)
elif args.suite == 'stress':
    for name, options in [('impaired-separate', []), ('impaired-pair', ['--pair'])]:
        run(name, [sys.executable, str(tools / 'lan_room_test.py'), '--software-gpu', '--app', str(app),
                   '--logs', str(logs / name), '--impaired', *options], 120)
else:
    for name, options in [('limited-separate', []), ('limited-pair', ['--pair'])]:
        run(name, [sys.executable, str(tools / 'lan_room_test.py'), '--software-gpu', '--app', str(app),
                   '--logs', str(logs / name), '--limited', *options], 120)
summary = '\n'.join(f"- {'PASS' if r['passed'] else 'FAIL'}: {r['name']}" for r in results)
print(summary, flush=True)
if os.environ.get('GITHUB_STEP_SUMMARY'):
    with open(os.environ['GITHUB_STEP_SUMMARY'], 'a', encoding='utf-8') as output:
        output.write('Actual published Windows executable on Windows Server 2022\n\n' + summary + '\n')
sys.exit(0 if all(r['passed'] for r in results) else 1)
