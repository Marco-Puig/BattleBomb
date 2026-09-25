# HUD and menus: approved mockups v1

Exported 2026-09-24 from the Claude Design canvas "BattleBomb — HUD & Menus"
(https://claude.ai/artifact/Uk3zqg3FFVMfyH9JPDiaRj, private to Michael), after Michael approved
the direction: *"I really like the general style… once everything is in place we may change some
colors slightly."* The canvas is the living design; these files are the snapshot M9 builds from.
Open any of them in a browser; images load from the repo's own art.

| File | Screen |
|---|---|
| `hud-solo.html` | In-game HUD, one player: panel top-left, a Legendary drop with its loot card, damage numbers |
| `hud-couch.html` | Two players: P2 down, P1 reviving on the beat; P2's prompts on keyboard |
| `loot-cards.html` | Loot card states: better than worn, worse, level too low, sack full |
| `revive-and-wipe.html` | Revive heartbeat states and the WIPED OUT banner |
| `damage-numbers.html` | Normal, crit (white-hot), burn tick; the three size settings |
| `title.html` · `character-select.html` · `chapter-select.html` · `results.html` · `pause.html` · `settings.html` | Front end |

**Built on** UI Pass 01's palette and fonts (Passion One, Archivo, Space Mono), `QualityColors`,
the Groundwork menu map (A confirm, B back, X/Y item shortcuts, LB/RB tabs), and 24 px button
badges.

**Things these screens add to the game today** (recorded on the board's M9 backlog):
- ▲▼ against the worn piece, plus the rank name, on the floating loot card.
- "Sack full" shown on the card itself (GAME_DESIGN §5.4), not over the player.
- Crit numbers white-hot, because today's `#FF7326` clashes with Fire and Legendary.
- A pause menu separate from settings.
- Settings for volume, damage numbers (on/off, three sizes), and a controls view.
- Settings and Quit on the title screen.

Heroes appear as `[Fire hero]` and so on, with element emblems, until the story bible names them.
