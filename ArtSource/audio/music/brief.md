# Brief: temporary music (`music/<key>`)

**What:** placeholder music to develop against: menus, the checkpoint room, a stage loop, a boss
loop, and three short stings. **It is temporary.** Region and boss music waits for the story
bible (D56), and the tracks that ship get remade in the final tool (AUDIO.md §4). Nothing here
names a place, a character or a story.

**The sound (the Art lane's assumption from your two references; change it freely):** a bouncy
orchestral-folk adventure, Rayman's whimsy with fiddle, flute and pizzicato strings, over punchy
modern drums and bass for Castle Crashers energy. Instrumental only, no vocals.

**Which tool:** as of September 2026 ChatGPT cannot generate audio; OpenAI's music tool is
reported but unreleased. The prompts below are plain descriptions, so they paste into any
text-to-music tool. Each also carries a short **tags** line for tools with a "style" box (Suno),
and a **settings** line for tools with sliders (AIVA: key, tempo, length). If ChatGPT gains music,
paste the description as is.

**Pipeline:** save each result into the inbox `ArtSource/_raw/` as `<key>.mp3` (the Art lane files it under
`ArtSource/audio/music/<key>/AI/_raw/`) (WAV if offered,
otherwise MP3), and keep the tool's name. The Art lane trims the loop points and files it as
`<key>__ai_v1.wav`.

---

## Title theme (`music/title-theme`)

```
Instrumental video game music for the title screen and menus of a cartoon fantasy co-op
brawler. Upbeat, heroic and playful, with a memorable, whistleable main melody. Orchestral folk
adventure: fiddle and tin whistle carry the tune over pizzicato strings, accordion chords, brass
stabs, and a punchy modern drum kit with bass. Bright and confident, a little mischievous. No
vocals. About 112 BPM, D major, 1 minute 30 seconds, and it must loop seamlessly: the end leads
straight back into the start.
Tags: orchestral folk, adventure, playful, heroic, fiddle, tin whistle, pizzicato, punchy drums, instrumental, video game
Settings: D major · 112 BPM · 1:30 · loop
```

## Checkpoint room (`music/checkpoint-room`)

```
Instrumental video game music for a calm safe room where players sort their loot, buy from a
shopkeeper and rest before the next fight. Warm, cosy and unhurried, with a gentle melody that
never demands attention, because players are reading item stats. Acoustic guitar, soft marimba,
light pizzicato strings, a quiet flute, and soft hand percussion. Low energy and no big peaks.
No vocals. About 84 BPM, F major, 2 minutes, looping seamlessly.
Tags: cozy, calm, acoustic guitar, marimba, pizzicato, soft percussion, shop music, instrumental, video game
Settings: F major · 84 BPM · 2:00 · loop
```

## Stage loop (`music/stage-fixture`)

```
Instrumental video game music for fighting through a side-scrolling stage in a cartoon fantasy
co-op brawler. Energetic, bouncy and driving, so it keeps players moving forward through waves
of enemies. A fiddle and flute melody over galloping strings, a punchy drum kit, slap bass and
brass hits. The energy stays steady from start to end, with no quiet breakdown. No vocals.
About 128 BPM, E minor with a lift to G major in the second half, 2 minutes 30 seconds, looping
seamlessly.
Tags: energetic, adventure, battle, orchestral folk, fiddle, galloping strings, punchy drums, slap bass, instrumental, video game
Settings: E minor · 128 BPM · 2:30 · loop
```

## Boss loop, placeholder (`music/boss-temp`)

```
Instrumental video game music for a boss fight in a cartoon fantasy brawler. Intense and
heavy, but with a comic, over-the-top edge rather than grim. Big pounding drums, low brass and
low strings, driving electric bass in eighth notes, sharp fiddle runs, and brass stabs on the
accents. Relentless, with no quiet section. No vocals and no choir. About 140 BPM, D minor,
2 minutes, looping seamlessly.
Tags: epic, intense, boss battle, big drums, brass, low strings, electric bass, fiddle, instrumental, video game
Settings: D minor · 140 BPM · 2:00 · loop
```

## Sting: stage clear (`music/sting-stage-clear`)

```
A short instrumental victory fanfare for a video game, about 5 seconds long: a quick triumphant
flourish on brass, fiddle and tin whistle with a drum roll, ending on a bright held major chord.
Cheerful and cartoonish. No vocals. D major, not looping.
Tags: victory fanfare, short sting, brass, fiddle, drum roll, cheerful, video game
Settings: D major · about 5 seconds · no loop
```

## Sting: wipe (`music/sting-wipe`)

```
A short instrumental game-over sting for a cartoon video game, about 4 seconds long: a comic,
gently sad descending phrase on muted trombone and pizzicato strings, ending on a soft low note.
Funny and sympathetic, never mocking or grim. No vocals. D minor, not looping.
Tags: game over, comic, sad trombone, pizzicato, short sting, video game
Settings: D minor · about 4 seconds · no loop
```

## Sting: level up (`music/sting-level-up`)

```
A very short instrumental level-up flourish for a video game, about 2 seconds long: a bright,
rising run on glockenspiel and strings that ends on a sparkling high major chord. Joyful. No
vocals. D major, not looping.
Tags: level up, flourish, glockenspiel, strings, sparkle, short sting, video game
Settings: D major · about 2 seconds · no loop
```
