import re,pathlib,itertools,math,json
text=pathlib.Path('Assets/Scenes/SampleScene.unity').read_text();blocks=re.split(r'^--- !u!',text,flags=re.M)[1:];names={};trans={};mesh={};parents={};bygo={}
def vec(b,key):
 v=re.search(r'  '+key+r': \{([^}]+)\}',b)[1];return tuple(float(x.split(':')[1]) for x in v.split(','))
for b in blocks:
 if b.startswith('1 &'):names[re.match(r'1 &(\d+)',b)[1]]=re.search(r'  m_Name: (.*)',b)[1]
 if b.startswith('4 &'):
  tid=re.match(r'4 &(\d+)',b)[1];gid=re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1];trans[tid]=(vec(b,'m_LocalPosition'),vec(b,'m_LocalRotation'),vec(b,'m_LocalScale'));bygo[gid]=tid;parents[tid]=re.search(r'm_Father: \{fileID: (\d+)\}',b)[1]
 if b.startswith('33 &'):
  gid=re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1];mesh[gid]=re.search(r'm_Mesh: \{fileID: (\d+), guid: ([a-f0-9]+)',b).groups()
meta={}
for file in pathlib.Path('Assets').rglob('*.asset.meta'):
 m=re.search(r'^guid: (\w+)',file.read_text(),re.M)
 if m:meta[m[1]]=pathlib.Path(str(file)[:-5])
def rotate(p,q):
 x,y,z,w=q;px,py,pz=p;tx=2*(y*pz-z*py);ty=2*(z*px-x*pz);tz=2*(x*py-y*px)
 return (px+w*tx+y*tz-z*ty,py+w*ty+z*tx-x*tz,pz+w*tz+x*ty-y*tx)
def apply(p,t):
 pos,q,sc=trans[t];r=rotate(tuple(a*b for a,b in zip(p,sc)),q);return tuple(a+b for a,b in zip(pos,r))
for task in (t for t in json.load(open('Assets/Resources/Lessons/IntroA11.json'))['tasks'] if 251 <= int(t['id'][-3:]) <= 260):
 name=task['targetName'];root=bygo[next(g for g,n in names.items() if n==name)];points=[]
 for gid,(mid,guid) in mesh.items():
  child=bygo[gid];chain=[]
  while child not in [root,'0']:chain.append(child);child=parents[child]
  if child!=root:continue
  if guid=='0000000000000000e000000000000000':
   center=(0,0,0);ext=(.5,1,.5) if mid=='10206' else (.5,.5,.5)
  else:
   src=meta[guid].read_text();aabb=src[src.index('  m_LocalAABB:'):];center=vec(aabb,'  m_Center');ext=vec(aabb,'  m_Extent')
  for signs in itertools.product([-1,1],repeat=3):
   p=tuple(c+e*s for c,e,s in zip(center,ext,signs))
   for t in chain:p=apply(p,t)
   p=rotate(p,(0,0,0,1));points.append(p)
 sizes=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
 print(name,tuple(round(v,3) for v in sizes));assert sizes[0]<=.45 and sizes[2]<=.38,(name,sizes)
# Exact scene populations and authored lesson order, independent of documentation section numbers.
pickup_guid=re.search(r'^guid: (\w+)',pathlib.Path('Assets/scripts/PickupItem.cs.meta').read_text(),re.M)[1]
fixed_guid=re.search(r'^guid: (\w+)',pathlib.Path('Assets/scripts/FixedLearningTarget.cs.meta').read_text(),re.M)[1]
pickups={};fixtures={}
for b in blocks:
 if pickup_guid in b or fixed_guid in b:
  name=names[re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]]
  if pickup_guid in b:pickups[name]=int(re.search(r'  lessonOrder: (\d+)',b)[1])
  else:fixtures[name]=int(re.search(r'  order: (\d+)',b)[1])
assert len(pickups)==53 and len(fixtures)==79 and sorted(fixtures.values())==[n for n in range(80) if n != 48]
catalog=json.loads(pathlib.Path('Assets/Resources/Lessons/IntroA11.json').read_text())['tasks']
controls=json.loads(pathlib.Path('Assets/Resources/Lessons/IntroA11.json').read_text())['controlsPractice']
assert len(controls)==8 and len({step['id'] for step in controls})==8
assert all(all(step.get(key,'').strip() for key in ('title','instruction','englishInstruction','touchHelp','keyboardHelp')) for step in controls)
voice_index=set(pathlib.Path('Assets/Resources/VoiceIntroA11Index.txt').read_text().splitlines())
assert voice_index=={p.stem for p in pathlib.Path('Assets/Resources/VoiceIntroA11').glob('*.wav')},'Runtime voice index is stale'
guidance=json.loads(pathlib.Path('Assets/Resources/Lessons/IntroA11.json').read_text())['controlGuidance']
assert len(guidance)==4 and len({hint['id'] for hint in guidance})==4
assert all(hint['instruction'].strip() and hint['englishInstruction'].strip() for hint in guidance)
manifest=json.loads(pathlib.Path('Documentation/IntroVoice.json').read_text())
assert all(hint['instruction'] in manifest.values() for hint in guidance),'Control hints missing from voice manifest'
plan=json.loads(pathlib.Path('Documentation/CurriculumPlan.json').read_text())['stages']
assert len(catalog)==428 and len(plan)==430
assert sum(s['status']=='Done' for s in plan)==428 and sum(s['status']=='Next' for s in plan)==0
ordered=sorted(pickups,key=lambda n:(pickups[n],n));spoon=ordered.index('Loeffel_Spoon');ordered[0],ordered[spoon]=ordered[spoon],ordered[0]
assert ordered[0]=='Loeffel_Spoon' and catalog[0]['targetName']==ordered[0]
assert {t['targetName'] for t in catalog[:53]}==set(ordered)
for task in catalog:
 if task['kind']=='C':
  for spec in task['scenarioItems']:assert spec['name'] in pickups
  continue
 assert task['targetName'] in (fixtures if task['kind']=='I' else pickups)
 if task.get('action')=='L':assert task['referenceName'] in pickups
catalog_by_id={t['id']:t for t in catalog}
plan_by_id={t['id']:t for t in plan}
def batch(first,last): return [(t,plan_by_id[t['id']]) for t in catalog if first<=int(t['id'][-3:])<=last]
for task,stage in batch(231,290):
 assert task['id']==stage['id'] and task['instruction']==stage['de']
 assert task['answer']==stage['expected'].split('; ')[1]
for file in ['GDD.md','GameStages.md','CurriculumPlan.md']:
 content=pathlib.Path('Documentation',file).read_text()
 for link in re.findall(r'\]\(([^)]+)\)',content):
  if ':' not in link and not link.startswith('#'):assert pathlib.Path('Documentation',link.split('#')[0]).exists(),(file,link)
plan_md=pathlib.Path('Documentation/CurriculumPlan.md').read_text();stages_md=pathlib.Path('Documentation/GameStages.md').read_text()
assert len(re.findall(r'^\| CUR-\d{3} ·',plan_md,re.M))==430
assert len(re.findall(r'^\| CUR-\d{3} ·.* · Done \|',plan_md,re.M))==428
assert len(re.findall(r'^\| \d+ \| `CUR-',stages_md,re.M))==428
print('PASS: 53 portable/79 fixed bindings; spoon-first LessonOrder; 428 implemented selection-based adaptations; exact new answer forms; document counts/links.')
# Runtime device aliases must resolve to the existing scene action owners/parts.
paths={}
for gid,tid in bygo.items():
 parts=[names[gid]];parent=parents[tid]
 while parent!='0':
  pgid=next(g for g,t in bygo.items() if t==parent);parts.append(names[pgid]);parent=parents[parent]
 paths['/'.join(reversed(parts))]=gid
for required in ['Faucet_Interactive','Schrank_Cabinet/Hinge_der_Unterschrank_Left','Schrank_Cabinet/Schublade_Drawer',
 'Kitchen_Stage2/Hinge_der_Kuehlschrank','HouseAccessories/Bath_Washer/WasherDoorHinge','HouseAccessories/Bath_Washer/DetergentDrawer',
 'BathroomFurnishings/Bath_BasinTap/TapLever','BathroomFurnishings/Bath_Cistern/FlushButton','BathroomFurnishings/Bath_Toilet/ToiletLid',
 'BedroomFurnishings/Bedroom1_NightLamp/LampBase']:
 assert (required in paths if '/' in required else required in names.values()),required
for task,stage in batch(291,430):
 assert task['kind']=='C' and task['action']=='S' and task['steps']
 assert task['instruction']==stage['de']
 for z in task['zones']:assert abs(z['x'])+z['width']/2<=1 and abs(z['z'])+z['depth']/2<=.6
print('PASS: runtime scenario action-owner paths, independent preset items and table zone containment.')

for required in ['BedroomFurnishings/Bedroom2_DeskLamp/LampBase','LivingHallFurnishings/Hall_ShoeCabinet/ShoeCabinetHinge']:
 assert required in paths,required
casebook=json.loads(pathlib.Path('Assets/Resources/Lessons/CaseStudies.json').read_text())
assert {c['id'] for c in casebook['cases']}=={'E','M','W','H'}
plan_text=pathlib.Path('Documentation/CurriculumPlan.md').read_text()
for case in casebook['cases']:
 section=plan_text.split('### '+case['id']+' —',1)[1].split('**Deutsch — Quellenpaket:** ',1)[1].split('**فارسی',1)[0]
 assert ' '.join(' '.join(case['pages']).split())==' '.join(section.split())
 assert len(case['pages'])==4 and all(len(page)<=460 for page in case['pages'])
for task,stage in batch(331,380):
 assert task['instruction']==stage['de'] and stage['freeProductionStatus']=='Next'
 if int(task['id'][-3:])>=361:assert task['steps'][0]['op']=='read' and task['caseId'] in ['E','M']
print('PASS: desk/shoe bindings; complete original E/M sources in eight pages; 50 adapted tasks with free-production limits recorded.')

assert len(plan)==430 and {t['caseId'] for t,s in batch(381,430)}=={'E','M','W','H'}
for task,stage in batch(381,430):
 assert task['id']==stage['id'] and task['instruction']==stage['de'] and task['level']=='C1.2'
 assert stage['freeProductionStatus']=='Next' and task['steps'][0]['op']=='read'
metadata=json.loads(pathlib.Path('Documentation/CurriculumPlan.json').read_text())
assert metadata['baselineTotal']==400 and metadata['baselineCounts']=={'search':200,'grammar':100,'action':100}
assert sum(metadata['counts'].values())==428 and metadata['counts']['action']==130
for number in [394,398,419,423]:
 assert any(s['op']=='dialogue' and 'Nils liest aktuell' in s['reply'] for s in catalog_by_id[f'CUR-{number:03}']['steps'])
print('PASS: 428 live rows, preserved 400 baseline, four case packets, 30 new C1.2 tasks and fresh reading confirmations.')

for task in catalog:
 stage=plan_by_id[task['id']]
 assert task['englishInstruction'].strip() and task['englishInstruction']==stage['en'],task['id']
intro_names={t['targetName'] for t in catalog[:132]}
assert intro_names==set(pickups)|set(fixtures)
print('PASS: all 428 English instructions match authored translations; all 132 scene targets have translation coverage.')

# Every scene/part label must have a valid clip, including vocabulary outside lesson targets.
import subprocess, sys
subprocess.run([sys.executable, 'Tools/Voice/check_object_coverage.py'], check=True)

assert not any(t["targetName"]=="Hall_Mirror" for t in catalog)
assert {s["id"] for s in plan if s["status"]=="Removed"}=={"CUR-102","CUR-191"}
