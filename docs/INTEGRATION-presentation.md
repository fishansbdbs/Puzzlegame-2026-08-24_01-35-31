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
for schemas): 40 characters, 194 enemies, 20 chapters × 25 stages, 89
dialogue scenes, 34 banners, 18 events, 18 reward tables, 52 schedule
entries. These are draft schemas mirroring the design spec's contract
list — when core schemas land, migrate the files (mapping is ~1:1)
rather than forking vocabulary. Chapters 2-20, generated banners/events
and their schedule windows are produced by `node
Tools/content-gen/generate.js` from authored tables; regenerate instead
of hand-editing `*generated*`/`chapter_02+` files.

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
