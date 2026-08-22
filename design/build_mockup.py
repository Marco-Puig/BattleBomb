# -*- coding: utf-8 -*-
"""Builds the chest-screen UI mockup with Michael's real art embedded."""
import base64, io, os

ART = r"C:\Users\Michael\Documents\BattleBomb\Assets\_BattleBomb\Art"
OUT = r"C:\Users\Michael\Documents\BattleBomb\design\chest-screen.html"

def uri(rel):
    p = os.path.join(ART, rel)
    assert os.path.exists(p), "missing art: " + p
    return "../Assets/_BattleBomb/Art/" + rel.replace(" ", "%20")

W = "UI/Items/Weapons/"
IMG = {
    'katana':   uri(W + "Katana.png"),
    'fire':     uri(W + "fire_sword.png"),
    'crystal':  uri(W + "crystal_sword.png"),
    'saber':    uri(W + "saberSword.png"),
    'staff':    uri(W + "staff.png"),
    'red':      uri(W + "red_sword.png"),
    'blue':     uri(W + "blue_sword.png"),
    'green':    uri(W + "green_sword.png"),
    'green2':   uri(W + "green_sword2.png"),
    'pirate':   uri(W + "Pirate_Sword.png"),
    'onyx':     uri(W + "Onyx_Ghoul.png"),
    'circle':   uri(W + "Circle_blade.png"),
    'helmet':   uri("UI/Items/Armor/Leather_Helmet.png"),
    'hp':       uri("UI/Items/Consumables/HealthPotion.png"),
    'mp':       uri("UI/Items/Consumables/ManaPotion.png"),
    'title':    uri("UI/Frontend/Title.png"),
}

# D33 ladder -> QualityColors.cs, exact values
Q = {
    'Battlescarred': '#9e8061',
    'Torn':          '#bfb899',
    'Rusty':         '#d1803d',
    'Shiny':         '#ffe652',
    'Pristine':      '#73e6ff',
    'Legendary':     '#ff8c26',
    'Mythical':      '#b866f2',
    'Godly':         '#ff4738',
}

# (icon-key|None, name, quality, count, locked)
BAG = [
    ('katana',  'Katana',          'Shiny',        1, False),
    ('fire',    'Flamebrand',      'Legendary',    1, False),
    ('crystal', 'Crystal Edge',    'Pristine',     1, False),
    ('saber',   'Saber',           'Rusty',        1, False),
    ('staff',   'Oak Staff',       'Torn',         1, False),
    ('helmet',  'Leather Helmet',  'Torn',         1, False),
    ('hp',      'Health Potion',   'Shiny',        4, False),
    ('mp',      'Mana Potion',     'Shiny',        2, False),

    ('red',     'Bloodfang',       'Godly',        1, True),
    ('blue',    'Tidecutter',      'Battlescarred',1, False),
    ('green',   'Verdant Blade',   'Shiny',        1, True),
    ('pirate',  'Corsair Cutlass', 'Rusty',        1, False),
    ('onyx',    'Onyx Ghoul',      'Mythical',     1, False),
    ('circle',  'Circle Blade',    'Pristine',     1, False),
    (None,      'Hunting Bow',     'Torn',         1, False),
    (None,      'Leather Boots',   'Rusty',        1, False),

    ('green2',  'Moss Sabre',      'Battlescarred',1, False),
    (None,      'Steel Chestplate','Pristine',     1, False),
    (None,      'Ember Stone',     'Legendary',    1, False),
    (None,      'Terrier',         'Shiny',        1, False),
]

COLS, ROWS = 8, 5
SELECTED = 0

cells = []
for i in range(COLS * ROWS):
    if i >= len(BAG):
        cells.append('<div class="cell empty"></div>')
        continue
    key, name, qual, count, locked = BAG[i]
    c = Q[qual]
    sel = ' sel' if i == SELECTED else ''
    art = (f'<img src="{IMG[key]}" alt="">' if key
           else f'<span class="noicon">{name}</span>')
    badges = ''
    if count > 1:
        badges += f'<span class="count">{count}</span>'
    if locked:
        badges += '<span class="lock">&#128274;</span>'
    cells.append(
        f'<div class="cell{sel}" style="--q:{c}" title="{qual} {name}">'
        f'<div class="art">{art}</div>{badges}</div>')

grid = "\n      ".join(cells)

ladder = "".join(
    f'<span class="rank"><i style="background:{v}"></i>{k}</span>' for k, v in Q.items())

html = f"""<title>Chest Screen</title>
<style>
  :root {{
    --ink:#dbe6f7; --dim:#8f9ebd; --panel:#1a2130; --inner:#242b40;
    --focus:#6b99eb; --better:#7de08a; --worse:#e07d7d; --coin:#e8c259;
    --edge:rgba(255,255,255,.07);
  }}
  * {{ box-sizing:border-box; }}
  body {{
    margin:0; background:#0b0e15; color:var(--ink);
    font:14px/1.45 "Segoe UI",system-ui,sans-serif; padding:24px;
  }}
  h1 {{ font-size:15px; letter-spacing:.14em; text-transform:uppercase;
       color:var(--dim); font-weight:600; margin:0 0 4px; }}
  .sub {{ color:var(--dim); font-size:13px; margin:0 0 20px; max-width:900px; }}
  .screen {{
    width:100%; max-width:1180px; aspect-ratio:16/9; margin:0 auto;
    background:linear-gradient(160deg,#1a2130,#151b28);
    border:1px solid var(--edge); border-radius:10px; overflow:hidden;
    display:flex; flex-direction:column;
    box-shadow:0 24px 60px rgba(0,0,0,.55);
  }}
  .topbar {{ display:flex; align-items:center; gap:16px;
             padding:10px 14px; border-bottom:1px solid var(--edge); }}
  .who {{ font-weight:600; letter-spacing:.03em; }}
  .coin {{ color:var(--coin); font-variant-numeric:tabular-nums; }}
  .cap {{ color:var(--dim); font-variant-numeric:tabular-nums; }}
  .close {{ margin-left:auto; background:#4d2628; border:1px solid #6d3436;
            padding:5px 12px; border-radius:5px; font-size:12.5px; }}
  .tabs {{ display:flex; gap:6px; padding:8px 14px 0; }}
  .tab {{ padding:6px 16px; font-size:12.5px; letter-spacing:.1em;
          text-transform:uppercase; color:var(--dim);
          border-bottom:2px solid transparent; }}
  .tab.on {{ color:var(--ink); border-bottom-color:var(--focus); }}
  .body {{ flex:1; display:flex; gap:12px; padding:12px 14px; min-height:0; }}
  .left {{ flex:0 0 60%; display:flex; flex-direction:column; gap:10px; min-height:0; }}
  .right {{ flex:1; background:var(--inner); border:1px solid var(--edge);
            border-radius:7px; padding:14px; min-height:0; overflow:hidden; }}
  .filters {{ display:flex; gap:4px; flex-wrap:wrap; }}
  .f {{ font-size:11.5px; padding:4px 10px; border-radius:4px; color:var(--dim); }}
  .f.on {{ background:rgba(107,153,235,.16); color:var(--ink);
           box-shadow:inset 0 0 0 1px rgba(107,153,235,.45); }}
  .grid {{ flex:1; display:grid; gap:6px; min-height:0;
           grid-template-columns:repeat({COLS},1fr); grid-template-rows:repeat({ROWS},1fr); }}
  .cell {{ position:relative; border-radius:6px; background:var(--inner);
           border:1px solid var(--edge); display:flex;
           align-items:center; justify-content:center; overflow:hidden; }}
  .cell.empty {{ background:rgba(255,255,255,.015); border-style:dashed;
                 border-color:rgba(255,255,255,.05); }}
  .cell:not(.empty) {{
    background:
      radial-gradient(circle at 50% 42%, color-mix(in srgb,var(--q) 26%,transparent), transparent 70%),
      var(--inner);
    border-color:color-mix(in srgb,var(--q) 55%,transparent);
  }}
  .cell.sel {{ border-color:var(--focus); box-shadow:0 0 0 2px rgba(107,153,235,.5),
               0 0 18px rgba(107,153,235,.3); }}
  .art {{ width:86%; height:86%; display:flex; align-items:center; justify-content:center; }}
  .art img {{ max-width:100%; max-height:100%; object-fit:contain;
              filter:drop-shadow(0 2px 4px rgba(0,0,0,.6)); }}
  .noicon {{ font-size:9px; line-height:1.15; letter-spacing:.02em; color:var(--dim);
             text-align:center; padding:3px; overflow-wrap:anywhere; }}
  .count {{ position:absolute; right:3px; bottom:2px; font-size:10.5px;
            font-weight:700; color:var(--ink); text-shadow:0 1px 3px #000; }}
  .lock {{ position:absolute; left:3px; top:2px; font-size:9px; opacity:.85; }}
  .actions {{ background:var(--inner); border:1px solid var(--edge); border-radius:7px;
              padding:9px 12px; font-size:12px; color:var(--dim); }}
  .actions b {{ color:var(--ink); font-weight:600; }}
  .hint {{ padding:8px 14px; border-top:1px solid var(--edge);
           font-size:11.5px; color:var(--dim); }}
  .hint em {{ color:var(--coin); font-style:normal; }}
  .iname {{ font-size:16px; font-weight:600; margin-bottom:2px; }}
  .imeta {{ font-size:11.5px; color:var(--dim); margin-bottom:10px; }}
  .vs {{ font-size:11.5px; color:var(--dim); margin-bottom:6px; }}
  .stat {{ display:flex; align-items:baseline; gap:8px; font-size:12.5px;
           padding:2px 0; border-bottom:1px solid rgba(255,255,255,.04); }}
  .stat .k {{ flex:1; color:var(--dim); }}
  .stat .v {{ font-variant-numeric:tabular-nums; font-weight:600; }}
  .stat .d {{ font-variant-numeric:tabular-nums; font-size:11.5px; min-width:44px;
              text-align:right; }}
  .up {{ color:var(--better); }} .dn {{ color:var(--worse); }}
  .ladder {{ display:flex; flex-wrap:wrap; gap:12px; margin:18px auto 0;
             max-width:1180px; font-size:11.5px; color:var(--dim); }}
  .rank {{ display:flex; align-items:center; gap:6px; }}
  .rank i {{ width:10px; height:10px; border-radius:2px; display:block; }}
  .note {{ max-width:1180px; margin:22px auto 0; color:var(--dim); font-size:13px; }}
  .note b {{ color:var(--ink); }}
</style>

<h1>Chest screen &mdash; icons on the left half</h1>
<p class="sub">Real art, real layout: the 8&times;5 grid keeps the left 60% and the detail panel keeps
the right 40%, exactly as <code>ChestScreen.Visuals.cs</code> builds it today. Cell tint and border
come from the item's rank on the D33 ladder.</p>

<div class="screen">
  <div class="topbar">
    <span class="who">P1 &mdash; Chest</span>
    <span class="coin">248 coin</span>
    <span class="cap">Sack 20/40</span>
    <span class="close">&#10005; Esc</span>
  </div>
  <div class="tabs">
    <span class="tab on">Item Sack</span><span class="tab">Hero</span>
  </div>
  <div class="body">
    <div class="left">
      <div class="filters">
        <span class="f on">All</span><span class="f">Weapons</span><span class="f">Armor</span>
        <span class="f">Pets</span><span class="f">Equipment</span><span class="f">Consumables</span>
      </div>
      <div class="grid">
      {grid}
      </div>
      <div class="actions">
        <b>Equip</b> &nbsp; <b>Upgrade</b> (17) &nbsp; <b>Combine&hellip;</b> &nbsp;
        <b>Sell</b> (12) &nbsp; <b>Lock</b>
      </div>
    </div>
    <div class="right">
      <div class="iname" style="color:{Q['Shiny']}">Shiny Katana</div>
      <div class="imeta">Shiny &nbsp;&middot;&nbsp; requires level 12</div>
      <div class="vs">vs Torn Hunting Knife</div>
      <div class="stat"><span class="k">Damage</span><span class="v">24</span><span class="d up">+6</span></div>
      <div class="stat"><span class="k">Swing</span><span class="v">0.12</span><span class="d up">+0.04</span></div>
      <div class="stat"><span class="k">Defence</span><span class="v">0</span><span class="d">&mdash;</span></div>
      <div class="stat"><span class="k">Weight</span><span class="v">3.5</span><span class="d dn">+0.8</span></div>
      <div class="stat"><span class="k">Crit</span><span class="v">7</span><span class="d up">+2</span></div>
      <div class="stat"><span class="k">Crit dmg</span><span class="v">45</span><span class="d">&mdash;</span></div>
      <div class="stat"><span class="k">Life steal</span><span class="v">0</span><span class="d">&mdash;</span></div>
    </div>
  </div>
  <div class="hint">Stick: move &nbsp; Light: select &nbsp; Heavy: back &nbsp;
    <em>Esc / Start or &#10005;: leave</em></div>
</div>

<div class="ladder">{ladder}</div>

<p class="note"><b>Look at the four cells without art</b> &mdash; Hunting Bow, Leather Boots, Steel
Chestplate, Ember Stone, Terrier. They fall back to a text stub. That gap is the real design
question on this screen, and it is what the rest of the roster will look like until more art exists.</p>
"""

with io.open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write(html)
print("wrote", OUT, os.path.getsize(OUT) // 1024, "KB")
