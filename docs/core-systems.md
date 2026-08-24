# PuzzleGame Core Systems

This document describes the deterministic core implemented on `codex/core-systems` and the thin Unity vertical-slice adapter that exercises it. The implementation targets Unity 6000.5.8f1, Input System 1.20.0, and Unity Test Framework 1.7.0.

## Dependency direction and ownership

The dependency rule is `Contracts/Core <- Unity adapters/presentation`: code in `PuzzleGame.Unity` depends on `PuzzleGame.Core`; core code never imports Unity APIs. Tests may depend on both runtime assemblies, while production assemblies never depend on test assemblies.

| Assembly | Owns | May depend on |
| --- | --- | --- |
| `PuzzleGame.Core` | Contracts, validation, deterministic state, rules, transactions, snapshots, sample composition | Standard C# only (`noEngineReferences: true`) |
| `PuzzleGame.Unity` | Input translation, board rendering, runtime composition, Unity lifecycle, file-system storage | `PuzzleGame.Core`, Unity runtime, Input System |
| `PuzzleGame.Tests.EditMode` | Core and persistence adapter tests | Core, Unity adapter, NUnit/Test Framework |
| `PuzzleGame.Tests.PlayMode` | Input backend, lifecycle, rendering ownership, and playable-smoke tests | Runtime assemblies, Input System test framework |

The owner that creates mutable state destroys or disposes it. `VerticalSliceBootstrap` owns its child runtime root, controller, view, and pointer source. `PlayableBattleController` owns its input subscriptions. `BoardView` owns and destroys its generated cells, texture, sprite, and material. Core result collections are read-only snapshots; authored sample getters return deep copies. Runtime state (`Board`, `Party`, `Enemy`, `StageSession`, and `BattleContext`) is deliberately mutable and isolated per factory call.

## Public subsystems and extension points

| Area | Public surface | Extension or injection point |
| --- | --- | --- |
| Content contracts | `CharacterData`, skills, enemies, stages, banners, events, schedules, rewards, enums | Construct DTOs, then call `ContractValidation.Validate`; stable string IDs connect catalogs |
| Board | `BoardState`, `BoardPosition`, `MatchDetector`, `BoardResolver`, `BoardGenerator`, `DragSession` | Inject board `IRandomSource` and refill `IOrbSource` for deterministic sequences |
| Party and battle | `CharacterRuntime`, `PartyState`, `BattleContext`, `CombatCalculator`, `BattleEngine` | Consume immutable resolution/event objects; mutate only through public state commands |
| Skills and effects | `SkillEngine`, `BoardEffectState`, battle/charge/effect results | Add data-driven `SkillEffectData` and `EnemyEffectData` values supported by the engines |
| Enemies and stages | `EnemyRuntime`, `EnemyActionEngine`, `BossMechanicEngine`, `StageCatalog`, `StageSession`, `StageObjectiveEvaluator` | Register validated stage/enemy DTOs in `StageCatalog`; query threshold and objective snapshots |
| Progression and economy | `CharacterProgress`, `ProgressionService`, `MaterialInventory`, `Wallet` | Inject validated character/skill progression data and currency/material balances |
| Gacha | `BannerRuntimeState`, `SummonService`, `SummonCollection`, `PackSummonFlow` | Inject gacha `IRandomSource`, wallet, catalog, collection, and persisted rotation state |
| Scheduling | `IClock`, `SystemLocalClock`, `RotationScheduler`, `ActiveContent` | Inject a clock and schedule DTO collection |
| Persistence | `SaveData`, `SaveSerializer`, `SaveService`, `ISaveStorage` | Implement `ISaveStorage`; Unity supplies `FileSaveStorage` |
| Integrated sample | `VerticalSliceFactory`, `VerticalSliceSample` | Pass a seed; use snapshot catalogs and the isolated runtime entry points |
| Unity input/view | `IBoardPointerSource`, `BoardPointerInput`, `BoardLayout`, `BoardView`, `PlayableBattleController` | Substitute any pointer event source; consume controller/view events for UI and VFX |
| Unity composition | `VerticalSliceBootstrap`, `VerticalSlicePointerDriver` | Runtime auto-composition or explicit deterministic test construction |

## Content registration and validation

Contracts are serializable DTOs, but construction does not imply validity. Call `ContractValidation.Validate` for each record before registration. Validation covers required IDs, enum ranges, stats, progression/Ascension/Awakening shapes, visuals, effect payloads, enemy actions and thresholds, stage objectives and rewards, banner pools/steps, event references, and schedule windows.

`StageCatalog` is the semantic registration boundary for stages and enemies. It snapshots inputs, rejects duplicate IDs and unknown enemy references, and creates isolated `StageSession` instances. `SummonService` similarly snapshots a validated banner and `SummonCharacterDefinition` catalog, checks referenced IDs and guarantee candidates, and rejects invalid step shapes. `RotationScheduler.GetActive` snapshots and validates its complete query input, including duplicate schedule IDs, before evaluating it.

Stable IDs, not object identity, connect authoring records. Visual keys are opaque references for presentation consumers; the core neither loads nor owns presentation assets.

## Board and input event ordering

The board is always 6 columns by 5 rows. `BoardGenerator` rejects invalid RNG output and retries until the generated board has no opening match. `MatchDetector` merges connected horizontal/vertical runs into distinct groups. `BoardResolver` repeatedly snapshots the pre-clear board, clears all groups in a layer simultaneously, applies gravity/refill, snapshots the post-refill board, and stops at stability or the configured cascade cap.

`BoardPointerInput` is the only production class that reads the Input System directly. It normalizes mouse-primary and active-touch input into `PointerPressed`, `PointerMoved`, and `PointerReleased`. `BoardLayout.ScreenToCell` rejects invalid rectangles and out-of-board coordinates.

`PlayableBattleController` processes a move in this order:

1. Press maps to a cell, starts `DragSession`, and emits the initial `TimerChanged` value.
2. Movement traverses every crossed grid boundary, so a fast pointer jump performs each adjacent swap and refreshes the board after each swap.
3. Release or exact unscaled timeout ends the drag once and emits timer zero. Later releases/time advances are ignored.
4. `BoardResolver` completes the entire deterministic resolution and `StageSession` commits the battle turn.
5. For each cascade layer, the view displays `PreClearBoard`, emits `MatchGroupsResolved` and `CascadeLayerResolved`, then displays `PostRefillBoard`.
6. The view settles on the authoritative `BattleContext.Board` even when presentation events are consumed synchronously.
7. Separate per-character `AttackResolved` events are emitted, followed by `HealResolved`, the resulting enemy countdown, and any enemy actions.
8. Board-resolution count is emitted. If the one enemy is defeated, stage completion and the three objective results follow.

Presentation animation is an event consumer seam. It must not delay, reroll, or mutate the already-completed core transaction.

## Battle lifecycle

`BattleContext` owns the current board reference, party, enemy, board effects, and move-time modifiers. `BattleEngine.CompleteBoardResolution` resolves combat, charges skills from matched groups, installs the final board snapshot, advances the enemy countdown/action, and then ticks only effects captured at the start of that turn. Newly applied timed effects therefore retain their full authored duration.

Combat emits one attack per matching character and match group rather than collapsing same-element groups. Element affinity, leader conditions, passive effects, active boosts, shields, binds, board locks/hazards, combo shields, absorb, enrage, Heart recovery, and saturating state arithmetic remain domain rules. A defeated party skips further battle work. `BossMechanicEngine` records fired threshold IDs on `EnemyRuntime`, so crossing a threshold is observable exactly once.

`StageSession` snapshots authored structure, lazily creates current-wave enemies, records a resolution object once, advances only after every current enemy is defeated, and reports permanent-story stages as stamina-free. The current Unity `PlayableBattleController` deliberately enforces exactly one wave with exactly one enemy; multi-wave/multi-enemy sessions remain a core capability but need a broader presentation controller.

## Progression and economy

`Wallet` and `MaterialInventory` reject negative changes, use checked/saturating rules where defined, and expose preflight affordability plus atomic spend operations. `CharacterProgress` snapshots its character/skill inputs and derives level stats, effective Active Skill charge, Ascension bonuses, rarity, and current visuals.

`ProgressionService.ApplyExperience` advances against cumulative level requirements. `ApplyDuplicate` grants the next Ascension rank through rank 5; overflow duplicates grant the configured universal resource. Awakening is independent of duplicates: `CanAwaken`/`Awaken` require the authored level, materials, and Gold, then switch to 6-star rarity and the distinct awakened portrait/card/model/VFX keys. The service plans duplicate changes before commit so gacha can preserve transaction atomicity.

## Gacha transactions and pack hooks

`SummonService.Quote` is read-only. `PurchaseAndRoll` first validates banner availability, pull shape, wallet balance, pool, step guarantee, and overflow safety; it rolls and builds a complete transaction plan before spending Gems or changing ownership/Ascension/step state. A failure leaves wallet, collection, progress, and banner state unchanged.

Standard banners support explicit one/ten pulls. Gather-In and Step-Up banners consume the authored next step. A boosted five-star guarantee weights featured five-stars more heavily within the guaranteed final pick; a featured guarantee limits that pick to featured five-stars. The returned `SummonBatch` is fixed before presentation begins.

Create `PackSummonFlow` only from a completed, non-empty batch. Its ordered hooks are `PurchaseValidated`, `PackPresented`, `PackRipStarted`, `PackOpened`, repeated `CardReady`/`CardRevealed`, `AllCardsRevealed`, and `ResultsComplete`. `RevealAll` follows the same state machine. Reentrant transitions and out-of-order commands throw; presentation can observe but cannot change summon results.

## Scheduler semantics

`RotationScheduler` reads `IClock.Now` once per query. Absolute windows are start-inclusive and end-exclusive. Disabled entries never activate. Non-recurring entries use their outer `Start`/`End` directly.

Recurring weekdays and minute-of-day values are evaluated at the fixed UTC offset stored in `Start`; cross-midnight windows inspect the previous local weekday and are clipped to the outer absolute window. Results are ordered by descending priority, then ordinal schedule ID. This is intentionally fixed-offset recurrence: there is no named timezone database and no daylight-saving transition handling.

`SystemLocalClock` uses the device's local `DateTimeOffset.Now`. For authoritative live operations, replace it with a backend-synchronized `IClock`; v1 has no backend authority or anti-tamper policy.

## Save schema, status, and storage

`SaveData.CurrentVersion` is 2. The schema persists fixed and event wallet balances, character experience/Ascension/Awakening, materials, exactly five party slots, banner/rotation/step shape, stage clear/three stars/best HP basis points/best move count, and settings. Serialization validates and deep-snapshots the entire profile, sorts keyed collections ordinally, writes deterministic JSON, and enforces input, nesting, member, and collection bounds.

`SaveSerializer.Deserialize` reports current v2 as `Loaded`, maps supported v1 Gold/Gems/characters/party data to v2 as `Migrated`, and reports a future version as `UnsupportedVersion` without destroying it. Malformed, invalid, or non-UTF-8 data is corrupt. `SaveService.LoadOrCreate` returns `CreatedNew`, `Loaded`, `Migrated`, `RecoveredFromCorrupt`, or `UnsupportedVersion`; corrupt recovery asks storage to preserve a backup and returns a new default profile.

`ISaveStorage` is the platform boundary: `Read`, `WriteAtomic`, and `BackupCorrupt`. `FileSaveStorage` accepts a caller-selected file path, canonicalizes it, rejects empty paths, roots, directory markers, and existing directories, and never chooses a broad path implicitly. It strictly decodes bounded UTF-8. Writes use an exclusively created sibling, flush through to disk, then atomically replace/move the target; owned temporary files are cleaned on failure. Corrupt backups are uniquely named UTC-stamped siblings. The application layer must choose an appropriate file below `Application.persistentDataPath`; this core does not hard-code that policy.

## Integrated sample and bootstrap controls

`VerticalSliceFactory.Create()` defaults to seed `24082026`. It creates five validated elemental characters, a five-member party, generated and crafted boards, skills, a threshold boss, the permanent one-wave stage, progression examples, Standard and two Gather-In examples, fixed summon batches, full save roundtrip, and active examples for every `ContentType`. Same-seed calls are deterministic and share no mutable authored or runtime state. The crafted playable board and refill sequence intentionally produce disconnected Fire groups, Heart healing, and a second cascade.

`VerticalSliceBootstrap` is code-only composition; `SampleScene.unity` has no serialized bootstrap edits. Runtime initialization runs after scene load, finds an existing `DontSave` bootstrap (including across disabled domain reload), or creates one persistent root. It suppresses automatic creation in EditMode and command-line test runs. `EnsureRuntimeBootstrap()` is an explicit idempotent request; `CreateForTests(seed)` explicitly creates an isolated instance with `VerticalSlicePointerDriver` instead of raw Input System reads.

The bootstrap exposes `Sample`, `Controller`, `View`, `PointerInput`/`PointerDriver`, board rectangle, timer limit, gameplay/presentation events, deterministic cell press/move/release, explicit unscaled-time advancement, and boss evaluation. Destroying its root unsubscribes forwarded events, disposes the deterministic driver, and lets the owned child/view release all generated Unity resources.

## Reproducible verification

The main project may be open in Unity and hold its generated `Library`. In that case, copy `Assets/PuzzleGame` into an isolated project using the same `ProjectVersion.txt` and package versions, then compare SHA-256 hashes before running batch mode. Do not close or mutate the unrelated live editor.

Direct core/EditMode baseline (PowerShell, from the repository root):

```powershell
$compiler = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mcs.bat'
$mono = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\MonoBleedingEdge\bin\mono.exe'
$nunit = (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@*\net472\unity-custom\nunit.framework.dll').Path
$out = 'Temp\core-systems-direct'
New-Item -ItemType Directory -Force $out | Out-Null
$core = Get-ChildItem 'Assets\PuzzleGame\Core' -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object FullName
$unityPersistence = Get-ChildItem 'Assets\PuzzleGame\Unity\Persistence' -Filter '*.cs' | Sort-Object FullName | ForEach-Object FullName
$tests = Get-ChildItem 'Assets\PuzzleGame\Tests\EditMode' -Recurse -Filter '*.cs' | Sort-Object FullName | ForEach-Object FullName
& $compiler -sdk:4.8 -warn:4 -target:library -out:"$out\PuzzleGame.Core.dll" $core
& $compiler -sdk:4.8 -warn:4 -target:library -out:"$out\PuzzleGame.Unity.dll" -r:"$out\PuzzleGame.Core.dll" $unityPersistence
& $compiler -sdk:4.8 -warn:4 -target:library -out:"$out\PuzzleGame.Tests.EditMode.dll" -r:"$out\PuzzleGame.Core.dll" -r:"$out\PuzzleGame.Unity.dll" -r:$nunit $tests
# Task9ReflectionRunner.exe is the repository's ignored deterministic attribute runner used by task reports.
Copy-Item 'Temp\Task9ReflectionRunner.exe' "$out\Task9ReflectionRunner.exe" -Force
& $mono "$out\Task9ReflectionRunner.exe" $nunit "$out\PuzzleGame.Tests.EditMode.dll" 'PuzzleGame.Tests.EditMode'
```

Unity Test Framework, using an exact isolated project path:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe'
$project = (Resolve-Path 'Temp\task5-harness').Path
& $unity -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode `
  -testFilter PuzzleGame.Tests.EditMode -testResults Temp\core-editmode.xml -logFile Temp\core-editmode.log
& $unity -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode `
  -testFilter PuzzleGame.Tests.PlayMode -testResults Temp\core-playmode.xml -logFile Temp\core-playmode.log
```

At Task 10 completion the direct and Unity EditMode suites contain 270 passing cases; PlayMode contains 35 passing cases. Scan the full logs for first-party compiler errors, exceptions, failed assertions, and unexpected `Debug.Log*` output in addition to checking XML totals.

## Known limitations

- The v1 runtime trusts a local/device clock and has no backend scheduling, economy authority, account sync, or anti-tamper layer.
- Recurrence uses the authored fixed offset, not a named timezone, so it does not apply daylight-saving rules.
- The current Unity vertical controller supports exactly one wave and one enemy even though the core stage session supports broader authored structures.
- Cascade, combat, summon-pack, and stage events are synchronous consumer seams; final UI/VFX animation and content loading remain presentation work.
- The sample uses deterministic in-memory save storage. Shipping code must select a scoped persistent file path and instantiate `FileSaveStorage`.
