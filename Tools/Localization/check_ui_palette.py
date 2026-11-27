"""Check final opaque UI text colours, not a claim of WCAG/device conformance."""
import re
from pathlib import Path

root=Path(__file__).resolve().parents[2]
source=(root/'Assets/scripts/RoomExperience.Visuals.cs').read_text()
def token(name):
    value=re.search(r'Color '+name+r' = new Color\(([^)]+)\)',source)[1]
    return tuple(float(c.strip().removesuffix('f')) for c in value.split(',')[:3])
def luminance(rgb):
    linear=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in rgb]
    return sum(c*w for c,w in zip(linear,(.2126,.7152,.0722)))
def contrast(foreground,background):
    a,b=sorted((luminance(foreground),luminance(background)))
    return (b+.05)/(a+.05)
for name in ('UiText','UiMuted','UiAccent'):
    ratio=contrast(token(name),token('UiSurface'))
    assert ratio>=4.5,(name,ratio)
    print(f'{name} on UiSurface: {ratio:.2f}:1')
# The notice close control must keep a generous hit area, independent of its icon.
assert re.search(r'NoticeCloseRect => new Rect\([^;]*,44,44\)',source)
assert 'size<=12' not in source, 'Long notices must scroll instead of shrinking to tiny type'
print('PASS: opaque palette text contrast; 44-unit close target; fixed readable notice styles.')
