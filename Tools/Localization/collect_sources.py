import json,re,pathlib
root=pathlib.Path(__file__).resolve().parents[2]
c=json.loads((root/'Assets/Resources/Lessons/IntroA11.json').read_text())
sources={t['englishInstruction'] for t in c['tasks']}
for step in c['controlsPractice']:
 sources.update(step[k] for k in ['title','englishInstruction','touchHelp','keyboardHelp'])
s=(root/'Assets/scripts/RoomExperience.cs').read_text()
# These are player-facing text literals, not source code. Include the full reference and menus.
for part in [s[s.index('private readonly string[] tutorialTitles'):s.index('public int TutorialStepCount')],s[s.index('private string TutorialText'):s.index('public Rect TutorialHighlightRect')],s[s.index('private string TutorialAction'):s.index('// Prefer a roomy side')]]:
 for literal in re.findall(r'"((?:[^"\\]|\\.)*)"',part):
  val=json.loads('"'+literal+'"')
  if val.strip():sources.add(val)
extra=['Choose your support language','Language','German + translation','German only','Keep showing German and your language for the next 50 tasks?','Continue with both languages','Continue in German only','Back','Next','Skip','Finish','Start lesson','German instructions and your selected translation appear together in the centre, then disappear. Every 50 completed tasks, choose whether to keep both languages. Change language or translation in Settings.','Your support language','Settings','How to play','Controls reference','Start / Continue','Welcome! Start the course and discover the objects.','Well done! You have completed the course. You can keep practising freely.','Keep German task text visible even when narration is selected. Translation never gives the answer or completes a task.']
sources.update(extra)
sources.update(["You're ready!",'Nice work! You can now explore the room and start your lesson.'])
sources.add('Stage')
sources.update(['BASICS','LESSON TOOLS','LATER TASKS','MENU & SOUND'])
sources.update(['Menu','Pause','Text & voice','Text only','Voice only','Both','Voice','Test voice','I cannot hear sound','Sound works','Save & back'])
sources.update(['Repeat instruction', 'Take your time', 'Hold to read · X or outside to close', 'Hold and scroll to read · X or outside to close', 'Alt + scroll to read · X or outside to close', 'Restart course', 'Controls: touch', 'Controls: mouse + keyboard', 'Music on', 'Music off', 'Restart this course?', 'Your saved course progress will be cleared.', 'Keep my progress', 'Start over'])
def collect_sources():
 return sorted(sources)

if __name__ == '__main__':
 print(json.dumps(collect_sources(),ensure_ascii=False,indent=2))
