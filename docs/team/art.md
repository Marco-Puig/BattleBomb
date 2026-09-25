# Lane: Art

**Mission:** the **art and audio track (D55)**: the art bible, the provenance system, the asset
list in draft order, and a brief plus an AI prompt for every asset — so Michael can generate drafts
now and redraw them by hand over time. Audio: the sound list and a licensed-library shortlist.

**Owns:** `docs/art/` · `ArtSource/` (drafts, outside Unity — see below) · this file.
**No writes under `Assets/`** — Unity imports anything there, which needs the sim.

**Read first (in order):** `CLAUDE.md` · `docs/team/PROTOCOL.md` · `docs/team/BOARD.md` · this file
· `docs/DECISIONS.md` D14, D15, D16, D33, D47, D55 · `docs/ROADMAP.md` §5.2 · `docs/GAME_DESIGN.md`
§1, §2.3, §4 · the project memory notes on the art library (two import mistakes that each took the
project down) and the quality ladder (Clean added, Godly held back).

---

## The rules of this lane

- **Michael generates the images; you write the prompts.** You cannot generate images here. Ask him
  which AI tool he is using first — prompt style differs by tool.
- **Every asset has provenance** (D55): a folder (`AI/` or `Hand/`), a filename (`__ai_v1`), and a
  row in the manifest. The game will reference **stable slots**, never files, so a hand-drawn
  replacement is one re-pointed entry.
- **D16 binds the drafts:** layered, cleanly separated body parts at high resolution, fitted to one
  shared rig (D15) — or a draft cannot be rigged.
- **No story content until the World lane's bible exists.** Heroes, regions, enemy families, and
  bosses wait for it. Do not invent characters or places to prompt for.
- **Binaries:** drafts live under `ArtSource/`, outside Unity. Git policy is O2 (no LFS; nothing near
  50 MB; iterate outside git, commit keepers). Propose which files get committed in your first
  report; the orchestrator decides.

## The work, in order (story-independent first)

1. **Provenance spec** → `docs/art/PROVENANCE.md`: folder layout under `ArtSource/`, filename
   convention, the manifest's format and columns (asset, slot, category, AI draft path, hand path,
   status, tool, date), the statuses, and how slots map into Unity (sprite library categories and
   labels for rig parts; item icons by definition id — see `ItemIcons` and `SimulationDriver.ItemIcons`).
   Write the requirements for the enforcement test M9 will build; do not build it.
2. **Art bible v0** → `docs/art/ART_BIBLE.md`: D16's direction made concrete — outline weight,
   palette per element, character lighting, how backgrounds stay out of the shadows' way (§2.3).
   Ask Michael for references and taste; write the prompts for a first set of style frames.
3. **The hero template** → the part list for one shared 2D skeleton: inspect the Templar Knight rig
   under `Assets/_BattleBomb/Art/Templar Knight/` and its source under
   `Assets/_BattleBomb/Art/Sprites/Characters/Templar Knight/`; propose parts, sizes, pivots, and
   layering; brief and prompt.
4. **Story-independent briefs:** item icons (from the definitions in `Data/Items` and the slots),
   effects (the four signature casts, aura, leap, statuses, hits — D39, D46), the drop glow.
5. **Audio v0** → `docs/art/AUDIO.md`: a sound list from the design docs (M9 will generate the exact
   list from the game's own events) and a shortlist of licensed sound-effect libraries, with their
   licence terms.

The asset manifest itself (`docs/art/MANIFEST.csv`, or the format your spec chooses) starts with
step 1 and grows with every brief.

---

## Waiting on

- **The World lane's story bible:** for heroes, regions, enemies, and bosses.
- **Michael's queue, not to be pressed:** licensed audio vs the public repo (the collaborator
  owns the repo; the orchestrator recommended making it private); the music tool (ChatGPT can't
  do audio: AIVA free or Suno Pro); the bible sign-off (walk him through changes since v0:
  Templar proportions, storybook environments, Earth ground cracks dropped); the Fire colour
  change; **a new pet art direction** (he raised it; he'll say when); the five HUD additions
  (yes or no to each, BOARD item 4).
- **Orchestrator address** (unchanged as of 2026-09-25):
  `uds:\\.\pipe\LOCAL\cc-msg-e4fdbd589a82ce7d4a7fdf41546e0095` (session "Battlebomb"). If a send
  fails, run ListAgents and message the non-lane BattleBomb session.

## Current state

**All image steps are done (round 1 + round 2).** GPT_PROMPTS shows 25 of 32, and the next step
is 26, the music (which waits on his tool choice).
**Round 2 (Michael, 2026-09-24):** the storybook redo landed: SF3b plus the 5 fixture pieces.
He likes them more than v1. I reviewed them (SF3b is on target; ground mean 80%, p95 84%) and
prepared them as `fixture-*__ai_v2` (6 files). `prepare.py` now **cuts each backdrop's repeat
where its two edges already match**; the old edge-to-edge blend left half-transparent ghost trees
at the join of see-through layers. The mid backdrop keeps 70% of its width, the far and near
backdrops about 95%. The preview (a scratch file, not committed) stacks the layers with two SF1
knights; greyscale shadows read.
**The terrier redo is parked** (Michael): no attempt beat v1, so v1 stays, and he wants to change
the pet art direction later. The step is removed from GPT_PROMPTS, the cut_sheets job is gone, and
the brief section is marked parked.
**COMMITTED 2813733.** The lane is idle until Michael picks a music tool or a pet direction, or
the World bible lands (orchestrator, 2026-09-24).

**GPT progress (2026-09-25):** steps 1–25 done (SF1–SF6, Sheets A and B, SF3b, the 5 fixture
v2 pieces, 7 effect kits, the weapon sheet, icon Batches A–C). Steps 26–32 (7 music tracks) are
blocked on the music tool. The terrier redo is out of the queue (parked).

**Tools and workflow (run everything from the repo root):**
- `PYTHONIOENCODING=utf-8 python ArtSource/make_gpt_prompts.py [--sort]` rebuilds
  `ArtSource/GPT_PROMPTS.md` from the briefs, ticking steps whose file exists; `--sort` first moves
  results from the inbox to their `AI/_raw/` folders. **Never hand-edit the output**; the phase
  and step order, save paths and attach overrides live in its `PHASES` list.
- **The inbox is `ArtSource/_raw/`** (git-ignored, like every `_raw/`). Michael sometimes saves into
  `ArtSource/_style/AI/_raw/` instead; `--sort` checks both. Music saves are
  `<track>.mp3` or `.wav`, filed into `ArtSource/audio/music/<track>/AI/_raw/`.
- `python ArtSource/cut_sheets.py [effects|icons|weapons]` cuts grid sheets. The effect `KITS`,
  loot `RECTS`, icon `JOBS` (a `'skip'` cell is cut, then thrown away) and `WEAPONS` (grip on
  the HERO_TEMPLATE section 9 pivot) all live there.
- `python ArtSource/environments/fixture/prepare.py` prepares the fixture kit. It prefers `-v2`
  raws and writes `__ai_v2`, levels the ground to p95 84%, cuts backdrop repeats at matching
  edges (`best_join`), and splits the frames into a and b.
- `python ArtSource/rig/hero-template/cut_parts.py [--measure]` fits the knight's parts to the
  section 4 canvases. `guides/make_guides.py` draws the guides.
- To preview a stage, stack far, then mid, then near backdrops over the tiled ground, with the
  frames at the corners and SF1 knights with ellipse shadows, then check it in greyscale. The
  scratch script was not kept; rebuild it with PIL in the scratchpad if needed.
- **Traps:** non-ASCII text (§, …, ✓) passed through a bash heredoc gets mangled, so use
  Edit/Write for those and set `PYTHONIOENCODING=utf-8` to print them. Importing the tools creates
  `__pycache__`; delete it. MANIFEST.csv notes that contain commas need quotes; check with
  `csv.reader` (12 columns, 42 rows). If a new folder is missing from `git status`, run
  `git check-ignore -v`.
HUD canvas: https://claude.ai/artifact/Uk3zqg3FFVMfyH9JPDiaRj (approved direction; exported
2026-09-24). To revise, `read` from the artifact first.

**Provenance findings, 2026-09-24 (raised with Michael and the orchestrator):**
- Michael answered: Low_Swordman, Templar Knight, the item icons, and the front-end art are
  "made by you or collaborator"; the world props (Door, Door_Alt, Asteroid) are a bought or free pack.
- **Evidence against the packs being hand-made:** `Art/AI/maincharactertemp.ai` is
  **byte-identical** to `Sprites/Characters/Templar Knight/AI/Templar Knight.ai` (same size
  1173275, same MD5 412de5b76d, same XMP DocumentID). Both were created 2017-10-20 at UTC+07:00
  in Illustrator CS5.1. The Swordman PSD was created 2017-10-31 at UTC+09:00 in Photoshop CS5. The
  2019 project postdates both, and the folder layouts match commercial asset packs.
- **The repo is PUBLIC**: github.com/Marco-Puig/BattleBomb. If the packs are third-party, their
  source files are publicly downloadable from it. The PROVENANCE §9 git policy would also make
  Michael's hand masters and all AI drafts public.
- **Michael's decision (after seeing the evidence): the packs are their own work, keep as is;
  the public repo is fine for art.** Recorded that way, with the 2017 dates noted in the manifest.
- World props: third-party, licence unknown, **referenced by nothing**, so delete at M9.
- The memory note `battlebomb-art-library` calls maincharactertemp.ai "Michael's own character
  source". That is wrong (it is a copy of the Templar file); tell the orchestrator, who owns memory.

**HUD survey facts (2026-09-24, Explore agent):**
- The HUD is all IMGUI placeholders. `UI/Combat/PlayerHealthBars.cs`: top-centre, one 240×22 bar per
  player **stacked**, reading `P1 Lv3  87/100` (with `★2` for prestige, `DOWN`), plus 4px XP
  `#F2CC40` and mana `#4D8CFF` strips, a splash-cost tick, `loot {n}` and `potion x{n} (cd)`.
  No wallet on the HUD.
- `ReviveHud.cs`: `P1 DOWN — Light on the beat`, a 90×10 progress bar and a pulsing square
  heartbeat; `WIPED OUT` banner.
- `LootHud.cs`: floats over the drop; name in its quality colour, one core-stats line joined with
  ` · `, affix lines, `Upgrades 0/n`, `Requires level n`, `Light to grab`. **No compare-vs-worn,
  no rank name, no price.** Sack full: a red ✕ `#EB3D38` over the player, with `used/cap`.
- `DamageNumbers.cs`: normal `#FFED59`, crit `#FF7326` ×1.4 (**same orange as Fire and
  Legendary**), DoT `#FF9E4D` ×0.7. Size 30, not wired to settings.
- `FrontendFlow.cs`: title (`Continue`/`Start`); character select for 2 slots with only a name
  (the roster is one character, `Default`); chapter select (`Fixture Chapter`, 2 stages, tier
  row Normal/Hard/Nightmare). `ResultsScreen.cs`: `CHAPTER COMPLETE`, per player
  `grabbed n  level n`. Pause **is** Settings (`SettingsMenu.cs`): auto-equip, auto-sell,
  debug rows, return to chapter select. **No volume, damage-number or controls settings.**
- **UI Pass 01 palette (the chest only):** Bed `#1c1424`, Bone `#f6efe2`, Brass `#c9ab6a`,
  BrassDim `#7d6636`, OnBrass `#231a2b`, Board `#2e2239`, BoardDeep `#1e1628`, Well `#120d18`,
  Gold `#edc65a`, Up `#63c96e`, Down `#e0574f`, Muted = Bone 62%, Faint = Bone 40%. Fonts:
  Passion One = display, Archivo = UI, Space Mono = small caps and labels. Legendary cell =
  octagon, Mythical = hexagon. The older graybox palette is in `UiBuild.cs`.

**Facts gathered (so they survive a reset):**
- Michael's own character source `Art/AI/maincharactertemp.ai` is an Illustrator file (PDF-based,
  layered) — he draws in vector. The Templar Knight source is `.ai` + 10 `.eps` parts + a Spriter
  `Animations.scml` under `Sprites/Characters/Templar Knight/PNG/Vector Parts/`.
- Templar Knight rig parts (`Art/Templar Knight/Graphics/`): Head 480², Body 320², four limbs and
  two hands 128² each, Sword 400×128, SlashFX 496². Ten parts: Head, Body, L/R Arm, L/R Hand,
  L/R Leg, Sword, SlashFX. Package: `com.unity.2d.animation` 15.1.0.
- Item icons are joined by **definition id**: `ItemDefinition.Icon` (Sprite, presentation-only)
  → `ItemIconLibrary.For(id)`, reached via `SimulationDriver.ItemIcons`; a missing icon is normal
  and draws a placeholder plate. 13 item definitions in `Data/Items/` (ids 1–13); only 3 have
  icons (1 Leather Helmet, 9 Health, 12 Mana). 12 weapon PNGs in `Art/UI/Items/Weapons/` are
  unassigned. Existing icons are 375–670 px square RGBA.
- Enums: ItemSlot Helmet0 Chest1 Boots2 Weapon3 Pet4 Equipment5 Consumable6; WeaponClass
  Sword1 Bow2; PetClass Attacker1 StatBoost2 Unique3.

## Next steps

1. Music: when Michael picks a tool and saves tracks, `--sort` them, loop-trim to WAV, and record
   `ai_tool` in the manifest.
2. Pets: when Michael is ready to set the new pet direction, bring him a few concrete options
   (a style reference each), then write a new pet brief; the terrier gets `__ai_v2` from it.
3. When images land: review against bible section 10, cut or prepare them (parts: HERO_TEMPLATE
   section 4; icons: 512 px; effects: EFFECTS section 6; ground: 55–85% brightness), and update
   the manifest.

## Answers and decisions

- 2026-09-24 (Art lane, HERO_TEMPLATE.md): **rigid cut-out parts, bends by label swap**, 21
  drawings per hero (head x4, torso, arm x2x2, hand x2x3, leg x2x2, foot x2) plus optional
  back-piece and waist-piece; weapon is an item, not in the skin. Fixed canvas and pivot per part
  (section 4). Head 0.39 H, root at hips 0.30 H. First skin = the SF1 knight (a stand-in, not a hero).
  Filenames `<asset>-<part>-<label>__ai_vN.png`.
- 2026-09-24 (Michael): **round 2 accepted**: he likes the storybook backgrounds more than v1.
  **The terrier stays at v1**: "the dog never came out better than the v1. lets leave it for now
  but I will want to change the pet art direction". So the "simple, chunky pets" line below is
  provisional until he sets the new direction.
- 2026-09-24 (Michael): **environments = "Rayman storybook"**: painted and glowing, but with
  simplified storybook shapes and rich, bright colour, never semi-realistic or hazy (round-1
  review). **Pets = simple, like Castle Crashers / Dungeon Defenders pets**: few chunky shapes,
  flat colour, no fur texture.
- 2026-09-24 (Michael): **hero proportions = the Templar Knight's** ("castle-crashers sized
  proportions from templar knight... didn't want to change that"). **Supersedes** the earlier
  "head 1/3–2/5" answer, which came from a misworded question (I had described 1/3 as "like
  your Templar rig"; the Templar's head is actually about 0.66 H). Measured on the Templar idle
  frame: chin 0.35 H, body to 0.08 H, stub legs. The hero template is now v1: rigid parts as in
  the Templar rig, **no elbows, knees or separate feet**, legs include the boot; 15 drawings
  (head x4, torso, arm x2, hand x2x3, leg x2) plus optional back and waist pieces. Root 0.11 H,
  neck 0.34 H. Bow resized to 0.40 H, arrow to 0.25 H.
- 2026-09-24 (Michael): **approved the HUD and menus canvas direction**: "you chose the right
  directions... I really like the general style... we may change some colors slightly" once
  everything is in place. Exported to `design/hud-menus/` for the Builder.
- 2026-09-24 (Michael): **temporary music via "GPT"**, and a single GPT prompt file to feed.
  Finding (web, 2026-09-24): **ChatGPT cannot generate audio**; OpenAI's music tool is
  reported but unreleased. So the music prompts are tool-neutral (description + tags +
  settings). Temporary options told to Michael: AIVA free (non-commercial, with credit, a few
  downloads a month) or Suno Pro; ChatGPT if its music feature arrives.
- 2026-09-24 (Michael): **HUD taste calls.** Player panels **along the top** (P1 top-left, P2
  next to them; the ground and its shadows fill the lower half). The **loot card gets ▲▼ vs worn
  plus the rank name.** Panel contents are the **Castle Crashers set**: portrait (an element
  emblem until hero art exists), level, health, mana with the splash-cost tick, a thin XP strip,
  and the quick-use slot with a cooldown dial; the `loot n` counter is dropped. Also in the
  plan: crit numbers become white-hot (the current `#FF7326` clashes with Fire and Legendary);
  a real pause menu separate from settings; settings gains volume and damage-number options.
- 2026-09-24 (Michael): **HUD and menu mockups go on a Claude Design canvas** (the Artifact tool's
  "Design" type, `type_url` https://claude.ai/artifact/QKN21svewxgyPb6SYRqWnd; no design system is
  attached, so carry over UI Pass 01's fonts, colours and frames from the game). Every screen is an
  artboard; Michael and the collaborator comment there. Once approved, save the HTML into
  `design/` for the Builder. Note: DesignSync is only for the user-started `/design-sync` skill;
  don't use it.
- 2026-09-24 (Orchestrator): **fixture environment kit approved, minimal**: ground strip, backdrop,
  foreground framing, labelled fixture-only with no region identity. **Then the HUD and menu
  mockups**; the Art lane may now write **new files under `design/`** (PROTOCOL). Ask Michael the
  format first. Browser-pane previews pause during QUIET.
- 2026-09-24 (Art lane, AUDIO.md, web-checked): **Udio has no downloads** (UMG deal);
  **ElevenLabs Music self-serve excludes commercial games**; **Suno paid = licence, not ownership**;
  **AIVA Pro = ownership + MIDI**. SFX shortlist: Sonniss GDC (free, no credit, no AI training),
  Kenney (CC0), ZapSplat (credit unless Premium), Freesound (per-file; no NC), BOOM (paid,
  single user). **Only CC0 is safe in the public repo.**
- 2026-09-24 (Art lane, EFFECTS.md): **effects are sprite kits animated by Unity particles**, not
  AI flipbooks; 7 kits = 7 slots (fire, ice, earth, air, melee, loot, feedback). Loot glow is
  greyscale, tinted by QualityColors. No dark ground marks from effects (bible section 4). Weapons:
  drawn pointing up, pivot = grip centre (HERO_TEMPLATE section 9).
- 2026-09-24 (Orchestrator): **git trap**: `.gitignore`'s `Icon?` rule was hiding
  `ArtSource/icons/` (fixed in 05919b0). If a new folder doesn't show in
  `git status --untracked-files=all`, run `git check-ignore -v <path>` and tell the orchestrator.
- 2026-09-24 (Orchestrator): the bible review is item 5 in Michael's queue; don't press. The Fire
  colour change and unlit-painted-cel recommendation are parked on the M9 backlog until he decides.
- 2026-09-24 (Michael): **art direction = Castle Crashers + Rayman Legends; clean vector
  outlines; big-head heroes** (proportions later corrected to the Templar's; see above). Bible line: "Castle Crashers-style
  characters living in a Rayman Legends world". Differentiation is carried by the lit, lush 3D world,
  elemental climate, cel tone and rim light, and character design (bible §2).
- 2026-09-24 (Art lane, measured): Templar ink `#1A1917`, silhouette line about 1/36 of the character's height.
  Element colours clash with quality colours; rule = separate by form (loot owns ring-and-beam);
  proposal: Fire `#FF7326` → `#FF4A1C` (Michael to confirm; Builder data change at M9).
- 2026-09-24 (Michael): **ArtSource layout is side by side** — one folder per asset holding
  `brief.md`, `AI/` (with `_raw/`), and `Hand/`. Not two mirrored trees.
- 2026-09-24 (Art lane, in PROVENANCE.md): a **slot is the unit swapped whole** — a whole
  character skin, not one body part (refines D55's "Fire hero — torso" example; flagged to the
  orchestrator). **Traced/edited AI stays AI**; Hand = drawn fresh over a locked reference layer.
  Correction to what Michael was told in the tool question: Image Trace does NOT make a draft Hand.
- 2026-09-24 (Michael): **the AI image tool is ChatGPT images.** Prompts are written for it: long,
  exact, layout-precise; transparent backgrounds; follow-up edits against a reference image for
  consistency. Drafts are raster; Michael redraws over them in Illustrator (never traces).
- 2026-09-24 (Orchestrator): **reach the orchestrator at the `from=` address of its last message**,
  not by name — Michael renames sessions (PROTOCOL rule 6, 853ad08). Last known:
  `uds:\\.\pipe\LOCAL\cc-msg-e4fdbd589a82ce7d4a7fdf41546e0095` (session "Battlebomb"; the address
  changes when that session restarts, so run ListAgents if a send fails). Hero, region, enemy and boss
  prompts stay parked until the World bible lands; the orchestrator will say when.
- 2026-09-24 (Michael): AI drafts first for all art, organised so he can tell AI from his own work
  and redraw each (D55). Audio: AI-drafted music plus licensed sound-effect libraries.

## Log

- 2026-09-25 — PREPARE TO COMPACT: lane file brought up to date (GPT progress, tools, traps, address); READY TO COMPACT sent.
- 2026-09-24 — COMMITTED 2813733 (fixture v2, terrier parked); D16 marks pets provisional (2832ea3). Idle.
- 2026-09-24 — Round 2 in: storybook fixture v2 prepared (clean repeat joins); terrier redo parked, pet direction to be redone; DONE sent.
- 2026-09-24 — Round 1 art finished; icons and weapons cut; review → storybook environments + simple pets; round 2 prompts queued; DONE sent.
- 2026-09-24 — GPT steps 9–20 processed (step 11 missing): single inbox, sort, fixture prepare, 49 effect pieces; DONE sent.
- 2026-09-24 — Reviewed GPT steps 1–8; the prompt file tracks progress; knight cut into 15 parts + preview; fixture backdrops made building-free; DONE sent.
- 2026-09-24 — Proportions switched to the Templar's (Michael); hero template v1 (15 parts); bible, SF1, parts sheets, guides, and queue rebuilt; DONE sent.
- 2026-09-24 — GPT_PROMPTS run order aligned to ROADMAP section 5.2 (env before effects; weapons before icons); DONE sent.
- 2026-09-24 — COMMITTED: HUD export, GPT_PROMPTS, music brief. The orchestrator restarted, so its address changed.
- 2026-09-24 — Canvas approved and exported to design/hud-menus/; GPT_PROMPTS.md (31 prompts) + generator; temporary music brief; DONE sent.
- 2026-09-24 — HUD and menus canvas v1 published (11 artboards); waiting on Michael's review.
- 2026-09-24 — Michael chose a Claude Design canvas for the HUD and menus; requirements survey started.
- 2026-09-24 — COMMITTED 80b5030 (fixture kit).
- 2026-09-24 — Fixture environment kit brief (6 pieces, measured ground acceptance); DONE sent.
- 2026-09-24 — COMMITTED b144d79 (audio v0).
- 2026-09-24 — Audio v0 (sound list, SFX shortlist, music tool research); DONE + public-repo QUESTION sent.
- 2026-09-24 — COMMITTED 8f16b69 (effects, weapons).
- 2026-09-24 — Effects spec + 7 kit prompts + weapons brief + manifest (34 rows); DONE sent.
- 2026-09-24 — COMMITTED 3ce0274 (item icons).
- 2026-09-24 — Item icon brief (3 batches, 10 icons) + manifest rows; DONE sent.
- 2026-09-24 — COMMITTED f9b6611 (hero template).
- 2026-09-24 — Hero template spec + parts-sheet prompts + generated guides written; DONE sent.
- 2026-09-24 — COMMITTED 75fcc3b (art bible v0, style-frame brief).
- 2026-09-24 — Art bible v0 + six style-frame prompts written; DONE sent.
- 2026-09-24 — COMMITTED 4435833 (classification). Orchestrator corrected memory; M9 backlog 85d567d.
- 2026-09-24 — Existing art classified with Michael; maincharactertemp.ai found to be a copy of the Templar .ai; DONE sent.
- 2026-09-24 — COMMITTED dc58c91 (provenance spec, manifest, README). Orchestrator added the `_raw/` ignore and amended D55 (0a1406d).
- 2026-09-24 — Provenance spec v0 + seeded manifest written; DONE sent.
- 2026-09-24 — ONLINE; required reading done; facts on rig, icons, and source art recorded.
- 2026-09-24 — Lane seeded by the orchestrator.
