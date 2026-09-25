# Builds ArtSource/GPT_PROMPTS.md from the briefs, in the order the prompts must be run.
# The briefs stay the only place a prompt is written; rerun this after changing one:
#   python ArtSource/make_gpt_prompts.py          rebuild, ticking steps whose result exists
#   python ArtSource/make_gpt_prompts.py --sort   first move results from the inbox to their folders
import datetime
import os
import re
import shutil
import sys

ROOT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(ROOT)
RAW = 'ArtSource/{}/AI/_raw/{}'
INBOX = 'ArtSource/_raw'
INBOXES = [INBOX, 'ArtSource/_style/AI/_raw']

PHASES = [
    ('Style frames', 'One chat for the whole phase. Each frame builds on the approved one before it.', [
        ('_style/brief.md', 'SF1', None, RAW.format('_style', 'sf1.png')),
        ('_style/brief.md', 'SF2', None, RAW.format('_style', 'sf2.png')),
        ('_style/brief.md', 'SF3', None, RAW.format('_style', 'sf3.png')),
        ('_style/brief.md', 'SF4', None, RAW.format('_style', 'sf4.png')),
        ('_style/brief.md', 'SF5', None, RAW.format('_style', 'sf5.png')),
        ('_style/brief.md', 'SF6', None, RAW.format('_style', 'sf6.png')),
    ]),
    ('Hero template: the knight cut into parts', 'Same chat as the style frames, so it remembers the knight.', [
        ('rig/hero-template/brief.md', 'Sheet A', None, RAW.format('rig/hero-template', 'sheet-a.png')),
        ('rig/hero-template/brief.md', 'Sheet B', None, RAW.format('rig/hero-template', 'sheet-b.png')),
    ]),
    ('Storybook world (round 2: test stage only, never ships)',
     'Your call after round 1: a storybook world, not the semi-realistic first try. Make SF3b first; '
     'the five stage pieces attach it.', [
        ('_style/brief.md', 'SF3b', None, RAW.format('_style', 'sf3b.png')),
        ('environments/fixture/brief.md', '1. Ground', 'your approved SF3b', RAW.format('environments/fixture', 'ground-v2.png')),
        ('environments/fixture/brief.md', '2. Backdrop far', 'your approved SF3b', RAW.format('environments/fixture', 'backdrop-far-v2.png')),
        ('environments/fixture/brief.md', '3. Backdrop mid', 'your approved SF3b', RAW.format('environments/fixture', 'backdrop-mid-v2.png')),
        ('environments/fixture/brief.md', '4. Backdrop near', 'your approved SF3b', RAW.format('environments/fixture', 'backdrop-near-v2.png')),
        ('environments/fixture/brief.md', '5. Foreground frames', 'your approved SF3b', RAW.format('environments/fixture', 'frames-v2.png')),
    ]),
    ('Effect kits', 'One sheet per kit.', [
        ('effects/brief.md', 'Fire', None, RAW.format('effects', 'fire.png')),
        ('effects/brief.md', 'Ice', None, RAW.format('effects', 'ice.png')),
        ('effects/brief.md', 'Earth', None, RAW.format('effects', 'earth.png')),
        ('effects/brief.md', 'Air', None, RAW.format('effects', 'air.png')),
        ('effects/brief.md', 'Melee', None, RAW.format('effects', 'melee.png')),
        ('effects/brief.md', 'Loot glow', None, RAW.format('effects', 'loot.png')),
        ('effects/brief.md', 'Feedback', None, RAW.format('effects', 'feedback.png')),
    ]),
    ('Held weapons', 'Before the icons: M9 needs the knight holding his knife; the weapon icons are drawn from this sheet.', [
        ('weapons/brief.md', 'The weapon sheet', None, RAW.format('weapons', 'sheet.png')),
    ]),
    ('Item icons', 'Nothing in M9 waits on these; missing icons show a placeholder plate. Attach SF6 each time.', [
        ('icons/items/brief.md', 'Batch A', None, RAW.format('icons/items', 'batch-a.png')),
        ('icons/items/brief.md', 'Batch B', None, RAW.format('icons/items', 'batch-b.png')),
        ('icons/items/brief.md', 'Batch C', None, RAW.format('icons/items', 'batch-c.png')),
        ('icons/items/brief.md', 'Terrier redo', None, RAW.format('icons/items', 'terrier-v2.png')),
    ]),
    ('Temporary music', 'ChatGPT cannot make audio yet (September 2026). Paste these into the music tool you use; see the note in ArtSource/audio/music/brief.md.', [
        ('audio/music/brief.md', 'Title theme', 'nothing', 'ArtSource/audio/music/title-theme/AI/_raw/'),
        ('audio/music/brief.md', 'Checkpoint room', 'nothing', 'ArtSource/audio/music/checkpoint-room/AI/_raw/'),
        ('audio/music/brief.md', 'Stage loop', 'nothing', 'ArtSource/audio/music/stage-fixture/AI/_raw/'),
        ('audio/music/brief.md', 'Boss loop', 'nothing', 'ArtSource/audio/music/boss-temp/AI/_raw/'),
        ('audio/music/brief.md', 'Sting: stage clear', 'nothing', 'ArtSource/audio/music/sting-stage-clear/AI/_raw/'),
        ('audio/music/brief.md', 'Sting: wipe', 'nothing', 'ArtSource/audio/music/sting-wipe/AI/_raw/'),
        ('audio/music/brief.md', 'Sting: level up', 'nothing', 'ArtSource/audio/music/sting-level-up/AI/_raw/'),
    ]),
]


def section(text, heading):
    match = re.search(r'^## ' + re.escape(heading) + r'.*?$(.*?)(?=^## |\Z)', text, re.M | re.S)
    if not match:
        raise SystemExit('No section starting "## %s"' % heading)
    return re.sub(r'^\d+\.\s*', '', match.group(0).splitlines()[0][3:]), match.group(1)


def attach_of(body):
    match = re.search(r'\*\*Attach[^*]*?:\*\*\s*(.+?)(?=\n\*\*|\n\n|\Z)', body, re.S)
    return ' '.join(match.group(1).split()) if match else None


def inbox_name(save_as):
    parts = save_as.rstrip('/').split('/')
    return parts[-3] if save_as.endswith('/') else os.path.splitext(parts[-1])[0]


def filed(save_as):
    path = os.path.join(REPO, save_as)
    if save_as.endswith('/'):
        return os.path.isdir(path) and any(os.scandir(path))
    return os.path.exists(path)


def in_inbox(save_as):
    stem = inbox_name(save_as)
    for box in INBOXES:
        folder = os.path.join(REPO, box)
        if not os.path.isdir(folder):
            continue
        for entry in os.scandir(folder):
            if entry.is_file() and os.path.splitext(entry.name)[0] == stem:
                return entry.path
    return None


def is_done(save_as):
    return filed(save_as) or in_inbox(save_as) is not None


def sort_inbox():
    for _, _, entries in PHASES:
        for _, _, _, save_as in entries:
            found = in_inbox(save_as)
            if filed(save_as) or not found:
                continue
            if save_as.endswith('/'):
                dest = os.path.join(REPO, save_as, os.path.basename(found))
            else:
                dest = os.path.join(REPO, os.path.dirname(save_as), inbox_name(save_as) + os.path.splitext(found)[1])
            if os.path.normcase(os.path.abspath(found)) == os.path.normcase(os.path.abspath(dest)):
                continue
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            shutil.move(found, dest)
            print('filed', os.path.relpath(found, REPO), '->', os.path.relpath(dest, REPO))


def save_line(save_as, done):
    if save_as.endswith('/'):
        name = '`%s.mp3` (or `.wav`)' % inbox_name(save_as)
    else:
        name = '`%s`' % os.path.basename(save_as)
    return '- **Save as:** %s in `%s/`%s' % (name, INBOX, '  (saved)' if done else '')


def prompt_of(body):
    match = re.search(r'```\n(.*?)\n```', body, re.S)
    if not match:
        raise SystemExit('No prompt block')
    return match.group(1)


if '--sort' in sys.argv:
    sort_inbox()

steps = []
for title, note, entries in PHASES:
    for brief, heading, attach_override, save_as in entries:
        text = open(os.path.join(ROOT, brief), encoding='utf-8').read()
        full_heading, body = section(text, heading)
        steps.append((title, note, brief, full_heading, body, attach_override, save_as, is_done(save_as)))

done_count = sum(1 for st in steps if st[7])
next_index = next((i for i, st in enumerate(steps) if not st[7]), None)
if next_index is None:
    where = '**All %d steps are done.**' % len(steps)
else:
    where = '**Done: %d of %d. Next: step %d, %s** (%s).' % (
        done_count, len(steps), next_index + 1, steps[next_index][3], steps[next_index][0])

out = [
    '# GPT prompts: feed these in order',
    '',
    '**Generated by `ArtSource/make_gpt_prompts.py` from the briefs. Do not edit this file;',
    'ask the Art lane to change a brief or to refresh your progress.**',
    '',
    '## Where you are',
    '',
    where,
    '',
    '**Save every result into one folder, `ArtSource/_raw/`,** named as the step says. A step is',
    'ticked ✓ once its file is there (or the Art lane has filed it). Progress as of %s.' % (
        datetime.date.today().isoformat()),
    '',
]
for i, st in enumerate(steps):
    mark = '✓' if st[7] else ('▶' if i == next_index else ' ')
    out.append('- %s %d. %s' % (mark, i + 1, st[3]))
out += [
    '',
    'How to use it:',
    '1. Go down the list in order. Later prompts attach images you approved earlier.',
    '2. For each step: attach what it says, paste the prompt, and generate.',
    "3. If a result is close but wrong, **edit, don't re-roll**: \"Keep everything the same; change only …\".",
    '4. Save the result into `ArtSource/_raw/` with the name the step gives, then tell the Art lane.',
    '   It files it, reviews it against the art bible, cuts sheets into parts, and records it in',
    '   the manifest.',
    '',
    'Paths starting `Assets/` or `ArtSource/` are inside the BattleBomb folder.',
    '',
]
current_phase = None
for i, (title, note, brief, full_heading, body, attach_override, save_as, done) in enumerate(steps):
    if title != current_phase:
        current_phase = title
        out += ['---', '', '## ' + title, '']
        if note:
            out += ['*' + note + '*', '']
    attach = attach_override or attach_of(body) or 'nothing'
    status = '✓ ' if done else ('▶ NEXT: ' if i == next_index else '')
    out += [
        '### %s%d. %s' % (status, i + 1, full_heading),
        '',
        '- **Attach:** ' + attach,
        save_line(save_as, done),
        '- *Brief:* `ArtSource/' + brief + '`',
        '',
        '```',
        prompt_of(body),
        '```',
        '',
    ]

open(os.path.join(ROOT, 'GPT_PROMPTS.md'), 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
print('GPT_PROMPTS.md: %d prompts, %d done' % (len(steps), done_count))
