"""Validate imported-character wiring and old visual removal without running Unity."""
import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]
manifest=json.loads((root/'Documentation/PlayerCharacter.json').read_text())
for field in ('runtimePrefab','adapter','sourcePrefab','model','controller','material','sharedMotionHelper'):
    assert (root/manifest[field]).exists(), (field,manifest[field])
for old in manifest['removed']:
    assert not (root/old).exists() and not Path(str(root/old)+'.meta').exists(), old
for source in (root/'Assets/scripts').glob('*.cs'):
    assert not re.search(r'FirstPersonBody|AvatarAnatomy',source.read_text()), source
prefab=(root/manifest['runtimePrefab']).read_text()
assert 'UniversalAnimationViewer' not in prefab and 'd58e07532703f714da787730fa279a70' not in prefab
assert not re.search(r'^--- !u!(?:114|65|136|143) ',prefab,re.M), 'Runtime visual includes demo script/collision'
for name in ('Head','Hips','RightHand','LeftHand','RightFoot','LeftFoot'):
    assert f'  m_Name: {name}\n' in prefab,name
meta=Path(str(root/manifest['model'])+'.meta').read_text()
assert 'isReadable: 1' in meta and 'animationType: 3' in meta
assert manifest['modelGuid'] in prefab
controller=(root/manifest['controller']).read_text()
for state in manifest['states']:assert f'  m_Name: {state}\n' in controller,state
assert controller.count('    m_Motion:') == 16, 'Expected sixteen directional blend-tree children'
for gait in ('Walk','Run'):
    for direction in manifest['locomotion']['directions']:
        source=root/manifest['locomotion']['sourceRoot']/gait/f'HumanM@{gait}01_{direction}.fbx'
        assert source.exists(), source
        meta=Path(str(source)+'.meta').read_text()
        assert 'animationType: 3' in meta and 'loopTime: 1' in meta
        guid=re.search(r'^guid: (.+)$',meta,re.M).group(1)
        assert guid in controller, source
assert 'ImportedPlayerBody' in (root/'Assets/scripts/FirstPersonMovement.cs').read_text()
assert 'ImportedPlayerBody.ActiveBodies' in (root/'Assets/scripts/HouseMirror.cs').read_text()
lessons=json.loads((root/'Assets/Resources/Lessons/IntroA11.json').read_text())['tasks']
assert len(lessons)==428 and lessons[0]['targetName']=='Loeffel_Spoon' and lessons[-1]['id']=='CUR-430'
assert manifest['portableTargets']==53 and manifest['fixedTargets']==79 and manifest['newLearningTargets']==0
assert manifest['retainedSettings']['pickupSeconds']==.28 and manifest['retainedSettings']['placeSeconds']==.24
for old_guid in ('48d411449d9274c21aa7d060aaf75030','5e7b4be527c245aba9bce0d69497bc63'):
    assert old_guid not in (root/'Assets/Scenes/SampleScene.unity').read_text()
print('PASS: imported prefab/rig/controller/material wiring; no demo/collision in runtime visual; old generated character removed; retained motion timing and 428-task population.')
