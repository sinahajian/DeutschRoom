"""Build the mixed-script IMGUI font from OFL-licensed Noto sources (fonttools required)."""
from pathlib import Path
from fontTools.merge import Merger
from fontTools.ttLib import TTFont

root = Path(__file__).resolve().parents[2]
fonts = root / 'Assets/Resources/Localization/Fonts'
merged = Merger().merge([str(root / 'Tools/Localization/Fonts/NotoNaskhArabic-Regular.ttf'), str(root / 'Tools/Localization/Fonts/NotoSans-Regular.ttf')])
# Modified font uses its own name; keep the upstream copyright/license records.
for record in merged['name'].names:
    if record.nameID in (1, 4, 6, 16):
        name = 'DeutschRoom Support' if record.nameID != 6 else 'DeutschRoomSupport'
        record.string = name.encode(record.getEncoding())
    elif record.nameID == 3:
        record.string = 'DeutschRoomSupport-1'.encode(record.getEncoding())
# The runtime already arranges visual lines. Alias RTL codepoints to private-use
# characters (bidi class L) to prevent a second native bidi pass reversing them.
# Keep original cmap entries so source text and font inspection remain meaningful.
def display_codepoint(value):
    if 0x0600 <= value <= 0x08FF:
        return 0xE000 + value - 0x0600
    if 0xFB50 <= value <= 0xFEFF:
        return 0xE400 + value - 0xFB50
    return value

original = merged.getBestCmap().copy()
for table in merged['cmap'].tables:
    if table.isUnicode() and table.format in (4, 12):
        for codepoint, glyph in original.items():
            alias = display_codepoint(codepoint)
            if alias != codepoint:
                table.cmap[alias] = glyph
merged.save(fonts / 'DeutschRoomSupport.ttf')
coverage = TTFont(fonts / 'DeutschRoomSupport.ttf').getBestCmap()
for c in 'ABCäöüїєґپچژگ\ufeb3':
    assert ord(c) in coverage, f'Missing glyph: {c}'
print('PASS: merged font covers German, Ukrainian, Persian and Arabic presentation forms.')

import unicodedata
aliases = [(codepoint, display_codepoint(codepoint)) for codepoint in original if display_codepoint(codepoint) != codepoint]
for codepoint, alias in aliases:
    assert coverage[alias] == coverage[codepoint], f'Alias changed glyph: {codepoint:X}'
    assert unicodedata.bidirectional(chr(alias)) == 'L'
print(f'PASS: {len(aliases)} identical-glyph aliases cannot be reordered as RTL by native bidi.')
