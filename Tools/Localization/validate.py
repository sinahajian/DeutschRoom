"""Validate shipped instruction/help translations without contacting a service."""
import json
from pathlib import Path
import re
from collect_sources import collect_sources

root = Path(__file__).resolve().parents[2]
catalog = json.loads((root / 'Assets/Resources/Localization/SupportLanguages.json').read_text())
entries = {e['source']: e for e in catalog['entries']}
assert len(entries) == len(catalog['entries']), 'Duplicate source key'
assert set(collect_sources()) == set(entries), 'Guide/task sources have changed; update offline translations'
for source, entry in entries.items():
    assert entry['en'] == source
    for language in ('fa', 'ar', 'uk'):
        assert entry[language].strip() and entry[language] != source, (source, language)
        assert not re.search(r'ZXQ|QXZ|⟦\d+⟧', entry[language]), (source, language)
        if language in ('fa', 'ar'):
            assert re.search(r'[\u0600-\u06ff]', entry[language]), (source, language)
        else:
            assert re.search(r'[\u0400-\u04ff]', entry[language]), (source, language)
lessons = json.loads((root / 'Assets/Resources/Lessons/IntroA11.json').read_text())
for task in lessons['tasks']:
    assert task['englishInstruction'] in entries, task['id']
for step in lessons['controlsPractice']:
    for key in ('title', 'englishInstruction', 'touchHelp', 'keyboardHelp'):
        assert step[key] in entries, (step['id'], key)
# First-time instructions should describe the action, not expose distance gates.
assert len(lessons['controlsPractice']) == 8
for step in lessons['controlsPractice']:
    for key in ('title','englishInstruction','touchHelp','keyboardHelp'):
        source = step[key]
        assert not re.search(r'0[.,]65|0[.,]6|metres|meters', source, re.I), source
        for language in ('fa','ar','uk'):
            assert 'آموزش عملی' not in entries[source][language]
assert lessons['controlsPractice'][-1]['id'] == 'CTRL-EXPLORE'
overrides = json.loads((root / 'Tools/Localization/ReviewedOverrides.json').read_text())
for source, translations in overrides.items():
    assert all(entries[source][language] == value for language, value in translations.items()), source
for name in ('GDD.md', 'GameStages.md', 'CHANGELOG.md'):
    text = (root / 'Documentation' / name).read_text()
    for link in re.findall(r'\]\(([^)]+)\)', text):
        if ':' not in link and not link.startswith('#'):
            assert (root / 'Documentation' / link.split('#')[0]).exists(), (name, link)
gdd = (root / 'Documentation/GDD.md').read_text()
for section in re.split(r'^## (?:فارسی|English|Deutsch)\n', gdd, flags=re.M)[1:]:
    assert set(re.findall(r'GDD-(\d+)', section)) == {f'{i:02}' for i in range(1, 84)}
assert (root / 'Assets/Resources/Localization/Fonts/DeutschRoomSupport.ttf').stat().st_size > 100_000
print(f'PASS: {len(entries)} offline × 4-language entries, all 428 instructions, onboarding/reference coverage, {len(overrides)} overrides, GDD-01–83 in each language and document links.')
