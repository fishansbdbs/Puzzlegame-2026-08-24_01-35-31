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

        [Test]
        public void Rectangles_with_nonfinite_derived_extents_are_rejected()
        {
            Assert.That(BoardLayout.ScreenToCell(new Vector2(float.MaxValue, .5f),
                new Rect(float.MaxValue, 0f, float.MaxValue, 1f)), Is.Null);
            Assert.That(BoardLayout.ScreenToCell(new Vector2(.5f, float.MaxValue),
                new Rect(0f, float.MaxValue, 1f, float.MaxValue)), Is.Null);
        }

        [Test]
        public void Layout_updates_validate_and_defer_until_the_active_drag_finishes()
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                var original = fixture.Controller.BoardScreenRect;
                var updated = new Rect(100f, 200f, 1200f, 1000f);

                Assert.That(() => fixture.Controller.UpdateBoardScreenRect(new Rect(0f, 0f, 0f, 1f)),
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(fixture.Controller.BoardScreenRect, Is.EqualTo(original));

                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Controller.UpdateBoardScreenRect(updated);

                Assert.That(fixture.Controller.BoardScreenRect, Is.EqualTo(original));
                fixture.Input.Release(fixture.CellCenter(0, 0));
                Assert.That(fixture.Controller.BoardScreenRect, Is.EqualTo(updated));
            }
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
                CollectionAssert.AreEqual(new[] { 10f, 1f, 0f }, timers);
            }
        }

        [Test]
        public void Throwing_timeout_zero_observer_cannot_prevent_committed_threshold_action_count_or_retry()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000,
                       enemyCountdown: 1, thresholdTriggers: new[] { Threshold("enrage", 75f, 2f, 2) }, actionDamage: 10))
            {
                fixture.Context.Enemy.ApplyDamage(300);
                var initialBoard = fixture.Context.Board;
                var terminalZeros = 0;
                fixture.Controller.TimerChanged += item =>
                {
                    if (item.RemainingSeconds != 0f) return;
                    terminalZeros++;
                    throw new InvalidOperationException("timer observer");
                };
                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Controller.AdvanceTime(9f);

                Assert.That(() => fixture.Controller.AdvanceTime(1f),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("timer observer"));
                Assert.That(terminalZeros, Is.EqualTo(1));
                Assert.That(fixture.Controller.CurrentBoard, Is.SameAs(fixture.Context.Board));
                Assert.That(fixture.Context.Board, Is.Not.SameAs(initialBoard));
                Assert.That(MatchDetector.FindGroups(fixture.Context.Board), Is.Empty);
                Assert.That(fixture.Context.Enemy.CurrentHp, Is.EqualTo(675));
                Assert.That(fixture.Context.Enemy.AttackMultiplier, Is.EqualTo(2f));
                Assert.That(fixture.Context.Enemy.EnrageTurns, Is.EqualTo(2));
                Assert.That(fixture.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(fixture.Party.CurrentHp, Is.EqualTo(80));
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Session.IsCompleted, Is.False);
                Assert.That(fixture.Controller.IsDragging, Is.False);
                Assert.That(fixture.Controller.IsResolving, Is.False);

                var committedBoard = fixture.Context.Board;
                Assert.That(() => fixture.Controller.AdvanceTime(20f), Throws.Nothing);

                Assert.That(terminalZeros, Is.EqualTo(1));
                Assert.That(fixture.Context.Board, Is.SameAs(committedBoard));
                Assert.That(fixture.Context.Enemy.CurrentHp, Is.EqualTo(675));
                Assert.That(fixture.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(fixture.Party.CurrentHp, Is.EqualTo(80));
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
            }
        }

        [Test]
        public void Throwing_timeout_zero_observer_cannot_prevent_stage_completion_or_duplicate_on_retry()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1))
            {
                var terminalZeros = 0;
                fixture.Controller.TimerChanged += item =>
                {
                    if (item.RemainingSeconds != 0f) return;
                    terminalZeros++;
                    throw new InvalidOperationException("timer observer");
                };
                fixture.Input.Press(fixture.CellCenter(5, 4));

                Assert.That(() => fixture.Controller.AdvanceTime(10f),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("timer observer"));
                Assert.That(terminalZeros, Is.EqualTo(1));
                Assert.That(fixture.Context.Enemy.IsDefeated, Is.True);
                Assert.That(fixture.Session.IsCompleted, Is.True);
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Controller.IsDragging, Is.False);
                Assert.That(fixture.Controller.IsResolving, Is.False);

                var committedBoard = fixture.Context.Board;
                Assert.That(() => fixture.Controller.AdvanceTime(20f), Throws.Nothing);

                Assert.That(terminalZeros, Is.EqualTo(1));
                Assert.That(fixture.Context.Board, Is.SameAs(committedBoard));
                Assert.That(fixture.Session.IsCompleted, Is.True);
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
            }
        }

        [Test]
        public void Zero_duration_throwing_zero_observer_commits_once_before_propagating()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000,
                       enemyCountdown: 1, thresholdTriggers: new[] { Threshold("enrage", 75f, 2f, 2) }, actionDamage: 10,
                       moveTimeSeconds: 0f))
            {
                fixture.Context.Enemy.ApplyDamage(300);
                var initialBoard = fixture.Context.Board;
                var terminalZeros = 0;
                fixture.Controller.TimerChanged += item =>
                {
                    if (item.RemainingSeconds != 0f) return;
                    terminalZeros++;
                    throw new InvalidOperationException("timer observer");
                };

                Assert.That(() => fixture.Input.Press(fixture.CellCenter(5, 4)),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("timer observer"));
                Assert.That(terminalZeros, Is.EqualTo(1));
                Assert.That(fixture.Controller.CurrentBoard, Is.SameAs(fixture.Context.Board));
                Assert.That(fixture.Context.Board, Is.Not.SameAs(initialBoard));
                Assert.That(MatchDetector.FindGroups(fixture.Context.Board), Is.Empty);
                Assert.That(fixture.Context.Enemy.CurrentHp, Is.EqualTo(675));
                Assert.That(fixture.Context.Enemy.AttackMultiplier, Is.EqualTo(2f));
                Assert.That(fixture.Context.Enemy.EnrageTurns, Is.EqualTo(2));
                Assert.That(fixture.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(fixture.Party.CurrentHp, Is.EqualTo(80));
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Session.IsCompleted, Is.False);
                Assert.That(fixture.Controller.IsDragging, Is.False);
                Assert.That(fixture.Controller.IsResolving, Is.False);

                var committedBoard = fixture.Context.Board;
                Assert.That(() => fixture.Controller.AdvanceTime(20f), Throws.Nothing);

                Assert.That(terminalZeros, Is.EqualTo(1));
                Assert.That(fixture.Context.Board, Is.SameAs(committedBoard));
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
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
                fixture.Input.Move(new Vector2(-1f, -1f));

                Assert.That(fixture.Context.Board.Get(0, 0), Is.EqualTo(first));
                Assert.That(fixture.Context.Board.Get(1, 0), Is.EqualTo(second));
                fixture.Input.Move(fixture.CellCenter(1, 0));

                Assert.That(fixture.Context.Board.Get(0, 0), Is.EqualTo(second));
                Assert.That(fixture.Context.Board.Get(1, 0), Is.EqualTo(first));
            }
        }

        [Test]
        public void Fast_horizontal_vertical_and_diagonal_sweeps_traverse_every_crossed_adjacent_cell()
        {
            AssertSweep(new BoardPosition(0, 0), new BoardPosition(5, 0), new[]
            {
                new BoardPosition(1, 0), new BoardPosition(2, 0), new BoardPosition(3, 0),
                new BoardPosition(4, 0), new BoardPosition(5, 0)
            });
            AssertSweep(new BoardPosition(0, 0), new BoardPosition(0, 4), new[]
            {
                new BoardPosition(0, 1), new BoardPosition(0, 2), new BoardPosition(0, 3), new BoardPosition(0, 4)
            });
            AssertSweep(new BoardPosition(0, 0), new BoardPosition(2, 2), new[]
            {
                new BoardPosition(1, 0), new BoardPosition(1, 1), new BoardPosition(2, 1), new BoardPosition(2, 2)
            });
            AssertSweep(new BoardPosition(5, 4), new BoardPosition(0, 4), new[]
            {
                new BoardPosition(4, 4), new BoardPosition(3, 4), new BoardPosition(2, 4),
                new BoardPosition(1, 4), new BoardPosition(0, 4)
            });
            AssertSweep(new BoardPosition(5, 4), new BoardPosition(5, 0), new[]
            {
                new BoardPosition(5, 3), new BoardPosition(5, 2), new BoardPosition(5, 1), new BoardPosition(5, 0)
            });
            AssertSweep(new BoardPosition(5, 4), new BoardPosition(3, 2), new[]
            {
                new BoardPosition(4, 4), new BoardPosition(4, 3), new BoardPosition(3, 3), new BoardPosition(3, 2)
            });
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
        public void Zero_timer_callback_cannot_start_or_release_a_reentrant_drag()
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                var attemptedReentry = false;
                fixture.Controller.TimerChanged += item =>
                {
                    if (item.RemainingSeconds != 0f || attemptedReentry) return;
                    attemptedReentry = true;
                    fixture.Input.Press(fixture.CellCenter(2, 0));
                    fixture.Input.Release(fixture.CellCenter(2, 0));
                };

                fixture.Input.Press(fixture.CellCenter(0, 0));
                fixture.Input.Release(fixture.CellCenter(1, 0));

                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Controller.IsDragging, Is.False);
            }
        }

        [Test]
        public void Throwing_zero_timer_observer_cannot_prevent_committed_board_threshold_action_and_count()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000,
                       enemyCountdown: 1, thresholdTriggers: new[] { Threshold("enrage", 75f, 2f, 2) }, actionDamage: 10))
            {
                fixture.Context.Enemy.ApplyDamage(300);
                var initialBoard = fixture.Context.Board;
                fixture.Controller.TimerChanged += item =>
                {
                    if (item.RemainingSeconds == 0f) throw new InvalidOperationException("timer observer");
                };
                fixture.Input.Press(fixture.CellCenter(5, 4));

                Assert.That(() => fixture.Input.Release(fixture.CellCenter(5, 4)),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("timer observer"));
                Assert.That(fixture.Controller.CurrentBoard, Is.SameAs(fixture.Context.Board));
                Assert.That(fixture.Context.Board, Is.Not.SameAs(initialBoard));
                Assert.That(MatchDetector.FindGroups(fixture.Context.Board), Is.Empty);
                Assert.That(fixture.Context.Enemy.CurrentHp, Is.EqualTo(675));
                Assert.That(fixture.Context.Enemy.AttackMultiplier, Is.EqualTo(2f));
                Assert.That(fixture.Context.Enemy.EnrageTurns, Is.EqualTo(2));
                Assert.That(fixture.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(fixture.Party.CurrentHp, Is.EqualTo(80));
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Session.IsCompleted, Is.False);
                Assert.That(fixture.Controller.IsDragging, Is.False);
                Assert.That(fixture.Controller.IsResolving, Is.False);
                Assert.That(fixture.Context.Enemy.EvaluateThresholds(), Is.Empty);

                var committedBoard = fixture.Context.Board;
                Assert.That(() => fixture.Input.Release(fixture.CellCenter(5, 4)), Throws.Nothing);
                fixture.Controller.AdvanceTime(20f);

                Assert.That(fixture.Context.Board, Is.SameAs(committedBoard));
                Assert.That(fixture.Context.Enemy.CurrentHp, Is.EqualTo(675));
                Assert.That(fixture.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(fixture.Party.CurrentHp, Is.EqualTo(80));
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
            }
        }

        [Test]
        public void Throwing_zero_timer_observer_cannot_prevent_committed_stage_completion_or_duplicate_on_retry()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1))
            {
                var completionEvents = 0;
                var starEvents = 0;
                fixture.Controller.TimerChanged += item =>
                {
                    if (item.RemainingSeconds == 0f) throw new InvalidOperationException("timer observer");
                };
                fixture.Controller.StageCompleted += delegate { completionEvents++; };
                fixture.Controller.StarResultsReady += delegate { starEvents++; };
                fixture.Input.Press(fixture.CellCenter(5, 4));

                Assert.That(() => fixture.Input.Release(fixture.CellCenter(5, 4)),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("timer observer"));
                Assert.That(fixture.Context.Enemy.IsDefeated, Is.True);
                Assert.That(fixture.Session.IsCompleted, Is.True);
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(fixture.Controller.IsDragging, Is.False);
                Assert.That(fixture.Controller.IsResolving, Is.False);
                Assert.That(completionEvents, Is.Zero);
                Assert.That(starEvents, Is.Zero);

                var committedBoard = fixture.Context.Board;
                Assert.That(() => fixture.Input.Release(fixture.CellCenter(5, 4)), Throws.Nothing);
                fixture.Controller.AdvanceTime(20f);

                Assert.That(fixture.Context.Board, Is.SameAs(committedBoard));
                Assert.That(fixture.Session.IsCompleted, Is.True);
                Assert.That(fixture.Session.BoardResolutionCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(completionEvents, Is.Zero);
                Assert.That(starEvents, Is.Zero);
            }
        }

        [Test]
        public void Multiwave_and_multienemy_sessions_are_rejected_before_controller_ownership()
        {
            var first = InitializerFixture.EnemyData("first");
            var second = InitializerFixture.EnemyData("second");
            var multiwave = InitializerFixture.Stage(new[]
            {
                new WaveData { Id = "wave-1", EnemyIds = new[] { first.Id } },
                new WaveData { Id = "wave-2", EnemyIds = new[] { second.Id } }
            });
            using (var fixture = InitializerFixture.Create(multiwave, new[] { first, second }))
            {
                Assert.That(() => fixture.Initialize(fixture.Input), Throws.TypeOf<ArgumentException>());
                Assert.That(fixture.Input.SubscriptionCount, Is.Zero);
                Assert.That(fixture.Session.CurrentWaveIndex, Is.Zero);
                Assert.That(fixture.Session.BoardResolutionCount, Is.Zero);
            }

            var multienemy = InitializerFixture.Stage(new[]
            {
                new WaveData { Id = "wave", EnemyIds = new[] { first.Id, second.Id } }
            });
            using (var fixture = InitializerFixture.Create(multienemy, new[] { first, second }))
            {
                Assert.That(() => fixture.Initialize(fixture.Input), Throws.TypeOf<ArgumentException>());
                Assert.That(fixture.Input.SubscriptionCount, Is.Zero);
                Assert.That(fixture.Session.BoardResolutionCount, Is.Zero);
            }
        }

        [Test]
        public void Multiwave_session_cannot_be_masked_by_a_separate_single_wave_stage_argument()
        {
            var first = InitializerFixture.EnemyData("first");
            var second = InitializerFixture.EnemyData("second");
            var sessionStage = InitializerFixture.Stage(new[]
            {
                new WaveData { Id = "wave-1", EnemyIds = new[] { first.Id } },
                new WaveData { Id = "wave-2", EnemyIds = new[] { second.Id } }
            });
            var adapterStage = InitializerFixture.Stage(new[]
            {
                new WaveData { Id = "wave-1", EnemyIds = new[] { first.Id } }
            });
            using (var fixture = InitializerFixture.Create(sessionStage, new[] { first, second }))
            {
                Assert.That(() => fixture.Initialize(fixture.Input, adapterStage), Throws.TypeOf<ArgumentException>());
                Assert.That(fixture.Input.SubscriptionCount, Is.Zero);
                Assert.That(fixture.Session.CurrentWaveIndex, Is.Zero);
                Assert.That(fixture.Session.BoardResolutionCount, Is.Zero);
            }
        }

        [Test]
        public void Failed_subscription_setup_rolls_back_and_initialization_can_be_retried()
        {
            var enemy = InitializerFixture.EnemyData("enemy");
            var stage = InitializerFixture.Stage(new[] { new WaveData { Id = "wave", EnemyIds = new[] { enemy.Id } } });
            using (var fixture = InitializerFixture.Create(stage, new[] { enemy }))
            {
                var failing = new ThrowingPointerSource();

                Assert.That(() => fixture.Initialize(failing), Throws.TypeOf<InvalidOperationException>());
                Assert.That(failing.SubscriptionCount, Is.Zero);
                Assert.That(() => fixture.Initialize(fixture.Input), Throws.Nothing);
                Assert.That(fixture.Input.SubscriptionCount, Is.EqualTo(3));
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
        public void Cascade_events_snapshot_each_transition_and_view_presents_layers_before_the_final_board()
        {
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000))
            {
                var layers = new List<CascadeLayerEvent>();
                var presentations = new List<BoardSnapshot>();
                fixture.Controller.CascadeLayerResolved += layers.Add;
                fixture.View.BoardDisplayed += presentations.Add;

                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Input.Release(fixture.CellCenter(5, 4));

                Assert.That(layers, Is.Not.Empty);
                Assert.That(presentations, Has.Count.EqualTo(layers.Count * 2 + 1));
                for (var index = 0; index < layers.Count; index++)
                {
                    Assert.That(presentations[index * 2], Is.SameAs(layers[index].PreClearBoard));
                    Assert.That(presentations[index * 2 + 1], Is.SameAs(layers[index].PostRefillBoard));
                    if (index + 1 < layers.Count)
                        AssertSnapshotsEqual(layers[index].PostRefillBoard, layers[index + 1].PreClearBoard);
                }
                AssertSnapshotEqualsBoard(presentations[presentations.Count - 1], fixture.Context.Board);

                var firstSnapshot = layers[0].PreClearBoard;
                var captured = firstSnapshot.Get(0, 0);
                fixture.Context.Board.Set(0, 0, captured == OrbType.Fire ? OrbType.Water : OrbType.Fire);
                Assert.That(firstSnapshot.Get(0, 0), Is.EqualTo(captured));
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
        public void Boss_mechanics_emit_in_authored_order_after_combat_and_before_countdown_and_action()
        {
            var thresholds = new[]
            {
                Threshold("first", 75f, 2f, 2),
                Threshold("second", 60f, 3f, 3)
            };
            using (var fixture = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000,
                       enemyCountdown: 1, thresholdTriggers: thresholds, actionDamage: 10))
            {
                fixture.Context.Enemy.ApplyDamage(390);
                var order = new List<string>();
                var mechanics = new List<EnemyEffectSnapshot>();
                fixture.Controller.AttackResolved += delegate { order.Add("attack"); };
                fixture.Controller.HealResolved += delegate { order.Add("heal"); };
                fixture.Controller.BossMechanicTriggered += item =>
                {
                    mechanics.Add(item);
                    order.Add("mechanic-" + item.Multiplier);
                };
                fixture.Controller.EnemyCountdownChanged += delegate { order.Add("countdown"); };
                fixture.Controller.EnemyActionResolved += delegate { order.Add("action"); };

                fixture.Input.Press(fixture.CellCenter(5, 4));
                fixture.Input.Release(fixture.CellCenter(5, 4));

                Assert.That(mechanics.Select(item => item.Multiplier), Is.EqualTo(new[] { 2f, 3f }));
                Assert.That(order.LastIndexOf("attack"), Is.LessThan(order.IndexOf("mechanic-2")));
                Assert.That(order.LastIndexOf("heal"), Is.LessThan(order.IndexOf("mechanic-2")));
                Assert.That(order.IndexOf("mechanic-2"), Is.LessThan(order.IndexOf("mechanic-3")));
                Assert.That(order.IndexOf("mechanic-3"), Is.LessThan(order.IndexOf("countdown")));
                Assert.That(order.IndexOf("countdown"), Is.LessThan(order.IndexOf("action")));
                Assert.That(fixture.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(fixture.Party.CurrentHp, Is.EqualTo(fixture.Party.MaxHp - 30),
                    "The stronger authored threshold must affect the due action in the same core turn.");
            }
        }

        [Test]
        public void Throwing_attack_or_boss_observer_cannot_prevent_already_applied_threshold_and_enemy_action_state()
        {
            using (var attackObserver = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000,
                       enemyCountdown: 1, thresholdTriggers: new[] { Threshold("enrage", 75f, 2f, 2) }, actionDamage: 10))
            {
                attackObserver.Context.Enemy.ApplyDamage(300);
                attackObserver.Controller.AttackResolved += delegate { throw new InvalidOperationException("attack observer"); };
                attackObserver.Input.Press(attackObserver.CellCenter(5, 4));

                Assert.That(() => attackObserver.Input.Release(attackObserver.CellCenter(5, 4)),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("attack observer"));
                Assert.That(attackObserver.Context.Enemy.AttackMultiplier, Is.EqualTo(2f));
                Assert.That(attackObserver.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(attackObserver.Party.CurrentHp, Is.EqualTo(attackObserver.Party.MaxHp - 20));
            }

            using (var bossObserver = PlayableBattleFixture.Create(BoardPattern.WithFireAndHeartMatches(), enemyHp: 1000,
                       enemyCountdown: 1, thresholdTriggers: new[] { Threshold("enrage", 75f, 2f, 2) }, actionDamage: 10))
            {
                bossObserver.Context.Enemy.ApplyDamage(300);
                bossObserver.Controller.BossMechanicTriggered += delegate { throw new InvalidOperationException("boss observer"); };
                bossObserver.Input.Press(bossObserver.CellCenter(5, 4));

                Assert.That(() => bossObserver.Input.Release(bossObserver.CellCenter(5, 4)),
                    Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("boss observer"));
                Assert.That(bossObserver.Context.Enemy.AttackMultiplier, Is.EqualTo(2f));
                Assert.That(bossObserver.Context.Enemy.Countdown, Is.EqualTo(2));
                Assert.That(bossObserver.Party.CurrentHp, Is.EqualTo(bossObserver.Party.MaxHp - 20));
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

        private static void AssertSweep(BoardPosition start, BoardPosition end, BoardPosition[] path)
        {
            using (var fixture = PlayableBattleFixture.Create())
            {
                var expected = fixture.Context.Board.Clone();
                var current = start;
                for (var index = 0; index < path.Length; index++)
                {
                    expected.Swap(current, path[index]);
                    current = path[index];
                }

                fixture.Input.Press(fixture.CellCenter(start.X, start.Y));
                fixture.Input.Move(fixture.CellCenter(end.X, end.Y));

                for (var y = 0; y < BoardState.Rows; y++)
                for (var x = 0; x < BoardState.Columns; x++)
                    Assert.That(fixture.Context.Board.Get(x, y), Is.EqualTo(expected.Get(x, y)),
                        "sweep " + start.X + "," + start.Y + " to " + end.X + "," + end.Y + " at " + x + "," + y);
            }
        }

        private static void AssertSnapshotsEqual(BoardSnapshot expected, BoardSnapshot actual)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                Assert.That(actual.Get(x, y), Is.EqualTo(expected.Get(x, y)), "snapshot " + x + "," + y);
        }

        private static void AssertSnapshotEqualsBoard(BoardSnapshot snapshot, BoardState board)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                Assert.That(snapshot.Get(x, y), Is.EqualTo(board.Get(x, y)), "final " + x + "," + y);
        }

        private static EnemyThresholdTriggerData Threshold(string id, float percent, float multiplier, int turns)
        {
            return new EnemyThresholdTriggerData
            {
                Id = id,
                HpThresholdPercent = percent,
                Effect = new EnemyEffectData
                {
                    Type = EnemyEffectType.Enrage,
                    Payload = new EffectPayloadData { Multiplier = multiplier, TurnCount = turns }
                }
            };
        }
    }

    internal sealed class InitializerFixture : IDisposable
    {
        private readonly BoardState board;
        private readonly PartyState party;
        private readonly EnemyRuntime enemy;
        private readonly BattleContext context;
        private readonly StageData stage;

        private InitializerFixture(StageData stageData, EnemyData[] enemyData)
        {
            Root = new GameObject("InitializerFixture");
            Controller = Root.AddComponent<PlayableBattleController>();
            View = Root.AddComponent<BoardView>();
            Input = new FakePointerSource();
            board = BoardPattern.Stable();
            party = PlayableBattleFixture.CreateParty();
            stage = stageData;
            Session = new StageSession(stage, enemyData);
            enemy = Session.CurrentEnemies[0];
            context = new BattleContext(board, party, enemy);
        }

        internal GameObject Root { get; private set; }
        internal PlayableBattleController Controller { get; private set; }
        internal BoardView View { get; private set; }
        internal FakePointerSource Input { get; private set; }
        internal StageSession Session { get; private set; }

        internal static InitializerFixture Create(StageData stage, EnemyData[] enemies)
        {
            return new InitializerFixture(stage, enemies);
        }

        internal void Initialize(IBoardPointerSource source)
        {
            Initialize(source, stage);
        }

        internal void Initialize(IBoardPointerSource source, StageData objectiveStage)
        {
            Controller.Initialize(source, View, new Rect(0f, 0f, 600f, 500f), board, new CyclingOrbSource(),
                party, enemy, Session, context, objectiveStage);
        }

        internal static EnemyData EnemyData(string id)
        {
            return new EnemyData
            {
                Id = id,
                Element = ElementType.Nature,
                BaseStats = new StatBlock { Hp = 100, Attack = 1 },
                InitialCountdown = 2,
                Actions = new[] { new EnemyActionData { Id = "wait", ResetCountdown = 2, Effects = new EnemyEffectData[0] } }
            };
        }

        internal static StageData Stage(WaveData[] waves)
        {
            return new StageData
            {
                Id = "stage",
                Waves = waves,
                StarObjectives = new[]
                {
                    new StarObjectiveData { Type = StarObjectiveType.Clear },
                    new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = .5f },
                    new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = 3 }
                }
            };
        }

        public void Dispose()
        {
            if (Root != null) UnityEngine.Object.DestroyImmediate(Root);
            Root = null;
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

        internal static PlayableBattleFixture Create(BoardState board = null, int enemyHp = 1000, int enemyCountdown = 2,
            bool damageParty = false, EnemyThresholdTriggerData[] thresholdTriggers = null, int actionDamage = 1,
            float moveTimeSeconds = BoardState.DefaultDragDurationSeconds)
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
                        Effects = new[] { new EnemyEffectData { Type = EnemyEffectType.Damage, Payload = new EffectPayloadData { Amount = actionDamage } } }
                    }
                },
                ThresholdTriggers = thresholdTriggers ?? Array.Empty<EnemyThresholdTriggerData>()
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
            fixture.Context = new BattleContext(board, fixture.Party, enemy, moveTimeSeconds);
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

        internal static PartyState CreateParty()
        {
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light, ElementType.Dark };
            var members = new CharacterRuntime[5];
            for (var index = 0; index < members.Length; index++)
            {
                var skill = new SkillData
                {
                    Id = "hero-" + index + "-active", ChargeElement = ElementType.Heart, ChargeRequired = int.MaxValue,
                    Effects = new[] { new SkillEffectData { Type = SkillEffectType.Heal, Payload = new EffectPayloadData { Amount = 0 } } }
                };
                var data = CharacterData.CreateForTests("hero-" + index, elements[index], 1);
                data.BaseStats = new StatBlock { Hp = 20, Attack = 10, Recovery = 10 };
                data.LevelCurve.StatsByLevel[0] = new StatBlock { Hp = 20, Attack = 10, Recovery = 10 };
                data.ActiveSkillId = skill.Id;
                members[index] = new CharacterRuntime(data, skill);
            }
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

    internal sealed class ThrowingPointerSource : IBoardPointerSource
    {
        private Action<Vector2> pressed;
        public event Action<Vector2> PointerPressed { add { pressed += value; } remove { pressed -= value; } }
        public event Action<Vector2> PointerMoved { add { throw new InvalidOperationException("setup failed"); } remove { } }
        public event Action<Vector2> PointerReleased { add { } remove { } }
        internal int SubscriptionCount { get { return pressed == null ? 0 : pressed.GetInvocationList().Length; } }
    }
}
