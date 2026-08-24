using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Stages;
using PuzzleGame.Unity.Bootstrap;
using UnityEngine;
using UnityEngine.TestTools;

namespace PuzzleGame.Tests.PlayMode.Integration
{
    public sealed class VerticalSliceSmokeTests
    {
        private readonly List<VerticalSliceBootstrap> created = new List<VerticalSliceBootstrap>();

        [TearDown]
        public void TearDown()
        {
            for (var index = created.Count - 1; index >= 0; index--)
            {
                var bootstrap = created[index];
                if (bootstrap != null) Object.DestroyImmediate(bootstrap.gameObject);
            }
            created.Clear();
        }

        [UnityTest]
        public IEnumerator Bootstrap_code_composes_a_playable_six_by_five_board_with_a_ten_second_move()
        {
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            yield return null;

            {
                Assert.That(bootstrap.Sample, Is.Not.Null);
                Assert.That(bootstrap.Controller, Is.Not.Null);
                Assert.That(bootstrap.View, Is.Not.Null);
                Assert.That(bootstrap.PointerDriver, Is.Not.Null);
                Assert.That(bootstrap.PointerInput, Is.Null);
                Assert.That(bootstrap.View.CellCount, Is.EqualTo(30));
                Assert.That(bootstrap.MoveTimeLimitSeconds, Is.EqualTo(10f));
                Assert.That(bootstrap.Controller.CurrentBoard, Is.SameAs(bootstrap.Sample.Board));
                Assert.That(bootstrap.PointerDriver.SubscriptionCount, Is.EqualTo(3));
            }
        }

        [UnityTest]
        public IEnumerator Early_release_resolves_two_cascade_layers_once_and_emits_separate_attacks_healing_and_countdown()
        {
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var cascades = new List<PuzzleGame.Unity.Battle.CascadeLayerEvent>();
            var displayed = new List<BoardSnapshot>();
            var attacks = new List<AttackEvent>();
            var heals = new List<HealEvent>();
            var actions = new List<EnemyActionSnapshot>();
            var countdowns = new List<int>();
            bootstrap.CascadeLayerResolved += cascades.Add;
            bootstrap.View.BoardDisplayed += displayed.Add;
            bootstrap.AttackResolved += attacks.Add;
            bootstrap.HealResolved += heals.Add;
            bootstrap.EnemyActionResolved += actions.Add;
            bootstrap.EnemyCountdownChanged += item => countdowns.Add(item.Countdown);

            bootstrap.PressCell(new BoardPosition(5, 4));
            bootstrap.ReleaseCell(new BoardPosition(5, 4));
            bootstrap.ReleaseCell(new BoardPosition(5, 4));
            bootstrap.AdvanceUnscaledTime(20f);
            yield return null;

            var fireAttacks = attacks.Where(item => item.Element == ElementType.Fire).ToArray();
            {
                Assert.That(bootstrap.Controller.CompletedBoardResolutions, Is.EqualTo(1));
                Assert.That(cascades, Has.Count.EqualTo(2));
                Assert.That(fireAttacks, Has.Length.EqualTo(2));
                Assert.That(fireAttacks.Select(item => item.GroupId).Distinct().Count(), Is.EqualTo(2));
                Assert.That(heals.Sum(item => item.Healing), Is.GreaterThan(0));
                Assert.That(actions.Select(item => item.Id), Is.EqualTo(new[] { "boss-strike" }));
                Assert.That(countdowns.Last(), Is.EqualTo(2));
                Assert.That(displayed, Has.Count.EqualTo(cascades.Count * 2 + 1));
            }
            AssertSnapshotEqualsBoard(displayed.Last(), bootstrap.Controller.CurrentBoard);
        }

        [UnityTest]
        public IEnumerator Deterministic_pointer_seam_traverses_every_cell_in_a_fast_crossed_row()
        {
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var expected = bootstrap.Controller.CurrentBoard.Clone();
            var current = new BoardPosition(0, 0);
            for (var x = 1; x < BoardState.Columns; x++)
            {
                var next = new BoardPosition(x, 0);
                expected.Swap(current, next);
                current = next;
            }

            bootstrap.PressCell(new BoardPosition(0, 0));
            bootstrap.MoveToCell(new BoardPosition(5, 0));

            AssertBoardsEqual(expected, bootstrap.Controller.CurrentBoard);
            bootstrap.ReleaseCell(new BoardPosition(5, 0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Exact_unscaled_timeout_resolves_once()
        {
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            bootstrap.PressCell(new BoardPosition(5, 4));
            bootstrap.AdvanceUnscaledTime(9f);
            Assert.That(bootstrap.Controller.CompletedBoardResolutions, Is.Zero);

            bootstrap.AdvanceUnscaledTime(1f);
            bootstrap.AdvanceUnscaledTime(10f);
            yield return null;

            Assert.That(bootstrap.Controller.CompletedBoardResolutions, Is.EqualTo(1));
            Assert.That(bootstrap.Controller.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator Boss_threshold_then_final_resolution_completes_the_stage_and_awards_three_stars()
        {
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var bossEvents = new List<EnemyEffectSnapshot>();
            var completions = new List<PuzzleGame.Unity.Battle.StageCompletionEvent>();
            IReadOnlyList<StarResult> stars = null;
            bootstrap.BossMechanicTriggered += bossEvents.Add;
            bootstrap.StageCompleted += completions.Add;
            bootstrap.StarResultsReady += value => stars = value;

            var enemy = bootstrap.Sample.Enemy;
            enemy.ApplyDamage(enemy.CurrentHp - enemy.MaxHp / 2);
            Assert.That(bootstrap.EvaluateBossMechanics(), Has.Count.EqualTo(1));
            Assert.That(bootstrap.EvaluateBossMechanics(), Is.Empty);
            enemy.ApplyDamage(enemy.CurrentHp - 1);

            bootstrap.PressCell(new BoardPosition(5, 4));
            bootstrap.ReleaseCell(new BoardPosition(5, 4));
            yield return null;

            {
                Assert.That(bossEvents, Has.Count.EqualTo(1));
                Assert.That(bossEvents[0].Type, Is.EqualTo(EnemyEffectType.Enrage));
                Assert.That(completions, Has.Count.EqualTo(1));
                Assert.That(bootstrap.Sample.StageSession.IsCompleted, Is.True);
                Assert.That(stars, Has.Count.EqualTo(3));
                Assert.That(stars.All(item => item.Earned), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator Runtime_ensure_is_idempotent_and_destruction_releases_events_and_render_resources()
        {
            var beforeMaterials = CountRuntimeBoardMaterials();
            var beforeViews = CountRuntimeBoardViews();
            var beforeOwnedRoots = CountOwnedRuntimeRoots();
            var first = Track(VerticalSliceBootstrap.EnsureRuntimeBootstrap());
            var second = VerticalSliceBootstrap.EnsureRuntimeBootstrap();
            Assert.That(second, Is.SameAs(first));
            var driver = first.PointerDriver;
            Assert.That(driver, Is.Null, "Runtime composition owns the real Input System adapter.");
            Assert.That(first.PointerInput, Is.Not.Null);
            Assert.That(FindBootstraps(), Has.Length.EqualTo(1));

            created.Remove(first);
            Object.Destroy(first.gameObject);
            yield return null;

            Assert.That(FindBootstraps(), Is.Empty);
            Assert.That(CountRuntimeBoardMaterials(), Is.EqualTo(beforeMaterials));
            Assert.That(CountRuntimeBoardViews(), Is.EqualTo(beforeViews));
            Assert.That(CountOwnedRuntimeRoots(), Is.EqualTo(beforeOwnedRoots));

            var testBootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var deterministicDriver = testBootstrap.PointerDriver;
            Assert.That(deterministicDriver.SubscriptionCount, Is.EqualTo(3));

            created.Remove(testBootstrap);
            Object.Destroy(testBootstrap.gameObject);
            yield return null;

            Assert.That(deterministicDriver.SubscriptionCount, Is.Zero);
            Assert.That(() => deterministicDriver.Press(Vector2.zero), Throws.TypeOf<System.ObjectDisposedException>());
            Assert.That(FindBootstraps(), Is.Empty);
            Assert.That(CountRuntimeBoardMaterials(), Is.EqualTo(beforeMaterials));
            Assert.That(CountRuntimeBoardViews(), Is.EqualTo(beforeViews));
            Assert.That(CountOwnedRuntimeRoots(), Is.EqualTo(beforeOwnedRoots));
        }

        private VerticalSliceBootstrap Track(VerticalSliceBootstrap bootstrap)
        {
            created.Add(bootstrap);
            return bootstrap;
        }

        private static VerticalSliceBootstrap[] FindBootstraps()
        {
            return Resources.FindObjectsOfTypeAll<VerticalSliceBootstrap>();
        }

        private static int CountRuntimeBoardMaterials()
        {
            return Resources.FindObjectsOfTypeAll<Material>().Count(item => item != null && item.name == "Runtime Board Orb Material");
        }

        private static int CountRuntimeBoardViews()
        {
            return Resources.FindObjectsOfTypeAll<PuzzleGame.Unity.Battle.BoardView>().Length;
        }

        private static int CountOwnedRuntimeRoots()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>().Count(item => item != null && item.name == "Owned Core Runtime");
        }

        private static void AssertBoardsEqual(BoardState expected, BoardState actual)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                Assert.That(actual.Get(x, y), Is.EqualTo(expected.Get(x, y)), "cell " + x + "," + y);
        }

        private static void AssertSnapshotEqualsBoard(BoardSnapshot snapshot, BoardState board)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                Assert.That(snapshot.Get(x, y), Is.EqualTo(board.Get(x, y)), "cell " + x + "," + y);
        }
    }
}
