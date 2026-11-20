#!/usr/bin/env python3
"""Regenerate public lesson phrases, stage/validate every clip, then replace the WAVs.
Install requirements.txt in an isolated environment first. Generation needs internet;
the Unity game itself continues to use offline audio assets.
"""
import argparse
import asyncio
from array import array
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import wave
import uuid

import edge_tts

ROOT = Path(__file__).resolve().parents[2]


def validate(path):
    with wave.open(str(path), 'rb') as recording:
        if recording.getnchannels() != 1 or recording.getsampwidth() != 2 or recording.getframerate() != 24000:
            raise ValueError('Expected mono PCM16 at 24000 Hz: ' + str(path))
        frames = recording.readframes(recording.getnframes())
        samples = array('h', frames)
        if len(frames) < 4800 or sum(value * value for value in samples) < 2500 * len(samples):
            raise ValueError('Empty, short or silent recording: ' + str(path))


async def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--voice', default='de-DE-KatjaNeural')
    parser.add_argument('--rate', default='-8%')
    parser.add_argument('--jobs', type=int, default=2)
    args = parser.parse_args()
    manifest = json.loads((ROOT / 'Documentation/Voice.json').read_text())
    profile = {'voice': args.voice, 'rate': args.rate, 'sample_rate': 24000,
               'generator': 'edge-tts 7.3.1', 'clips': len(manifest)}
    profile_path = ROOT / 'Documentation/VoiceProfile.json'
    installed = json.loads(profile_path.read_text()) if profile_path.exists() else {}
    reuse_installed = all(installed.get(key) == profile[key] for key in ('voice', 'rate', 'sample_rate', 'generator'))
    identity = {key: value for key, value in profile.items() if key != 'clips'}
    fingerprint = hashlib.sha256(json.dumps(identity, sort_keys=True).encode()).hexdigest()[:12]
    cache = Path(tempfile.gettempdir()) / ('deutschroom-voice-' + fingerprint)
    cache.mkdir(parents=True, exist_ok=True)
    semaphore = asyncio.Semaphore(max(1, min(args.jobs, 3)))
    completed = 0

    async def generate(key, text):
        nonlocal completed
        async with semaphore:
            digest = hashlib.sha256(text.encode()).hexdigest()[:12]
            wav = cache / (key + '-' + digest + '.wav')
            mp3 = wav.with_suffix('.mp3')
            if wav.exists():
                try:
                    validate(wav)
                except (ValueError, wave.Error, EOFError):
                    wav.unlink()
            existing = ROOT / 'Assets/Resources/Voice' / (key + '.wav')
            if not wav.exists() and reuse_installed and existing.exists():
                try:
                    validate(existing)
                    shutil.copy2(existing, wav)
                except (ValueError, wave.Error, EOFError):
                    pass
            if not wav.exists():
                for attempt in range(3):
                    try:
                        await edge_tts.Communicate(text, args.voice, rate=args.rate,
                                                  connect_timeout=15, receive_timeout=45).save(str(mp3))
                        subprocess.run(['/usr/bin/afconvert', '-f', 'WAVE', '-d', 'LEI16@24000', str(mp3), str(wav)],
                                       check=True, capture_output=True)
                        validate(wav)
                        break
                    except Exception:
                        wav.unlink(missing_ok=True)
                        if attempt == 2:
                            raise
                        await asyncio.sleep(3 * (attempt + 1))
                await asyncio.sleep(.25)
            completed += 1
            if completed % 25 == 0 or completed == len(manifest):
                print(f'Validated {completed}/{len(manifest)} neural clips', flush=True)
            return key, wav

    staged = await asyncio.gather(*(generate(key, text) for key, text in manifest.items()))
    # Network/conversion failures above leave the entire original voice pack untouched.
    destination = ROOT / 'Assets/Resources/Voice'
    backup = Path(tempfile.mkdtemp(prefix='deutschroom-previous-voice-'))
    for key, _ in staged:
        if (destination / (key + '.wav')).exists():
            shutil.copy2(destination / (key + '.wav'), backup / (key + '.wav'))
    try:
        for key, wav in staged:
            target = destination / (key + '.wav')
            if not target.exists() or target.read_bytes() != wav.read_bytes():
                shutil.copyfile(wav, target)
            meta = Path(str(target) + '.meta')
            if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n')
        (ROOT / 'Documentation/VoiceProfile.json').write_text(json.dumps(profile, ensure_ascii=False, indent=2) + '\n')
    except BaseException:
        for key, _ in staged:
            target = destination / (key + '.wav')
            if (backup / (key + '.wav')).exists():
                shutil.copyfile(backup / (key + '.wav'), target)
            else:
                target.unlink(missing_ok=True)
                Path(str(target) + '.meta').unlink(missing_ok=True)
        raise
    print(f'Installed {len(staged)} neural recordings; Unity filenames and .meta GUIDs preserved.', flush=True)
    print(f'Previous recordings backed up at {backup}', flush=True)


if __name__ == '__main__':
    asyncio.run(main())
