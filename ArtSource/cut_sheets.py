# Cuts ChatGPT sheets that were drawn on a grid into one image per piece, each centred on its canvas.
# Run from the repository root:
#   python ArtSource/cut_sheets.py            cut every sheet whose raw file exists
#   python ArtSource/cut_sheets.py effects    only the jobs whose name starts with "effects"
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.abspath(__file__))
FILL = 0.85

SMALL = {'ember', 'snowflake', 'leaf', 'pebble', 'sparkle-a', 'sparkle-b', 'spark',
         'hit-light', 'hit-heavy', 'hit-crit'}
LONG = {'bolt', 'arc-level', 'arc-down', 'arc-up', 'arc-heavy', 'arc-launcher', 'speed-line'}
TALL = {'glow-beam'}


def effect_canvas(piece):
    if piece in SMALL:
        return 256, 256
    if piece in LONG:
        return 1024, 512
    if piece in TALL:
        return 512, 1024
    return 512, 512


KITS = {
    'fire': (4, 2, ['flame-tongue-a', 'flame-tongue-b', 'flame-tongue-c', 'flame-burst',
                    'ember', 'burn-flame', 'spark', None]),
    'ice': (4, 2, ['bolt', 'shard-a', 'shard-b', 'shard-c', 'frost-puff', 'snowflake', 'frost-crust', 'spark']),
    'earth': (4, 2, ['rock-a', 'rock-b', 'rock-c', 'pillar-a', 'pillar-b', 'dust-puff', 'pebble', 'spark']),
    'air': (4, 2, ['swoosh-a', 'swoosh-b', 'swoosh-c', 'spiral', 'leaf', 'speed-line', 'spark', None]),
    'melee': (5, 2, ['arc-level', 'arc-down', 'arc-up', 'arc-heavy', 'arc-launcher',
                     'hit-light', 'hit-heavy', 'hit-crit', 'dust-a', 'dust-b']),
    'loot': (4, 1, ['glow-ring', 'glow-beam', 'sparkle-a', 'sparkle-b']),
    'feedback': (5, 1, ['levelup-burst', 'sparkle-rise', 'heart', 'heart-pulse', 'fizzle-puff']),
}

# Soft glows that bleed across grid lines are cut by measured rectangles (x0, y0, x1, y1) instead.
RECTS = {
    'effects/loot': {'glow-ring': (0, 540, 505, 800), 'glow-beam': (505, 40, 800, 800)},
}

JOBS = [('effects/' + kit, 'effects/AI/_raw/%s.png' % kit, cols, rows, names,
         'effects/%s/AI/%s-{}__ai_v1.png' % (kit, kit), effect_canvas)
        for kit, (cols, rows, names) in KITS.items()]

# 'skip' marks a cell that was drawn only for reference: cut, then thrown away.
ICON = 'icons/items/{0}/AI/{0}__ai_v%d.png'
JOBS += [
    ('icons/batch-a', 'icons/items/AI/_raw/batch-a.png', 3, 2,
     ['skip', 'leather-chestplate', 'leather-boots', 'steel-helmet', 'steel-chestplate', 'steel-boots'],
     ICON % 1, lambda piece: (512, 512)),
    ('icons/batch-b', 'icons/items/AI/_raw/batch-b.png', 2, 1, ['hunting-knife', 'hunting-bow'],
     ICON % 1, lambda piece: (512, 512)),
    ('icons/batch-c', 'icons/items/AI/_raw/batch-c.png', 3, 1, ['terrier', 'lucky-charm', 'ember-stone'],
     ICON % 1, lambda piece: (512, 512)),
]
FILLS = {'icons': 0.80}

# Held weapons sit on HERO_TEMPLATE §9's canvases with the grip on the pivot, not centred.
# name: (output, canvas, pivot fraction from bottom-left, length along the long axis, grip rule)
WEAPONS = {
    'knife': ('weapons/hunting-knife/AI/hunting-knife__ai_v1.png', (256, 640), (0.5, 0.125), 480, 'handle'),
    'bow': ('weapons/hunting-bow/AI/hunting-bow__ai_v1.png', (384, 768), (0.4, 0.5), 640, 'middle'),
    'arrow': ('weapons/hunting-bow/AI/hunting-bow-arrow__ai_v1.png', (512, 128), (0.5, 0.5), 400, 'centre'),
}


def trimmed(rgba, box):
    x0, y0, x1, y1 = box
    region = rgba[y0:y1, x0:x1]
    ys, xs = np.nonzero(region[:, :, 3] > 8)
    return Image.fromarray(region[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy())


def box_gap(box, y, x):
    y0, x0, y1, x1 = box
    dy = max(y0 - y, 0, y - y1)
    dx = max(x0 - x, 0, x - x1)
    return (dx * dx + dy * dy) ** 0.5


def cut(sheet, cols, rows, names, rects=None):
    rgba = np.array(Image.open(sheet).convert('RGBA'))
    h, w = rgba.shape[:2]
    ink = rgba[:, :, 3] > 8
    lab, n = ndimage.label(ink, structure=np.ones((3, 3)))
    sizes = ndimage.sum(ink, lab, range(1, n + 1))
    centres = ndimage.center_of_mass(ink, lab, range(1, n + 1))
    cells = {}
    for i, (size, (cy, cx)) in enumerate(zip(sizes, centres)):
        if size < 30:
            continue
        cell = min(int(cy / (h / rows)), rows - 1) * cols + min(int(cx / (w / cols)), cols - 1)
        cells.setdefault(cell, []).append(i + 1)
    rects = rects or {}
    named = {c: ndimage.find_objects(np.isin(lab, cells[c]).astype(int))[0]
             for c, name in enumerate(names) if name and name not in rects and c in cells}
    for c, name in enumerate(names):
        if name is None and c in cells:
            for mark in cells.pop(c):
                cy, cx = centres[mark - 1]
                boxes = {k: (s[0].start, s[1].start, s[0].stop, s[1].stop) for k, s in named.items()}
                nearest = min(boxes, key=lambda k: box_gap(boxes[k], cy, cx))
                cells[nearest].append(mark)
    pieces, warnings = {}, []
    for name, box in rects.items():
        pieces[name] = trimmed(rgba, box)
    for cell, name in enumerate(names):
        found = cells.get(cell)
        if name is None or name in rects or name == 'skip':
            continue
        if not found:
            warnings.append('%s: cell %d is empty' % (name, cell + 1))
            continue
        keep = ndimage.binary_dilation(np.isin(lab, found), iterations=6)
        ys, xs = np.nonzero(keep)
        crop = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        crop[~keep[ys.min():ys.max() + 1, xs.min():xs.max() + 1]] = 0
        pieces[name] = Image.fromarray(crop)
    return pieces, warnings


def on_canvas(piece, size, fill=FILL):
    cw, ch = size
    scale = min(cw * fill / piece.width, ch * fill / piece.height)
    img = piece.resize((max(1, round(piece.width * scale)), max(1, round(piece.height * scale))), Image.LANCZOS)
    canvas = Image.new('RGBA', size, (0, 0, 0, 0))
    canvas.alpha_composite(img, ((cw - img.width) // 2, (ch - img.height) // 2))
    return canvas


def grip(piece, rule):
    a = np.array(piece)[:, :, 3] > 128
    h, w = a.shape
    if rule == 'centre':
        return w / 2, h / 2
    if rule == 'handle':
        y = int(h * 0.84)
    else:
        y = h // 2
    xs = np.nonzero(a[y])[0]
    runs = np.split(xs, np.nonzero(np.diff(xs) > 1)[0] + 1)
    wood = max(runs, key=len)
    return float(wood.mean()), float(y)


def place_weapons(sheet):
    pieces, warnings = cut(sheet, 3, 1, list(WEAPONS))
    for name, piece in pieces.items():
        out, (cw, ch), (fx, fy), length, rule = WEAPONS[name]
        scale = length / max(piece.width, piece.height)
        img = piece.resize((max(1, round(piece.width * scale)), max(1, round(piece.height * scale))), Image.LANCZOS)
        gx, gy = grip(img, rule)
        canvas = Image.new('RGBA', (cw, ch), (0, 0, 0, 0))
        canvas.alpha_composite(img, (round(cw * fx - gx), round(ch * (1 - fy) - gy)))
        kept = (np.array(canvas)[:, :, 3] > 128).sum() / max((np.array(img)[:, :, 3] > 128).sum(), 1)
        path = os.path.join(ROOT, out)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        canvas.save(path)
        print('weapons          %-6s on %dx%d, %.0f%% inside the canvas' % (name, cw, ch, kept * 100))
    for w in warnings:
        print('   ! ' + w)


if __name__ == '__main__':
    only = sys.argv[1] if len(sys.argv) > 1 else ''
    weapon_sheet = os.path.join(ROOT, 'weapons/AI/_raw/sheet.png')
    if 'weapons'.startswith(only) and os.path.exists(weapon_sheet):
        place_weapons(weapon_sheet)
    for job, sheet, cols, rows, names, pattern, canvas_for in JOBS:
        path = os.path.join(ROOT, sheet)
        if not job.startswith(only) or not os.path.exists(path):
            continue
        pieces, warnings = cut(path, cols, rows, names, RECTS.get(job))
        for name, piece in pieces.items():
            out = os.path.join(ROOT, pattern.format(name))
            os.makedirs(os.path.dirname(out), exist_ok=True)
            on_canvas(piece, canvas_for(name), FILLS.get(job.split('/')[0], FILL)).save(out)
        print('%-16s %d pieces%s' % (job, len(pieces), ''.join('\n   ! ' + w for w in warnings)))
