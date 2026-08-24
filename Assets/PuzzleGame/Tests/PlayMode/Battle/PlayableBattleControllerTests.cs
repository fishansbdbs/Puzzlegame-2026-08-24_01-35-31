using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Stages;
using PuzzleGame.Unity.Battle;
using PuzzleGame.Unity.Input;
using UnityEngine;

namespace PuzzleGame.Tests.PlayMode.Battle
{
    public sealed class BoardLayoutTests
    {
        [Test]
        public void Center_of_every_landscape_cell_maps_to_its_board_position()
        {
            var rect = new Rect(10f, 20f, 600f, 500f);
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
            {
                var center = new Vector2(10f + (x + .5f) * 100f, 20f + (y + .5f) * 100f);
                Assert.That(BoardLayout.ScreenToCell(center, rect), Is.EqualTo(new BoardPosition(x, y)), "cell " + x + "," + y);
            }
        }

        [Test]
        public void Exact_minimum_and_maximum_edges_are_inclusive_and_internal_edges_choose_the_upper_cell()
        {
            var rect = new Rect(10f, 20f, 600f, 500f);

            Assert.That(BoardLayout.ScreenToCell(new Vector2(10f, 20f), rect), Is.EqualTo(new BoardPosition(0, 0)));
            Assert.That(BoardLayout.ScreenToCell(new Vector2(610f, 520f), rect), Is.EqualTo(new BoardPosition(5, 4)));
            Assert.That(BoardLayout.ScreenToCell(new Vector2(110f, 120f), rect), Is.EqualTo(new BoardPosition(1, 1)));
        }

        [Test]
        public void Outside_zero_size_and_nonfinite_inputs_are_rejected()
        {
            var rect = new Rect(10f, 20f, 600f, 500f);

            Assert.That(BoardLayout.ScreenToCell(new Vector2(9.999f, 20f), rect), Is.Null);
            Assert.That(BoardLayout.ScreenToCell(new Vector2(610.001f, 520f), rect), Is.Null);
            Assert.That(BoardLayout.ScreenToCell(new Vector2(float.NaN, 20f), rect), Is.Null);
            Assert.That(BoardLayout.ScreenToCell(new Vector2(10f, float.PositiveInfinity), rect), Is.Null);
            Assert.That(BoardLayout.ScreenToCell(Vector2.zero, new Rect(0f, 0f, 0f, 500f)), Is.Null);
            Assert.That(BoardLayout.ScreenToCell(Vector2.zero, new Rect(0f, 0f, 600f, -1f)), Is.Null);
        }
    }

    public sealed class PlayableBattleControllerTests
    {
        [Test]
        public void Releasing_pointer_ends_drag_and_resolves_exactly_once()
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Input.Release(fixture.CellCenter(1, 0));
                fixture.Input.Release(fixture.CellCenter(1, 0));
                fixture.Controller.AdvanceTime(20f);

                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Timeout_at_exact_duration_ends_once_using_explicit_unscaled_time_step()
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                var timers = new List<float>();
                fixture.Controller.TimerChanged += item => timers.Add(item.RemainingSeconds);
                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Controller.AdvanceTime(9f);

                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.Zero);
                fixture.Controller.AdvanceTime(1f);
                fixture.Controller.AdvanceTime(1f);

                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(timers.Last(), Is.EqualTo(0f));
            }
        }

        [Test]
        public void Only_adjacent_cell_changes_move_the_dragged_orb()
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                var first = fixture.Context.Board.Get(0, 0);
                var second = fixture.Context.Board.Get(1, 0);
                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Input.Move(fixture.CellCenter(0, 0));
                fixture.Input.Move(fixture.CellCenter(0, 0));
                fixture.Input.Move(fixture.CellCenter(2, 0));

                Assert.That(fixture.Context.Board.Get(0, 0), Is.EqualTo(first));
                Assert.That(fixture.Context.Board.Get(1, 0), Is.EqualTo(second));
                fixture.Input.Move(fixture.CellCenter(1, 0));

                Assert.That(fixture.Context.Board.Get(0, 0), Is.EqualTo(second));
                Assert.That(fixture.Context.Board.Get(1, 0), Is.EqualTo(first));
            }
        }

        [Test]
        public void Invalid_cells_and_reentrant_press_while_resolving_are_ignored()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000))
            {
                fixture.Input.Press(new Vector2(-1f, -1f));
                Assert.That(fixture.Controller.IsDragging, Is.False);
                fixture.Controller.CascadeLayerResolved += delegate { fixture.Input.Press(fixture.CellCenter(5, 4)); };

                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Input.Release(fixture.CellCenter(5, 4));

                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Controller.IsDragging, Is.False);
            }
        }

        [Test]
        public void Disable_during_drag_resolves_by_rule_and_reenable_balances_subscriptions()
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                Assert.That(fixture.Input.SubscriptionCount, Is.EqualTo(3));
                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Root.SetActive(false);
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Input.SubscriptionCount, Is.Zero);
                fixture.Root.SetActive(true);
                Assert.That(fixture.Input.SubscriptionCount, Is.EqualTo(3));
                fixture.Input.Release(fixture.CellCenter(0, 0));
                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Input.Release(fixture.CellCenter(0, 0));

                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(2));
            }
        }

        [Test]
        public void Final_board_is_installed_and_all_thirty_view_cells_refresh_consistently()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000))
            {
                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Input.Release(fixture.CellCenter(5, 4));

                Assert.That(fixture.View.CellCount, Is.EqualTo(30));
                Assert.That(fixture.Controller.CurrentBoard, Is.SameAs(fixture.Context.Board));
                for (var y = 0; y < BoardState.Rows; y++)
                for (var x = 0; x < BoardState.Columns; x++)
                    Assert.That(fixture.View.GetDisplayedOrb(new BoardPosition(x, y)), Is.EqualTo(fixture.Context.Board.Get(x, y)));
            }
        }

        [Test]
        public void All_six_orb_types_have_distinct_labels_and_colors()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.Stable()))
            {
                var labels = new HashSet<string>();
                var colors = new HashSet<Color>();
                for (var x = 0; x < 6; x++)
                {
                    labels.Add(fixture.View.GetLabel(new BoardPosition(x, 0)));
                    colors.Add(fixture.View.GetColor(new BoardPosition(x, 0)));
                }

                Assert.That(labels, Has.Count.EqualTo(6));
                Assert.That(colors, Has.Count.EqualTo(6));
            }
        }

        [Test]
        public void Match_layers_attack_and_heal_are_emitted_as_separate_immutable_events()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000, damageParty: true))
            {
                var groups = new List<MatchGroupsEvent>();
                var layers = new List<CascadeLayerEvent>();
                var attacks = new List<AttackEvent>();
                var heals = new List<HealEvent>();
                fixture.Controller.MatchGroupsResolved += groups.Add;
                fixture.Controller.CascadeLayerResolved += layers.Add;
                fixture.Controller.AttackResolved += attacks.Add;
                fixture.Controller.HealResolved += heals.Add;

                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Input.Release(fixture.CellCenter(5, 4));

                Assert.That(groups, Is.Not.Empty);
                Assert.That(layers, Is.Not.Empty);
                Assert.That(attacks, Is.Not.Empty);
                Assert.That(heals, Is.Not.Empty);
                Assert.That(groups[0].Groups, Is.Not.Empty);
            }
        }

        [Test]
        public void Countdown_and_due_enemy_actions_are_emitted_after_combat()
        {
            using (var countdown = PlayableBattleFixture.Create(enemyCountdown: 2))
            {
                var values = new List<int>();
                var actions = new List<EnemyActionSnapshot>();
                countdown.Controller.EnemyCountdownChanged += item => values.Add(item.Countdown);
                countdown.Controller.EnemyActionResolved += actions.Add;
                countdown.Input.Press(countdown.CellCenter(0, 0));
                countdown.Input.Release(countdown.CellCenter(0, 0));
                CollectionAssert.AreEqual(new[] { 1 }, values);
                Assert.That(actions, Is.Empty);
            }

            using (var due = PlayableBattleFixture.Create(enemyCountdown: 1))
            {
                var actions = new List<EnemyActionSnapshot>();
                due.Controller.EnemyActionResolved += actions.Add;
                due.Input.Press(due.CellCenter(0, 0));
                due.Input.Release(due.CellCenter(0, 0));
                Assert.That(actions.Select(item => item.Id), Is.EqualTo(new[] { "tap" }));
            }
        }

        [Test]
        public void Final_enemy_defeat_counts_completes_stage_and_emits_three_star_results_in_order()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1))
            {
                var completionCount = 0;
                IReadOnlyList<StarResult> stars = null;
                fixture.Controller.StageCompleted += delegate { completionCount++; };
                fixture.Controller.StarResultsReady += value => stars = value;

                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Input.Release(fixture.CellCenter(5, 4));

                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Session.IsCompleted, Is.True);
                Assert.That(completionCount, Is.EqualTo(1));
                Assert.That(stars, Has.Count.EqualTo(3));
                Assert.That(stars[0].Objective.Type, Is.EqualTo(StarObjectiveType.Clear));
                Assert.That(stars.All(item => item.Earned), Is.True);
            }
        }

        [Test]
        public void Defeated_party_is_a_no_op_and_does_not_count_a_resolution()
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                fixture.Party.ApplyDamage(int.MaxValue);
                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Input.Release(fixture.CellCenter(0, 0));
                fixture.Controller.AdvanceTime(10f);

                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.Zero);
                Assert.That(fixture.Session.BoardResolutionCount, Is.Zero);
            }
        }

        [Test]
        public void Resolution_events_follow_timer_groups_cascade_combat_countdown_count_completion_stars_order()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1, damageParty: true))
            {
                var order = new List<string>();
                fixture.Controller.TimerChanged += item => { if (item.RemainingSeconds == 0f) order.Add("timer"); };
                fixture.Controller.MatchGroupsResolved += delegate { order.Add("groups"); };
                fixture.Controller.CascadeLayerResolved += delegate { order.Add("cascade"); };
                fixture.Controller.AttackResolved += delegate { order.Add("attack"); };
                fixture.Controller.HealResolved += delegate { order.Add("heal"); };
                fixture.Controller.EnemyCountdownChanged += delegate { order.Add("countdown"); };
                fixture.Controller.BoardResolutionCountChanged += delegate { order.Add("count"); };
                fixture.Controller.StageCompleted += delegate { order.Add("complete"); };
                fixture.Controller.StarResultsReady += delegate { order.Add("stars"); };

                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Input.Release(fixture.CellCenter(5, 4));

                Assert.That(order.IndexOf("timer"), Is.LessThan(order.IndexOf("groups")));
                Assert.That(order.IndexOf("groups"), Is.LessThan(order.IndexOf("cascade")));
                Assert.That(order.IndexOf("cascade"), Is.LessThan(order.IndexOf("attack")));
                Assert.That(order.IndexOf("attack"), Is.LessThan(order.IndexOf("heal")));
                Assert.That(order.IndexOf("heal"), Is.LessThan(order.IndexOf("countdown")));
                Assert.That(order.IndexOf("countdown"), Is.LessThan(order.IndexOf("count")));
                Assert.That(order.IndexOf("count"), Is.LessThan(order.IndexOf("complete")));
                Assert.That(order.IndexOf("complete"), Is.LessThan(order.IndexOf("stars")));
            }
        }

        [Test]
        public void Destroy_unsubscribes_from_pointer_source()
        {
            var fixture = PlayableBattleFixture.Create();
            Assert.That(fixture.Input.SubscriptionCount, Is.EqualTo(3));

            UnityEngine.Object.DestroyImmediate(fixture.Root);

            Assert.That(fixture.Input.SubscriptionCount, Is.Zero);
            fixture.Dispose();
        }
    }

    internal sealed class PlayableBattleFixture : IDisposable
    {
        private PlayableBattleFixture() { }

        internal GameObject Root { get; private set; }
        internal FakePointerSource Input { get; private set; }
        internal PlayableBattleController Controller { get; private set; }
        internal BoardView View { get; private set; }
        internal PartyState Party { get; private set; }
        internal StageSession Session { get; private set; }
        internal BattleContext Context { get; private set; }

        internal static PlayableBattleFixture Create(BoardState board = null, int enemyHp = 1000, int enemyCountdown = 2, bool damageParty = false)
        {
            var fixture = new PlayableBattleFixture();
            fixture.Root = new GameObject("PlayableBattleFixture");
            fixture.Input = new FakePointerSource();
            fixture.View = fixture.Root.AddComponent<BoardView>();
            fixture.Controller = fixture.Root.AddComponent<PlayableBattleController>();
            board = board ?? BoardPattern.Stable();
            fixture.Party = CreateParty();
            if (damageParty) fixture.Party.ApplyDamage(25);
            var enemyData = new EnemyData
            {
                Id = "enemy",
                Element = ElementType.Nature,
                BaseStats = new StatBlock { Hp = enemyHp, Attack = 1 },
                InitialCountdown = enemyCountdown,
                Actions = new[]
                {
                    new EnemyActionData
                    {
                        Id = "tap",
                        ResetCountdown = 2,
                        Effects = new[] { new EnemyEffectData { Type = EnemyEffectType.Damage, Payload = new EffectPayloadData { Amount = 1 } } }
                    }
                }
            };
            var stage = new StageData
            {
                Id = "stage",
                Waves = new[] { new WaveData { Id = "wave", EnemyIds = new[] { enemyData.Id } } },
                StarObjectives = new[]
                {
                    new StarObjectiveData { Type = StarObjectiveType.Clear },
                    new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = .5f },
                    new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = 3 }
                }
            };
            fixture.Session = new StageSession(stage, new[] { enemyData });
            var enemy = fixture.Session.CurrentEnemies[0];
            fixture.Context = new BattleContext(board, fixture.Party, enemy);
            fixture.View.Initialize(board);
            fixture.Controller.Initialize(fixture.Input, fixture.View, new Rect(0f, 0f, 600f, 500f), board,
                new CyclingOrbSource(), fixture.Party, enemy, fixture.Session, fixture.Context, stage);
            return fixture;
        }

        internal Vector2 CellCenter(int x, int y) { return new Vector2((x + .5f) * 100f, (y + .5f) * 100f); }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.DestroyImmediate(Root);
            Root = null;
        }

        private static PartyState CreateParty()
        {
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light, ElementType.Dark };
            var members = new CharacterRuntime[5];
            for (var index = 0; index < members.Length; index++)
                members[index] = new CharacterRuntime(new CharacterData
                {
                    Id = "hero-" + index,
                    Element = elements[index],
                    BaseRarity = 1,
                    BaseStats = new StatBlock { Hp = 20, Attack = 10, Recovery = 10 }
                });
            return new PartyState(members);
        }
    }

    internal static class BoardPattern
    {
        internal static BoardState Stable()
        {
            var board = new BoardState();
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                board.Set(x, y, (OrbType)((x + y * 2) % 6));
            return board;
        }

        internal static BoardState WithFireAndHeartMatches()
        {
            var board = Stable();
            for (var x = 0; x < 3; x++)
            {
                board.Set(x, 0, OrbType.Fire);
                board.Set(x, 1, OrbType.Heart);
            }
            return board;
        }
    }

    internal sealed class CyclingOrbSource : IOrbSource
    {
        private int index;
        public OrbType NextOrb() { return (OrbType)(index++ % 6); }
    }

    internal sealed class FakePointerSource : IBoardPointerSource
    {
        private Action<Vector2> pressed;
        private Action<Vector2> moved;
        private Action<Vector2> released;

        public event Action<Vector2> PointerPressed { add { pressed += value; } remove { pressed -= value; } }
        public event Action<Vector2> PointerMoved { add { moved += value; } remove { moved -= value; } }
        public event Action<Vector2> PointerReleased { add { released += value; } remove { released -= value; } }

        internal int SubscriptionCount
        {
            get
            {
                return Count(pressed) + Count(moved) + Count(released);
            }
        }

        internal void Press(Vector2 position) { if (pressed != null) pressed(position); }
        internal void Move(Vector2 position) { if (moved != null) moved(position); }
        internal void Release(Vector2 position) { if (released != null) released(position); }
        private static int Count(Delegate value) { return value == null ? 0 : value.GetInvocationList().Length; }
    }
}
