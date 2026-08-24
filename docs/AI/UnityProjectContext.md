# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `D:/puzzlegame/puzzle-codex`
- Last analyzed: 2026-08-24
- Last analyzed commit: `a32ef23`
- Confirmed: clean Unity 2D starter reserved for the core-systems implementation on `codex/core-systems`.

## Confirmed Environment

- Unity version: 6000.5.8f1 (Unity 6.5), revision `5cb7df797b7d`
- Render pipeline: Universal Render Pipeline with the 2D Renderer
- Input system: Input System package, `activeInputHandler: 1`
- Target platforms: Windows/PC editor confirmed; Android/mobile modules and touch bindings are available. Product requirements also target mobile.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.x with Renderer2D assets | Confirmed | `Packages/manifest.json`, `Assets/Settings/UniversalRP.asset`, `Assets/Settings/Renderer2D.asset` |
| Input | Input System 1.20.0 with mouse and touch UI bindings | Confirmed | `Packages/manifest.json`, `Assets/Settings/InputSystem_Actions.inputactions` |
| Testing | Unity Test Framework 1.7.0 is installed | Confirmed | `Packages/manifest.json` |
| 2D | Unity 2D animation, sprites, tilemaps, SpriteShape, and import tooling | Confirmed | `Packages/manifest.json` |
| AI tooling | Unity AI Assistant package is installed, but no Unity MCP client tools are exposed to this Codex session | Confirmed | `Packages/manifest.json`, active Codex tool inventory |
| Networking | No runtime networking framework or first-party multiplayer code | Confirmed | package and source inventory |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scenes` | Contains the starter `SampleScene` | Confirmed | repository inventory |
| `Assets/Settings` | URP 2D, volume, scene-template, and default Input System assets | Confirmed | repository inventory |
| `Assets/PuzzleGame` | Planned first-party root for runtime, presentation adapters, content examples, and tests | Planned | Codex core implementation plan |
| `docs` | Design, agent prompts, project context, and implementation plans | Confirmed | repository inventory |

## Assembly Boundaries

No first-party assemblies existed at analysis time. The implementation will establish:

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `PuzzleGame.Core` | Deterministic contracts, board, combat, progression, gacha, stages, scheduling, persistence DTOs | Standard C# and Unity serialization types only | Presentation-independent domain logic |
| `PuzzleGame.Unity` | MonoBehaviour input, scene composition, simple playable-slice view | `PuzzleGame.Core`, Input System, Unity runtime | Thin Unity adapter layer |
| `PuzzleGame.Tests.EditMode` | Domain and integration tests | `PuzzleGame.Core`, Unity Test Framework | Main automated suite |
| `PuzzleGame.Tests.PlayMode` | Input/lifecycle and playable-slice smoke tests | runtime assemblies, Unity Test Framework | Only engine-dependent behavior |

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/SampleScene.unity` (enabled)
- Likely startup scene: `SampleScene`
- Scene loading flow: no first-party loader exists yet; the vertical slice will bootstrap additively from a runtime component without redefining presentation-owned assets.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Domain model | Plain C# deterministic services and state objects | Planned | testability and ownership requirements in the core prompt |
| Authoring contracts | Serializable stable DTO/config classes, with ScriptableObject catalog adapters where useful | Planned | shared data-contract requirements |
| Runtime integration | Thin MonoBehaviours translate Input System and Unity lifecycle into domain commands/events | Planned | PC/mobile input and presentation separation requirements |
| Presentation boundary | Core exposes immutable results/events; UI/VFX consumes them without owning gameplay rules | Planned | Codex/Claude parallel ownership rules |

## Coding Conventions

- Namespace style: `PuzzleGame.<Feature>` with focused feature folders.
- Serialized fields: private serialized references in Unity adapters; serializable data contracts expose authorable fields with safe defaults.
- Async: no framework selected; deterministic core remains synchronous. Persistence uses explicit storage interfaces.
- Comments/docs: document public contracts and non-obvious game rules; avoid comments that narrate straightforward code.

## Testing And Validation

- EditMode tests: none initially; Unity Test Framework is installed and will host deterministic core tests.
- PlayMode tests: none initially; add only for MonoBehaviour lifecycle/input integration.
- CI/build validation: no CI configuration found. Unity Editor executable is available at `C:/Program Files/Unity/Hub/Editor/6000.5.8f1/Editor/Unity.exe`; batch-mode validation may require a non-open project copy because the project is currently open in Unity.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity editor process/version | available | running Unity process and `ProjectVersion.txt` |
| Unity batch-mode executable | available | installed editor path |
| Unity MCP connection, console, scene, asset, and test tools | unavailable | no Unity MCP tools exposed to the session |
| Repository and serialized-asset inspection | available | local filesystem and Git |

## Important Constraints

- Landscape-only, PC and mobile, mouse and touch.
- Core owns shared contracts and game rules; presentation/content consumers must not be overwritten.
- Build shared contracts before the vertical slice, then complete all remaining core systems.
- Keep summon RNG complete before reveal presentation begins.
- Keep gameplay rules deterministic and testable outside presentation.
- Do not add real-money payment processing.

## Unknowns And Confidence

- Live Unity Console and Test Runner state cannot be queried through MCP; validate via Editor logs and batch mode where possible.
- Final UI/VFX and bulk content are intentionally outside this branch.
- The product title remains replaceable; existing `productName` is the placeholder `puzzle game`.

## Source Files Inspected

- `docs/agent-prompts/CODEX_CORE_PROMPT.md`
- `docs/superpowers/specs/2026-08-24-puzzlegame-design.md`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Settings/InputSystem_Actions.inputactions`

<!-- unity-onboarding:generated:end -->
