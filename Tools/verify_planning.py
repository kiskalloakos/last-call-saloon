#!/usr/bin/env python3
"""Run the production planning and round logic without starting the Unity editor."""
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
sources = ['Assets/Scripts/SaloonStoryMap.cs', 'Assets/Scripts/SaloonRules.cs', 'Assets/Scripts/SaloonRounds.cs',
           'Assets/Scripts/SaloonRoomReservations.cs', 'Assets/Scripts/SaloonPracticePlanning.cs',
           'Assets/Scripts/SaloonPlanning.cs', 'Assets/Scripts/SaloonRoomReservationVerification.cs',
           'Tools/PlanningVerification.cs']


def production_method(filename, signature):
    """Compile current authorization methods without Unity's rendering dependencies."""
    source = (ROOT / 'Assets/Scripts' / filename).read_text()
    start = source.index(signature)
    body = source.index('{', start)
    depth = 1
    end = body + 1
    while depth:
        if source[end] == '{':
            depth += 1
        elif source[end] == '}':
            depth -= 1
        end += 1
    return source[start:end]


with tempfile.TemporaryDirectory(prefix='lastcall-planning-') as temporary:
    output = Path(temporary)
    methods = [('SaloonSocial.cs', 'bool DuelAllows('),
               ('SaloonSocial.cs', 'void AdvanceRoomTurn('),
               ('SaloonSocial.cs', 'RoomTurnView PrivateRoomTurn('),
               ('SaloonGame.cs', 'string ActionBlockReason('),
               ('SaloonGame.cs', 'bool SpendAction('),
               ('SaloonHeist.cs', 'void DeclareEscape('),
               ('SaloonHeist.cs', 'void ResolveEscapes('),
               ('SaloonBeerPour.cs', 'int PrepareBeerHealing('),
               ('SaloonBeerPour.cs', 'bool CommitBeerUse('),
               ('SaloonBeerPour.cs', 'void DrinkBeer('),
               ('SaloonBeerPour.cs', 'void LeaveBeer(')]
    authorization = output / 'ProductionAuthorization.cs'
    authorization.write_text('using System.Linq; using UnityEngine; namespace Saloon { public partial class SaloonGame {\n'
                             + '\n'.join(production_method(*method) for method in methods) + '\n}}')
    args = ['-nologo', '-target:exe', '-langversion:9.0', f'-out:"{output}/PlanningChecks.dll"']
    args += [f'-r:"{path}"' for path in references.glob('*.dll')]
    args += [f'"{ROOT / source}"' for source in sources]
    args += [f'"{authorization}"', f'"{ROOT / "Assets/Scripts/SaloonPosse.cs"}"']
    response = output / 'compile.rsp'
    response.write_text('\n'.join(args))
    subprocess.run([str(sdk / 'dotnet'), str(compiler), '@' + str(response)], check=True)
    (output / 'PlanningChecks.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {
        'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': runtime}}}))
    subprocess.run([str(sdk / 'dotnet'), str(output / 'PlanningChecks.dll')], check=True)
