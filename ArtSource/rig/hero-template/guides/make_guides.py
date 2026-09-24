import math, os, sys
from PIL import Image, ImageDraw, ImageFont

H = 1600
INK = (26, 25, 23, 255)
FRONT = (205, 205, 200, 255)
BACK = (160, 160, 156, 255)
RED = (220, 40, 40, 255)
BLUE = (40, 90, 220, 255)
# Run from the repository root: python ArtSource/rig/hero-template/guides/make_guides.py
OUT = os.path.dirname(os.path.abspath(__file__))
FONT_PATH = 'Assets/_BattleBomb/Art/UI/Fonts/SpaceMono-Regular.ttf'
FONT = ImageFont.truetype(FONT_PATH, 22)
FONT_S = ImageFont.truetype(FONT_PATH, 16)


def capsule(a, b, r):
    return ('capsule', a, b, r)


def ellipse(c, rx, ry):
    return ('ellipse', c, rx, ry)


def rrect(x0, y0, x1, y1, r):
    return ('rrect', (x0, y0, x1, y1), r)


def poly(pts):
    return ('poly', pts)


# Part-local coordinates in px at H=1600: x right (facing), y up, origin = the part's pivot joint.
VARIANTS = {
    'torso': {'default': [rrect(-240, -90, 240, 540, 170)]},
    'head': {k: [ellipse((20, 315), 300, 315)] for k in ('neutral', 'attack', 'hurt', 'ko')},
    'arm': {
        'straight': [capsule((0, 45), (0, -320), 62)],
        'bent': [capsule((0, 45), (0, -170), 62), capsule((0, -170), (170, -230), 62)],
    },
    'hand': {k: [ellipse((0, -60), 85, 85)] for k in ('open', 'fist', 'grip')},
    'leg': {
        'straight': [capsule((0, 50), (0, -356), 80)],
        'bent': [capsule((0, 50), (90, -170), 80), capsule((90, -170), (0, -356), 80)],
    },
    'foot': {'default': [rrect(-80, -104, 210, 45, 50)]},
    'back-piece': {'default': [poly([(-150, 20), (150, 20), (170, -560), (-280, -560)])]},
    'waist-piece': {'default': [poly([(-150, 20), (170, 20), (190, -260), (-120, -260)])]},
}
CHILD_JOINTS = {
    'torso': {'neck': (8, 490), 'shoulder-front': (90, 400), 'shoulder-back': (-110, 415),
              'hip-front': (70, -20), 'hip-back': (-70, -10), 'back-piece': (-60, 440),
              'waist-piece': (30, 30)},
    'arm': {'wrist': (0, -320)},
    'leg': {'ankle': (0, -356)},
}
ROOT_Y = 480  # hips centre above the ground (0.30 H)


def bounds(shapes):
    xs, ys = [], []
    for s in shapes:
        if s[0] == 'capsule':
            (ax, ay), (bx, by), r = s[1], s[2], s[3]
            xs += [ax - r, ax + r, bx - r, bx + r]; ys += [ay - r, ay + r, by - r, by + r]
        elif s[0] == 'ellipse':
            (cx, cy), rx, ry = s[1], s[2], s[3]
            xs += [cx - rx, cx + rx]; ys += [cy - ry, cy + ry]
        elif s[0] == 'rrect':
            x0, y0, x1, y1 = s[1]; xs += [x0, x1]; ys += [y0, y1]
        else:
            xs += [p[0] for p in s[1]]; ys += [p[1] for p in s[1]]
    return min(xs), min(ys), max(xs), max(ys)


def canvas_for(cat):
    allshapes = [s for v in VARIANTS[cat].values() for s in v]
    x0, y0, x1, y1 = bounds(allshapes)
    m = 40
    w = math.ceil((x1 - x0 + 2 * m) / 32) * 32
    h = math.ceil((y1 - y0 + 2 * m) / 32) * 32
    left = x0 - m - ((w - (x1 - x0 + 2 * m)) / 2)
    bottom = y0 - m - ((h - (y1 - y0 + 2 * m)) / 2)
    return w, h, (-left) / w, (-bottom) / h


def draw_shapes(d, shapes, tx, fill, scale=1.0, ink_w=6):
    for s in shapes:
        if s[0] == 'capsule':
            (ax, ay), (bx, by), r = s[1], s[2], s[3]
            A, B = tx(ax, ay), tx(bx, by); rr = r * scale
            ang = math.atan2(B[1] - A[1], B[0] - A[0]); nx, ny = -math.sin(ang) * rr, math.cos(ang) * rr
            body = [(A[0] + nx, A[1] + ny), (B[0] + nx, B[1] + ny), (B[0] - nx, B[1] - ny), (A[0] - nx, A[1] - ny)]
            for P in (A, B):
                d.ellipse([P[0] - rr, P[1] - rr, P[0] + rr, P[1] + rr], fill=fill, outline=INK, width=ink_w)
            d.polygon(body, fill=fill)
            d.line([body[0], body[1]], fill=INK, width=ink_w); d.line([body[2], body[3]], fill=INK, width=ink_w)
        elif s[0] == 'ellipse':
            (cx, cy), rx, ry = s[1], s[2], s[3]
            C = tx(cx, cy)
            d.ellipse([C[0] - rx * scale, C[1] - ry * scale, C[0] + rx * scale, C[1] + ry * scale], fill=fill, outline=INK, width=ink_w)
        elif s[0] == 'rrect':
            x0, y0, x1, y1 = s[1]
            P0, P1 = tx(x0, y1), tx(x1, y0)
            d.rounded_rectangle([P0[0], P0[1], P1[0], P1[1]], radius=s[2] * scale, fill=fill, outline=INK, width=ink_w)
        else:
            d.polygon([tx(*p) for p in s[1]], fill=fill, outline=INK, width=ink_w)


def cross(d, P, c=RED, r=14, w=4):
    d.line([P[0] - r, P[1], P[0] + r, P[1]], fill=c, width=w); d.line([P[0], P[1] - r, P[0], P[1] + r], fill=c, width=w)


LAYERS = ['back-piece', 'arm-back', 'hand-back', 'leg-back', 'foot-back', 'leg-front', 'foot-front',
          'torso', 'waist-piece', 'head', 'arm-front', 'weapon', 'hand-front']


def cat_of(slot):
    return slot.replace('-front', '').replace('-back', '') if slot not in ('back-piece', 'waist-piece') else slot


def assembled(labelled):
    W, Hc = 1400, 1900
    img = Image.new('RGBA', (W, Hc), (255, 255, 255, 255)); d = ImageDraw.Draw(img)
    gx, gy = 620, 1800  # ground point under the root
    d.line([80, gy, W - 80, gy], fill=(120, 120, 120, 255), width=3)
    torso = CHILD_JOINTS['torso']
    joint_world = {'torso': (0, ROOT_Y)}
    for slot, key in (('head', 'neck'), ('arm-front', 'shoulder-front'), ('arm-back', 'shoulder-back'),
                      ('leg-front', 'hip-front'), ('leg-back', 'hip-back'), ('back-piece', 'back-piece'),
                      ('waist-piece', 'waist-piece')):
        joint_world[slot] = (torso[key][0], ROOT_Y + torso[key][1])
    for side in ('front', 'back'):
        a = joint_world['arm-' + side]; joint_world['hand-' + side] = (a[0], a[1] - 320)
        l = joint_world['leg-' + side]; joint_world['foot-' + side] = (l[0], l[1] - 356)
    joint_world['weapon'] = joint_world['hand-front']
    for slot in LAYERS:
        if slot in ('back-piece', 'waist-piece'):
            continue
        if slot == 'weapon':
            j = joint_world['hand-front']
            tx = lambda x, y, j=j: (gx + j[0] + x, gy - (j[1] + y))
            d.polygon([tx(-14, -40), tx(14, -40), tx(14, -520), tx(0, -560), tx(-14, -520)], fill=(230, 230, 240, 255), outline=INK, width=6)
            d.rectangle([tx(-60, -30)[0], tx(-60, -30)[1], tx(60, -52)[0], tx(60, -52)[1]], fill=(200, 170, 90, 255), outline=INK, width=5)
            continue
        cat = cat_of(slot)
        variant = list(VARIANTS[cat].keys())[0]
        j = joint_world[slot]
        tx = lambda x, y, j=j: (gx + j[0] + x, gy - (j[1] + y))
        fill = BACK if slot.endswith('-back') or slot == 'back-piece' else FRONT
        draw_shapes(d, VARIANTS[cat][variant], tx, fill)
    for slot, j in joint_world.items():
        if slot in ('weapon', 'back-piece', 'waist-piece'):
            continue
        P = (gx + j[0], gy - j[1]); cross(d, P)
        if labelled:
            d.text((P[0] + 18, P[1] - 12), slot, fill=RED, font=FONT_S)
    if labelled:
        for frac, name in ((1.0, 'top of head 1.00 H'), (0.606, 'chin/neck 0.61 H (head = 0.39 H)'), (0.30, 'root: hips 0.30 H'), (0.0, 'ground')):
            y = gy - frac * H
            d.line([60, y, 200, y], fill=BLUE, width=3); d.text((60, y - 30), name, fill=BLUE, font=FONT_S)
        d.text((60, 30), 'Hero template: rest pose, facing right. Red crosses = pivots (joints).', fill=INK, font=FONT)
        d.text((60, 60), 'Back parts drawn darker. H = 1600 px (art bible section 7). Optional cape and waist flap not shown.', fill=INK, font=FONT)
    img.save(os.path.join(OUT, 'template-assembled%s.png' % ('-labelled' if labelled else '')))


SHEET_A = [('head', 'neutral'), ('torso', 'default'), ('arm-front', 'straight'), ('arm-back', 'straight'),
           ('hand-front', 'open'), ('hand-back', 'open'), ('leg-front', 'straight'), ('leg-back', 'straight'),
           ('foot-front', 'default'), ('foot-back', 'default')]
SHEET_B = [('head', 'attack'), ('head', 'hurt'), ('head', 'ko'), ('arm-front', 'bent'), ('arm-back', 'bent'),
           ('hand-front', 'fist'), ('hand-front', 'grip'), ('hand-back', 'fist'), ('hand-back', 'grip'),
           ('leg-front', 'bent'), ('leg-back', 'bent')]


def sheet(name, items, rows, labelled, s):
    W, Hs = 1024, 1536
    img = Image.new('RGBA', (W, Hs), (255, 255, 255, 255)); d = ImageDraw.Draw(img)
    y = 40
    idx = 0
    for row in rows:
        row_items = items[idx: idx + row]; idx += row
        dims = [canvas_for(cat_of(sl)) for sl, _ in row_items]
        row_h = max(h for _, h, _, _ in dims) * s
        total_w = sum(w for w, _, _, _ in dims) * s
        gap = (W - total_w) / (len(row_items) + 1)
        x = gap
        for (slot, var), (cw, ch, px, py) in zip(row_items, dims):
            cat = cat_of(slot)
            ox, oy = x + px * cw * s, y + row_h - py * ch * s
            tx = lambda X, Y, ox=ox, oy=oy: (ox + X * s, oy - Y * s)
            fill = BACK if slot.endswith('-back') else FRONT
            draw_shapes(d, VARIANTS[cat][var], tx, fill, scale=s, ink_w=4)
            if labelled:
                d.rectangle([x, y + row_h - ch * s, x + cw * s, y + row_h], outline=(180, 180, 255, 255), width=2)
                cross(d, (ox, oy), r=8, w=3)
                d.text((x, y + row_h + 4), '%s/%s' % (slot, var), fill=RED, font=FONT_S)
            x += cw * s + gap
        y += row_h + 70
    img.save(os.path.join(OUT, '%s%s.png' % (name, '-labelled' if labelled else '')))
    return y


os.makedirs(OUT, exist_ok=True)
for lab in (False, True):
    assembled(lab)
    print('sheet A height used', sheet('parts-sheet-a', SHEET_A, [2, 4, 4], lab, 0.55))
    print('sheet B height used', sheet('parts-sheet-b', SHEET_B, [2, 3, 4, 2], lab, 0.5))

print('\ncategory      canvas(px)   pivot(x,y from bottom-left)  variants')
for cat in VARIANTS:
    w, h, px, py = canvas_for(cat)
    print('%-13s %4dx%-4d    (%.3f, %.3f)                 %s' % (cat, w, h, px, py, ', '.join(VARIANTS[cat])))
print('\nbones (child joint offsets from parent pivot, px @H1600):')
for p, cj in CHILD_JOINTS.items():
    for k, v in cj.items():
        print('  %s -> %s: %s' % (p, k, v))
