# Provenance: where every piece of art and sound came from

**v0 · 2026-09-24 · Art lane.** Implements D55 (AI drafts first, provenance always) under D16
(layered source art) and O2 (no LFS). **M9 sets the final Unity paths (D55); everything else here
is the proposal it builds from.**

---

## 1. The idea

Every asset is drafted with AI first and redrawn by hand over time. So we always need to be able
to answer three questions: **who made this file** (AI, a person, or a licensed pack), **which
version is the game showing right now**, and **what is left to redraw**. Three things answer
them together:

1. **The folder:** an asset's AI draft sits in its `AI/` folder, the hand version in its `Hand/` folder.
2. **The filename:** `steel-helmet__ai_v1.png`, `steel-helmet__hand_v1.png`.
3. **The manifest,** `docs/art/MANIFEST.csv`: one row per slot, with its status. It is the redraw
   backlog and the source for Steam's AI disclosure.

**The game never points at a file. It points at a slot** (§6). A slot is a stable name like
`item-icon/4`, meaning "the Steel Helmet's icon". Swapping the AI draft for the hand version means
changing one entry. Nothing else is rewired, and the draft stays alongside for reference.

---

## 2. Folder layout: side by side (Michael, 2026-09-24)

`ArtSource/` sits at the repository root, outside Unity. It is where art is made and kept. Each
asset gets **one folder holding everything about it**: the brief and prompt, the raw AI output,
the cleaned AI draft, and the redraw.

```
ArtSource/
  _style/                      style frames and reference images; never ship
  rig/hero-template/
    brief.md                   the brief and the ChatGPT prompt(s)
    AI/
      _raw/                    untouched ChatGPT output, any names
      hero-template-torso__ai_v1.png
      hero-template-head__ai_v1.png  …
    Hand/
      hero-template__hand_v1.ai      Michael's layered Illustrator master
      hero-template-torso__hand_v1.png  …
  icons/items/
    brief.md                   one brief can cover a set; it covers every folder beneath it
    steel-helmet/
      AI/   steel-helmet__ai_v1.png
      Hand/                    empty until redrawn
  heroes/ enemies/ bosses/     wait for the World lane's story bible (D56)
  weapons/ effects/ environments/ ui/ store/
  audio/music/ audio/sfx/
```

- **`AI/`** holds `_raw/` (whatever ChatGPT produced, untouched) plus the cleaned exports:
  backgrounds removed, cut into parts, named by §4. Only the named exports can ship.
- **`Hand/`** holds the layered master (`.ai`, per D16) and its exports.
- **`brief.md`** holds the asset's brief and exact prompts. An asset folder with no `brief.md`
  uses the nearest one above it.
- Folder names are lowercase words joined by hyphens. Heroes, regions, enemy families, and bosses
  get folders only when the story bible names them. **No lane invents them.**

---

## 3. What counts as AI, Hand, and Licensed

| Source | Means | Examples |
|---|---|---|
| **AI** | Any file whose pixels or shapes came from an AI generator, **including after cleanup**: cut into parts, background removed, recoloured, touched up, or Image-Traced into vector. Editing does not turn AI into Hand. | A ChatGPT parts sheet cut into ten PNGs; that same art traced into Illustrator and tidied |
| **Hand** | Drawn by Michael or his collaborator. The AI draft may sit underneath as a **locked reference layer**, but none of its pixels or traced shapes survive into the hand file. | An Illustrator redraw made over the draft |
| **Licensed** | Third-party work used under a licence: asset packs, fonts, sound libraries. Record the licence. | The Archivo font (SIL OFL 1.1) |

**The quick check for Hand:** hide the reference layer. If what remains is entirely your own
strokes, it is Hand.

**Why the line matters.**
- **Steam.** Its content survey (rewritten January 2026) asks about content that "ships with your
  game and is consumed by players" and was "created with the help of AI tools". A traced draft is
  plainly AI. Whether a true redraw made over an AI reference also needs disclosing is a
  judgement for the Producer lane. The manifest keeps the AI draft on record after the redraw
  (§5), so either answer can be given accurately.
- **Ownership.** In the US, purely AI-generated images are not protected by copyright. A real
  redraw is your work and is protected. This is not legal advice; the Producer lane checks it
  before the store page goes live.

---

## 4. Filenames

```
<name>__<ai|hand>_v<N>.<ext>
```

- **name:** lowercase words joined by hyphens: `steel-helmet`. A part of a rigged character is
  `<asset>-<part>-<label>`, for example `hero-template-arm-front-bent`. A part with a single
  drawing uses the label `default`. Parts and labels come from `docs/art/HERO_TEMPLATE.md` §2.
- **`__ai` or `__hand`,** after a double underscore. Licensed files keep their vendor's names;
  their folder and manifest row carry the provenance.
- **`_vN`** starts at 1. It goes up when a new version **replaces the one the game uses**. AI and
  hand count separately. Work-in-progress saves do not bump it.
- **Old versions stay in `ArtSource/`**; nothing there is deleted, because a redraw may want an
  earlier draft. Only the current version goes into Unity (§7).
- The pattern for the test (§8): `^[a-z0-9]+(-[a-z0-9]+)*__(ai|hand)_v[1-9][0-9]*\.[a-z0-9]+$`.

---

## 5. The manifest: `docs/art/MANIFEST.csv`

A spreadsheet file: it opens in Excel, and the M9 test can read it. UTF-8, comma-separated,
header on the first row, and any field containing a comma goes in double quotes.

**One row per slot.** A slot is **the unit that gets swapped as a whole**: one icon, one effect,
one music track, and **one whole character skin, not one body part**. A half-redrawn character
(hand-drawn head, AI arms) would look wrong on screen, so a character's parts change over
together. The parts live inside the skin and are addressed by the rig's categories (§6). *This
refines D55's example slot, "Fire hero — torso". The single re-pointed entry becomes the whole
skin rather than one part.*

| Column | Filled when | Holds |
|---|---|---|
| `slot` | always | The stable id (§6). Never renamed, never reused. |
| `asset` | always | A human name: "Steel Helmet icon". |
| `category` | always | One of: `rig` `hero` `enemy` `boss` `npc` `weapon` `item-icon` `effect` `environment` `prop` `ui` `font` `store` `music` `sfx` `pack` `reference` |
| `status` | always | See below. |
| `ai_path` | an AI draft exists | The asset's `AI` folder, relative to the art root (§7): `icons/items/steel-helmet/AI` |
| `hand_path` | a hand version exists | The asset's `Hand` folder, same form. |
| `path` | art outside the `AI/`/`Hand/` folders | The file(s) or folder in use, relative to the art root: licensed art, and hand-made art that predates this scheme (§10). Separate several with `;`. |
| `licence` | `licensed` | The licence and where its proof lives: `SIL OFL 1.1 — UI/Fonts/OFL-Archivo.txt` |
| `ai_tool` | an AI draft exists | `ChatGPT images` |
| `date` | an AI draft or hand version exists | When the current version was made, `YYYY-MM-DD`. |
| `brief` | from `briefed` on | The `brief.md` path, relative to `ArtSource/`. |
| `notes` | optional | Free text: who is redrawing, what is wrong with the draft. |

**The current version** of a folder is its highest `vN`.

**Statuses:**

| Status | Means | What the game shows |
|---|---|---|
| `briefed` | Brief and prompt written; no draft yet | nothing (a placeholder) |
| `ai-draft` | The AI draft exists | the AI draft |
| `redrawing` | Someone is redrawing it (name them in `notes`) | the AI draft |
| `hand-final` | The hand version is done and swapped in | the hand version |
| `licensed` | Third-party, under a recorded licence | the licensed file |
| `unclassified` | Art already in the project whose source or licence is not yet established | — **the M9 test fails on it** |

A `hand-final` row **keeps its `ai_path`**. That record of "this was redrawn from an AI draft"
is what keeps the Steam disclosure accurate (§3).

---

## 6. Slots: how the game points at art

A slot id is `<category>/<key>`. The key is lowercase words joined by hyphens, or a number where
the game already knows the thing by number.

### Item icons: `item-icon/<definition id>`, bound now

- The key is the item's **definition id** (`ItemDefinition._id`, ids 1–13 today). That id is what
  a saved item carries and what `ItemIconLibrary` already joins on, so it is the one handle that
  never moves. The folder uses the item's name for people (`icons/items/steel-helmet/`); the slot
  uses the id for the game.
- **Binding:** the `Icon` field of the `ItemDefinition` with that id, read through
  `ItemIconLibrary.For(id)` via `SimulationDriver.ItemIcons`. **Re-pointing is changing that one
  field.** A missing icon stays a normal state: the sack draws its placeholder plate.

### Character skins: `rig/hero-template`, then `hero/…` `enemy/…` `boss/…` `npc/…`

These are requirements for M9; the rig itself is the hero template's work (step 3).

- **Each version of a skin is its own Sprite Library Asset.** The AI version lives in the skin's
  `AI/` folder and the hand version in its `Hand/` folder.
- **Categories are the template's part names; labels are a part's variants** (a hand might be
  open, fist, or grip). The names are fixed by the hero template and identical in every skin, so
  every animation works on every skin (D15).
- **The character's data points at exactly one of the two libraries.** Swapping the AI skin for
  the hand skin is **one field**.
- Recommended: make every skin's library a variant of the template's library (the Sprite Library
  Asset's *Main Library*), so category and label names cannot drift apart. M9 should confirm this
  works as expected in 2D Animation 15.1.
- Keys for story characters come from the story bible and are never invented here. Enemy keys will
  be `enemy/<family>-<archetype>`, with the archetype one of `grunt` `ranged` `caster` `brute`.

### Reserved: the id is fixed now, the binding is designed when M9 builds the pipeline

`weapon/<definition id>` (the sprite held on the rig, separate from the icon) · `effect/<key>` ·
`environment/<region>-<piece>` · `prop/<key>` · `ui/<key>` · `store/<key>` · `music/<key>` ·
`sfx/<key>` · `font/<key>` · `pack/<key>` (a licensed pack not bound to a single slot) ·
`reference/<key>` (source kept for reference, never shipped).

---

## 7. Getting art into Unity (M9)

- **One relative path, two roots.** The manifest's paths are relative to an *art root*.
  `ArtSource/` is the source root; `Assets/_BattleBomb/Art/` is the Unity root. An asset keeps
  the **same relative path in both**, so importing never changes the manifest.
  `ArtSource/icons/items/steel-helmet/AI/steel-helmet__ai_v1.png` imports to
  `Assets/_BattleBomb/Art/icons/items/steel-helmet/AI/steel-helmet__ai_v1.png`. Audio uses the
  same root (`audio/…`); M9 may rename the Unity root, but the relative paths stay.
- **Only exports go in:** the current `__ai_vN` or `__hand_vN` files in shipping formats (`.png`;
  `.wav`/`.ogg` for audio). Masters (`.ai`), `_raw/`, and `brief.md` never enter `Assets/`.
- **Copy the file alone, never its `.meta`.** That is August trap 2 (duplicate GUIDs). Unity
  makes a fresh `.meta`.
- **A new version is a new file.** Import it, re-point the slot, then remove the superseded file
  from Unity. It stays in `ArtSource/`.
- **Nobody edits inside `Assets/…/Art/`.** Change the art in `ArtSource/` and re-import it.

---

## 8. The enforcement test: requirements for M9

M9 builds these as EditMode tests. Like `ArchitectureFitnessTests`, they read the project from
disk. Every failure message names the file and the fix ("add a row to docs/art/MANIFEST.csv",
"rename to …__ai_v1.png").

"Art file" means an image, vector, audio, font, or model file: `png jpg jpeg tga psd psb svg ai
eps wav ogg mp3 ttf otf fbx blend`. Unity-authored files (`prefab`, `controller`, `anim`,
`asset`, `mat`, `spriteLib`) are not art files.

| # | Requirement |
|---|---|
| **R1: coverage** | Every art file under the Unity art root is covered by a manifest row: it sits in the row's `ai_path` or `hand_path`, or matches its `path`. **This is D55's "shipping art missing from the manifest fails the suite."** |
| **R2: names match folders** | A file inside an `AI/` folder matches §4's pattern with `__ai_`. Inside `Hand/`, it matches with `__hand_`. A file carrying `__ai_`/`__hand_` outside its matching folder fails. |
| **R3: no code in Art** | No `.cs` file anywhere under the Unity art root (August trap 1). |
| **R4: no duplicate GUIDs** | No `guid:` value appears in two `.meta` files anywhere under `Assets/` (August trap 2). |
| **R5: manifest is well-formed** | Exact header; `slot` unique and matching `^[a-z0-9-]+/[a-z0-9-]+$`; `category` and `status` from §5's lists; **no `unclassified` rows**; the columns §5 requires for each status are filled. |
| **R6: manifest paths exist** | `ai_path` and `hand_path` exist under `ArtSource/`; `path` entries exist under the Unity art root. |
| **R7: the game shows the current version** | For every `ItemDefinition` with an icon: a row `item-icon/<id>` exists, and the icon's file sits in that row's current folder (`hand_path`, or `path` for older art, if `hand-final`; `ai_path` if `ai-draft` or `redrawing`; the `path` if `licensed`). The same check covers skins once M9 binds them. **This is what keeps the Steam disclosure true.** |
| **R8: no art outside the root** | Every sprite an `ItemDefinition` or a skin library references lives under the Unity art root, so nothing escapes R1. |

**Also for M9, not a test:** an editor menu item that writes the **disclosure report**. It lists
every slot whose current version is AI, by category, plus every `hand-final` slot that began as an
AI draft. The Producer lane fills Steam's content survey from it.

---

## 9. Git: proposal for the orchestrator (O2)

- **Commit:** `docs/art/**`, every `brief.md`, every export once it becomes a slot's current
  version (small PNGs), and Michael's hand masters (`__hand_vN.ai`). The masters are D16's source
  of truth and must not live on one disk.
- **Do not commit** `ArtSource/**/_raw/`. Raw generations are 1–3 MB each and numerous, and they
  remain in Michael's ChatGPT history. Proposed `.gitignore` line: `ArtSource/**/_raw/`.
- **Commit keepers only** (O2): a version is committed when it becomes current, not while it is
  being iterated.
- `.gitattributes` has no line for `.psb`, `.kra`, or `.clip`. Add `binary` lines if any of
  them is adopted.
- Nothing here comes near O2's 50 MB tripwire: ChatGPT images are at most about 1536 px.

---

## 10. Art already in the project (classified by Michael, 2026-09-24)

| Under `Assets/_BattleBomb/Art/` | What it is | Provenance | M9 action |
|---|---|---|---|
| `Low_Swordman/` | 2D swordman rig plus its `Demo/` | **hand** (Michael: his or his collaborator's own work; the PSD dates from 2017) | **Delete the 4 scripts in `Demo/Scripts/`.** R3 fails on them today, and they are a live August-trap-1 tripwire. |
| `Templar Knight/` | Rig: 10 part PNGs, prefab, controller, 3 anims | **hand** (as above; the `.ai` dates from 2017) | Keep. It is the reference for the hero template (step 3). |
| `Sprites/Characters/Templar Knight/` | Its source: `.ai`, 10 `.eps`, 189 frames, Spriter `.scml` | **hand** | Move it to `ArtSource/_reference/`, because masters do not belong in Unity (§7). |
| `UI/Items/` | 15 icons: 12 weapons (none wired), leather helmet, health and mana potions (wired to ids 1, 9, 12) | **hand** | Keep |
| `UI/Frontend/` | Title logo, game icon, "Earth" chapter-select globe | **hand** | Keep |
| `World/Props/` | Door, Door_Alt, Asteroid | **third-party pack**, name and licence unknown | **Delete them.** No scene, prefab, or asset references them, and art with an unknown licence cannot ship. Stays `unclassified` until then. |
| `UI/Fonts/` | Archivo, Passion One, Space Mono | **licensed**, SIL OFL 1.1, licence files present | Done |
| `AI/maincharactertemp.ai` | **A byte-identical copy of `Templar Knight.ai`** (same checksum and document ID), not a separate drawing | **hand** (the Templar source) | **Delete it as a duplicate.** It also sits in a folder named `AI`, which under this scheme means AI-generated. |

This older hand-made art sits outside the `AI/`/`Hand/` folders, so its rows use `path` (§5)
rather than `hand_path`. It moves into the scheme only if and when it is redrawn or reorganised.
