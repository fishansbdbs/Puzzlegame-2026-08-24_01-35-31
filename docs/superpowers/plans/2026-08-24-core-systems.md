# PuzzleGame Core Systems Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a tested Unity 6 core architecture and playable 6x5 match-color battle slice, followed by the complete progression, economy, gacha, stage, scheduler, and versioned persistence scope assigned to Codex.

**Architecture:** `PuzzleGame.Core` contains deterministic plain-C# contracts and domain services. `PuzzleGame.Unity` is a thin MonoBehaviour/Input System adapter and a code-composed playable sample, so presentation/content branches can consume stable contracts without taking ownership of game rules. Authoring data uses serializable records with stable IDs; runtime state is separate and persistence crosses an explicit storage boundary.

**Tech Stack:** Unity 6000.5.8f1, C#, Unity Input System 1.20, Unity Test Framework 1.7, NUnit EditMode/PlayMode tests, URP 2D.

**Spec:** `docs/superpowers/specs/2026-08-24-puzzlegame-design.md`

## Global Constraints

- Board size is exactly 6 columns by 5 rows with Fire, Water, Nature, Light, Dark, and Heart orbs and minimum match size 3.
- Dragging swaps the selected orb through crossed adjacent cells, lasts at most 10 seconds, and may end early on release.
- All simultaneous matches resolve together; cascades refill and continue until stable; distinct match groups remain distinct.
- Each separate elemental match creates a separate attack event; Heart heals; global combo count may multiply output.
- Fire is strong against Nature, Nature against Water, Water against Fire; Light and Dark are mutually advantageous.
- A party has exactly five slots and slot 1 supplies the Leader Skill.
- Enemy countdown decreases once per completed board resolution; at zero the configured action executes and countdown resets.
- Pullable rarity is 1-5; 6-star Awakening is gameplay-only and never requires duplicates; duplicate Ascension is capped at 5 with overflow converted to a universal resource.
- Standard summons cost 150 Gems for one and 1,500 Gems for ten. Gather-In steps and final guarantees are data-driven.
- Summon results are fully determined before presentation begins and presentation advances through the specified pack-state hooks.
- Permanent story consumes no stamina. Default stage stars are clear, finish above a configured HP threshold, and clear within a configured board-resolution count.
- Rotation schedules use an injected local clock and require no backend in v1.
- Save data is versioned and corrupted/unsupported data fails safely without silently overwriting it.
- Core systems do not modify Claude-owned UI, VFX, dialogue, or bulk content files.

---

### Task 1: Assembly Foundation And Shared Data Contracts

**Files:**
- Create: `Assets/PuzzleGame/Core/PuzzleGame.Core.asmdef`
- Create: `Assets/PuzzleGame/Core/Contracts/GameplayContracts.cs`
- Create: `Assets/PuzzleGame/Core/Contracts/ContentContracts.cs`
- Create: `Assets/PuzzleGame/Core/Contracts/ContractValidation.cs`
- Create: `Assets/PuzzleGame/Unity/PuzzleGame.Unity.asmdef`
- Create: `Assets/PuzzleGame/Tests/EditMode/PuzzleGame.Tests.EditMode.asmdef`
- Create: `Assets/PuzzleGame/Tests/EditMode/Contracts/ContractValidationTests.cs`

**Interfaces:**
- Produces: `OrbType`, `ElementType`, `Rarity`, `BannerType`, `ContentType`, `SkillEffectType`, `EnemyEffectType`, `RewardType`.
- Produces: serializable `CharacterData`, `SkillData`, `LeaderSkillData`, `EnemyData`, `StageData`, `WaveData`, `BannerData`, `EventData`, `RotationScheduleData`, and `RewardData`.
- Produces: `ContractValidation.Validate(IIdentifiedData data) : IReadOnlyList<string>` and stable string IDs through `IIdentifiedData.Id`.

- [ ] **Step 1: Create assemblies and write failing contract validation tests**

```csharp
[Test]
public void Character_rejects_heart_as_a_playable_element()
{
    var data = CharacterData.CreateForTests("healer", ElementType.Heart, 3);
    CollectionAssert.Contains(ContractValidation.Validate(data), "Character element cannot be Heart.");
}

[Test]
public void Stage_requires_at_least_one_wave_and_three_objectives()
{
    var data = new StageData { Id = "stage-1", Waves = Array.Empty<WaveData>(), StarObjectives = Array.Empty<StarObjectiveData>() };
    Assert.That(ContractValidation.Validate(data), Has.Count.GreaterThanOrEqualTo(2));
}
```

- [ ] **Step 2: Run EditMode tests and verify RED**

Run the contracts test assembly through Unity Test Framework. Expected: compile/test failure because the contracts and validator do not exist.

- [ ] **Step 3: Implement stable serializable contracts and validation**

```csharp
public interface IIdentifiedData { string Id { get; } }

[Serializable]
public sealed class CharacterData : IIdentifiedData
{
    public string Id;
    public string DisplayName;
    public ElementType Element;
    public int BaseRarity;
    public StatBlock BaseStats;
    public string ActiveSkillId;
    public string LeaderSkillId;
    public string PassiveId;
    public string[] Tags = Array.Empty<string>();
    public ProgressionCurveData LevelCurve = new();
    public AwakeningRequirementData Awakening = new();
    public VisualReferenceSet BaseVisuals = new();
    public VisualReferenceSet AwakenedVisuals = new();
    string IIdentifiedData.Id => Id;
}
```

Define all required contracts with safe empty-array/default instances, explicit effect payload records, no direct scene references, and validation for IDs, rarity ranges, weights, waves, objective counts, and schedule ranges.

- [ ] **Step 4: Run contract tests and full EditMode suite**

Expected: contract tests pass and Unity reports no compile errors.

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/PuzzleGame
git commit -m "feat: define shared game data contracts"
```

---

### Task 2: Deterministic Board, Drag Path, Match Groups, And Cascades

**Files:**
- Create: `Assets/PuzzleGame/Core/Board/BoardPosition.cs`
- Create: `Assets/PuzzleGame/Core/Board/BoardState.cs`
- Create: `Assets/PuzzleGame/Core/Board/BoardGenerator.cs`
- Create: `Assets/PuzzleGame/Core/Board/DragSession.cs`
- Create: `Assets/PuzzleGame/Core/Board/MatchDetector.cs`
- Create: `Assets/PuzzleGame/Core/Board/BoardResolver.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Board/BoardTests.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Board/BoardResolverTests.cs`

**Interfaces:**
- Consumes: `OrbType`.
- Produces: `BoardState.Width == 6`, `BoardState.Height == 5`, `Get`, `Set`, `Swap`, and `Clone`.
- Produces: `DragSession.TryMove(BoardPosition next, float elapsedSeconds) : bool` and `End()`; only orthogonally adjacent moves succeed before 10 seconds.
- Produces: `MatchDetector.FindGroups(BoardState) : IReadOnlyList<MatchGroup>` where each `MatchGroup` has a stable group ID, orb type, and unique cell set.
- Produces: `BoardResolver.Resolve(BoardState, IOrbSource) : BoardResolution` containing ordered cascade layers and a final stable board.

- [ ] **Step 1: Write failing board dimension, drag, grouping, and cascade tests**

```csharp
[Test]
public void Crossing_adjacent_cells_swaps_the_dragged_orb()
{
    var board = BoardFixtures.Sequential();
    var drag = new DragSession(board, new BoardPosition(0, 0), 10f);
    Assert.That(drag.TryMove(new BoardPosition(1, 0), 0.25f), Is.True);
    Assert.That(board.Get(1, 0), Is.EqualTo(OrbType.Fire));
}

[Test]
public void Intersecting_horizontal_and_vertical_runs_form_one_group()
{
    var groups = MatchDetector.FindGroups(BoardFixtures.FireCross());
    Assert.That(groups, Has.Count.EqualTo(1));
    Assert.That(groups[0].Cells, Has.Count.EqualTo(5));
}

[Test]
public void Resolver_keeps_two_disconnected_fire_matches_separate()
{
    var result = new BoardResolver().Resolve(BoardFixtures.TwoFireRows(), new QueueOrbSource(BoardFixtures.Refill));
    Assert.That(result.CascadeLayers[0].Groups.Count(g => g.OrbType == OrbType.Fire), Is.EqualTo(2));
}
```

- [ ] **Step 2: Run board tests and verify RED**

Expected: failure because board types are absent.

- [ ] **Step 3: Implement the minimal deterministic board model**

Use a flat 30-cell array with bounds checks, Fisher-Yates generation through an injected `IRandomSource`, and an initial-fill retry that avoids automatic matches.

- [ ] **Step 4: Implement drag, match grouping, gravity, refill, and stable cascade resolution**

Build horizontal and vertical run cell sets, union overlapping runs of the same orb type, clear each layer simultaneously, compact each column downward, refill from `IOrbSource`, and repeat until no groups remain. Reject non-adjacent drag jumps and moves at or after the configured duration.

- [ ] **Step 5: Run board tests and full EditMode suite**

Expected: all board and contract tests pass.

- [ ] **Step 6: Commit**

```powershell
git add -- Assets/PuzzleGame/Core/Board Assets/PuzzleGame/Tests/EditMode/Board
git commit -m "feat: implement board drag and cascade resolution"
```

---

### Task 3: Party, Skills, Combat Events, Healing, And Enemy Countdown

**Files:**
- Create: `Assets/PuzzleGame/Core/Battle/CharacterRuntime.cs`
- Create: `Assets/PuzzleGame/Core/Battle/PartyState.cs`
- Create: `Assets/PuzzleGame/Core/Battle/CombatCalculator.cs`
- Create: `Assets/PuzzleGame/Core/Battle/SkillEngine.cs`
- Create: `Assets/PuzzleGame/Core/Battle/EnemyRuntime.cs`
- Create: `Assets/PuzzleGame/Core/Battle/EnemyActionEngine.cs`
- Create: `Assets/PuzzleGame/Core/Battle/BattleEngine.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Battle/CombatTests.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Battle/EnemyAndSkillTests.cs`

**Interfaces:**
- Consumes: board resolution groups and character/skill/leader/enemy contracts.
- Produces: `PartyState` with exactly five `CharacterRuntime` members, aggregate current/max HP, and slot-zero leader evaluation.
- Produces: `CombatCalculator.Resolve(BoardResolution, PartyState, EnemyRuntime) : CombatResolution` with one `AttackEvent` per eligible character per separate elemental match and `HealEvent` entries for Heart groups.
- Produces: `SkillEngine.CanActivate`, `Activate`, and `ChargeFrom(BoardResolution)` for conversion, enhancement, attack boost, heal, timer extension, delay/countdown manipulation, shield, and direct damage effects.
- Produces: `EnemyActionEngine.AdvanceAfterBoardResolution` and reusable effect execution for all required enemy effect types.

- [ ] **Step 1: Write failing party, attack separation, affinity, Heart, leader, active-skill, and countdown tests**

```csharp
[Test]
public void Two_separate_fire_groups_create_two_attacks_for_each_fire_character()
{
    var result = CombatFixtures.Resolve(groups: MatchFixtures.TwoSeparate(OrbType.Fire), party: PartyFixtures.OneFire());
    Assert.That(result.Attacks.Count, Is.EqualTo(2));
}

[Test]
public void Heart_group_heals_from_party_recovery_without_creating_attack_events()
{
    var result = CombatFixtures.Resolve(groups: MatchFixtures.One(OrbType.Heart, 3), party: PartyFixtures.Wounded());
    Assert.That(result.TotalHealing, Is.GreaterThan(0));
    Assert.That(result.Attacks, Is.Empty);
}

[Test]
public void Countdown_one_executes_then_resets_after_completed_board_resolution()
{
    var enemy = EnemyFixtures.CountdownOneDamageAction();
    var outcome = new EnemyActionEngine().AdvanceAfterBoardResolution(enemy, BattleFixtures.Context());
    Assert.That(outcome.ExecutedActions, Has.Count.EqualTo(1));
    Assert.That(enemy.Countdown, Is.EqualTo(enemy.Data.BaseCountdown));
}
```

- [ ] **Step 2: Run battle tests and verify RED**

Expected: failure because battle runtime types are absent.

- [ ] **Step 3: Implement party, formulas, affinity, and event generation**

Use deterministic integer formulas: match scale `1 + 0.25 * (orbCount - 3)`, combo scale `1 + 0.25 * (comboCount - 1)`, leader multiplier from slot zero only, and affinity multipliers `2.0`, `0.5`, or `1.0`. Clamp healing and damage at state boundaries.

- [ ] **Step 4: Implement skills and enemy action effects**

Interpret serialized effect records through explicit switch-based handlers. Support damage, conversion, locks, poison/hazards, blockers, binds, timer reduction, absorbs, combo shields, enrage, and countdown manipulation as state changes or battle modifiers with observable result events.

- [ ] **Step 5: Run battle tests and full EditMode suite**

Expected: all tests pass with separate attack and healing events preserved.

- [ ] **Step 6: Commit**

```powershell
git add -- Assets/PuzzleGame/Core/Battle Assets/PuzzleGame/Tests/EditMode/Battle
git commit -m "feat: add party combat skills and enemy actions"
```

---

### Task 4: Stage Session, Boss Mechanic, And Three-Star Vertical-Slice Rules

**Files:**
- Create: `Assets/PuzzleGame/Core/Stages/StageCatalog.cs`
- Create: `Assets/PuzzleGame/Core/Stages/StageSession.cs`
- Create: `Assets/PuzzleGame/Core/Stages/StageObjectiveEvaluator.cs`
- Create: `Assets/PuzzleGame/Core/Stages/BossMechanicEngine.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Stages/StageSessionTests.cs`

**Interfaces:**
- Consumes: `StageData`, `WaveData`, `EnemyData`, `BattleEngine`.
- Produces: `StageCatalog.GetRequired(string id)` and supports hundreds of stage records without stage-specific code.
- Produces: `StageSession` wave advancement, resolution count, completion state, and no-stamina permanent-story entry.
- Produces: `StageObjectiveEvaluator.Evaluate(StageData, StageResult) : IReadOnlyList<StarResult>`.
- Produces: a threshold-enrage boss mechanic and combo-shield behavior through generic enemy effects.

- [ ] **Step 1: Write failing stage loading, wave, boss threshold, and star tests**

```csharp
[Test]
public void Default_objectives_award_clear_hp_threshold_and_resolution_limit_independently()
{
    var stars = StageObjectiveEvaluator.Evaluate(StageFixtures.VerticalSlice(), new StageResult(true, 0.61f, 8));
    CollectionAssert.AreEqual(new[] { true, true, true }, stars.Select(x => x.Earned));
}

[Test]
public void Boss_enrages_once_when_hp_crosses_configured_threshold()
{
    var boss = EnemyFixtures.EnrageAtHalfHealth();
    boss.TakeDamage(boss.MaxHp / 2 + 1);
    Assert.That(new BossMechanicEngine().Evaluate(boss).Single().Type, Is.EqualTo(EnemyEffectType.Enrage));
}
```

- [ ] **Step 2: Run stage tests and verify RED**

Expected: failure because stage runtime types are absent.

- [ ] **Step 3: Implement catalogs, stage session, objectives, and boss effects**

Validate IDs at catalog construction, instantiate waves lazily, count one move only after a full stable board resolution, and evaluate each star independently from the immutable `StageResult`.

- [ ] **Step 4: Run stage and full EditMode tests**

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/PuzzleGame/Core/Stages Assets/PuzzleGame/Tests/EditMode/Stages
git commit -m "feat: add stage sessions objectives and boss mechanics"
```

---

### Task 5: Mouse/Touch Input Adapter And Playable Board Slice

**Files:**
- Create: `Assets/PuzzleGame/Unity/Input/BoardPointerInput.cs`
- Create: `Assets/PuzzleGame/Unity/Battle/BoardLayout.cs`
- Create: `Assets/PuzzleGame/Unity/Battle/BoardView.cs`
- Create: `Assets/PuzzleGame/Unity/Battle/PlayableBattleController.cs`
- Create: `Assets/PuzzleGame/Tests/PlayMode/PuzzleGame.Tests.PlayMode.asmdef`
- Create: `Assets/PuzzleGame/Tests/PlayMode/Input/BoardPointerInputTests.cs`
- Create: `Assets/PuzzleGame/Tests/PlayMode/Battle/PlayableBattleControllerTests.cs`

**Interfaces:**
- Consumes: `DragSession`, `BoardResolver`, `BattleEngine`, `StageSession`.
- Produces: pointer-down/move/up commands from mouse or primary touch without placing raw input in domain code.
- Produces: `BoardLayout.ScreenToCell(Vector2, Rect) : BoardPosition?` for landscape scaling.
- Produces: a code-composed 6x5 board view and controller events for timer, matches, cascades, attacks, healing, countdown, stage completion, and objective results.

- [ ] **Step 1: Write failing PlayMode tests for coordinate mapping, early release, timeout, and controller resolution**

```csharp
[Test]
public void Center_of_each_landscape_cell_maps_to_its_board_position()
{
    var rect = new Rect(0, 0, 600, 500);
    Assert.That(BoardLayout.ScreenToCell(new Vector2(550, 450), rect), Is.EqualTo(new BoardPosition(5, 4)));
}

[UnityTest]
public IEnumerator Releasing_pointer_ends_drag_and_resolves_exactly_once()
{
    var fixture = PlayableBattleFixture.Create();
    fixture.Input.Press(fixture.CellCenter(0, 0));
    fixture.Input.Release(fixture.CellCenter(1, 0));
    yield return null;
    Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run PlayMode tests and verify RED**

Expected: compile/test failure because Unity adapters are absent.

- [ ] **Step 3: Implement input lifecycle and board layout**

Use Input System pointer/touch controls in `Update`, prefer active primary touch over mouse, begin one drag per press, move only on cell changes, use unscaled elapsed time, release on pointer-up, and force release at 10 seconds. Balance input subscriptions in `OnEnable`/`OnDisable`.

- [ ] **Step 4: Implement the minimal playable board and battle controller**

Compose sprites from built-in primitives/materials at runtime, color and label all orb types, animate cascade layers without owning final presentation, and expose events instead of hard-coding HUD/VFX decisions.

- [ ] **Step 5: Run PlayMode tests, EditMode suite, and inspect Unity compile log**

Expected: tests pass, the running editor imports scripts, and no new compile errors appear.

- [ ] **Step 6: Commit**

```powershell
git add -- Assets/PuzzleGame/Unity Assets/PuzzleGame/Tests/PlayMode
git commit -m "feat: add mouse touch board input and playable battle slice"
```

---

### Task 6: Character Leveling, Ascension, Awakening, And Economy

**Files:**
- Create: `Assets/PuzzleGame/Core/Progression/CharacterProgress.cs`
- Create: `Assets/PuzzleGame/Core/Progression/ProgressionService.cs`
- Create: `Assets/PuzzleGame/Core/Economy/Wallet.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Progression/ProgressionTests.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Economy/WalletTests.cs`

**Interfaces:**
- Produces: level XP application and derived stats from `ProgressionCurveData`.
- Produces: `ApplyDuplicate` increasing Ascension 0-5, improving Active Skill at early ranks, passive/stats at later ranks, and returning universal-resource overflow after rank 5.
- Produces: `CanAwaken` and `Awaken` requiring configured level/materials/gold but no duplicates, switching to rarity 6 and awakened visual references.
- Produces: integer wallet balances for Gold, Gems, Tickets, universal duplicate resource, and event currencies with atomic spend/add operations.

- [ ] **Step 1: Write failing progression and wallet tests**

```csharp
[Test]
public void Sixth_duplicate_after_ascension_five_becomes_universal_resource()
{
    var result = ProgressionFixtures.MaxAscended().ApplyDuplicate();
    Assert.That(result.NewAscension, Is.EqualTo(5));
    Assert.That(result.UniversalResourceGranted, Is.GreaterThan(0));
}

[Test]
public void Awakening_consumes_materials_but_never_checks_duplicate_count()
{
    var unit = ProgressionFixtures.AwakeningReady(ascension: 0);
    Assert.That(unit.Awaken(ProgressionFixtures.Materials()), Is.True);
    Assert.That(unit.EffectiveRarity, Is.EqualTo(6));
}
```

- [ ] **Step 2: Run tests and verify RED**

- [ ] **Step 3: Implement progression and wallet boundaries**

Reject negative amounts, make failed spends leave balances unchanged, clamp level and Ascension, calculate improvements from authored tables, and return explicit result records for UI consumption.

- [ ] **Step 4: Run progression/economy and full EditMode tests**

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/PuzzleGame/Core/Progression Assets/PuzzleGame/Core/Economy Assets/PuzzleGame/Tests/EditMode/Progression Assets/PuzzleGame/Tests/EditMode/Economy
git commit -m "feat: add progression ascension awakening and economy"
```

---

### Task 7: Gacha Probability, Guarantees, Gather-In, And Pack State Hooks

**Files:**
- Create: `Assets/PuzzleGame/Core/Gacha/SummonService.cs`
- Create: `Assets/PuzzleGame/Core/Gacha/BannerRuntimeState.cs`
- Create: `Assets/PuzzleGame/Core/Gacha/GachaRandom.cs`
- Create: `Assets/PuzzleGame/Core/Gacha/PackSummonFlow.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Gacha/SummonServiceTests.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Gacha/PackSummonFlowTests.cs`

**Interfaces:**
- Consumes: `BannerData`, `CharacterData`, `Wallet`, progression duplicate handling, injected `IRandomSource`.
- Produces: `Quote`, `CanSummon`, and atomic `PurchaseAndRoll` returning a complete immutable `SummonBatch` before pack presentation.
- Produces: Standard, Featured, Step-Up, and Gather-In banner state with per-rotation one-time steps.
- Produces: final-step `GuaranteedFiveStarFeaturedBoost` and `GuaranteedFeaturedFiveStar` policies.
- Produces: ordered hooks `PurchaseValidated`, `PackPresented`, `PackRipStarted`, `PackOpened`, `CardReady`, `CardRevealed`, `AllCardsRevealed`, `ResultsComplete`.

- [ ] **Step 1: Write failing pricing, weighted-roll, guarantee, step-state, atomicity, and pack-transition tests**

```csharp
[TestCase(1, 150)]
[TestCase(10, 1500)]
public void Standard_banner_uses_fixed_gem_prices(int count, int expectedCost)
{
    Assert.That(SummonFixtures.Standard().Quote(count).GemCost, Is.EqualTo(expectedCost));
}

[Test]
public void Rotation_two_final_gather_in_step_guarantees_featured_five_star()
{
    var batch = SummonFixtures.GatherInRotationTwoFinal().PurchaseAndRoll();
    Assert.That(batch.Results, Has.Some.Matches<SummonResult>(x => x.CharacterId == "featured-five"));
}

[Test]
public void Results_exist_before_pack_presented_event()
{
    var flow = PackSummonFlow.Start(SummonFixtures.BatchOfTen());
    Assert.That(flow.Batch.Results, Has.Count.EqualTo(10));
    Assert.That(flow.State, Is.EqualTo(PackSummonState.PurchaseValidated));
}
```

- [ ] **Step 2: Run gacha tests and verify RED**

- [ ] **Step 3: Implement deterministic weighted rolls and guarantee replacement**

Validate positive weights, roll through cumulative integer weights, apply final guarantees without changing batch size, identify NEW/DUPLICATE before reveal, and update banner step state only after an affordable purchase succeeds.

- [ ] **Step 4: Implement strict pack state machine**

Reject invalid transitions, expose transition events carrying the immutable batch/card index, support per-card reveal and Reveal All, and finish at `ResultsComplete`.

- [ ] **Step 5: Run gacha tests and full EditMode suite**

- [ ] **Step 6: Commit**

```powershell
git add -- Assets/PuzzleGame/Core/Gacha Assets/PuzzleGame/Tests/EditMode/Gacha
git commit -m "feat: implement gacha guarantees and pack summon flow"
```

---

### Task 8: Calendar Rotation Scheduler And Content Queries

**Files:**
- Create: `Assets/PuzzleGame/Core/Scheduling/IClock.cs`
- Create: `Assets/PuzzleGame/Core/Scheduling/RotationScheduler.cs`
- Create: `Assets/PuzzleGame/Core/Scheduling/SystemLocalClock.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Scheduling/RotationSchedulerTests.cs`

**Interfaces:**
- Produces: `IClock.Now : DateTimeOffset` and local-time `SystemLocalClock`.
- Produces: `RotationScheduler.GetActive(IEnumerable<RotationScheduleData>) : IReadOnlyList<ActiveContent>`.
- Supports Featured, Gather-In, Step-Up, event chapters, daily/weekly dungeons, Awakening materials, towers, and Boss Rush schedules with absolute windows and recurring weekday/time windows.

- [ ] **Step 1: Write failing boundary, recurrence, and category tests with a fake clock**

```csharp
[Test]
public void Content_is_active_at_inclusive_start_and_inactive_at_exclusive_end()
{
    var clock = new FakeClock(new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.FromHours(-5)));
    var scheduler = new RotationScheduler(clock);
    Assert.That(scheduler.GetActive(new[] { ScheduleFixtures.StartsNow() }), Has.Count.EqualTo(1));
    clock.Now = ScheduleFixtures.StartsNow().End;
    Assert.That(scheduler.GetActive(new[] { ScheduleFixtures.StartsNow() }), Is.Empty);
}
```

- [ ] **Step 2: Run scheduler tests and verify RED**

- [ ] **Step 3: Implement injected-clock schedule evaluation**

Use `[start, end)` semantics, preserve local offsets, evaluate recurring weekday windows including overnight windows, filter disabled records, and sort active content deterministically by priority then ID.

- [ ] **Step 4: Run scheduler and full EditMode tests**

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/PuzzleGame/Core/Scheduling Assets/PuzzleGame/Tests/EditMode/Scheduling
git commit -m "feat: add local calendar rotation scheduler"
```

---

### Task 9: Versioned Save/Load And Migration

**Files:**
- Create: `Assets/PuzzleGame/Core/Persistence/SaveData.cs`
- Create: `Assets/PuzzleGame/Core/Persistence/ISaveStorage.cs`
- Create: `Assets/PuzzleGame/Core/Persistence/SaveSerializer.cs`
- Create: `Assets/PuzzleGame/Core/Persistence/SaveService.cs`
- Create: `Assets/PuzzleGame/Unity/Persistence/FileSaveStorage.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Persistence/SaveServiceTests.cs`

**Interfaces:**
- Produces: `SaveData.CurrentVersion`, wallet balances, character progress, inventory/materials, party IDs, banner states, stage progress, and settings-safe defaults.
- Produces: `ISaveStorage.Read`, `WriteAtomic`, and `BackupCorrupt`.
- Produces: `SaveService.LoadOrCreate` and `Save` with v1-to-current migration and explicit `SaveLoadResult` status.

- [ ] **Step 1: Write failing new-save, round-trip, missing-field, v1 migration, and corrupt-data tests**

```csharp
[Test]
public void Corrupt_save_is_backed_up_and_returns_fresh_state_without_overwriting_source()
{
    var storage = new MemorySaveStorage("{broken-json");
    var result = new SaveService(storage).LoadOrCreate();
    Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromCorrupt));
    Assert.That(storage.Backups, Has.Count.EqualTo(1));
    Assert.That(storage.OriginalText, Is.EqualTo("{broken-json"));
}
```

- [ ] **Step 2: Run persistence tests and verify RED**

- [ ] **Step 3: Implement DTOs, serializer, migration, and atomic storage boundary**

Serialize with Unity-compatible JSON records, validate version before mapping to runtime state, migrate missing v1 fields to safe defaults, write to a temporary sibling file then replace, and retain corrupt input as a timestamped backup.

- [ ] **Step 4: Run persistence and full EditMode tests**

- [ ] **Step 5: Commit**

```powershell
git add -- Assets/PuzzleGame/Core/Persistence Assets/PuzzleGame/Unity/Persistence Assets/PuzzleGame/Tests/EditMode/Persistence
git commit -m "feat: add versioned save load and migration"
```

---

### Task 10: Integrated Sample Content, Runtime Bootstrap, And End-To-End Validation

**Files:**
- Create: `Assets/PuzzleGame/Core/Sample/VerticalSliceFactory.cs`
- Create: `Assets/PuzzleGame/Unity/Bootstrap/VerticalSliceBootstrap.cs`
- Create: `Assets/PuzzleGame/Tests/EditMode/Integration/CoreVerticalSliceTests.cs`
- Create: `Assets/PuzzleGame/Tests/PlayMode/Integration/VerticalSliceSmokeTests.cs`
- Modify: `docs/AI/UnityProjectContext.md`
- Create: `docs/core-systems.md`

**Interfaces:**
- Consumes all prior core systems.
- Produces one sample five-character party, Leader Skill, Active Skill, enemy action, boss mechanic, stage, Ascension case, Awakening case, Standard banner, two Gather-In rotations, pack flow, save profile, and scheduled rotating event entirely through shared contracts.
- Produces a self-bootstrapping sample in the enabled `SampleScene` without hand-editing presentation-owned scene YAML.

- [ ] **Step 1: Write failing end-to-end domain and PlayMode smoke tests**

```csharp
[Test]
public void Sample_slice_exercises_every_required_core_system()
{
    var sample = VerticalSliceFactory.Create(seed: 24082026);
    Assert.Multiple(() =>
    {
        Assert.That(sample.Party.Members, Has.Count.EqualTo(5));
        Assert.That(sample.StandardBanner.Type, Is.EqualTo(BannerType.Standard));
        Assert.That(sample.GatherInBanner.Steps.Last().PullCount, Is.EqualTo(10));
        Assert.That(sample.Stage.StarObjectives, Has.Count.EqualTo(3));
        Assert.That(sample.Schedule.ContentType, Is.EqualTo(ContentType.EventChapter));
    });
}

[UnityTest]
public IEnumerator Bootstrap_creates_a_playable_six_by_five_board()
{
    var bootstrap = VerticalSliceBootstrap.CreateForTests();
    yield return null;
    Assert.That(bootstrap.Controller.BoardView.CellCount, Is.EqualTo(30));
    Assert.That(bootstrap.Controller.MoveTimeLimitSeconds, Is.EqualTo(10f));
}
```

- [ ] **Step 2: Run integration tests and verify RED**

- [ ] **Step 3: Implement sample data factory and bootstrap**

Use runtime initialization to attach the bootstrap only when no authored `VerticalSliceBootstrap` exists. Keep all sample definitions under `Core/Sample`, use deterministic seed `24082026`, and expose presentation events/contracts without shipping bulk content.

- [ ] **Step 4: Document contracts, extension points, testing, and sample controls**

Document the assembly dependency direction, contract ownership, how presentation/content code registers catalogs and subscribes to pack/battle events, mouse/touch drag behavior, save path boundary, scheduler clock injection, and Unity test commands.

- [ ] **Step 5: Run full EditMode and PlayMode suites in Unity**

Run all tests with the strongest available Unity method. Expected: every test passes, no compile errors, no unexpected warnings from first-party code.

- [ ] **Step 6: Perform playable-slice smoke validation**

Enter Play Mode when safe, verify the 6x5 board accepts mouse drag and touch-compatible pointer input, releases early, times out at 10 seconds, resolves cascades, emits separate attacks and Heart healing, advances enemy countdown, and evaluates stage stars.

- [ ] **Step 7: Review serialized assets, generated metadata, and ownership boundaries**

Verify every new asset has a unique `.meta`, no existing scene/prefab/content asset was unintentionally changed, and `git diff --check` is clean.

- [ ] **Step 8: Commit**

```powershell
git add -- Assets/PuzzleGame docs/AI/UnityProjectContext.md docs/core-systems.md
git commit -m "feat: complete playable core systems vertical slice"
```

