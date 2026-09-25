# Cuts the ChatGPT parts sheets into rig parts on the template's canvases (HERO_TEMPLATE.md §4).
# Run from the repository root:
#   python ArtSource/rig/hero-template/cut_parts.py --measure   print each part's extent around its pivot
#   python ArtSource/rig/hero-template/cut_parts.py             write the parts and a preview
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, 'guides'))
import make_guides as g  # noqa: E402

RAW = os.path.join(HERE, 'AI', '_raw')
OUT = os.path.join(HERE, 'AI')
ASSET = 'hero-template'

SHEETS = [
    ('sheet-a.png', [2, 4, 2], ['head-neutral', 'torso-default', 'arm-front-default', 'arm-back-default',
                                'hand-front-open', 'hand-back-open', 'leg-front-default', 'leg-back-default']),
    ('sheet-b.png', [2, 1, 4], ['head-attack', 'head-hurt', 'head-ko', 'hand-front-fist', 'hand-front-grip',
                                'hand-back-fist', 'hand-back-grip']),
]
PIVOT_END = {'head': 'bottom', 'torso': 'bottom', 'arm': 'top', 'hand': 'top', 'leg': 'top'}
NORMALISE_TO = {'head': 'head-neutral', 'hand-front': 'hand-front-open', 'hand-back': 'hand-back-open'}
BASE = {'head': 'head-neutral', 'torso': 'torso-default', 'arm': 'arm-front-default',
        'hand': 'hand-front-open', 'leg': 'leg-front-default'}


def template_size(cat):
    shapes = [sh for v in g.VARIANTS[cat].values() for sh in v]
    x0, y0, x1, y1 = g.bounds(shapes)
    if cat == 'head':
        return 'above', y1
    if cat == 'torso':
        return 'neck', g.CHILD_JOINTS['torso']['neck'][1]
    return 'below', -y0


def drawn_size(img, pivot, how):
    px, py = pivot
    if how == 'above':
        return py
    if how == 'neck':
        return py - ball_pivot(img, 'top')[1]
    return img.shape[0] - py


def category(name):
    return name.split('-')[0]


def extract(path, rows, names):
    rgba = np.array(Image.open(path).convert('RGBA'))
    solid = rgba[:, :, 3] > 128
    lab, n = ndimage.label(solid, structure=np.ones((3, 3)))
    sizes = ndimage.sum(solid, lab, range(1, n + 1))
    objs = ndimage.find_objects(lab)
    big = [i + 1 for i, sz in enumerate(sizes) if sz > solid.size * 0.002]
    centres = {i: ndimage.center_of_mass(solid, lab, i) for i in big}
    order = sorted(big, key=lambda i: centres[i][0])
    ordered, k = [], 0
    for r in rows:
        ordered += sorted(order[k:k + r], key=lambda i: centres[i][1]); k += r
    owner = {i: [i] for i in ordered}
    for j, sz in enumerate(sizes):
        j += 1
        if j in owner or sz < 20:
            continue
        sy, sx = ndimage.center_of_mass(solid, lab, j)
        best = min(ordered, key=lambda i: gap(objs[i - 1], sy, sx))
        if gap(objs[best - 1], sy, sx) < 90:
            owner[best].append(j)
    parts = {}
    for name, i in zip(names, ordered):
        keep = np.isin(lab, owner[i])
        ys, xs = np.nonzero(keep)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        crop = rgba[y0:y1, x0:x1].copy()
        crop[~keep[y0:y1, x0:x1]] = 0
        parts[name] = crop
    return parts


def gap(sl, y, x):
    dy = max(sl[0].start - y, 0, y - sl[0].stop)
    dx = max(sl[1].start - x, 0, x - sl[1].stop)
    return (dx * dx + dy * dy) ** 0.5


def spans(img):
    solid = img[:, :, 3] > 128
    widths, centres = [], []
    for row in solid:
        xs = np.nonzero(row)[0]
        widths.append(xs[-1] - xs[0] + 1 if len(xs) else 0)
        centres.append((xs[0] + xs[-1]) / 2 if len(xs) else 0)
    return np.array(widths, float), np.array(centres, float)


def ball_pivot(img, end):
    widths, centres = spans(img)
    h = len(widths)
    order = range(h) if end == 'top' else range(h - 1, -1, -1)
    smooth = np.convolve(widths, np.ones(5) / 5, mode='same')
    rows = list(order)[: int(h * 0.35)]
    peak = rows[len(rows) // 3]
    for a, b, c in zip(rows, rows[1:], rows[2:]):
        if smooth[b] >= smooth[a] and smooth[b] > smooth[c] + 0.5 and abs(b - rows[0]) > 4:
            peak = b
            break
    return float(centres[peak]), float(peak)


def trim_hand_off_arm(img):
    widths, _ = spans(img)
    h = len(widths)
    lo, hi = int(h * 0.45), int(h * 0.9)
    wrist = lo + int(np.argmin(widths[lo:hi]))
    out = img.copy()
    out[wrist + 4:] = 0
    ys = np.nonzero(out[:, :, 3] > 0)[0]
    return out[: ys.max() + 1]


def area(img):
    return float((img[:, :, 3] > 128).sum())


def resized(img, scale):
    im = Image.fromarray(img)
    w, h = im.size
    return np.array(im.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS))


def prepare():
    parts = {}
    for sheet, rows, names in SHEETS:
        parts.update(extract(os.path.join(RAW, sheet), rows, names))
    for name in list(parts):
        if category(name) == 'arm':
            parts[name] = trim_hand_off_arm(parts[name])
    pivots = {n: ball_pivot(img, PIVOT_END[category(n)]) for n, img in parts.items()}
    fit = {}
    for cat, ref in BASE.items():
        how, target = template_size(cat)
        fit[cat] = target / drawn_size(parts[ref], pivots[ref], how)
    scales = {}
    for name in parts:
        key = 'head' if category(name) == 'head' else '-'.join(name.split('-')[:2])
        ref = NORMALISE_TO.get(key)
        ratio = (area(parts[ref]) / area(parts[name])) ** 0.5 if ref and ref != name else 1.0
        if category(name) in ('arm', 'leg') and ref is None:
            ratio = 1.0
        scales[name] = fit[category(name)] * ratio
    placed = {}
    for name, img in parts.items():
        s = scales[name]
        px, py = pivots[name]
        placed[name] = (resized(img, s), px * s, py * s)
    return placed


def extents(placed):
    ext = {}
    for name, (img, px, py) in placed.items():
        cat = category(name)
        h, w = img.shape[:2]
        box = (-px, -(h - py), w - px, py)
        old = ext.get(cat, box)
        ext[cat] = (min(old[0], box[0]), min(old[1], box[1]), max(old[2], box[2]), max(old[3], box[3]))
    return ext


def on_canvas(name, img, px, py):
    cw, ch, fx, fy = g.canvas_for(category(name))
    canvas = Image.new('RGBA', (cw, ch), (0, 0, 0, 0))
    ox, oy = round(cw * fx - px), round(ch * (1 - fy) - py)
    canvas.alpha_composite(Image.fromarray(img), (max(ox, 0), max(oy, 0)),
                           (max(-ox, 0), max(-oy, 0)))
    clipped = area(img) - float((np.array(canvas)[:, :, 3] > 128).sum())
    return canvas, clipped / max(area(img), 1)


def preview(canvases):
    W, H = 1500, 1800
    img = Image.new('RGBA', (W, H), (236, 233, 222, 255))
    gx, gy = 700, 1720
    joints = {'torso': (0, g.ROOT_Y)}
    t = g.CHILD_JOINTS['torso']
    for slot, key in (('head', 'neck'), ('arm-front', 'shoulder-front'), ('arm-back', 'shoulder-back'),
                      ('leg-front', 'hip-front'), ('leg-back', 'hip-back')):
        joints[slot] = (t[key][0], g.ROOT_Y + t[key][1])
    for side in ('front', 'back'):
        a = joints['arm-' + side]
        joints['hand-' + side] = (a[0], a[1] + g.CHILD_JOINTS['arm']['wrist'][1])
    labels = {'head': 'neutral', 'hand-front': 'grip', 'hand-back': 'open'}
    for slot in g.LAYERS:
        if slot not in joints:
            continue
        name = '%s-%s' % (slot, labels.get(slot, 'default'))
        canvas = canvases[name]
        cw, ch, fx, fy = g.canvas_for(category(name))
        jx, jy = joints[slot]
        img.alpha_composite(canvas, (round(gx + jx - cw * fx), round(gy - jy - ch * (1 - fy))))
    return img


if __name__ == '__main__':
    placed = prepare()
    if '--measure' in sys.argv:
        for cat, box in extents(placed).items():
            print("    '%s': (%d, %d, %d, %d)," % (cat, *[round(v) for v in box]))
        sys.exit()
    canvases = {}
    for name, (img, px, py) in placed.items():
        canvas, clipped = on_canvas(name, img, px, py)
        canvases[name] = canvas
        canvas.save(os.path.join(OUT, '%s-%s__ai_v1.png' % (ASSET, name)))
        print('%-20s %dx%d  clipped %.1f%%' % (name, canvas.width, canvas.height, clipped * 100))
    view = preview(canvases)
    view.resize((view.width // 2, view.height // 2), Image.LANCZOS).save(os.path.join(RAW, 'assembled-preview.png'))
    print('preview: ArtSource/rig/hero-template/AI/_raw/assembled-preview.png')
