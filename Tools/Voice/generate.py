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
from datetime import date, datetime
from zoneinfo import ZoneInfo

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
    parser.add_argument('--pack', choices=('legacy', 'intro'), default='legacy')
    parser.add_argument('--voice', help='Exact provider voice ID; defaults to the selected pack voice.')
    parser.add_argument('--rate', default='-8%')
    parser.add_argument('--jobs', type=int, default=2)
    parser.add_argument('--document-date', type=date.fromisoformat,
                        default=datetime.now(ZoneInfo('Europe/Berlin')).date())
    parser.add_argument('--manifest', type=Path)
    parser.add_argument('--destination', type=Path)
    parser.add_argument('--audit-only', action='store_true', help='Validate installed clips without network, synthesis or writes.')
    args = parser.parse_args()
    intro = args.pack == 'intro'
    expected_manifest = ROOT / 'Documentation' / ('IntroVoice.json' if intro else 'Voice.json')
    expected_destination = ROOT / 'Assets/Resources' / ('VoiceIntroA11' if intro else 'Voice')
    args.manifest = args.manifest or expected_manifest
    args.destination = args.destination or expected_destination
    args.voice = args.voice or ('de-DE-TanjaNeural' if intro else 'de-DE-KatjaNeural')
    manifest = json.loads(args.manifest.read_text())
    if args.audit_only:
        missing, invalid = [], []
        for key, text in manifest.items():
            value = 2166136261
            encoded = text.encode('utf-16-le')
            for index in range(0, len(encoded), 2):
                value = ((value ^ int.from_bytes(encoded[index:index + 2], 'little')) * 16777619) & 0xffffffff
            if key != f'{value:08x}':
                raise ValueError('Manifest key does not match GermanVoice.Key: ' + key)
            path = args.destination / (key + '.wav')
            if not path.exists():
                missing.append(key)
            else:
                try:
                    validate(path)
                except (ValueError, wave.Error, EOFError):
                    invalid.append(key)
        print(f'Voice audit: {len(manifest)} phrases, {len(manifest) - len(missing) - len(invalid)} valid, '
              f'{len(missing)} missing, {len(invalid)} invalid. No files changed.')
        return
    if args.manifest.resolve() != expected_manifest.resolve() or args.destination.resolve() != expected_destination.resolve():
        raise ValueError('Generation only supports the configured legacy/intro pack; other paths are audit-only.')
    import edge_tts
    profile = {'voice': args.voice, 'rate': args.rate, 'sample_rate': 24000,
               'generator': 'edge-tts 7.3.1', 'clips': len(manifest)}
    if intro:
        profile.update(course='intro-a1-1', installed_voice=args.voice,
                       resource_folder='VoiceIntroA11', status='Installed',
                       updated=args.document_date.isoformat(), previous_requested_voice='Sonna (unavailable)',
                       selection='User authorized another available German voice')
    profile_path = ROOT / 'Documentation' / ('IntroVoiceProfile.json' if intro else 'VoiceProfile.json')
    installed = json.loads(profile_path.read_text()) if profile_path.exists() else {}
    reuse_installed = all(installed.get(key) == profile[key] for key in ('voice', 'rate', 'sample_rate', 'generator'))
    identity = {key: profile[key] for key in ('voice', 'rate', 'sample_rate', 'generator')}
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
            existing = args.destination / (key + '.wav')
            if not wav.exists() and reuse_installed and existing.exists():
                try:
                    validate(existing)
                    shutil.copy2(existing, wav)
                except (ValueError, wave.Error, EOFError):
                    pass
            if not wav.exists():
                for attempt in range(3):
                    try:
                        # WebSocket keepalives can prevent the transport receive timeout from firing.
                        # Bound the entire phrase, including a missing final audio/end frame.
                        await asyncio.wait_for(
                            edge_tts.Communicate(text, args.voice, rate=args.rate,
                                                 connect_timeout=15, receive_timeout=45).save(str(mp3)),
                            timeout=75)
                        subprocess.run(['/usr/bin/afconvert', '-f', 'WAVE', '-d', 'LEI16@24000', str(mp3), str(wav)],
                                       check=True, capture_output=True)
                        validate(wav)
                        break
                    except Exception as error:
                        print(f'Retry {attempt + 1}/3 for {key}: {type(error).__name__}', flush=True)
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
    destination = args.destination
    destination.mkdir(parents=True, exist_ok=True)
    folder_meta = Path(str(destination) + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nfolderAsset: yes\n')
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
        temporary_profile = profile_path.with_suffix('.json.tmp')
        temporary_profile.write_text(json.dumps(profile, ensure_ascii=False, indent=2) + '\n')
        temporary_profile.replace(profile_path)
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
