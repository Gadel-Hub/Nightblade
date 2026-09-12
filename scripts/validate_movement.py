#!/usr/bin/env python3
"""Static movement asset checks. Requires Python 3 + PyYAML; does not run Unity."""
import json
import math
from pathlib import Path
import re
import subprocess
import sys

import yaml

ROOT = Path(__file__).resolve().parents[1]
REFERENCE = '5a8f52038b15e32edd64d6c4a3f35b1e417359bd'


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True).strip()


def read(path):
    return (ROOT / path).read_text()


def documents(path):
    """Strip Unity tags/anchors; preserve file IDs as keys, including stripped objects."""
    text = read(path)
    ids = re.findall(r'^--- !u!\d+ &(-?\d+)(?: stripped)?$', text, re.M)
    require(len(ids) == len(set(ids)), f'Duplicate file IDs: {path}')
    text = re.sub(r'^%.*\n', '', text, flags=re.M)
    text = re.sub(r'^--- !u!\d+ &-?\d+(?: stripped)?$', '---', text, flags=re.M)
    values = list(yaml.safe_load_all(text))
    require(len(values) == len(ids), f'Invalid Unity document: {path}')
    return dict(zip(map(int, ids), values))


def component(docs, kind):
    values = [doc[kind] for doc in docs.values() if kind in doc]
    require(len(values) == 1, f'Expected one {kind}, found {len(values)}')
    return values[0]


def guid(path):
    return yaml.safe_load(read(str(path) + '.meta'))['guid']


def close(actual, expected, description):
    require(math.isclose(actual, expected, rel_tol=1e-6, abs_tol=1e-7), description)


def validate():
    require(read('ProjectSettings/ProjectVersion.txt').strip() ==
            'm_EditorVersion: 6000.3.24f1', 'Wrong Unity editor version')
    require(json.loads(read('Packages/manifest.json'))['dependencies']['com.unity.inputsystem'] ==
            '1.20.0', 'Unexpected Input System package change')

    # Compare independently authored prefab and C# defaults to the actual Git reference.
    source = git('show', f'{REFERENCE}:src/player/tuning.ts')
    reference = {name: float(value) for name, value in
                 re.findall(r'(\w+):\s*(-?[\d.]+)', source)}
    prefab_path = 'Assets/Prefabs/Player.prefab'
    prefab = documents(prefab_path)
    movement = component(prefab, 'MonoBehaviour')
    code = read('Assets/Scripts/Player/PlayerMovement.cs')
    conversions = {
        'runSpeed': ('maxHorizontalSpeed', 1 / 32),
        'gravity': ('gravity', 1 / 32),
        'jumpVelocity': ('jumpVelocity', -1 / 32),
        'wallSlideSpeed': ('wallSlideMaxDownwardSpeed', 1 / 32),
        'wallJumpHorizontalVelocity': ('wallJumpHorizontalVelocity', 1 / 32),
        'wallJumpVerticalVelocity': ('wallJumpVerticalVelocity', -1 / 32),
        'wallJumpPushDuration': ('wallJumpPushDuration', 1),
    }
    for field, (key, factor) in conversions.items():
        expected = reference[key] * factor
        close(movement[field], expected, f'Prefab tuning drift: {field}')
        declaration = re.search(r'private float ' + field + r' = ([\d.]+)f;', code)
        require(declaration is not None, f'Missing tuning field: {field}')
        close(float(declaration[1]), expected, f'C# default drift: {field}')
    require(movement['m_Script']['guid'] == guid('Assets/Scripts/Player/PlayerMovement.cs'),
            'Prefab movement script reference is broken')
    body = component(prefab, 'Rigidbody2D')
    for key, value in {'m_BodyType': 0, 'm_Simulated': 1, 'm_UseAutoMass': 0,
                       'm_Mass': 1, 'm_LinearDrag': 0, 'm_AngularDrag': 0,
                       'm_GravityScale': 0, 'm_Interpolate': 0, 'm_SleepingMode': 0,
                       'm_CollisionDetection': 1, 'm_Constraints': 4}.items():
        require(body[key] == value, f'Unexpected Rigidbody configuration: {key}')
    box = component(prefab, 'BoxCollider2D')
    require(box['m_Size'] == {'x': 12 / 32, 'y': 20 / 32}, 'Gameplay collider size changed')
    require(box['m_IsTrigger'] == box['m_AutoTiling'] == 0, 'Collider must be solid and explicit')
    root_id = body['m_GameObject']['fileID']
    root = prefab[root_id]['GameObject']
    require(len(root['m_Component']) == 4, 'Player root should only have transform, body, box, movement')
    renderer = component(prefab, 'SpriteRenderer')
    require(renderer['m_GameObject']['fileID'] != root_id, 'Visual must be a separate child')
    visual = prefab[renderer['m_GameObject']['fileID']]['GameObject']
    visual_transform = prefab[visual['m_Component'][0]['component']['fileID']]['Transform']
    require(visual_transform['m_Father']['fileID'] == root['m_Component'][0]['component']['fileID'],
            'Visual is not a child of the gameplay root')
    require(body['m_GameObject'] == box['m_GameObject'] == movement['m_GameObject'],
            'Gameplay components must share the root')
    for doc in prefab.values():
        if 'Transform' in doc:
            require(doc['Transform']['m_LocalScale'] == dict(x=1, y=1, z=1), 'Scaled player transform')
    material_path = 'Assets/Prefabs/PlayerFrictionless.physicsMaterial2D'
    material = component(documents(material_path), 'PhysicsMaterial2D')
    require(material['friction'] == material['bounciness'] == 0, 'Player material is not frictionless')
    require(body['m_Material']['guid'] == box['m_Material']['guid'] == guid(material_path),
            'Missing physics material')

    layers = component(documents('ProjectSettings/TagManager.asset'), 'TagManager')['layers']
    terrain = layers.index('Terrain')
    require(root['m_Layer'] == layers.index('Player'), 'Wrong player layer')
    require(movement['terrainLayers']['m_Bits'] == 1 << terrain, 'Wrong contact mask')
    time = component(documents('ProjectSettings/TimeManager.asset'), 'TimeManager')
    close(time['Fixed Timestep'], 1 / 60, 'Movement requires 60 Hz physics')
    input_path = 'Assets/Input/Movement.inputactions'
    require(movement['inputActions'] == dict(fileID=-944628639613478452, guid=guid(input_path), type=3),
            'Broken InputActionAsset reference')
    actions = json.loads(read(input_path))['maps']
    require(len(actions) == 1 and actions[0]['name'] == 'Player', 'Unexpected input maps')
    action_map = actions[0]
    require({a['name']: a['type'] for a in action_map['actions']} == {'Move': 'Value', 'Jump': 'Button'},
            'Expected only Move and Jump')
    bindings = action_map['bindings']
    require(bindings[0]['path'] == '1DAxis(whichSideWins=0)' and bindings[0]['isComposite'],
            'Opposing directions must cancel in one composite')
    require({(b['name'], b['path']) for b in bindings[1:5]} == {
        ('negative', '<Keyboard>/a'), ('negative', '<Keyboard>/leftArrow'),
        ('positive', '<Keyboard>/d'), ('positive', '<Keyboard>/rightArrow')}, 'Wrong movement keys')
    require(all(b['isPartOfComposite'] and b['action'] == 'Move' for b in bindings[1:5]),
            'Unattached direction binding')
    require(len(bindings) == 6 and bindings[5]['path'] == '<Keyboard>/space'
            and bindings[5]['action'] == 'Jump', 'Wrong jump binding')
    all_ids = [action_map['id']] + [a['id'] for a in action_map['actions']] + [b['id'] for b in bindings]
    require(len(set(all_ids)) == len(all_ids), 'Duplicate input IDs')
    input_settings = 'Assets/Input/MovementInputSettings.asset'
    settings = component(documents(input_settings), 'MonoBehaviour')
    require(settings['m_Script']['guid'] == 'c46f07b5ed07e4e92aa78254188d3d10',
            'InputSettings points to the wrong package script')
    require(settings['m_UpdateMode'] == 2,
            'Input polling must run in FixedUpdate')
    build = component(documents('ProjectSettings/EditorBuildSettings.asset'), 'EditorBuildSettings')
    require(build['m_configObjects']['com.unity.input.settings']['guid'] == guid(input_settings),
            'Fixed input settings not registered')

    scene_path = 'Assets/Scenes/MovementLab.unity'
    scene = documents(scene_path)
    instance = component(scene, 'PrefabInstance')
    require(instance['m_SourcePrefab']['guid'] == guid(prefab_path), 'Lab must use the player prefab')
    require(all(m['propertyPath'].startswith('m_LocalPosition.') for m in
                instance['m_Modification']['m_Modifications']), 'Lab overrides player behavior')
    for name, fields in [('MovementLabCamera', {'target': 'Transform'}),
                         ('MovementDiagnostics', {'player': 'MonoBehaviour', 'playerCollider': 'BoxCollider2D'})]:
        script = guid(f'Assets/Scripts/Debug/{name}.cs')
        tools = [doc['MonoBehaviour'] for doc in scene.values() if 'MonoBehaviour' in doc
                 and doc['MonoBehaviour']['m_Script']['guid'] == script]
        require(len(tools) == 1, f'Missing or duplicate {name}')
        for field, kind in fields.items():
            target = scene[tools[0][field]['fileID']][kind]
            require(target['m_CorrespondingSourceObject']['guid'] == guid(prefab_path),
                    f'{name}.{field} must reference the player instance')
    course = {}
    for doc in scene.values():
        if 'BoxCollider2D' not in doc or 'm_Size' not in doc['BoxCollider2D']:
            continue  # Stripped player reference inherits its collider from the prefab.
        collider = doc['BoxCollider2D']
        obj = scene[collider['m_GameObject']['fileID']]['GameObject']
        require(obj['m_Layer'] == terrain and collider['m_IsTrigger'] == 0, 'Non-solid lab terrain')
        require(collider['m_Material']['guid'] == guid(material_path), 'Terrain friction mismatch')
        tr = scene[obj['m_Component'][0]['component']['fileID']]['Transform']
        require(tr['m_LocalScale'] == dict(x=1, y=1, z=1), 'Terrain size must be explicit')
        course[obj['m_Name']] = (tr['m_LocalPosition'], collider['m_Size'])
    needed = ['FlatGround', 'LandingGround', 'ShortPlatform', 'RaisedPlatform', 'LowCeiling',
              'TallWall', 'OppositeWall', 'NarrowShaftLeft', 'NarrowShaftRight',
              'WallJumpExit', 'NarrowShaftExit', 'EdgeLandingPlatform', 'RaisedEdgeLanding',
              'GapRecoveryFloor', 'GapRecoveryLeftStep', 'GapRecoveryRightStep']
    require(all('TEMP_' + name in course for name in needed), 'Missing movement test geometry')

    def left(name):
        position, size = course['TEMP_' + name]
        return position['x'] - size['x'] / 2

    def right(name):
        position, size = course['TEMP_' + name]
        return position['x'] + size['x'] / 2

    gap = left('LandingGround') - right('FlatGround')
    # Geometry sanity checks only; these equations do not simulate either engine.
    jump_range = movement['runSpeed'] * 2 * movement['jumpVelocity'] / movement['gravity']
    require(2 < gap < jump_range, 'Gap is not a useful normal-jump test')
    shaft = left('NarrowShaftRight') - right('NarrowShaftLeft')
    require(box['m_Size']['x'] < shaft < box['m_Size']['x'] +
            movement['wallJumpHorizontalVelocity'] * movement['wallJumpPushDuration'],
            'Narrow shaft cannot test early opposite-wall contact')
    require(any(s['enabled'] and s['path'] == scene_path and s['guid'] == guid(scene_path)
                for s in build['m_Scenes']), 'MovementLab missing from build scene list')

    # All local asset GUIDs and serialized scene/prefab references must resolve.
    guids = {}
    for path in (ROOT / 'Assets').rglob('*'):
        if path.name.startswith('.') or path.suffix == '.meta':
            continue
        relative = path.relative_to(ROOT)
        require(Path(str(path) + '.meta').is_file(), f'Missing meta: {relative}')
        key = guid(relative)
        require(key not in guids, f'Duplicate GUID: {key}')
        guids[key] = relative
    package_guids = {'6a160d838ff8b4b4693ac20007e008c7', 'c46f07b5ed07e4e92aa78254188d3d10',
                     '8404be70184654265930450def6a9037', '0000000000000000f000000000000000'}
    for path in [prefab_path, scene_path, input_settings]:
        docs = documents(path)
        for match in re.findall(r'\{fileID: [^}]+\}', read(path)):
            ref = yaml.safe_load(match)
            if 'guid' in ref:
                require(ref['guid'] in guids or ref['guid'] in package_guids, f'Broken GUID in {path}')
                if ref['guid'] == guid(prefab_path) and ref['fileID'] != 100100000:
                    require(ref['fileID'] in prefab, 'Broken prefab source file ID')
            else:
                require(ref['fileID'] == 0 or ref['fileID'] in docs, f'Broken local file ID in {path}')

    parity = read('docs/PORT_PARITY.md')
    for subsystem in ['Horizontal movement', 'Jump / gravity', 'Ground / wall detection',
                      'Wall slide', 'Wall jump / control restriction']:
        row = next(line for line in parity.splitlines() if line.startswith('| ' + subsystem + ' |'))
        require(row.endswith('| porting |'), f'Premature parity status: {subsystem}')
    for path in ['Library/check', 'Temp/check', 'Logs/check', 'obj/check', 'UserSettings/check',
                 'node_modules/check', 'dist/check', 'Builds/Web/check']:
        require(subprocess.run(['git', 'check-ignore', '-q', path], cwd=ROOT).returncode == 0,
                f'Generated directory not ignored: {path}')
    staged = git('diff', '--cached', '--name-only').splitlines()
    require(not any(p.split('/')[0] in ['Library', 'Temp', 'Logs', 'obj', 'UserSettings', 'node_modules', 'dist']
                    for p in staged), 'Generated content is staged')
    print('PASS: reference conversions, prefab structure/physics/material, input bindings/settings,')
    print('layers, timestep, lab geometry, asset references, parity statuses and generated-file exclusions.')
    print('STATIC ONLY: Unity import, C# compilation, physics/input behavior and feel were not tested.')


if __name__ == '__main__':
    try:
        validate()
    except (AssertionError, KeyError, StopIteration) as error:
        print(f'FAIL: {error}', file=sys.stderr)
        sys.exit(1)
