using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Sample;
using PuzzleGame.Core.Stages;
using PuzzleGame.Unity.Battle;
using PuzzleGame.Unity.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PuzzleGame.Tests.PlayMode.Integration
{
    public sealed class VerticalSliceSmokeTests
    {
        private readonly List<VerticalSliceBootstrap> created = new List<VerticalSliceBootstrap>();
        private readonly List<Scene> loadedScenes = new List<Scene>();
        private readonly List<GameObject> createdCameras = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            if (Camera.main == null) CreateCamera(new Rect(0f, 0f, 1600f, 900f), true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = created.Count - 1; index >= 0; index--)
            {
                var bootstrap = created[index];
                if (bootstrap != null) Object.DestroyImmediate(bootstrap.gameObject);
            }
            created.Clear();
            for (var index = loadedScenes.Count - 1; index >= 0; index--)
            {
                var scene = loadedScenes[index];
                if (!scene.IsValid() || !scene.isLoaded) continue;
                var unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null) while (!unload.isDone) yield return null;
            }
            loadedScenes.Clear();
            for (var index = createdCameras.Count - 1; index >= 0; index--)
                if (createdCameras[index] != null) Object.DestroyImmediate(createdCameras[index]);
            createdCameras.Clear();
        }

        [TestCase(1600f, 900f)]
        [TestCase(1200f, 900f)]
        [TestCase(2400f, 900f)]
        public void Orthographic_camera_projects_all_rendered_centers_back_to_their_cells_and_rejects_margins(
            float width, float height)
        {
            var camera = CreateCamera(new Rect(0f, 0f, width, height), false);
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026, camera));
            var bounds = bootstrap.View.WorldBounds;

            Assert.That(bounds.size.x, Is.EqualTo(6f).Within(.0001f));
            Assert.That(bounds.size.y, Is.EqualTo(5f).Within(.0001f));
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
            {
                var position = new BoardPosition(x, y);
                var world = bootstrap.View.GetCellWorldCenter(position);
                var projected = camera.WorldToScreenPoint(world);
                Assert.That(BoardLayout.ScreenToCell(new Vector2(projected.x, projected.y), bootstrap.BoardScreenRect),
                    Is.EqualTo(position), "cell " + x + "," + y);
            }

            var leftMargin = new Vector2(bootstrap.BoardScreenRect.xMin - 1f, bootstrap.BoardScreenRect.center.y);
            var rightMargin = new Vector2(bootstrap.BoardScreenRect.xMax + 1f, bootstrap.BoardScreenRect.center.y);
            Assert.That(leftMargin.x, Is.GreaterThan(camera.pixelRect.xMin));
            Assert.That(rightMargin.x, Is.LessThan(camera.pixelRect.xMax));
            Assert.That(BoardLayout.ScreenToCell(leftMargin, bootstrap.BoardScreenRect), Is.Null);
            Assert.That(BoardLayout.ScreenToCell(rightMargin, bootstrap.BoardScreenRect), Is.Null);
        }

        [UnityTest]
        public IEnumerator Projected_press_and_drag_swap_the_intended_rendered_cells()
        {
            var camera = CreateCamera(new Rect(20f, 30f, 1280f, 720f), false);
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026, camera));
            var first = bootstrap.Controller.CurrentBoard.Get(0, 0);
            var second = bootstrap.Controller.CurrentBoard.Get(1, 0);

            bootstrap.PressCell(new BoardPosition(0, 0));
            bootstrap.MoveToCell(new BoardPosition(1, 0));

            Assert.That(bootstrap.Controller.CurrentBoard.Get(0, 0), Is.EqualTo(second));
            Assert.That(bootstrap.Controller.CurrentBoard.Get(1, 0), Is.EqualTo(first));
            bootstrap.ReleaseCell(new BoardPosition(1, 0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Viewport_and_projection_changes_refresh_mapping_without_stale_centers()
        {
            var camera = CreateCamera(new Rect(0f, 0f, 1600f, 900f), false);
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026, camera));
            var firstRect = bootstrap.BoardScreenRect;

            camera.pixelRect = new Rect(100f, 50f, 800f, 600f);
            yield return null;

            var viewportRect = bootstrap.BoardScreenRect;
            Assert.That(viewportRect, Is.Not.EqualTo(firstRect));
            AssertEveryProjectedCenterMaps(camera, bootstrap);

            camera.orthographicSize = 5f;
            yield return null;

            var projectionRect = bootstrap.BoardScreenRect;
            Assert.That(projectionRect, Is.Not.EqualTo(viewportRect));
            AssertEveryProjectedCenterMaps(camera, bootstrap);

            bootstrap.View.transform.position += Vector3.right;
            yield return null;

            Assert.That(bootstrap.BoardScreenRect, Is.Not.EqualTo(projectionRect));
            AssertEveryProjectedCenterMaps(camera, bootstrap);
        }

        [UnityTest]
        public IEnumerator Bootstrap_owns_only_its_explicit_test_fallback_camera()
        {
            var cameraCount = Resources.FindObjectsOfTypeAll<Camera>().Length;
            var fallbackBootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var fallbackCamera = fallbackBootstrap.GameplayCamera;
            Assert.That(fallbackCamera, Is.Not.Null);

            created.Remove(fallbackBootstrap);
            Object.Destroy(fallbackBootstrap.gameObject);
            yield return null;

            Assert.That(fallbackCamera == null, Is.True);
            Assert.That(Resources.FindObjectsOfTypeAll<Camera>(), Has.Length.EqualTo(cameraCount));

            var externalCamera = CreateCamera(new Rect(0f, 0f, 1024f, 768f), false);
            var providedBootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026, externalCamera));
            created.Remove(providedBootstrap);
            Object.Destroy(providedBootstrap.gameObject);
            yield return null;

            Assert.That(externalCamera, Is.Not.Null);
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
                Assert.That(bootstrap.Controller.CurrentBoard, Is.SameAs(bootstrap.Sample.CurrentBoard));
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
            Assert.That(bootstrap.Controller.CurrentBoard, Is.SameAs(bootstrap.Sample.CurrentBoard));
            Assert.That(bootstrap.Sample.CurrentBoard, Is.SameAs(bootstrap.Sample.BattleContext.Board));
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
            enemy.ApplyDamage(enemy.CurrentHp - (enemy.MaxHp / 2 + 1));

            bootstrap.PressCell(new BoardPosition(5, 4));
            bootstrap.ReleaseCell(new BoardPosition(5, 4));
            Assert.That(bossEvents, Has.Count.EqualTo(1));

            enemy.ApplyDamage(enemy.CurrentHp - 1);
            for (var x = 0; x < BoardState.MinimumMatchSize; x++)
                bootstrap.Sample.CurrentBoard.Set(x, 0, OrbType.Fire);

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
        public IEnumerator Throwing_bootstrap_attack_or_boss_observer_cannot_prevent_core_enrage_and_due_action()
        {
            var attackObserver = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var attackEnemy = attackObserver.Sample.Enemy;
            attackEnemy.ApplyDamage(attackEnemy.CurrentHp - attackEnemy.MaxHp / 2);
            attackObserver.AttackResolved += delegate { throw new System.InvalidOperationException("attack observer"); };
            attackObserver.PressCell(new BoardPosition(5, 4));

            Assert.That(() => attackObserver.ReleaseCell(new BoardPosition(5, 4)),
                Throws.TypeOf<System.InvalidOperationException>().With.Message.EqualTo("attack observer"));
            Assert.That(attackEnemy.AttackMultiplier, Is.EqualTo(1.75f));
            Assert.That(attackEnemy.Countdown, Is.EqualTo(2));

            Object.DestroyImmediate(attackObserver.gameObject);
            created.Remove(attackObserver);

            var bossObserver = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var bossEnemy = bossObserver.Sample.Enemy;
            bossEnemy.ApplyDamage(bossEnemy.CurrentHp - bossEnemy.MaxHp / 2);
            bossObserver.BossMechanicTriggered += delegate { throw new System.InvalidOperationException("boss observer"); };
            bossObserver.PressCell(new BoardPosition(5, 4));

            Assert.That(() => bossObserver.ReleaseCell(new BoardPosition(5, 4)),
                Throws.TypeOf<System.InvalidOperationException>().With.Message.EqualTo("boss observer"));
            Assert.That(bossEnemy.AttackMultiplier, Is.EqualTo(1.75f));
            Assert.That(bossEnemy.Countdown, Is.EqualTo(2));
            yield return null;
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
            Assert.That(first.GameplayCamera, Is.SameAs(Camera.main));
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

        [UnityTest]
        public IEnumerator Runtime_composition_requires_an_active_main_camera_and_cleans_failed_root()
        {
            var mainCamera = Camera.main;
            Assert.That(mainCamera, Is.Not.Null);
            mainCamera.gameObject.SetActive(false);

            try
            {
                Assert.That(() => VerticalSliceBootstrap.EnsureRuntimeBootstrap(),
                    Throws.TypeOf<System.InvalidOperationException>());
            }
            finally
            {
                mainCamera.gameObject.SetActive(true);
            }

            yield return null;
            Assert.That(FindBootstraps(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator Destroying_only_the_bootstrap_component_releases_its_owned_graph_and_pointer_subscriptions()
        {
            var beforeMaterials = CountRuntimeBoardMaterials();
            var beforeViews = CountRuntimeBoardViews();
            var beforeOwnedRoots = CountOwnedRuntimeRoots();
            var bootstrap = Track(VerticalSliceBootstrap.CreateForTests(24082026));
            var outerRoot = bootstrap.gameObject;
            var driver = bootstrap.PointerDriver;
            Assert.That(driver.SubscriptionCount, Is.EqualTo(3));

            created.Remove(bootstrap);
            Object.Destroy(bootstrap);
            yield return null;
            yield return null;

            Assert.That(outerRoot, Is.Not.Null, "Destroying the component does not transfer ownership of its caller-visible outer GameObject.");
            Assert.That(driver.SubscriptionCount, Is.Zero);
            Assert.That(CountRuntimeBoardMaterials(), Is.EqualTo(beforeMaterials));
            Assert.That(CountRuntimeBoardViews(), Is.EqualTo(beforeViews));
            Assert.That(CountOwnedRuntimeRoots(), Is.EqualTo(beforeOwnedRoots));

            Object.Destroy(outerRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Both_outer_root_factories_are_failure_atomic_when_sample_composition_throws()
        {
            var beforeBootstraps = FindBootstraps().Length;
            var beforeMaterials = CountRuntimeBoardMaterials();
            var beforeViews = CountRuntimeBoardViews();
            var beforeOwnedRoots = CountOwnedRuntimeRoots();
            var beforeOuterRoots = CountOuterRuntimeRoots();
            var calls = 0;
            System.Func<int, VerticalSliceSample> failingFactory = seed =>
            {
                calls++;
                throw new System.InvalidOperationException("forced sample failure " + seed);
            };

            var explicitFailure = CaptureFailure(() => VerticalSliceBootstrap.CreateForTests(24082026, failingFactory));
            Assert.That(explicitFailure.Message, Is.EqualTo("forced sample failure 24082026"));
            yield return null;
            AssertRuntimeCounts(beforeBootstraps, beforeMaterials, beforeViews, beforeOwnedRoots, beforeOuterRoots);

            var runtimeFailure = CaptureFailure(() => VerticalSliceBootstrap.EnsureRuntimeBootstrap(failingFactory));
            Assert.That(runtimeFailure.Message, Is.EqualTo("forced sample failure 24082026"));
            yield return null;
            Assert.That(calls, Is.EqualTo(2));
            AssertRuntimeCounts(beforeBootstraps, beforeMaterials, beforeViews, beforeOwnedRoots, beforeOuterRoots);
        }

        [UnityTest]
        public IEnumerator Command_line_test_runs_suppress_auto_creation_on_scene_load_and_explicit_runtime_is_idempotent_on_reload()
        {
            Assert.That(FindBootstraps(), Is.Empty);
            yield return LoadSampleSceneAdditively();
            yield return null;
            Assert.That(FindBootstraps(), Is.Empty, "The command-line test process must not auto-compose gameplay while loading scenes.");
            yield return UnloadTrackedScenes();

            var explicitRuntime = Track(VerticalSliceBootstrap.EnsureRuntimeBootstrap());
            yield return LoadSampleSceneAdditively();
            yield return null;

            Assert.That(VerticalSliceBootstrap.EnsureRuntimeBootstrap(), Is.SameAs(explicitRuntime));
            Assert.That(FindBootstraps(), Has.Length.EqualTo(1));
        }

        [Test]
        public void Flagless_init_test_scene_suppression_persists_after_sample_scene_replaces_it_until_reset()
        {
            var startup = new AutomaticBootstrapStartup();
            var registrations = 0;
            var bootstraps = 0;

            var suppress = AutomaticBootstrapStartup.ShouldSuppress(new string[0], "InitTestScene-flagless");
            Assert.That(suppress, Is.True);
            Assert.That(AutomaticBootstrapStartup.ShouldSuppress(new[] { "-runTests" }, "SampleScene"), Is.True);
            Assert.That(AutomaticBootstrapStartup.ShouldSuppress(new[] { "-testPlatform" }, "SampleScene"), Is.True);
            startup.Begin(suppress, () => bootstraps++, () => registrations++);

            Assert.That(startup.IsSuppressed, Is.True);
            Assert.That(startup.CallbackRegistered, Is.False);
            Assert.That(registrations, Is.Zero);
            Assert.That(bootstraps, Is.Zero);

            startup.Begin(AutomaticBootstrapStartup.ShouldSuppress(new string[0], "SampleScene"),
                () => bootstraps++, () => registrations++);
            startup.SceneLoaded(() => bootstraps++);

            Assert.That(startup.IsSuppressed, Is.True);
            Assert.That(startup.CallbackRegistered, Is.False);
            Assert.That(registrations, Is.Zero);
            Assert.That(bootstraps, Is.Zero);

            startup.Reset();
            startup.Begin(AutomaticBootstrapStartup.ShouldSuppress(new string[0], "SampleScene"),
                () => bootstraps++, () => registrations++);

            Assert.That(startup.IsSuppressed, Is.False);
            Assert.That(startup.CallbackRegistered, Is.True);
            Assert.That(registrations, Is.EqualTo(1));
            Assert.That(bootstraps, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Normal_flagless_startup_registers_once_and_keeps_runtime_ensure_idempotent()
        {
            var startup = new AutomaticBootstrapStartup();
            var registrations = 0;
            System.Action sceneLoaded = null;
            VerticalSliceBootstrap initial = null;
            VerticalSliceBootstrap afterSceneLoad = null;

            var suppressed = AutomaticBootstrapStartup.ShouldSuppress(new string[0], "SampleScene");
            startup.Begin(suppressed,
                () => initial = Track(VerticalSliceBootstrap.EnsureRuntimeBootstrap()),
                () =>
                {
                    registrations++;
                    sceneLoaded = () => startup.SceneLoaded(
                        () => afterSceneLoad = VerticalSliceBootstrap.EnsureRuntimeBootstrap());
                });
            startup.Begin(suppressed, () => Assert.Fail("Startup was applied twice."),
                () => Assert.Fail("The scene callback was registered twice."));

            Assert.That(startup.IsSuppressed, Is.False);
            Assert.That(startup.CallbackRegistered, Is.True);
            Assert.That(registrations, Is.EqualTo(1));
            Assert.That(initial, Is.Not.Null);
            Assert.That(sceneLoaded, Is.Not.Null);

            sceneLoaded();
            yield return null;

            Assert.That(afterSceneLoad, Is.SameAs(initial));
            Assert.That(FindBootstraps(), Has.Length.EqualTo(1));
        }

        private VerticalSliceBootstrap Track(VerticalSliceBootstrap bootstrap)
        {
            created.Add(bootstrap);
            return bootstrap;
        }

        private Camera CreateCamera(Rect pixelRect, bool main)
        {
            var root = new GameObject(main ? "Main Camera" : "Mapped Board Camera");
            createdCameras.Add(root);
            if (main) root.tag = "MainCamera";
            var camera = root.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            camera.pixelRect = pixelRect;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void AssertEveryProjectedCenterMaps(Camera camera, VerticalSliceBootstrap bootstrap)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
            {
                var position = new BoardPosition(x, y);
                var projected = camera.WorldToScreenPoint(bootstrap.View.GetCellWorldCenter(position));
                Assert.That(BoardLayout.ScreenToCell(new Vector2(projected.x, projected.y), bootstrap.BoardScreenRect),
                    Is.EqualTo(position), "cell " + x + "," + y);
            }
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

        private static int CountOuterRuntimeRoots()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>().Count(item => item != null &&
                item.name.StartsWith("PuzzleGame Vertical Slice", System.StringComparison.Ordinal));
        }

        private IEnumerator LoadSampleSceneAdditively()
        {
            var load = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
            Assert.That(load, Is.Not.Null);
            while (!load.isDone) yield return null;
            var scene = SceneManager.GetSceneByName("SampleScene");
            Assert.That(scene.IsValid(), Is.True);
            loadedScenes.Add(scene);
        }

        private IEnumerator UnloadTrackedScenes()
        {
            for (var index = loadedScenes.Count - 1; index >= 0; index--)
            {
                var scene = loadedScenes[index];
                if (!scene.IsValid() || !scene.isLoaded) continue;
                var unload = SceneManager.UnloadSceneAsync(scene);
                if (unload != null) while (!unload.isDone) yield return null;
            }
            loadedScenes.Clear();
        }

        private static System.InvalidOperationException CaptureFailure(TestDelegate action)
        {
            var failure = Assert.Throws<System.InvalidOperationException>(action);
            Assert.That(failure, Is.Not.Null);
            return failure;
        }

        private static void AssertRuntimeCounts(int bootstraps, int materials, int views, int ownedRoots, int outerRoots)
        {
            Assert.That(FindBootstraps(), Has.Length.EqualTo(bootstraps));
            Assert.That(CountRuntimeBoardMaterials(), Is.EqualTo(materials));
            Assert.That(CountRuntimeBoardViews(), Is.EqualTo(views));
            Assert.That(CountOwnedRuntimeRoots(), Is.EqualTo(ownedRoots));
            Assert.That(CountOuterRuntimeRoots(), Is.EqualTo(outerRoots));
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
