"""Builds every gameplay HUD graphic as a separate SVG file (no text: all text is TextMeshPro in the game).

Sizes in the SVGs are sizes in the 1920x1080 layout (canvas units of the game). RenderPngs.py exports each one to a PNG at
SCALE (2 = one pixel per pixel on a 4K screen). All numbers come from the approved mockup (gameplay-mockup.html, version 13).
File and folder names are English PascalCase.
"""
import json
from pathlib import Path
import cv2

ROOT = Path(__file__).resolve().parent.parent
SVG = ROOT / 'Svg'
SCALE = 2
MANIFEST = []

CATEGORIES = {'bars': 'Bars', 'panels': 'Panels', 'buttons': 'Buttons', 'menu': 'Menu',
              'slots': 'Slots', 'elements': 'Elements', 'icons': 'Icons'}

# colors from the mockup
HOT, HOT_DEEP = '#ff1b47', '#641020'
COLD, COLD_DEEP = '#14c8d8', '#0a5861'
PINK, WARN, WHITE = '#d72e66', '#ff4d4d', '#ffffff'
PANEL, PANEL_A = '#121212', 0.86          # panel background: rgba(18,18,18,.86)
HI_A = 0.08                                # panel highlight: rgba(255,255,255,.08)


def Pascal(name):
    return ''.join(part[:1].upper() + part[1:] for part in name.split('-'))


def Fmt(v):
    return ('%.3f' % v).rstrip('0').rstrip('.')


def AttrsStr(attrs):
    return ' '.join(f'{k.replace("_", "-")}="{v}"' for k, v in attrs.items())


def Poly(points, **attrs):
    pts = ' '.join(f'{Fmt(x)},{Fmt(y)}' for x, y in points)
    return f'<polygon points="{pts}" {AttrsStr(attrs)}/>'


def Rect(x, y, w, h, **attrs):
    return f'<rect x="{Fmt(x)}" y="{Fmt(y)}" width="{Fmt(w)}" height="{Fmt(h)}" {AttrsStr(attrs)}/>'


def Write(category, name, w, h, body, view=None, scale=SCALE, note=''):
    """Writes one SVG. `view` is the viewBox size when it differs from the layout size (icons)."""
    folder = CATEGORIES[category]
    d = SVG / folder
    d.mkdir(parents=True, exist_ok=True)
    name = Pascal(name)
    vw, vh = view or (w, h)
    txt = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{Fmt(w)}" height="{Fmt(h)}" viewBox="0 0 {Fmt(vw)} {Fmt(vh)}">\n'
           f'{body}\n</svg>\n')
    (d / f'{name}.svg').write_text(txt, encoding='utf-8')
    MANIFEST.append({'category': folder, 'name': name, 'w': w, 'h': h, 'scale': scale, 'note': note})


def CutPoly(w, h, c):
    """Panel with the top right and bottom left corners cut (the clip-path of the mockup)."""
    return [(0, 0), (w - c, 0), (w, c), (w, h), (c, h), (0, h - c)]


def Panel(category, name, w, h, c=None, points=None, fill=PANEL, alpha=PANEL_A, top=None, note=''):
    pts = points or CutPoly(w, h, c)
    body = Poly(pts, fill=fill, fill_opacity=alpha)
    if top:  # 2 px strip on the top edge (the border-top clipped by the cut corner)
        body += '\n' + Poly([(0, 0), (w - c, 0), (w - c + 2, 2), (0, 2)], fill=top)
    Write(category, name, w, h, body, note=note)


# ------------------------------------------------------------------ commander bars
def CommanderBar(name, color, mirror):
    w, h, sh = 620, 96, 45
    shapes = [Poly([(0, 0), (398, 0), (398 - sh, h), (0, h)], fill=color)]
    for t in (417, 448, 477, 508, 539):
        shapes.append(Poly([(t, 0), (t + 12, 0), (t + 12 - sh, h), (t - sh, h)], fill=color))
    body = '\n'.join(shapes)
    if mirror:
        body = f'<g transform="translate({w} 0) scale(-1 1)">\n{body}\n</g>'
    Write('bars', name, w, h, body)


# the bar in pieces, so the game can hide the stripes one by one (a stripe is a living member of the team):
# the body (398 px) and one stripe (57 px), for each team; the stripes stand at x = 372, 403, 432, 463, 494 (red) in the 620 px bar
Write('bars', 'bar-body-red', 398, 96, Poly([(0, 0), (398, 0), (353, 96), (0, 96)], fill=HOT))
Write('bars', 'bar-body-blue', 398, 96, Poly([(0, 0), (398, 0), (398, 96), (45, 96)], fill=COLD))
Write('bars', 'bar-stripe-red', 57, 96, Poly([(45, 0), (57, 0), (12, 96), (0, 96)], fill=HOT))
Write('bars', 'bar-stripe-blue', 57, 96, Poly([(0, 0), (12, 0), (57, 96), (45, 96)], fill=COLD))
CommanderBar('bar-commander-red', HOT, False)
CommanderBar('bar-commander-blue', COLD, True)
Write('bars', 'pip-commander-on', 14, 14, Rect(0, 0, 14, 14, fill=WHITE))
Write('bars', 'pip-commander-off', 14, 14, Rect(0, 0, 14, 14, fill='#000000', fill_opacity=0.3))

# turn timer bar in the turn banner: 488 x 8 (track and fill; the fill is white, the game tints it)
Write('bars', 'timer-bar-track', 488, 8, Rect(0, 0, 488, 8, fill=WHITE, fill_opacity=0.14))
Write('bars', 'timer-bar-fill', 488, 8, Rect(0, 0, 488, 8, fill=WHITE))

# ------------------------------------------------------------------ panels
Panel('panels', 'panel-banner', 540, 112, 20)
Panel('panels', 'panel-hint', 800, 72, 14)
Panel('panels', 'panel-log', 384, 216, 16)
Panel('panels', 'panel-log-short', 384, 140, 16, note='battle log when the ability button sits above it')
Panel('panels', 'panel-log-collapsed', 384, 44, 16, note='battle log collapsed to its title')
Panel('panels', 'panel-settings', 840, 500, 20, note='settings of the pause menu and the end screen')
Panel('panels', 'panel-confirm', 840, 280, 20, note='the question "Are you sure?" of the pause menu')
Panel('panels', 'panel-callout', 340, 126, 14)
Panel('panels', 'panel-winner', 760, 112, 20)
Panel('panels', 'panel-summary', 760, 240, 20)
Panel('panels', 'panel-info', 872, 208, points=[(14, 0), (517, 0), (872, 208), (0, 208), (0, 14)])
Panel('panels', 'panel-team', 872, 208, points=[(354, 0), (857, 0), (872, 14), (872, 208), (0, 208)])

# ------------------------------------------------------------------ buttons
Panel('buttons', 'button-primary-red', 244, 64, 14, fill=HOT, alpha=1, top=WHITE)
Panel('buttons', 'button-primary-blue', 244, 64, 14, fill=COLD, alpha=1, top=WHITE)
Panel('buttons', 'button-call', 128, 64, 14, top=WHITE)
Panel('buttons', 'button-call-active', 128, 64, 14, fill=WHITE, alpha=HI_A, top=PINK, note='call mode: Cancel')
Panel('buttons', 'button-ability', 384, 64, 14, top=WHITE)
Panel('menu', 'button-menu', 372, 72, 16, top=WHITE)
Panel('menu', 'button-menu-selected', 372, 72, 16, top=PINK, note='first or hovered button, pink text')
Panel('menu', 'button-menu-wide', 760, 72, 16, top=WHITE, note='settings rows: resolution and difficulty')
# slider of the settings: the track, the fill (white, tinted by the game) and the handle
Write('menu', 'slider-track', 460, 8, Rect(0, 0, 460, 8, fill=WHITE, fill_opacity=0.14))
Write('menu', 'slider-fill', 460, 8, Rect(0, 0, 460, 8, fill=WHITE))
Write('menu', 'slider-handle', 16, 32, Rect(0, 0, 16, 32, fill=WHITE))
# vertical scrollbar (white): the track and the handle, flat so they stretch to any length
Write('menu', 'scrollbar-track', 8, 480, Rect(0, 0, 8, 480, fill=WHITE, fill_opacity=0.14))
Write('menu', 'scrollbar-handle', 16, 64, Rect(0, 0, 16, 64, fill=WHITE))
Write('menu', 'overlay-dim', 1920, 1080, Rect(0, 0, 1920, 1080, fill='#080808', fill_opacity=0.74),
      note='full screen dimming under the pause menu and the end screen')


# ------------------------------------------------------------------ team cards
def SlotFrame(name, fill, fill_a, stroke, stroke_a, sw, dash=None, note=''):
    w, h = 90, 114
    body = ''
    if fill:
        body += Rect(0, 0, w, h, fill=fill, fill_opacity=fill_a) + '\n'
    extra = f' stroke-dasharray="{dash}"' if dash else ''
    body += (f'<rect x="{Fmt(sw / 2)}" y="{Fmt(sw / 2)}" width="{Fmt(w - sw)}" height="{Fmt(h - sw)}" fill="none" '
             f'stroke="{stroke}" stroke-opacity="{stroke_a}" stroke-width="{sw}"{extra}/>')
    Write('slots', name, w, h, body, note=note)


SlotFrame('slot-frame', WHITE, 0.06, WHITE, 0.24, 2)
SlotFrame('slot-frame-selected-red', HOT_DEEP, 1, HOT, 1, 3, note='selected card or card to call')
SlotFrame('slot-frame-selected-blue', COLD_DEEP, 1, COLD, 1, 3)
SlotFrame('slot-frame-reserve', None, 0, WHITE, 0.24, 2, dash='6 4', note='reserve: dashed frame')
# the strike over the card of a killed unit: one bright dashed line from the bottom left to the top right corner, drawn like the
# dashed white frame of the reserve (stroke 2, dashes 6 and 4), but bright
Write('slots', 'slot-cross-out', 90, 114,
      f'<line x1="10" y1="104" x2="80" y2="10" stroke="{WHITE}" stroke-opacity="0.9" stroke-width="2" stroke-dasharray="6 4"/>',
      note='killed unit: struck out card')
Write('slots', 'pip-small-red', 5, 5, Rect(0, 0, 5, 5, fill=HOT))
Write('slots', 'pip-small-blue', 5, 5, Rect(0, 0, 5, 5, fill=COLD))
Write('slots', 'pip-small-off', 5, 5, Rect(0, 0, 5, 5, fill=WHITE, fill_opacity=0.16))

# ------------------------------------------------------------------ small flat elements (stretchable)
Write('elements', 'badge-accent', 64, 28, Rect(0, 0, 64, 28, fill=PINK))
Write('elements', 'badge-muted', 64, 28, Rect(0, 0, 64, 28, fill=WHITE, fill_opacity=0.3))
Write('elements', 'keycap', 64, 28, Rect(0, 0, 64, 28, fill=WHITE))
Write('elements', 'icon-frame', 36, 36, Rect(0, 0, 36, 36, fill=WHITE, fill_opacity=HI_A))
Write('elements', 'log-tick-red', 4, 18, Rect(0, 0, 4, 18, fill=HOT))
Write('elements', 'log-tick-blue', 4, 18, Rect(0, 0, 4, 18, fill=COLD))
Write('elements', 'divider-line', 360, 1, Rect(0, 0, 360, 1, fill=WHITE, fill_opacity=0.2))

# ------------------------------------------------------------------ tag icons (traced from the 42x42 originals of the game)
ICON_SRC_CANDIDATES = [ROOT.parent.parent / 'ChoiceScreen',
                       Path(r'D:/Game Dev School/Projekt 2/GitHub/project-two/Assets/Sprites/ChoiceScreen')]
ICON_SRC = next(p for p in ICON_SRC_CANDIDATES if p.exists())
ICONS = {'backstab': 'TagBACKSTAB', 'binary': 'TagBINARY', 'caller': 'TagCaller', 'confuser': 'TagConfuser',
         'gunman': 'TagGUNMAN', 'impair': 'TagIMPAIR', 'impale': 'TagIMPALE', 'piercing': 'TagPIERCING',
         'provoke': 'TagPROVOKE', 'recovery': 'TagRECOVERY', 'spy': 'TagSPY', 'swift': 'TagSWIFT',
         'tough': 'TagTOUGH', 'teleporter': 'TagTeleporter', 'zeal': 'TagZEAL'}
UP = 16          # enlargement before tracing
EPS = 2.2        # polygon simplification tolerance in pixels of the enlarged bitmap
BLUR = 4         # smoothing sigma before tracing (removes the steps of the low resolution originals)


def TraceIcon(src):
    im = cv2.imread(str(src), cv2.IMREAD_UNCHANGED)
    alpha = im[:, :, 3] if im.shape[2] == 4 else im[:, :, 0]
    # border, so shapes touching the edge (Caller, Teleporter) close along the image edge
    alpha = cv2.copyMakeBorder(alpha, 1, 1, 1, 1, cv2.BORDER_CONSTANT, value=0)
    big = cv2.resize(alpha, None, fx=UP, fy=UP, interpolation=cv2.INTER_LINEAR)
    big = cv2.GaussianBlur(big, (0, 0), BLUR)
    _, bw = cv2.threshold(big, 127, 255, cv2.THRESH_BINARY)
    cnts, _ = cv2.findContours(bw, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_NONE)
    d = []
    for c in cnts:
        if cv2.contourArea(c) < 0.6 * UP * UP:
            continue
        c = cv2.approxPolyDP(c, EPS, True)[:, 0, :].astype(float)
        c = (c - UP * 0.5) / UP - 1      # pixel centre, minus the border
        d.append('M' + ' L'.join(f'{Fmt(x)} {Fmt(y)}' for x, y in c) + ' Z')
    return ' '.join(d)


for key, stem in ICONS.items():
    path = TraceIcon(ICON_SRC / f'{stem}.png')
    body = f'<path d="{path}" fill="{WHITE}" fill-rule="evenodd"/>'
    # shown at 22 x 22 in the mockup; the viewBox is the original 42 x 42
    Write('icons', f'icon-tag-{key}', 22, 22, body, view=(42, 42), note='tag icon, 22 px in the info panel')

(ROOT / 'Manifest.json').write_text(json.dumps(MANIFEST, indent=1, ensure_ascii=False), encoding='utf-8')
print(len(MANIFEST), 'SVG')
