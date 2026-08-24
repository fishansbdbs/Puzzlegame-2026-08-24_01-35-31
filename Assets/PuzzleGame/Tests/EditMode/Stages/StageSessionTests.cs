using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Stages;

namespace PuzzleGame.Tests.EditMode.Stages
{
    public sealed class StageSessionTests
    {
        [Test]
        public void Catalog_rejects_blank_and_duplicate_stage_ids_and_unknown_stage_lookups()
        {
            var enemy = StageFixtures.Enemy("slime");
            Assert.That(() => new StageCatalog(new[] { StageFixtures.Stage(" ", enemy.Id) }, new[] { enemy }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new StageCatalog(new[] { StageFixtures.Stage("one", enemy.Id), StageFixtures.Stage("one", enemy.Id) }, new[] { enemy }), Throws.TypeOf<ArgumentException>());

            var catalog = new StageCatalog(new[] { StageFixtures.Stage("one", enemy.Id) }, new[] { enemy });
            Assert.That(() => catalog.GetRequired("missing"), Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void Catalog_rejects_blank_or_duplicate_enemies_and_unknown_wave_enemy_references()
        {
            var valid = StageFixtures.Enemy("slime");
            Assert.That(() => new StageCatalog(new[] { StageFixtures.Stage("one", "missing") }, new[] { valid }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new StageCatalog(new[] { StageFixtures.Stage("one", valid.Id) }, new[] { valid, StageFixtures.Enemy("slime") }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new StageCatalog(new[] { StageFixtures.Stage("one", valid.Id) }, new[] { StageFixtures.Enemy(" ") }), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Catalog_rejects_duplicate_wave_ids_and_invalid_star_boundaries()
        {
            var enemy = StageFixtures.Enemy("slime");
            var duplicateWave = StageFixtures.Stage("one", enemy.Id, enemy.Id);
            duplicateWave.Waves[1].Id = duplicateWave.Waves[0].Id;
            var invalidObjective = StageFixtures.Stage("two", enemy.Id);
            invalidObjective.StarObjectives[1].HpThresholdPercent = 1.01f;
            var invalidThreshold = StageFixtures.Enemy("bad-threshold", actions: new[] { new EnemyActionData { Id = "enrage", ResetCountdown = 1, Effects = new[]
            {
                new EnemyEffectData { Type = EnemyEffectType.Enrage, Payload = new EffectPayloadData { Amount = -1, Multiplier = 2f, TurnCount = 1 } }
            }} });

            Assert.That(() => new StageCatalog(new[] { duplicateWave }, new[] { enemy }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new StageCatalog(new[] { invalidObjective }, new[] { enemy }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new StageCatalog(new[] { StageFixtures.Stage("three", invalidThreshold.Id) }, new[] { invalidThreshold }), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Catalog_and_session_snapshot_authored_stage_wave_and_enemy_data()
        {
            var enemy = StageFixtures.Enemy("slime", hp: 30);
            var stage = StageFixtures.Stage("one", enemy.Id);
            var catalog = new StageCatalog(new[] { stage }, new[] { enemy });
            stage.Waves[0].EnemyIds[0] = "other";
            enemy.BaseStats.Hp = 999;
            var exposed = catalog.GetRequired("one");
            exposed.Waves[0].EnemyIds[0] = "other";

            var session = catalog.CreateSession("one");

            Assert.That(session.CurrentWave.EnemyIds.Single(), Is.EqualTo("slime"));
            Assert.That(session.CurrentEnemies.Single().MaxHp, Is.EqualTo(30));
        }

        [Test]
        public void Session_loads_waves_lazily_in_authored_order_and_exposes_detached_wave_data()
        {
            var first = StageFixtures.Enemy("first");
            var second = StageFixtures.Enemy("second");
            var stage = StageFixtures.Stage("two", first.Id, second.Id);
            var session = new StageCatalog(new[] { stage }, new[] { first, second }).CreateSession("two");

            Assert.That(session.HasLoadedCurrentWave, Is.False);
            Assert.That(session.CurrentWaveIndex, Is.EqualTo(0));
            var current = session.CurrentWave;
            current.EnemyIds[0] = "mutated";

            Assert.That(session.HasLoadedCurrentWave, Is.True);
            Assert.That(session.CurrentEnemies.Single().Data.Id, Is.EqualTo("first"));
            Assert.That(session.CurrentWave.EnemyIds.Single(), Is.EqualTo("first"));
        }

        [Test]
        public void Session_does_not_advance_a_wave_until_every_enemy_is_defeated()
        {
            var first = StageFixtures.Enemy("first");
            var second = StageFixtures.Enemy("second");
            var session = new StageCatalog(new[] { StageFixtures.Stage("two", first.Id, second.Id) }, new[] { first, second }).CreateSession("two");
            var enemies = session.CurrentEnemies;

            Assert.That(session.TryAdvanceWave(), Is.False);
            Assert.That(session.CurrentWaveIndex, Is.EqualTo(0));
            enemies.Single().ApplyDamage(int.MaxValue);

            Assert.That(session.TryAdvanceWave(), Is.True);
            Assert.That(session.CurrentWaveIndex, Is.EqualTo(1));
            Assert.That(session.CurrentEnemies.Single().Data.Id, Is.EqualTo("second"));
        }

        [Test]
        public void Session_counts_only_a_completed_non_defeated_battle_resolution()
        {
            var enemy = StageFixtures.Enemy("slime", hp: 1000);
            var session = new StageCatalog(new[] { StageFixtures.Stage("one", enemy.Id) }, new[] { enemy }).CreateSession("one");
            var aliveContext = StageFixtures.Context(session.CurrentEnemies.Single(), partyHp: 10);
            var defeatedContext = StageFixtures.Context(session.CurrentEnemies.Single(), partyHp: 10);
            defeatedContext.Party.ApplyDamage(int.MaxValue);
            var resolution = StageFixtures.EmptyResolution();

            var completed = session.CompleteBoardResolution(new BattleEngine(), resolution, aliveContext);
            session.RecordBoardResolution(completed);
            session.CompleteBoardResolution(new BattleEngine(), resolution, defeatedContext);

            Assert.That(session.BoardResolutionCount, Is.EqualTo(1));
        }

        [Test]
        public void Final_wave_completion_locks_further_stage_actions_and_counts()
        {
            var enemy = StageFixtures.Enemy("slime");
            var session = new StageCatalog(new[] { StageFixtures.Stage("one", enemy.Id) }, new[] { enemy }).CreateSession("one");
            session.CurrentEnemies.Single().ApplyDamage(int.MaxValue);

            Assert.That(session.TryAdvanceWave(), Is.True);
            Assert.That(session.IsCompleted, Is.True);
            Assert.That(() => session.TryAdvanceWave(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => session.RecordBoardResolution(StageFixtures.CompletedTurn()), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Permanent_story_sessions_observably_consume_no_stamina()
        {
            var enemy = StageFixtures.Enemy("slime");
            var stage = StageFixtures.Stage("story", enemy.Id);
            stage.IsPermanentStory = true;

            var session = new StageCatalog(new[] { stage }, new[] { enemy }).CreateSession("story");

            Assert.That(session.ConsumesStamina, Is.False);
        }

        [TestCase(true, 0.60f, 8, true, true, true)]
        [TestCase(true, 0.59f, 8, true, false, true)]
        [TestCase(true, 0.60f, 9, true, true, false)]
        [TestCase(true, 0.59f, 9, true, false, false)]
        [TestCase(false, 1.00f, 0, false, false, false)]
        public void Objectives_evaluate_each_authored_star_independently_at_inclusive_boundaries(bool clear, float hp, int count, bool clearStar, bool hpStar, bool countStar)
        {
            var stars = StageObjectiveEvaluator.Evaluate(StageFixtures.VerticalSlice(), new StageResult(clear, hp, count));

            CollectionAssert.AreEqual(new[] { clearStar, hpStar, countStar }, stars.Select(item => item.Earned));
        }

        [Test]
        public void Stage_result_rejects_nonfinite_out_of_range_hp_and_negative_counts()
        {
            Assert.That(() => new StageResult(true, float.NaN, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new StageResult(true, 1.01f, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new StageResult(true, 0f, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Objective_results_preserve_authored_order_and_snapshot_the_objective()
        {
            var stage = StageFixtures.VerticalSlice();
            var results = StageObjectiveEvaluator.Evaluate(stage, new StageResult(true, .60f, 8));
            stage.StarObjectives[0].Type = StarObjectiveType.ClearWithinBoardResolutionCount;
            var exposed = results[1].Objective;
            exposed.HpThresholdPercent = 0f;

            CollectionAssert.AreEqual(new[] { StarObjectiveType.Clear, StarObjectiveType.FinishAboveHpThreshold, StarObjectiveType.ClearWithinBoardResolutionCount }, results.Select(item => item.Objective.Type));
            Assert.That(results[1].Objective.HpThresholdPercent, Is.EqualTo(.60f));
        }

        [Test]
        public void Boss_enrages_once_when_hp_crosses_configured_threshold_and_preserves_a_stronger_state()
        {
            var boss = StageFixtures.EnrageBoss(thresholdPercent: 50, multiplier: 2f, turns: 3);
            boss.SetEnrage(4f, 1);
            boss.ApplyDamage(boss.MaxHp / 2 + 1);
            var mechanic = new BossMechanicEngine();

            var first = mechanic.Evaluate(boss);
            var repeated = mechanic.Evaluate(boss);

            Assert.That(first.Single().Type, Is.EqualTo(EnemyEffectType.Enrage));
            Assert.That(repeated, Is.Empty);
            Assert.That(boss.AttackMultiplier, Is.EqualTo(4f));
            Assert.That(boss.EnrageTurns, Is.EqualTo(3));
        }

        [Test]
        public void Boss_enrage_triggers_for_a_large_jump_and_initial_below_threshold_but_not_above_it()
        {
            var mechanic = new BossMechanicEngine();
            var largeJump = StageFixtures.EnrageBoss(thresholdPercent: 50);
            largeJump.ApplyDamage(99);
            var initialBelow = StageFixtures.EnrageBoss(thresholdPercent: 50);
            initialBelow.ApplyDamage(51);
            var above = StageFixtures.EnrageBoss(thresholdPercent: 50);
            above.ApplyDamage(49);

            Assert.That(mechanic.Evaluate(largeJump), Has.Count.EqualTo(1));
            Assert.That(mechanic.Evaluate(initialBelow), Has.Count.EqualTo(1));
            Assert.That(mechanic.Evaluate(above), Is.Empty);
        }

        [Test]
        public void Generic_combo_shield_effect_blocks_damage_at_or_below_its_authored_combo_threshold()
        {
            var enemy = StageFixtures.Enemy("shielded", hp: 100, actions: new[]
            {
                new EnemyActionData { Id = "shield", ResetCountdown = 1, Effects = new[]
                {
                    new EnemyEffectData { Type = EnemyEffectType.ComboShield, Payload = new EffectPayloadData { Amount = 2, TurnCount = 2 } }
                }}
            });
            var runtime = new EnemyRuntime(enemy);
            var context = StageFixtures.Context(runtime, partyHp: 10);
            new EnemyActionEngine().AdvanceAfterBoardResolution(runtime, context);

            var result = new CombatCalculator().Resolve(StageFixtures.TwoFireGroups(), context.Party, runtime);

            Assert.That(runtime.ComboShieldMinimum, Is.EqualTo(2));
            Assert.That(result.Attacks.All(attack => attack.WasBlocked && attack.Damage == 0), Is.True);
        }
    }

    internal static class StageFixtures
    {
        internal static StageData VerticalSlice()
        {
            var stage = Stage("vertical", "slime");
            stage.StarObjectives = new[]
            {
                new StarObjectiveData { Type = StarObjectiveType.Clear },
                new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = .60f },
                new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = 8 }
            };
            return stage;
        }

        internal static StageData Stage(string id, params string[] enemyIds)
        {
            var waves = new WaveData[enemyIds.Length];
            for (var index = 0; index < enemyIds.Length; index++)
                waves[index] = new WaveData { Id = "wave-" + index, EnemyIds = new[] { enemyIds[index] } };
            return new StageData { Id = id, Waves = waves, StarObjectives = VerticalSliceObjectives() };
        }

        internal static StarObjectiveData[] VerticalSliceObjectives()
        {
            return new[]
            {
                new StarObjectiveData { Type = StarObjectiveType.Clear },
                new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = .60f },
                new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = 8 }
            };
        }

        internal static EnemyData Enemy(string id, int hp = 100, EnemyActionData[] actions = null)
        {
            return new EnemyData
            {
                Id = id, Element = ElementType.Light, BaseStats = new StatBlock { Hp = hp, Attack = 1, Recovery = 0 }, InitialCountdown = 1,
                Actions = actions ?? new[] { new EnemyActionData { Id = "wait", ResetCountdown = 1, Effects = new EnemyEffectData[0] } }
            };
        }

        internal static EnemyRuntime EnrageBoss(int thresholdPercent, float multiplier = 2f, int turns = 3)
        {
            return new EnemyRuntime(Enemy("boss", actions: new[] { new EnemyActionData { Id = "enrage-threshold", ResetCountdown = 1, Effects = new[]
            {
                new EnemyEffectData { Type = EnemyEffectType.Enrage, Payload = new EffectPayloadData { Amount = thresholdPercent, Multiplier = multiplier, TurnCount = turns } }
            }} }));
        }

        internal static BattleContext Context(EnemyRuntime enemy, int partyHp)
        {
            var members = new CharacterRuntime[5];
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light, ElementType.Dark };
            for (var index = 0; index < members.Length; index++) members[index] = new CharacterRuntime(new CharacterData
            {
                Id = "character-" + index, Element = elements[index], BaseRarity = 1,
                BaseStats = new StatBlock { Hp = partyHp, Attack = 10, Recovery = 0 }
            });
            return new BattleContext(StableBoard(), new PartyState(members), enemy);
        }

        internal static BoardResolution EmptyResolution() { return new BoardResolver().Resolve(StableBoard(), new Refill()); }
        internal static BoardResolution TwoFireGroups()
        {
            var board = StableBoard();
            for (var x = 0; x < 3; x++) board.Set(x, 0, OrbType.Fire);
            for (var x = 3; x < 6; x++) board.Set(x, 4, OrbType.Fire);
            return new BoardResolver().Resolve(board, new Refill());
        }
        internal static BattleTurnResolution CompletedTurn() { return null; }
        private static BoardState StableBoard()
        {
            var board = new BoardState();
            for (var y = 0; y < BoardState.Rows; y++) for (var x = 0; x < BoardState.Columns; x++) board.Set(x, y, (OrbType)((x + y + 1) % 6));
            return board;
        }
        private sealed class Refill : IOrbSource
        {
            private int next;
            public OrbType NextOrb()
            {
                var pattern = new[] { OrbType.Light, OrbType.Dark, OrbType.Heart, OrbType.Nature, OrbType.Water };
                return pattern[next++ % pattern.Length];
            }
        }
    }
}
