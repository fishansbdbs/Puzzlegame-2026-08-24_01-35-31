# Presentation layer — integration notes for core (Codex)

Branch: `claude/content-presentation`. Everything lives under
`Assets/PuzzleGame/` plus `Tools/content-gen/`; no Codex-owned file was
modified. This doc is the handoff: what exists, where the seams are, and
how core systems replace the mocks.

## The seams (`Assets/PuzzleGame/Scripts/Integration/Sources.cs`)

Presentation code only talks to these interfaces, resolved through
`PresentationServices.Get<T>()`:

| Interface | Consumed by | Core system that should implement it |
|---|---|---|
| `IBattleEventSource` | BattleScreen, OrbBoardView, VFX | board/match/cascade engine, combat, countdown/action engine |
| `ISummonSource` | Summon hub, banner detail, pack opening | gacha probability/guarantee logic + pack summon flow hooks |
| `IRosterSource` | Roster, character detail, awakening | character progression / Ascension / Awakening logic + save |
| `IEconomySource` | header currency chips, summon costs | currencies/economy state |
| `IScheduleSource` | events hub, banner timers | calendar/rotation scheduler (presentation does **no** date math) |
| `IContentLibrary` | everywhere content is displayed | stage loader / content database |

**Binding:** call `PresentationServices.Register<T>(impl)` for each seam
before UI boots, then open screens via `UiRouter` (see
`PresentationDemoBootstrap.Launch` for panel setup). Any seam left
unregistered gets a demo mock from `MockInstaller.InstallMissing()` —
delete or bypass the mocks (`Integration/Mock/`) once real systems exist.
The mocks are explicitly demo-only: the summon mock rolls RNG only
because core gacha isn't merged; presentation itself never rolls.

`MockBattleSimulator` doubles as a written spec of the event flow the HUD
expects (ordering of `MatchResolved` → `AttackPerformed` → heals →
countdown ticks → `EnemyActed`; board reads only on `BoardChanged`).

## Content

JSON under `Assets/PuzzleGame/Resources/Content/` (see the README there
for schemas): 60 characters, 194 enemies, 20 chapters × 25 stages, 159
dialogue scenes, 40 banners, 29 events, 29 reward tables, 86 items, 16
pack themes, 69 schedule entries. These are draft schemas mirroring the
design spec's contract list — when core schemas land, migrate the files
(mapping is ~1:1) rather than forking vocabulary. Chapters 2-20,
generated banners/events and their schedule windows are produced by
`node Tools/content-gen/generate.js` from authored tables; regenerate
instead of hand-editing `*generated*`/`chapter_02+` files. `node
Tools/content-gen/audit.js` runs a deep whole-bank audit (duplicates,
references, empty files) without Unity.

### Stage modifiers (core interpretation needed)

Stages carry data-driven `modifiers` strings that presentation displays
as chips but core must implement mechanically:
`elite`, `start_locks:N`, `start_poison:N`, `start_blockers:N`,
`move_time_minus:N`, `combo_shield:N`, `enemy_haste`, `no_heart_orbs`,
`mono_element_only`, `element_bonus:<element>`, `healing_reduced`.
The validator enforces this vocabulary; extend it in ContentValidator +
ModifierDisplay together when adding new modifiers.

### Enemy action types

`damage, bigDamage, convert, lock, poison, block, bind, timerDown,
absorb, comboShield, enrage, heal, summon, taunt` — bosses use
absorb/comboShield/enrage/taunt/summon as signature behaviors. The demo
simulator treats summon/taunt as display-only; core should implement
them fully.

### Pack themes & items

`Content/packs/` defines per-banner summon presentation metadata (foil,
accent, base tier, tease style, fake-out permission, 5★ backdrop,
`revealStingRef` audio hook — audio system not yet present, hook only).
`Content/items/` is the item registry (names/categories/rarity/icons);
all reward, drop and token ids validate against it. Chapters define
`starMilestones` (25/50/75 stars) and a `mechanicNote`.

### Intentionally NOT implemented (avoid conflicts)

- Login/daily-bonus reward calendars (no schedule kind for it yet — add
  to core scheduler if wanted; presentation will render it).
- Stamina (story stages are stamina-free per spec; `staminaCost` field
  exists for future paid-entry content).
- Audio (sting refs are string hooks).
- Real gacha pity counters beyond the documented guarantees.

## Validation (all runnable headless)

```sh
# schema + cross-reference validation of every content file
Unity -batchmode -nographics -quit -projectPath . \
  -executeMethod PuzzleGame.Presentation.EditorTools.ContentValidator.Run
# summon/gather-in/duplicate/battle logic smoke (mock-driven)
Unity -batchmode -nographics -quit -projectPath . \
  -executeMethod PuzzleGame.Presentation.EditorTools.PresentationSmokeTest.Run
# real UI walkthrough: menu → roster → summon → pack rip → battle
Unity -runTests -batchmode -projectPath . -testPlatform PlayMode \
  -testResults results.xml
```

## Presentation specifics worth knowing

- **Naming:** the user-facing title comes from `GameInfo.Title` only.
- **Art:** every art reference is a string key resolved through
  `PlaceholderArt` (procedural). A real art pass swaps resolution, not keys.
- **Reduced motion / intensity:** all juice funnels through
  `MotionSettings` (PlayerPrefs-backed; UI in the main menu options box).
- **Demo entry:** `PresentationDemoBootstrap` auto-runs only in
  `PresentationDemo`/`SampleScene` scenes; core boot should register real
  sources and drive `UiRouter` itself.
- **Project settings:** landscape-only orientation is NOT yet enforced in
  `ProjectSettings.asset` (Codex-owned); `defaultScreenOrientation` and
  the autorotate flags still allow portrait. Please lock to landscape.
