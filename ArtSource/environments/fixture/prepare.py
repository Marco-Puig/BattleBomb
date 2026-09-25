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
MAX_P95 = 84.0


def brightness(rgb):
    a = rgb[..., :3].astype(float) / 255
    return (0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]) * 100


def seamless(a, axis):
    n = a.shape[axis]
    b = int(n * BAND)
    a = a.astype(float)
    head = np.take(a, range(b), axis=axis)
    tail = np.take(a, range(n - b, n), axis=axis)
    shape = [1] * a.ndim
    shape[axis] = b
    t = (np.arange(b) / b).reshape(shape)
    blended = tail * (1 - t) + head * t
    rest = np.take(a, range(b, n - b), axis=axis)
    return np.concatenate([blended, rest], axis=axis)


def save(a, name):
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'fixture-%s__ai_v1.png' % name))


def ground():
    a = np.array(Image.open(os.path.join(RAW, 'ground.png')).convert('RGB'))
    a = seamless(seamless(a, 1), 0)
    a = np.array(Image.fromarray(a.astype(np.uint8)).resize((1024, 1024), Image.LANCZOS)).astype(float)
    p95 = np.percentile(brightness(a), 95)
    if p95 > MAX_P95:
        a *= MAX_P95 / p95
    l = brightness(a)
    save(a, 'ground')
    print('ground: brightness %.0f%%, 5th-95th percentile %.0f-%.0f%%, darkest %.0f%%'
          % (l.mean(), np.percentile(l, 5), np.percentile(l, 95), l.min()))


def strip(name):
    path = os.path.join(RAW, name + '.png')
    if not os.path.exists(path):
        print('%s: not generated yet' % name)
        return
    a = np.array(Image.open(path).convert('RGBA'))
    save(seamless(a, 1), name)
    print('%s: made seamless left to right' % name)


def frames():
    rgba = np.array(Image.open(os.path.join(RAW, 'frames.png')).convert('RGBA'))
    lab, n = ndimage.label(rgba[:, :, 3] > 8, structure=np.ones((3, 3)))
    sizes = ndimage.sum(np.ones(lab.shape), lab, range(1, n + 1))
    big = sorted((i + 1 for i, s in enumerate(sizes) if s > lab.size * 0.02),
                 key=lambda i: ndimage.center_of_mass(lab == i)[1])
    for letter, i in zip('ab', big):
        keep = ndimage.binary_dilation(lab == i, iterations=4)
        ys, xs = np.nonzero(keep)
        crop = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        crop[~keep[ys.min():ys.max() + 1, xs.min():xs.max() + 1]] = 0
        save(crop, 'frame-' + letter)
    print('frames: %d clumps cut' % len(big))


if __name__ == '__main__':
    ground()
    for piece in ('backdrop-far', 'backdrop-mid', 'backdrop-near'):
        strip(piece)
    frames()
