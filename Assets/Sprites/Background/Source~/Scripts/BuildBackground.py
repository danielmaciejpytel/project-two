"""Builds the SVG layers of the gameplay background (Svg/Layers/*.svg) and Manifest.json.

The shapes come from the old frame-by-frame animation (Assets/Sprites/BackgroundAnimation): a flat grey base and four
nested shards (180, 190, 197, 202 grey), measured from frame 0001. Every layer is its own SVG on a canvas that is
BLEED px larger than 1920x1080 on every side, so a layer can move, rotate and scale in Unity without showing its edge.
Fills are flat on purpose: the dim over the background leaves only a few grey levels, so wide soft gradients show as banding.
Usage: python BuildBackground.py   then   python RenderPngs.py --manifest=Manifest.json
"""
import json
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FW, FH, BLEED = 1920, 1080, 128
W, H = FW + 2 * BLEED, FH + 2 * BLEED
LEFT, TOP, RIGHT, BOTTOM = -BLEED, -BLEED, FW + BLEED, FH + BLEED  # bleed rectangle in frame coordinates
PERIMETER = 2 * (RIGHT - LEFT + BOTTOM - TOP)
EDGE_TOL = 4

# Shards in frame coordinates, clipped to the frame (as measured), lightest = nearest to the viewer.
SHARDS = {
    'ShardA': (180, [(0, 0), (2, 914), (603, 1064), (652, 973), (1431, 1079), (1712, 1079), (1919, 778), (1919, 0)]),
    'ShardB': (190, [(0, 0), (0, 912), (605, 1063), (713, 838), (782, 886), (1073, 764), (1427, 880), (1824, 343), (1561, 0)]),
    'ShardC': (197, [(0, 0), (0, 735), (518, 694), (782, 885), (1287, 672), (1278, 103), (1041, 0)]),
    'ShardD': (202, [(0, 0), (0, 734), (602, 687), (789, 0)]),
}


def on_border(p):
    return p[0] <= EDGE_TOL or p[0] >= FW - 1 - EDGE_TOL or p[1] <= EDGE_TOL or p[1] >= FH - 1 - EDGE_TOL


def shared_border(a, b):
    return ((a[0] <= EDGE_TOL and b[0] <= EDGE_TOL) or (a[0] >= FW - 1 - EDGE_TOL and b[0] >= FW - 1 - EDGE_TOL)
            or (a[1] <= EDGE_TOL and b[1] <= EDGE_TOL) or (a[1] >= FH - 1 - EDGE_TOL and b[1] >= FH - 1 - EDGE_TOL))


def ray_exit(p, d):
    """Where the ray p + t*d leaves the bleed rectangle."""
    best = 1e9
    for lim, comp, sign in ((LEFT, 0, -1), (RIGHT, 0, 1), (TOP, 1, -1), (BOTTOM, 1, 1)):
        if d[comp] * sign > 1e-9:
            best = min(best, (lim - p[comp]) / d[comp])
    return (p[0] + d[0] * best, p[1] + d[1] * best)


def ray_cross(p1, d1, p2, d2):
    det = d1[0] * (-d2[1]) - d1[1] * (-d2[0])
    if abs(det) < 1e-9:
        return None
    s = ((p2[0] - p1[0]) * (-d2[1]) - (p2[1] - p1[1]) * (-d2[0])) / det
    t = (d1[0] * (p2[1] - p1[1]) - d1[1] * (p2[0] - p1[0])) / det
    if s <= 0 or t <= 0:
        return None
    q = (p1[0] + d1[0] * s, p1[1] + d1[1] * s)
    return q if LEFT <= q[0] <= RIGHT and TOP <= q[1] <= BOTTOM else None


def perimeter_pos(p, clockwise):
    """Distance along the bleed rectangle perimeter in the walking direction (starts at its top-left corner)."""
    w, h = RIGHT - LEFT, BOTTOM - TOP
    x, y = p[0] - LEFT, p[1] - TOP
    if abs(y) < 1e-6: pos = x
    elif abs(x - w) < 1e-6: pos = w + y
    elif abs(y - h) < 1e-6: pos = w + h + (w - x)
    else: pos = 2 * w + h + (h - y)
    return pos if clockwise else PERIMETER - pos


def extend(points):
    """Pushes the parts of a shard that lie on the frame border out to the bleed edge, along the neighbouring edges."""
    n = len(points)
    area = sum(points[i][0] * points[(i + 1) % n][1] - points[(i + 1) % n][0] * points[i][1] for i in range(n))
    clockwise = area > 0  # image coordinates, y down
    border_edge = [shared_border(points[i], points[(i + 1) % n]) for i in range(n)]
    start = next(i for i in range(n) if border_edge[i - 1] and not border_edge[i])  # first vertex after a border run
    pts = [points[(start + i) % n] for i in range(n)]
    be = [border_edge[(start + i) % n] for i in range(n)]
    corners = [(LEFT, TOP), (RIGHT, TOP), (RIGHT, BOTTOM), (LEFT, BOTTOM)]
    out = []
    i = 0
    while i < n:
        if not be[i]:
            out.append(pts[i])
            i += 1
            continue
        a = pts[i]  # first vertex of a run of border edges
        j = i
        while j < n and be[j]: j += 1
        b = pts[j % n]  # last vertex of the run
        prev, nxt = pts[i - 1], pts[(j + 1) % n]
        d1 = (a[0] - prev[0], a[1] - prev[1])
        d2 = (b[0] - nxt[0], b[1] - nxt[1])
        cross = ray_cross(a, d1, b, d2)
        if cross:
            out += [cross]
        else:
            x1, x2 = ray_exit(a, d1), ray_exit(b, d2)
            p1 = perimeter_pos(x1, clockwise)
            walk = sorted(((perimeter_pos(c, clockwise) - p1) % PERIMETER, c) for c in corners)
            span = (perimeter_pos(x2, clockwise) - p1) % PERIMETER
            out += [x1] + [c for off, c in walk if 0 < off < span] + [x2]
        i = j + 1
    return out


def tr(p):
    return p[0] + BLEED, p[1] + BLEED


def path(points):
    return 'M' + ' L'.join('%.1f %.1f' % tr(p) for p in points) + ' Z'


def svg(body, defs=''):
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d" viewBox="0 0 %d %d">\n<defs>%s</defs>\n%s\n</svg>\n'
            % (W, H, W, H, defs, body))


def grey(v):
    return '#%02x%02x%02x' % (v, v, v)


def make_shard(level, clipped):
    d = path(extend(clipped))
    body = ('<path d="%s" fill="%s"/>\n'
            '<path d="%s" fill="none" stroke="#fff" stroke-opacity="0.16" stroke-width="2" stroke-linejoin="round"/>' % (d, grey(level), d))
    return svg(body)


def make_shadow(clipped):
    """The soft shadow of a shard on its own layer, so it can be switched off in Unity."""
    d = path(extend(clipped))
    defs = '<filter id="s" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="16"/></filter>'
    return svg('<path d="%s" fill="#000" fill-opacity="0.11" filter="url(#s)" transform="translate(8 14)"/>' % d, defs)


def make_base():
    return svg('<rect width="%d" height="%d" fill="%s"/>' % (W, H, grey(175)))


def dust_shape(rnd, kind, r):
    """Points of one angular speck around the origin (r = size), in the sharp, flat style of the game."""
    if kind == 'triangle':
        return [(0, -r), (r * 0.9, r * 0.6), (-r * 0.9, r * 0.6)]
    if kind == 'diamond':
        return [(0, -r), (r * 0.62, 0), (0, r), (-r * 0.62, 0)]
    if kind == 'shard':  # a thin sliver
        return [(0, -r), (r * 0.22, 0), (0, r * 0.45), (-r * 0.14, 0)]
    if kind == 'quad':  # an irregular quadrilateral
        return [(-r * rnd.uniform(0.5, 0.9), -r * rnd.uniform(0.4, 0.8)), (r * rnd.uniform(0.6, 1.0), -r * rnd.uniform(0.2, 0.6)),
                (r * rnd.uniform(0.4, 0.8), r * rnd.uniform(0.5, 0.9)), (-r * rnd.uniform(0.6, 1.0), r * rnd.uniform(0.2, 0.6))]
    return [(-r * 0.7, -r * 0.7), (r * 0.7, -r * 0.7), (r * 0.7, r * 0.7), (-r * 0.7, r * 0.7)]  # square


def make_dust(seed, count, rmin, rmax, alpha, shades, dark):
    """A 1920x1080 tile of angular, flat, single-colour specks in several shades of grey, translucent, no outlines, no gradients.
    `dark` = (count, rmin, rmax, alpha, shades) adds darker quadrilaterals and rhombi. Specks near the top/bottom edge are
    repeated on the other side so the tile wraps vertically."""
    rnd = random.Random(seed)
    body = []

    def speck(kind, r, a, shade):
        x, y, angle = rnd.uniform(0, FW), rnd.uniform(0, FH), rnd.uniform(0, 360)
        pts = dust_shape(rnd, kind, r)
        d = 'M' + ' L'.join('%.1f %.1f' % p for p in pts) + ' Z'
        style = 'fill="%s" fill-opacity="%.2f"' % (grey(shade), a)
        for dy in (-FH, 0, FH):
            if -r <= y + dy <= FH + r:
                body.append('<path d="%s" %s transform="translate(%.1f %.1f) rotate(%.1f)"/>' % (d, style, x, y + dy, angle))

    for _ in range(count):
        r = rnd.uniform(rmin, rmax)
        kind = rnd.choice(['triangle', 'triangle', 'diamond', 'shard', 'square'])
        speck(kind, r, rnd.uniform(alpha * 0.6, alpha), rnd.choice(shades))
    dark_count, dmin, dmax, dalpha, dshades = dark
    for _ in range(dark_count):
        r = rnd.uniform(dmin, dmax)
        kind = rnd.choice(['diamond', 'square', 'quad'])
        speck(kind, r, rnd.uniform(dalpha * 0.6, dalpha), rnd.choice(dshades))
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d" viewBox="0 0 %d %d">\n%s\n</svg>\n'
            % (FW, FH, FW, FH, '\n'.join(body)))


def main():
    layers = {'BackgroundBase': (make_base(), W, H)}
    for name, (level, pts) in SHARDS.items():
        layers[name] = (make_shard(level, pts), W, H)
        layers[name + 'Shadow'] = (make_shadow(pts), W, H)
    layers['DustFar'] = (make_dust(7, 38, 6, 13, 0.55, (255, 232, 205, 170), (16, 7, 15, 0.40, (22, 38, 55))), FW, FH)
    layers['DustNear'] = (make_dust(21, 14, 16, 34, 0.38, (245, 215, 185, 150), (6, 17, 30, 0.34, (20, 34, 50))), FW, FH)
    out = ROOT / 'Svg' / 'Layers'
    out.mkdir(parents=True, exist_ok=True)
    manifest = []
    for name, (text, w, h) in layers.items():
        (out / (name + '.svg')).write_text(text, encoding='utf-8')
        manifest.append({'category': 'Layers', 'name': name, 'w': w, 'h': h, 'scale': 1})
    (ROOT / 'Manifest.json').write_text(json.dumps(manifest, indent=1), encoding='utf-8')
    print(len(manifest), 'layers')


main()
