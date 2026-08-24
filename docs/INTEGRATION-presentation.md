# Presentation ↔ Core integration (COMPLETED)

Branch `claude/content-presentation` now contains one cohesive game:
Codex's core systems (`Assets/PuzzleGame/Core`, `Assets/PuzzleGame/Unity`)
drive Claude's presentation and content
(`Assets/PuzzleGame/Scripts`, `Assets/PuzzleGame/Resources/Content`)
through the adapter layer in `Assets/PuzzleGame/Scripts/CoreIntegration`.

## Runtime composition

Scene map (all in build settings):

| Scene | Entry | Purpose |
|---|---|---|
| `Game` (first) | `GameBootstrap` | The real game: GameServices + core adapters + full UI |
| `SampleScene` | `VerticalSliceBootstrap` | Codex's deterministic vertical slice (unchanged) |
| `PresentationDemo` (disabled) | `PresentationDemoBootstrap` | Mock-driven presentation demo only |

`GameServices` (composition root) owns exactly one instance of each core
service: `Wallet`, `MaterialInventory`, `SummonCollection`,
`StageCatalog`, `RotationScheduler` + `SystemLocalClock`, `SaveService`
over `FileSaveStorage` (`persistentDataPath/puzzlegame_save.json`), and
per-banner `SummonService`s with persisted `BannerRuntimeState`. All
state mutation goes through public core APIs; profile restore replays
experience/duplicates and awakens via grant-then-`Awaken` so no core
invariants are bypassed.

Presentation seams → implementations:

| Seam | Real game | Demo |
|---|---|---|
| `IEconomySource` | `CoreEconomyAdapter` (Wallet) | MockEconomy |
| `IRosterSource` | `CoreRosterAdapter` (CharacterProgress/ProgressionService) | MockRoster |
| `ISummonSource` | `CoreSummonAdapter` (SummonService.PurchaseAndRoll + PackSummonFlow) | MockSummons |
| `IScheduleSource` | `CoreScheduleAdapter` (RotationScheduler) | MockSchedule |
| `IContentLibrary` | `CoreContentAdapter` (ContentDb display + save-backed stars/unlocks) | ContentDb |
| `IBattleFactory` | `CoreBattleFactory` → `CoreBattleAdapter` | MockBattleFactory |

Mocks live only in `Scripts/Integration/Mock`, are registered only by the
demo bootstrap, and never run in the `Game` scene.

## Battle integration

`CoreBattleAdapter` is the "broader presentation controller" the core
docs anticipated: multi-wave, multi-enemy stage sessions using
`DragSession` (movement legality + 10s authority), `BoardResolver`
(matching/cascades), `BattleEngine`/`StageSession` (combat commit),
`SkillEngine` (skills), `StageObjectiveEvaluator` (stars). The core
transaction always commits synchronously; the adapter then replays the
immutable result as the paced `IBattleEventSource` stream the HUD
consumes. Secondary wave enemies advance through `EnemyActionEngine`,
with their board/context effects mirrored from the emitted snapshots.
Combat targets the first living enemy (core's single-enemy context);
victory/defeat, rewards, stars and save-out all flow through
`GameServices.RecordStageCompletion`.

Stage modifiers (`start_locks`, `move_time_minus`, `combo_shield`,
`enemy_haste`, `no_heart_orbs`, ...) are applied at battle start through
core state APIs. `mono_element_only`, `element_bonus` and
`healing_reduced` remain display-only pending core support.

## Content translation

`ContentTranslator` is the canonical mapping from the authored JSON bank
onto core contracts (validated record-by-record with
`ContractValidation`, failing loudly): level curves + XP thresholds,
five-rank Ascension shapes, Awakening requirements (level + gold +
radiant/element cores), typed active/leader/passive effects synthesized
from the authoring keyword vocabulary, enemy actions/threshold enrages,
per-stage scaled enemy variants (`id~h1.35a1.35`, wave-unique), banner
weight pools honoring authored rarity rates + featured five-star share,
real Gather-In step guarantees, and scheduler windows/recurrence.
Display strings (names, epithets, jokes, skill text) still come from
`ContentDb`; numbers come from core.

Known approximations (documented, non-blocking):
- Leader skills apply party-wide (core has no element-scoped boosts);
  authored multipliers are capped at ×3.
- "Below X% HP" leader conditions can't be expressed (core supports
  minimum-HP floors only) and are treated as always-on.
- Enemy `heal`/`summon`/`taunt` action types have no core equivalent and
  translate away (names remain for flavor).
- Non-final banner-step "4★ guaranteed" copy was removed from content —
  core implements final-step Gather-In/Step-Up guarantees only.

## Verification (all green at integration completion)

- Core EditMode: 335 passed. PlayMode (core + presentation + integration): 94 passed.
- `ContentValidator.Run`: 60 characters / 194 enemies / 500 story stages
  / 40 banners / 29 events / 86 items / 16 pack themes — 0 errors.
- `CoreIntegrationSmokeTest.Run`: translation, scheduler with injected
  date, real summons (single/ten/duplicates/overflow), both Gather-In
  guarantees, core battle → victory → persisted stars/rewards, real
  Awakening with visuals switch, full save round-trip.
- `CoreGameUiTests` (PlayMode): boots the real `GameBootstrap`, performs
  a real ten-pull through `PackSummonFlow` state transitions, drives a
  core battle through the HUD loop to victory, and verifies the
  serialized save.

## Codex follow-ups (optional)

- Core interpretation for `mono_element_only` party restriction,
  `element_bonus`, `healing_reduced`, and an enemy self-heal effect type.
- Non-final banner-step pity guarantees, if desired (copy was removed).
- Audio: `revealStingRef` and pack-theme hooks are string keys awaiting a sound system.
- The vertical-slice bootstrap now self-starts only in `SampleScene`
  (one-line scene gate in `AutomaticBootstrapStartup.ShouldSuppress`).
