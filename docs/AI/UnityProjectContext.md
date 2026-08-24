# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project summary

- Project root: `D:/puzzlegame/puzzle-codex`
- Last analyzed: 2026-08-24
- Branch: `codex/core-systems`, through the Task 10 integrated vertical slice
- Product shape: deterministic landscape 2D match/drag RPG core with thin Unity input, view, persistence, and runtime-composition adapters

## Confirmed environment

- Unity: 6000.5.8f1 (Unity 6.5), revision `5cb7df797b7d`
- Rendering: Universal Render Pipeline 17.6.0 with the 2D Renderer
- Input: Input System 1.20.0, `activeInputHandler: 1`; mouse and primary-touch adapters are tested
- Testing: Unity Test Framework 1.7.0 and Unity's NUnit 2.1.0 package
- Confirmed editor target: Windows/PC; Android/mobile modules and touch bindings are installed
- Networking/backend: none in first-party runtime code

## First-party layout

| Path | Purpose |
| --- | --- |
| `Assets/PuzzleGame/Core/Contracts` | Serializable content DTOs and validation |
| `Assets/PuzzleGame/Core/Board` | 6x5 board, drag, matching, generation, cascade/refill snapshots |
| `Assets/PuzzleGame/Core/Battle` | Party/enemy runtime, skills, combat, timed effects, battle turns |
| `Assets/PuzzleGame/Core/Stages` | Catalogs, session lifecycle, threshold-delegate compatibility, objectives |
| `Assets/PuzzleGame/Core/Economy` and `Progression` | Wallet, materials, levels, duplicates/Ascension, Awakening |
| `Assets/PuzzleGame/Core/Gacha` | Banner state, atomic summon transactions, pack presentation flow |
| `Assets/PuzzleGame/Core/Scheduling` | Injected clock and fixed-offset schedule evaluation |
| `Assets/PuzzleGame/Core/Persistence` | Versioned save DTOs, bounded deterministic JSON, storage service |
| `Assets/PuzzleGame/Core/Sample` | Seeded, isolated integrated sample graph |
| `Assets/PuzzleGame/Unity/Input` | Raw Input System mouse/touch translation |
| `Assets/PuzzleGame/Unity/Battle` | Board layout/view and one-wave/one-enemy playable controller |
| `Assets/PuzzleGame/Unity/Persistence` | Strict UTF-8 atomic file storage adapter |
| `Assets/PuzzleGame/Unity/Bootstrap` | Code-only idempotent vertical-slice composition |
| `Assets/PuzzleGame/Tests/EditMode` | Deterministic core, storage, and integrated sample tests |
| `Assets/PuzzleGame/Tests/PlayMode` | Input/lifecycle/resource and complete playable-smoke tests |
| `docs/core-systems.md` | Current subsystem, extension, lifecycle, and verification guide |

## Assembly boundaries

| Assembly | Responsibility | References |
| --- | --- | --- |
| `PuzzleGame.Core` | Presentation-independent contracts and domain logic | Standard C# only; `noEngineReferences: true` |
| `PuzzleGame.Unity` | MonoBehaviour input, board view/controller, file storage, bootstrap | Core, Unity runtime, Input System |
| `PuzzleGame.Tests.EditMode` | Core integration and Unity persistence adapter tests | Core, Unity, Unity Test Framework |
| `PuzzleGame.Tests.PlayMode` | Input System and runtime vertical-slice tests | Core, Unity, Input System, Input Test Framework |

Dependency direction is `Contracts/Core <- Unity adapters/presentation`. Production assemblies do not reference test assemblies.

## Scenes and startup

- Enabled build scene: `Assets/Scenes/SampleScene.unity`
- No Task 10 scene, prefab, or presentation asset was edited.
- `VerticalSliceBootstrap` attaches after scene load through `RuntimeInitializeOnLoadMethod`, only if no bootstrap exists.
- The runtime root is code-composed and persistent across scene loads. `AfterSceneLoad` decides automatic startup once from test command-line flags or an initial `InitTestScene*`; a suppressed run never registers a scene callback, so later scene replacement cannot enable it. `SubsystemRegistration` unhooks the callback and resets that decision, but the complete Editor Enter Play Mode Options/domain-reload-disabled matrix is not automated.
- `EnsureRuntimeBootstrap` remains the explicit idempotent opt-in during suppressed runs; `CreateForTests` creates isolated deterministic-pointer instances.
- `BoardPointerInput` remains the only production raw Input System reader.
- Production mapping accepts exactly one active/enabled, display-0 camera tagged `MainCamera`; unavailable, ambiguous, secondary-display, or behind-board cameras invalidate idle input without preventing composition. Camera replacement and viewport/projection/transform/view-bounds changes recover through the nullable mapping provider. Pointer-down refreshes synchronously and freezes the accepted rectangle for that drag. Explicit test composition may instead supply a camera or own a deterministic fallback.
- Player settings commit a fixed landscape-left default, allow both landscape directions, and disable portrait and upside-down autorotation.

## Architecture and ownership

- Core services are synchronous plain C# with injected RNG, clock, refill, and storage boundaries.
- `EnemyRuntime` owns once-only boss-threshold state. Board turns evaluate thresholds after player damage and board installation but before enemy actions; direct-damage skills evaluate after their complete effect list. Immutable mechanic snapshots flow outward through the controller/bootstrap.
- `RemoveOrbs` skills require an explicit `IOrbSource`, stage compact/refill board changes before committing, preserve survivor order, and reject missing or invalid refill data without consuming charge or mutating battle state.
- Authored inputs and public results are validated snapshots. Each `VerticalSliceFactory.Create(seed)` call owns isolated mutable runtime state. `OpeningBoard` is the immutable initial snapshot, and `CurrentBoard` dynamically follows `BattleContext.Board` after replacement.
- Unity adapters translate lifecycle and input to domain commands/events; UI/VFX is an event consumer and does not own gameplay outcomes.
- `VerticalSliceBootstrap` owns its child runtime graph and event forwarding, destroys that graph even when only the component is removed, and cleans factory-owned roots after composition failure. `PlayableBattleController` owns pointer subscriptions. `BoardView` owns generated GameObjects and render resources and releases them on destruction.
- Summon purchase, rolls, guarantees, ownership/duplicate effects, wallet spend, and banner-step advancement are planned before commit. Pack reveal receives a fixed batch.
- Persistence isolates deterministic serialization/service policy from the Unity file path and atomic file-system boundary.

## Testing and validation

- Unity EditMode: 295/295 passing with Unity 6000.5.8f1.
- Unity PlayMode: 65/65 passing.
- PlayMode smoke proves 30 cells, camera-projected round trips at 16:9, 4:3, and ultrawide, authoritative pointer-down refresh with frozen drag mapping, invalid/stale mapping rejection, destroyed/replacement, disabled/restored, behind/restored, multiple-main, and secondary-display camera handling, live viewport/projection/view-bounds remapping, explicit camera ownership, ten-second moves, adjacent traversal of fast crossed cells, release/timeout exactly once, one post-core terminal timer zero (including zero-duration moves), two cascade layers ending on the authoritative board, separate attacks, Heart healing, core-owned once-only boss thresholds before due actions, stable mechanic event forwarding, release/timeout/zero-duration observer-exception state safety, stage completion/stars, command-line and flagless `InitTestScene` suppression, one-time normal callback registration, scene-reload idempotence, composition-failure atomicity, and component/root render-event cleanup.
- Task 5 mouse/touch Input Test Framework cases remain the direct backend proof; Task 10 uses a deterministic pointer seam rather than OS cursor automation.
- CI is not configured in this repository. Official Unity Test Framework batch commands for a clean checkout are documented in `docs/core-systems.md`.

## Tooling and lock caveat

- Unity batch executable: `C:/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe`.
- No Unity MCP tools were exposed to this Codex session.
- Unity batch mode cannot open a checkout while another editor owns its generated `Library`. The documented reproducible workflow runs the official Test Framework against the repository from a clean checkout; use another clean checkout/worktree if the first is locked.

## Product constraints and known limitations

- Landscape PC/mobile target; mouse and primary touch are supported.
- The current playable controller intentionally accepts exactly one wave and one enemy.
- Schedule recurrence uses an authored fixed offset, not named timezone/DST rules.
- v1 trusts a local clock and has no backend authority, account sync, payment, or anti-tamper implementation.
- Final UI/VFX animation and bulk content remain presentation responsibilities; core emits complete synchronous consumer events.
- The sample save roundtrip uses memory storage. Shipping composition must choose a scoped path below `Application.persistentDataPath` for `FileSaveStorage`.

## Primary evidence inspected

- `docs/agent-prompts/CODEX_CORE_PROMPT.md`
- `docs/superpowers/specs/2026-08-24-puzzlegame-design.md`
- `docs/superpowers/plans/2026-08-24-core-systems.md`
- `.superpowers/sdd/2026-08-24-core-systems/task-1-report.md` through `task-9-report.md`
- `ProjectSettings/ProjectVersion.txt`, `ProjectSettings/EditorBuildSettings.asset`, `Packages/manifest.json`, and `Packages/packages-lock.json`
- Current `Assets/PuzzleGame` sources, asmdefs, tests, and Unity batch XML/log outputs

<!-- unity-onboarding:generated:end -->
