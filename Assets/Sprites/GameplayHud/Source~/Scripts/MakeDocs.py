"""Builds Overview.png (every graphic on a checkerboard) and README.md with the table of files."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
manifest = json.loads((ROOT / 'Manifest.json').read_text(encoding='utf-8'))


def Pascal(name):
    return ''.join(part[:1].upper() + part[1:] for part in name.split('-'))


USE = {k: v for k, v in {
    'bar-body-red': 'Body of the commander bar of the red team (398x96), at the left edge of the bar. Pieces of the bar: the game hides stripes one by one.',
    'bar-body-blue': 'Body of the commander bar of the blue team (398x96), at the right edge of the bar (x222).',
    'bar-stripe-red': 'One stripe of the red bar (57x96): one living member of the team. Five of them at x372, 403, 432, 463, 494; the farthest from the name goes first.',
    'bar-stripe-blue': 'One stripe of the blue bar (57x96), mirrored: x191, 160, 131, 100, 69.',
    'scrollbar-track': 'Track of the vertical scrollbar (8x480), flat, stretches to the length of the view.',
    'scrollbar-handle': 'Handle of the vertical scrollbar (16x64), white, stretches to its length.',
    'bar-commander-red': 'The whole commander bar of the red team in one piece (reference; the game builds it from body and stripes). Top left corner, x24 y24.',
    'bar-commander-blue': 'The whole commander bar of the blue team in one piece (reference). Top right corner, x1276 y24, mirrored.',
    'pip-commander-on': 'Commander health point, full (white). 14 px, gap 6 px, the first at x+20 y+68.',
    'pip-commander-off': 'Commander health point, lost.',
    'timer-bar-track': 'Track of the time bar in the turn banner (x+26 y+92 inside the banner).',
    'timer-bar-fill': 'Fill of the time bar (Image Filled, horizontal). Tinted with the team color, below 10 s with #ff4d4d.',
    'panel-banner': 'Turn banner (x690 y24): turn number, player name, seconds, time bar.',
    'panel-hint': 'Hint panel (x560 y148).',
    'panel-log': 'Battle log in the command column (y212), full variant.',
    'panel-log-short': 'Battle log when the ability button sits above it (y288).',
    'panel-log-collapsed': 'Battle log collapsed to its title (the Hide/Show button).',
    'panel-settings': 'Settings panel of the pause menu and the end screen (840x500, middle of the screen).',
    'panel-confirm': 'The question "Are you sure?" of the pause menu (840x280).',
    'button-menu-wide': 'Wide menu button of the settings rows: resolution and difficulty (760x72).',
    'slider-track': 'Track of a settings slider (460x8).',
    'slider-fill': 'Fill of a settings slider, white, tinted with the accent color.',
    'slider-handle': 'Handle of a settings slider (16x32).',
    'panel-callout': 'Damage preview next to an enemy in range (x944 y404, 340x126).',
    'panel-winner': 'Winner banner of the end screen (x580 y190).',
    'panel-summary': 'Game summary of the end screen (x580 y318).',
    'panel-info': 'Unit and tile details (bottom left corner, x24 y848), the slant is parallel to the board edge.',
    'panel-team': 'Team panel with the cards (bottom right corner, x1024 y848), mirrored slant.',
    'button-primary-red': 'Main button (Surrender) in the red team turn, 244x64.',
    'button-primary-blue': 'Main button (Surrender) in the blue team turn.',
    'button-call': 'Call button (128x64).',
    'button-call-active': 'Call button in the call mode (Cancel), pink edge.',
    'button-ability': 'Unit ability button (Q), 384x64, under the row of the main buttons.',
    'button-menu': 'Button of the pause menu and the end screen (372x72).',
    'button-menu-selected': 'Selected or first button, pink edge.',
    'overlay-dim': 'Full screen dimming under the pause menu and the end screen.',
    'slot-frame': 'Frame of a team card (90x114), normal and already moved.',
    'slot-frame-selected-red': 'Frame of the selected card or the card to call, red team.',
    'slot-frame-selected-blue': 'Frame of the selected card or the card to call, blue team.',
    'slot-frame-reserve': 'Frame of the reserve (dashed, empty slot).',
    'slot-cross-out': 'Strike over the card of a killed unit: one dashed line from the bottom left to the top right (90x114), in the color of the dashed frame of the reserve (white, 24%); the card under it is greyed out like the reserve.',
    'pip-small-red': 'Health point under a card, red (5 px, gap 2 px).',
    'pip-small-blue': 'Health point under a card, blue.',
    'pip-small-off': 'Health point under a card, lost.',
    'badge-accent': 'Flat badge background: Hint, NEW (stretched to the text).',
    'badge-muted': 'Flat badge background: MOVED.',
    'keycap': 'Key (Space, C, Esc), text #111111, stretched to the text.',
    'icon-frame': 'Background of a tag icon in the info panel (36x36) and in the damage preview (22x22).',
    'log-tick-red': 'Battle log entry marker, red.',
    'log-tick-blue': 'Battle log entry marker, blue.',
    'divider-line': 'Turn header line in the battle log (stretched).',
}.items()}
USE = {Pascal(k): v for k, v in USE.items()}
TAGS = ['backstab', 'binary', 'caller', 'confuser', 'gunman', 'impair', 'impale', 'piercing', 'provoke', 'recovery',
        'spy', 'swift', 'tough', 'teleporter', 'zeal']
for t in TAGS:
    USE[Pascal('icon-tag-' + t)] = f'Tag icon: {t.capitalize()} (22 px in the layout).'

# ---------- overview
try:
    font = ImageFont.truetype('arial.ttf', 14)
except OSError:
    font = ImageFont.load_default()


def Checker(w, h, c=10):
    im = Image.new('RGBA', (w, h), (58, 58, 58, 255))
    d = ImageDraw.Draw(im)
    for y in range(0, h, c):
        for x in range(0, w, c):
            if (x // c + y // c) % 2:
                d.rectangle([x, y, x + c - 1, y + c - 1], fill=(74, 74, 74, 255))
    return im


items = []
for m in manifest:
    if m['name'] == 'OverlayDim':
        continue
    img = Image.open(ROOT / 'Png' / m['category'] / f"{m['name']}.png").convert('RGBA')
    w, h = m['w'], m['h']
    f = 1.0 if max(w, h) >= 200 else (4.0 if max(w, h) < 60 else 2.0)   # small items are enlarged
    f = min(f, 560 / w)
    tw, th = max(1, round(w * f)), max(1, round(h * f))
    items.append((m['name'], img.resize((tw, th), Image.LANCZOS if f < 2 else Image.NEAREST),
                  f"{w}x{h} -> {img.width}x{img.height}px"))

W = 1900
x = y = 20
rowh = 0
placed = []
for name, im, cap in items:
    if x + max(im.width, 150) + 20 > W:
        x = 20
        y += rowh + 54
        rowh = 0
    placed.append((x, y, name, im, cap))
    x += max(im.width, 150) + 24
    rowh = max(rowh, im.height)
H = y + rowh + 60
sheet = Checker(W, H)
d = ImageDraw.Draw(sheet)
for x, y, name, im, cap in placed:
    sheet.alpha_composite(im, (x, y))
    d.text((x, y + im.height + 6), name, fill=(235, 235, 235, 255), font=font)
    d.text((x, y + im.height + 22), cap, fill=(160, 160, 160, 255), font=font)
sheet.convert('RGB').save(ROOT / 'Overview.png')

# ---------- README
rows = []
titles = {'Bars': 'Bars', 'Panels': 'Panels', 'Buttons': 'Buttons', 'Menu': 'Pause menu and end screen',
          'Slots': 'Team cards (frames and health points)', 'Elements': 'Small elements', 'Icons': 'Tag icons'}
for cat, title in titles.items():
    rows.append(f'\n### {title}\n\n| File | SVG (layout px) | PNG (px) | Use |\n|---|---|---|---|')
    for m in manifest:
        if m['category'] != cat:
            continue
        pw, ph = round(m['w'] * m['scale']), round(m['h'] * m['scale'])
        rows.append(f"| `{m['name']}` | {m['w']}x{m['h']} | {pw}x{ph} | {USE.get(m['name'], m['note'])} |")

readme = f"""# Gameplay HUD graphics

Every graphic of the approved gameplay mockup (artifact version 13) in its own file: **{len(manifest)} elements**, each as an SVG and as a high resolution PNG.

- `Svg/<Category>/*.svg`: vector sources (sizes are units of the 1920x1080 layout).
- `Png/<Category>/*.png`: export at **2x**, which is one pixel per pixel on a 4K monitor (3840x2160); on Full HD it is an exact 2:1 downscale.
- `Overview.png`: every element on one sheet.
- `Scripts/`: generator (`BuildSvgs.py`), PNG renderer (`RenderPngs.py`, headless Edge) and this document's generator (`MakeDocs.py`).

Rules: **there is no text in the graphics** (all text is TextMeshPro, font JustinFont11Bold), no element is cut or sliced (no 9-slice), and elements are layered when needed (track and fill of the time bar, card frame and the card art from the game). Flat rectangles (badges, key, icon frame) stretch to the text without distortion because they have no slanted corners.

Not drawn here: unit cards, the board and the characters (game art).

Colors: red `#ff1b47` (dark `#641020`), blue `#14c8d8` (dark `#0a5861`), accent `#d72e66`, warning `#ff4d4d`, healing `#c9f2d4`, panel `#121212` at 86% opacity.

## Regeneration

```bash
python Scripts/BuildSvgs.py     # SVG + Manifest.json
python Scripts/RenderPngs.py    # PNG (headless Edge, about 2 s)
python Scripts/MakeDocs.py      # Overview.png and this README
```

The PNG scale is the `SCALE` constant in `BuildSvgs.py`. The tag icons are traced from the 42x42 originals of the game (`Assets/Sprites/ChoiceScreen/Tag*.png`).

## Files
{chr(10).join(rows)}
"""
(ROOT / 'README.md').write_text(readme, encoding='utf-8')
print('ok', len(manifest))
