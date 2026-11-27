#!/usr/bin/env python3
"""Audit every authored whole/part name against the active offline voice pack.
Use --sync-manifest before generate.py when scene vocabulary has changed.
"""
import argparse
import json
from pathlib import Path
import re
import sys
import wave
from generate import ROOT, validate


def key(text):
    value = 2166136261
    encoded = text.encode('utf-16-le')
    for index in range(0, len(encoded), 2):
        value = ((value ^ int.from_bytes(encoded[index:index + 2], 'little')) * 16777619) & 0xffffffff
    return f'{value:08x}'


def object_names():
    names = set()
    for pattern in ('*.unity', '*.prefab'):
        for path in (ROOT / 'Assets').rglob(pattern):
            data = path.read_bytes()
            # Imported binary Unity assets have no searchable YAML fields.
            if not data.startswith(b'%YAML'):
                continue
            for scalar in re.findall(r'^  displayName: (.+)$', data.decode('utf-8'), re.M):
                if scalar.startswith('"'):
                    # Unity YAML uses hexadecimal escapes unsupported by JSON.
                    scalar = json.loads(re.sub(r'\\x([0-9a-fA-F]{2})', r'\\u00\1', scalar))
                elif scalar.startswith("'"):
                    scalar = scalar[1:-1].replace("''", "'")
                if scalar.strip():
                    names.add(scalar)
    # Runtime action owners can also expose names to the identification button.
    registry = (ROOT / 'Assets/scripts/LessonDeviceRegistry.cs').read_text()
    names.update(re.findall(r'"((?:der|die|das) [^"\n]+)"', registry))
    return names


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--sync-manifest', action='store_true')
    args = parser.parse_args()
    path = ROOT / 'Documentation/IntroVoice.json'
    manifest = json.loads(path.read_text())
    names = object_names()
    added = []
    for name in sorted(names):
        hashed = key(name)
        if hashed in manifest and manifest[hashed] != name:
            raise ValueError('Voice hash collision: ' + name)
        if hashed not in manifest:
            added.append(name)
            if args.sync_manifest:
                manifest[hashed] = name
    if args.sync_manifest:
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
        print(f'Manifest: {len(names)} distinct scene/runtime names; {len(added)} added; {len(manifest)} phrases total.')
        return
    errors = []
    for name in sorted(names):
        hashed = key(name)
        if manifest.get(hashed) != name:
            errors.append('Missing manifest name: ' + name)
            continue
        try:
            validate(ROOT / 'Assets/Resources/VoiceIntroA11' / (hashed + '.wav'))
        except (OSError, ValueError, EOFError, wave.Error) as error:
            errors.append(name + ': ' + str(error))
    for error in errors:
        print(error, file=sys.stderr)
    print(f'Object voice coverage: {len(names)} names; {len(errors)} missing/invalid.')
    for name in ('die Dunstabzugshaube', 'die Mikrowelle'):
        assert name in names, 'Expected scene object missing: ' + name
        print(name + ' -> ' + key(name) + '.wav')
    if errors:
        sys.exit(1)


if __name__ == '__main__':
    main()
