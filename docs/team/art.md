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
- **Michael's queue, not to be pressed:** item 6, licensed audio vs the public repo (the
  collaborator owns the repo; the orchestrator recommended making it private); item 7, the
  music tool (AIVA recommended); item 5, the bible sign-off (walk him through changes since v0:
  Earth ground cracks dropped); the Fire colour change; running SF1–SF6.

## Current state

**Sent as DONE; do not touch until COMMITTED:** `design/hud-menus/` (11 standalone HTML exports
plus README), `ArtSource/GPT_PROMPTS.md` (31 prompts in run order), `ArtSource/make_gpt_prompts.py`,
`ArtSource/audio/music/brief.md` (7 temporary tracks), `ArtSource/weapons/brief.md` (+ heading),
`docs/art/MANIFEST.csv` (42 rows), `docs/team/art.md`.

**`GPT_PROMPTS.md` is generated.** Edit the briefs, then run
`python ArtSource/make_gpt_prompts.py`. Never hand-edit the output. The phase and step order,
save paths and attach overrides live in the script's `PHASES` list.

**Now waiting on Michael to run the queue.** After SF1 and SF3 he shows them to me: review
against bible section 10, adjust the style block if needed, and rebuild the file.
HUD canvas: https://claude.ai/artifact/Uk3zqg3FFVMfyH9JPDiaRj (approved direction; exported
2026-09-24). To revise, `read` from the artifact first.

**Waiting on Michael (no pressing; bible review is item 5 on his queue):** run SF1 to SF6, then
the Sheet A and B parts prompts. When `sheet-a.png` or `sheet-b.png` land in
`ArtSource/rig/hero-template/AI/_raw/`, **the Art lane cuts them** onto the section 4 canvases with
Python (PIL) and sends back an assembled preview.

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

1. When images or music land in `ArtSource/**/AI/_raw/`: review against bible section 10, cut
   sheets onto canvases with PIL (parts: HERO_TEMPLATE section 4; icons: 512 px; effects:
   EFFECTS section 6; ground: measure brightness 55–85%), rename to `__ai_v1`, and update the
   manifest rows (`ai-draft`, `ai_path`, `ai_tool`, `date`).
2. If SF1 or SF3 needs a style change: edit the style block in the briefs (bible section 9 too),
   rerun `make_gpt_prompts.py`, and tell Michael which steps changed.
3. Music: once Michael picks a real tool, record `ai_tool`; loop-trim to WAV.

## Answers and decisions

- 2026-09-24 (Art lane, HERO_TEMPLATE.md): **rigid cut-out parts, bends by label swap**, 21
  drawings per hero (head x4, torso, arm x2x2, hand x2x3, leg x2x2, foot x2) plus optional
  back-piece and waist-piece; weapon is an item, not in the skin. Fixed canvas and pivot per part
  (section 4). Head 0.39 H, root at hips 0.30 H. First skin = the SF1 knight (a stand-in, not a hero).
  Filenames `<asset>-<part>-<label>__ai_vN.png`.
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
  outlines; chunky big-head heroes (head 1/3–2/5 of height).** Bible line: "Castle Crashers-style
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
  `uds:\\.\pipe\LOCAL\cc-msg-a5467c87452d77bccc60c704d3ef83a9`. Hero, region, enemy and boss
  prompts stay parked until the World bible lands; the orchestrator will say when.
- 2026-09-24 (Michael): AI drafts first for all art, organised so he can tell AI from his own work
  and redraw each (D55). Audio: AI-drafted music plus licensed sound-effect libraries.

## Log

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
