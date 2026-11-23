#!/usr/bin/env python3
"""Compile the production dice rules and exercise claim truth and both judgments."""
import json
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
version = re.search(r'm_EditorVersion: (\S+)', (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text())[1]
sdk = Path(f'/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/Resources/Scripting/DotNetSdk')
compiler = next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))
references = next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0'))
runtime = sorted((sdk / 'shared/Microsoft.NETCore.App').iterdir())[-1].name
with tempfile.TemporaryDirectory(prefix='lastcall-dice-') as temporary:
    output = Path(temporary)
    args = ['-nologo', '-target:exe', f'-out:"{output}/DiceChecks.dll"']
    args += [f'-r:"{path}"' for path in references.glob('*.dll')]
    args += [f'"{ROOT / source}"' for source in ['Assets/Scripts/SaloonBluffDice.cs', 'Tools/BluffDiceVerification.cs']]
    response = output / 'compile.rsp'
    response.write_text('\n'.join(args))
    subprocess.run([str(sdk / 'dotnet'), str(compiler), '@' + str(response)], check=True)
    (output / 'DiceChecks.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {
        'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': runtime}}}))
    subprocess.run([str(sdk / 'dotnet'), str(output / 'DiceChecks.dll')], check=True)
