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

The owner that creates mutable state destroys or disposes it. `VerticalSliceBootstrap` owns its child runtime root, controller, view, and pointer source. `PlayableBattleController` owns its input subscriptions. `BoardView` owns and destroys its generated cells, texture, sprite, and material. Core result collections are read-only snapshots; authored sample getters return deep copies. Sample `OpeningBoard` is an immutable snapshot, while `CurrentBoard` always returns the authoritative `BattleContext.Board`. Runtime state (`CurrentBoard`, `Party`, `Enemy`, `StageSession`, and `BattleContext`) is deliberately mutable and isolated per factory call.

## Public subsystems and extension points

| Area | Public surface | Extension or injection point |
| --- | --- | --- |
| Content contracts | `CharacterData`, skills, enemies, stages, banners, events, schedules, rewards, enums | Construct DTOs, then call `ContractValidation.Validate`; stable string IDs connect catalogs |
| Board | `BoardState`, `BoardPosition`, `MatchDetector`, `BoardResolver`, `BoardGenerator`, `DragSession` | Inject board `IRandomSource` and refill `IOrbSource` for deterministic sequences |
| Party and battle | `CharacterRuntime`, `PartyState`, `BattleContext`, `CombatCalculator`, `BattleEngine` | Consume immutable resolution/event objects; mutate only through public state commands |
| Skills and effects | `SkillEngine`, `BoardEffectState`, battle/charge/effect results | Add data-driven `SkillEffectData` and `EnemyEffectData` values supported by the engines; supply `IOrbSource` to `Activate` for removal skills |
| Enemies and stages | `EnemyRuntime`, `EnemyActionEngine`, `BossMechanicEngine`, `StageCatalog`, `StageSession`, `StageObjectiveEvaluator` | Register validated stage/enemy DTOs in `StageCatalog`; consume core-owned threshold and objective snapshots |
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

1. Press maps to a cell and starts `DragSession`. Positive-duration moves emit the initial `TimerChanged` value; zero-duration moves proceed directly to completion.
2. Movement traverses every crossed grid boundary, so a fast pointer jump performs each adjacent swap and refreshes the board after each swap.
3. Release or exact unscaled timeout ends the drag once. `CompleteDrag` is the sole terminal-zero emitter and notifies only after the authoritative board, battle, count, and stage state commit; later releases/time advances are ignored.
4. `BoardResolver` completes the entire deterministic resolution and `StageSession` commits the battle turn.
5. For each cascade layer, the view displays `PreClearBoard`, emits `MatchGroupsResolved` and `CascadeLayerResolved`, then displays `PostRefillBoard`.
6. The view settles on the authoritative `BattleContext.Board` even when presentation events are consumed synchronously.
7. Separate per-character `AttackResolved` events are emitted, followed by `HealResolved`, core-owned boss-mechanic snapshots in authored order, the resulting enemy countdown, and any enemy actions.
8. Board-resolution count is emitted. If the one enemy is defeated, stage completion and the three objective results follow.

Presentation animation is an event consumer seam. It must not delay, reroll, or mutate the already-completed core transaction.

## Battle lifecycle

`BattleContext` owns the current board reference, party, enemy, board effects, and move-time modifiers. `BattleEngine.CompleteBoardResolution` resolves combat, charges skills from matched groups, installs the final board snapshot, evaluates once-only HP thresholds on `EnemyRuntime`, advances the enemy countdown/action, and then ticks only effects captured at the start of that turn. A threshold enrage therefore affects an action due on the crossing resolution and retains its full authored duration until future completed resolutions tick it. Newly applied timed effects otherwise retain the same full-duration rule.

Combat emits one attack per matching character and match group rather than collapsing same-element groups. Element affinity, leader conditions, passive effects, active boosts, shields, binds, board locks/hazards, combo shields, absorb, enrage, Heart recovery, and saturating state arithmetic remain domain rules. A defeated party skips further battle work, and a defeated enemy cannot trigger a new threshold. `BattleTurnResolution.Mechanics` and direct-damage `SkillResolution.Mechanics` are immutable snapshots. `BossMechanicEngine` is a compatibility delegate to `EnemyRuntime.EvaluateThresholds`; it owns no trigger state.

`SkillEngine.Activate(character, context)` remains the entry point for skills without removal. A skill containing `RemoveOrbs` must use `Activate(character, context, orbSource)`. The engine validates every effect and stages all board changes before committing state or consuming charge. Removal ignores `TargetOrb`, removes every matching source cell, compacts each column toward row zero without changing survivor order, then requests validated refills in column-and-top-vacancy order. A missing source, invalid refill, or source exception leaves the board, other effects, and charge unchanged. Matches created by removal remain on the board for the next normal board-resolution flow.

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

`VerticalSliceFactory.Create()` defaults to seed `24082026`. It creates and validates five elemental characters, their skills and leader skill, a five-member party, generated and crafted boards, a threshold boss, the permanent one-wave stage, progression examples, Standard and two Gather-In examples, an event plus schedules for every content type, fixed summon batches, a full save roundtrip, and active examples for every `ContentType`. The retained `StageCatalog` is the source of both the playable and preview `StageSession` instances and remains available for further isolated sessions. Same-seed calls are deterministic and share no mutable authored or runtime state. The preview turn crosses the boss threshold through `BattleEngine` and exposes the resulting mechanic snapshot rather than manually applying presentation-owned state. The crafted playable board and refill sequence intentionally produce disconnected Fire groups, Heart healing, and a second cascade. `OpeningBoard` preserves that initial layout; after resolution, `CurrentBoard` follows the replacement installed in `BattleContext.Board`.

`VerticalSliceBootstrap` is code-only composition; `SampleScene.unity` has no serialized bootstrap edits. `AfterSceneLoad` makes one automatic-startup decision from test command-line flags or the initial active scene name. A run starting in `InitTestScene*` is suppressed even without command-line flags: it neither creates a root nor registers the scene callback, so a later Single or additive `SampleScene` load cannot change that decision. `EnsureRuntimeBootstrap()` remains an explicit idempotent opt-in and requires an active `Camera.main`; `CreateForTests(seed)` explicitly creates an isolated instance with `VerticalSlicePointerDriver` and an owned deterministic camera, while its camera overload accepts an active caller-owned camera. `SubsystemRegistration` unhooks the callback and resets the persistent decision for the next run. PlayMode coverage simulates flagless `InitTestScene` replacement, verifies normal registration and runtime idempotence, and exercises actual command-line suppression during additive scene loads. The Editor's complete Enter Play Mode Options/domain-reload-disabled matrix is not automated here.

The bootstrap exposes `Sample`, `Controller`, `View`, `GameplayCamera`, `PointerInput`/`PointerDriver`, board rectangle, timer limit, gameplay/presentation events, deterministic cell press/move/release, and explicit unscaled-time advancement. `BoardView` exposes value-copy world bounds and per-cell world centers matching its rendered sprite bounds. The bootstrap projects those bounds through the gameplay camera and refreshes the controller mapping when the screen, camera viewport/projection/transform, or view bounds change. A validated mapping change during a drag is deferred until that drag completes, preserving one coordinate space for the locked pointer. The deterministic `PressCell` seam projects the same rendered center through the same camera. It only forwards mechanic snapshots already produced by the controller/Core; attack or mechanic observers cannot decide whether the threshold state is applied. Destroying either the bootstrap component or its outer root unsubscribes forwarded events, disposes the deterministic driver, and destroys the owned child/view graph, any test-owned camera, and generated Unity resources. Composition failure also destroys both the partially created child graph and the factory-owned outer root.

The committed player configuration is landscape-only for v1: `LandscapeLeft` is the fixed default interface orientation, both landscape directions remain allowed, and portrait plus upside-down autorotation are disabled.

## Reproducible verification

Run the official Unity Test Framework from a clean checkout of this repository. Unity allows only one editor process to own a project's generated `Library`; if this checkout is locked by an open editor, close that editor or create another clean checkout/worktree and run the same commands from that checkout. No copied harness, precompiled reflection runner, or pre-existing ignored artifact is required.

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe'
$project = (Resolve-Path '.').Path
$artifacts = Join-Path ([IO.Path]::GetTempPath()) ('puzzlegame-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $artifacts | Out-Null

function Invoke-PuzzleGameTests([string] $platform, [string] $filter) {
    $stem = $platform.ToLowerInvariant()
    $results = Join-Path $artifacts "$stem-results.xml"
    $log = Join-Path $artifacts "$stem-unity.log"
    $arguments = @(
        '-batchmode', '-nographics',
        '-projectPath', ('"' + $project + '"'),
        '-runTests', '-testPlatform', $platform,
        '-testFilter', $filter,
        '-testResults', ('"' + $results + '"'),
        '-logFile', ('"' + $log + '"')
    )
    $process = Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        Get-Content -LiteralPath $log -Tail 200
        throw "Unity $platform exited with code $($process.ExitCode). Full log: $log"
    }
    if (-not (Test-Path -LiteralPath $results)) {
        throw "Unity $platform produced no result XML. Full log: $log"
    }

    [xml] $xml = Get-Content -LiteralPath $results -Raw
    $run = $xml.'test-run'
    if ($run.result -ne 'Passed' -or [int] $run.failed -ne 0) {
        Get-Content -LiteralPath $log -Tail 200
        throw "Unity $platform tests failed. Results: $results; full log: $log"
    }

    [pscustomobject]@{
        Platform = $platform
        Result = $run.result
        Total = [int] $run.total
        Passed = [int] $run.passed
        Failed = [int] $run.failed
        Results = $results
        Log = $log
    }
}

Invoke-PuzzleGameTests EditMode 'PuzzleGame.Tests.EditMode'
Invoke-PuzzleGameTests PlayMode 'PuzzleGame.Tests.PlayMode'
```

At the camera-aligned board-input review fix, the Unity EditMode suite contains 295 passing cases and PlayMode contains 56 passing cases. The command reports the process exit code through failure, validates that result XML exists, and rejects a non-passing XML result. Retain and inspect the printed log paths when diagnosing compiler errors, exceptions, failed assertions, or unexpected `Debug.Log*` output.

## Known limitations

- The v1 runtime trusts a local/device clock and has no backend scheduling, economy authority, account sync, or anti-tamper layer.
- Recurrence uses the authored fixed offset, not a named timezone, so it does not apply daylight-saving rules.
- The current Unity vertical controller supports exactly one wave and one enemy even though the core stage session supports broader authored structures.
- Cascade, combat, summon-pack, and stage events are synchronous consumer seams; final UI/VFX animation and content loading remain presentation work.
- The sample uses deterministic in-memory save storage. Shipping code must select a scoped persistent file path and instantiate `FileSaveStorage`.
