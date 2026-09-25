# Turns the fixture kit's raw ChatGPT images into seamless, measured drafts.
# Run from the repository root: python ArtSource/environments/fixture/prepare.py
import os

import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'AI', '_raw')
OUT = os.path.join(HERE, 'AI')
BAND = 0.08
JOIN = 0.04
REACH = 0.3
WIDTH_COST = 0.3
MAX_P95 = 84.0


def brightness(rgb):
    a = rgb[..., :3].astype(float) / 255
    return (0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]) * 100


def seamless(a, axis, b=None):
    n = a.shape[axis]
    b = b or int(n * BAND)
    a = a.astype(float)
    head = np.take(a, range(b), axis=axis)
    tail = np.take(a, range(n - b, n), axis=axis)
    shape = [1] * a.ndim
    shape[axis] = b
    t = (np.arange(b) / b).reshape(shape)
    blended = tail * (1 - t) + head * t
    rest = np.take(a, range(b, n - b), axis=axis)
    return np.concatenate([blended, rest], axis=axis)


# A layer with a see-through sky cannot be blended edge to edge: a tree on one edge over sky on the
# other leaves a half-transparent ghost. So the repeat is cut where two columns already match,
# trading a little width for a clean join.
def best_join(a):
    w = a.shape[1]
    b = int(w * JOIN)
    reach = int(w * REACH)
    small = np.array(Image.fromarray(a).resize((w, 128), Image.BILINEAR)).astype(float) / 255
    cols = np.concatenate([small[..., :3] * small[..., 3:], small[..., 3:]], axis=2).transpose(1, 0, 2).reshape(w, -1)
    left, right = cols[:reach + b], cols[w - reach:]
    d = np.array([np.abs(left - c).mean(1) for c in right]).T
    for r in range(1, d.shape[0]):
        d[r, 1:] += d[r - 1, :-1]
    d = np.pad(d, ((1, 0), (1, 0)))
    i, j = np.meshgrid(np.arange(reach), np.arange(reach - b), indexing='ij')
    x0, x1 = i, w - reach + j
    score = (d[i + b, j + b] - d[i, j]) / b + WIDTH_COST * (1 - (x1 - x0) / w)
    k = np.unravel_index(score.argmin(), score.shape)
    x0, x1 = int(x0[k]), int(x1[k])
    return a[:, x0:x1 + b], b, (x1 - x0) / w


def latest(piece):
    for version, suffix in ((2, '-v2'), (1, '')):
        path = os.path.join(RAW, piece + suffix + '.png')
        if os.path.exists(path):
            return path, version
    return None, None


def save(a, name, version):
    out = os.path.join(OUT, 'fixture-%s__ai_v%d.png' % (name, version))
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(out)


def ground():
    path, version = latest('ground')
    a = np.array(Image.open(path).convert('RGB'))
    a = seamless(seamless(a, 1), 0)
    a = np.array(Image.fromarray(a.astype(np.uint8)).resize((1024, 1024), Image.LANCZOS)).astype(float)
    p95 = np.percentile(brightness(a), 95)
    if p95 > MAX_P95:
        a *= MAX_P95 / p95
    l = brightness(a)
    save(a, 'ground', version)
    print('ground v%d: brightness %.0f%%, 5th-95th percentile %.0f-%.0f%%, darkest %.0f%%'
          % (version, l.mean(), np.percentile(l, 5), np.percentile(l, 95), l.min()))


def strip(name):
    path, version = latest(name)
    if not path:
        print('%s: not generated yet' % name)
        return
    a, b, kept = best_join(np.array(Image.open(path).convert('RGBA')))
    save(seamless(a, 1, b), name, version)
    print('%s v%d: made seamless left to right, keeping %.0f%% of its width' % (name, version, kept * 100))


def frames():
    path, version = latest('frames')
    rgba = np.array(Image.open(path).convert('RGBA'))
    lab, n = ndimage.label(rgba[:, :, 3] > 8, structure=np.ones((3, 3)))
    sizes = ndimage.sum(np.ones(lab.shape), lab, range(1, n + 1))
    big = sorted((i + 1 for i, s in enumerate(sizes) if s > lab.size * 0.02),
                 key=lambda i: ndimage.center_of_mass(lab == i)[1])
    for letter, i in zip('ab', big):
        keep = ndimage.binary_dilation(lab == i, iterations=4)
        ys, xs = np.nonzero(keep)
        crop = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        crop[~keep[ys.min():ys.max() + 1, xs.min():xs.max() + 1]] = 0
        save(crop, 'frame-' + letter, version)
    print('frames v%d: %d clumps cut' % (version, len(big)))


if __name__ == '__main__':
    ground()
    for piece in ('backdrop-far', 'backdrop-mid', 'backdrop-near'):
        strip(piece)
    frames()
