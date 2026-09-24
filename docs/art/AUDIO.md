# Audio v0: the sound list, the libraries, and the music tool

**v0 · 2026-09-24 · Art lane.** D55: sound effects come from licensed libraries, and music is
drafted with AI, under the same manifest and provenance as the art. M9 builds the audio system
(buses for music, effects and UI; sounds fired by simulation events) and **generates the exact
list from the game's own events**. This is the draft list that it checks against.

---

## 1. ⚠ Decide before any licensed audio is committed: the repo is public

The repository is public on GitHub. **Most sound-library licences forbid redistributing the
raw files**: Sonniss forbids selling them individually, and BOOM Library forbids any
re-distribution. A public repo lets anyone download them. Only **CC0** audio (Kenney, CC0 files
on Freesound) is safe to publish. Fonts under OFL are fine too.

The options, for Michael and the orchestrator (git policy):
- **(a) Keep licensed audio out of the public repo.** A git-ignored `Assets/_BattleBomb/Art/audio/licensed/`,
  backed up privately. Builds are made on Michael's machine, and a fresh clone builds silent.
- **(b) Make the repository private.** Nothing else changes.
- **(c) Use CC0 audio only.** Safe, but the choice is thinner.

**Recommendation: (a) or (b).** (c) gives up the free professional libraries below for no gain.
Music drafted with a tool that gives ownership (§4) is unaffected.

---

## 2. The sound list (draft)

Named by what happens, so M9 can match each one to its event. **×N** means variations, so the
same sound never repeats back to back.

### Hero combat
| Sound | When | Notes |
|---|---|---|
| swing-light ×3 | Light 1, 2, 3 | whooshes that rise in pitch through the chain |
| swing-heavy · heavy-charge (loop) · heavy-release | Heavy, and the held charge | |
| swing-launcher | the L-L-H ender | a rising whoosh |
| bow-draw · bow-release · arrow-hit | bow Light | |
| hit-light ×3 · hit-heavy ×2 · hit-crit | a kinetic hit lands | the crit is the loudest, sharpest one |
| hit-fire · hit-ice · hit-earth · hit-air | an elemental hit (casts, infused weapons) | sizzle · crack · thud · whoosh |
| cast-⟨element⟩-signature · -aura · -leap | the three casts × four elements = 12 | |
| status-burn (tick) · status-chill (on, loop) · status-stun · status-launch | statuses | players hear them too (D40) |
| mana-fizzle | a press with an empty pool | a small sputter |
| jump · land ×2 · footstep ×4 | movement | ground surfaces come with environments |
| partner-bump | a partner's swing shoves you (D21) | comic, never painful |

### Hurt, downed, revive (D25, D31)
| Sound | When |
|---|---|
| hurt ×3 | a hero takes a hit |
| downed | a hero goes down |
| revive-heartbeat | every beat (45 steps, 0.75 s) over the downed partner |
| revive-press-onbeat · revive-press-rushed | the reviver's press: a rich hit versus a thin one |
| revived | back on their feet |
| wipe | both down; back to the chest (D49) |

**Hero voices** (grunts, barks) wait for the story bible: they belong to the characters (D56).

### Enemies (the four archetypes, before any region reskins them)
| Sound | When |
|---|---|
| grunt-attack · ranged-shot · caster-telegraph · caster-cast · brute-telegraph · brute-slam | each archetype's attack. **Telegraphs must be clearly audible**; they are the warning |
| enemy-hurt ×3 · enemy-death ×2 | per archetype, later reskinned per region |
| elite-armour (loop, quiet) | an elite moving: armour clank that advertises the drop (D22) |

### Loot (GAME_DESIGN §5.4, D30)
| Sound | When |
|---|---|
| drop-pop | an item pops from a corpse; **pitch rises with rank** |
| drop-sting-high | Legendary or better drops: the moment worth a sound of its own |
| drop-glow-hum (loop, quiet) | top-rank drops on the ground |
| item-grab | Light takes it |
| sack-full | the grab is refused (the red X, D43) |

### Chest, shop, investment (D42–D44)
| Sound | When |
|---|---|
| chest-open · chest-close | walking up to the chest and away |
| ui-move · ui-confirm · ui-back · ui-tab · ui-error | menu navigation, all screens |
| equip · unequip · lock-toggle | sack actions |
| sell (coins) · buy · junk-sweep | the economy (selling is the only faucet, D43) |
| upgrade-point | deepening a stat; **pitch rises as the cost doubles** |
| combine · combine-promote | the gamble; the 2% promotion gets a big sting |
| stat-allocate · level-up | the hero panel, and gaining a level |

### Flow and front end
| Sound | When |
|---|---|
| checkpoint-enter | stepping into a checkpoint room (the airlock, D48) |
| stage-start · stage-clear | stage boundaries |
| player-join · player-leave | player 2 joins at character select, or someone joins online (D51, M8) |
| pause-in · pause-out | |
| title-start | leaving the title screen |

---

## 3. Sound-effect libraries: the shortlist

| Library | Cost | Licence, as of 2026-09 | Credit needed | Safe in the public repo? | Good for |
|---|---|---|---|---|---|
| **Kenney audio packs** | free | **CC0** (public domain) | no | **yes** | UI clicks, impacts, simple RPG sounds |
| **Sonniss #GameAudioGDC bundles** | free | Royalty-free, commercial use in games; no attribution; not resold individually; **no AI training** | no | **no** | Professional impacts, whooshes, magic, foley: the backbone |
| **ZapSplat** | free (Standard), or paid (Premium) | Royalty-free; games included | **yes on Standard** ("ZapSplat" in the credits); no on Premium | no | A broad catalogue, including cartoon sounds |
| **Freesound** | free | **Per file**: CC0 fine; CC-BY fine with credit; **CC-BY-NC forbidden** (non-commercial) | per file | CC0 files only | Odd one-offs; quality varies |
| **BOOM Library** | paid, per library | Royalty-free, **single user**, no redistribution | no | no | Premium magic, whoosh and cartoon libraries, if the free ones fall short |

**Recommendation:** start with **Sonniss GDC** for the combat backbone and **Kenney** for UI,
and use ZapSplat and Freesound (CC0/CC-BY) to fill gaps. Pay for a BOOM library only if the feel
pass (M9) finds a hole. Each licence was checked on 2026-09-24 and is re-checked before a sound
ships. The licence goes in its manifest row's `licence` column.

---

## 4. Music: the AI tool (Michael's choice, D55)

| Tool | What you get, as of 2026-09 | Verdict |
|---|---|---|
| **AIVA** (Pro plan) | **You own the copyright** of the compositions (as an individual or small business under its revenue and staff limits). Instrumental, orchestral and cinematic. **Exports MIDI.** | **Recommended.** Ownership suits a commercial game and a public repo. The MIDI is the music equivalent of redrawing a draft: a composer can re-orchestrate from it later. |
| **Suno** (paid plans) | A **commercial-use licence, not ownership** (terms reworded after the Warner deal, Nov 2025). Monthly download caps. UMG and Sony litigation still open. The fullest-sounding productions. | A usable second choice if production quality matters more than ownership. |
| **Stable Audio** | Reported to grant ownership and commercial use; **not yet verified on the official site** | Check first |
| **ElevenLabs Music** | Self-serve plans **exclude commercial games** ("Studio Games"); Enterprise only | No |
| **Udio** | **Downloads disabled** since the UMG settlement (Oct 2025) | No |

**Tracks needed (draft):** title and menu theme; checkpoint room (calm: the chest, the shop, the
airlock); one exploration and combat track per region; boss; short stings for stage clear, wipe,
and level-up. **Regions and bosses wait for the story bible.** Their moods can follow the
region's climate element (bible §6).

**Format:** export WAV (48 kHz) where the tool allows. Loops need clean loop points. Music lives
at `ArtSource/audio/music/<track>/AI/<track>__ai_v1.wav` with the prompt in its `brief.md`.

---

## 5. Provenance

Same manifest, same rules (PROVENANCE.md). Library sounds are `licensed` rows, with `sfx/<key>`
slots and the library and licence in `licence`. AI music is `ai-draft` rows with `music/<key>`
slots and `ai_tool` set to the tool. A composer's re-orchestration is the `Hand/` version.
