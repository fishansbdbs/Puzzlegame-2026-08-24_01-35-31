using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Gacha;
using PuzzleGame.Core.Sample;
using PuzzleGame.Core.Stages;
using PuzzleGame.Unity.Battle;
using PuzzleGame.Unity.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzleGame.Unity.Bootstrap
{
    /// <summary>
    /// Code-composes the deterministic core sample. The enabled SampleScene requires no serialized wiring.
    /// Explicit test construction substitutes only the pointer source and keeps the real controller rules.
    /// </summary>
    public sealed class VerticalSliceBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "PuzzleGame Vertical Slice";
        private static readonly AutomaticBootstrapStartup automaticStartup = new AutomaticBootstrapStartup();
        private GameObject ownedRuntimeRoot;
        private bool initialized;
        private bool forwardingEvents;
        private Func<int, VerticalSliceSample> sampleFactory;
        private Camera injectedGameplayCamera;
        private bool hasInjectedCameraPolicy;
        private Camera[] cameraBuffer = new Camera[4];

        public event Action<MoveTimerEvent> TimerChanged;
        public event Action<MatchGroupsEvent> MatchGroupsResolved;
        public event Action<CascadeLayerEvent> CascadeLayerResolved;
        public event Action<AttackEvent> AttackResolved;
        public event Action<HealEvent> HealResolved;
        public event Action<EnemyCountdownEvent> EnemyCountdownChanged;
        public event Action<EnemyActionSnapshot> EnemyActionResolved;
        public event Action<int> BoardResolutionCountChanged;
        public event Action<StageCompletionEvent> StageCompleted;
        public event Action<IReadOnlyList<StarResult>> StarResultsReady;
        public event Action<EnemyEffectSnapshot> BossMechanicTriggered;
        public event Action<PackSummonFlowEvent> PackFlowTransitioned;

        public VerticalSliceSample Sample { get; private set; }
        public PlayableBattleController Controller { get; private set; }
        public BoardView View { get; private set; }
        public BoardPointerInput PointerInput { get; private set; }
        public VerticalSlicePointerDriver PointerDriver { get; private set; }
        public Camera GameplayCamera { get; private set; }
        public Rect BoardScreenRect { get { return Controller == null ? default(Rect) : Controller.BoardScreenRect; } }
        public float MoveTimeLimitSeconds { get { return Sample == null ? 0f : Sample.BattleContext.MoveTimeSeconds; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeHooks()
        {
            SceneManager.sceneLoaded -= OnRuntimeSceneLoaded;
            automaticStartup.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachAfterSceneLoad()
        {
            if (!Application.isPlaying) return;
            var suppress = AutomaticBootstrapStartup.ShouldSuppress(
                Environment.GetCommandLineArgs(), SceneManager.GetActiveScene().name);
            automaticStartup.Begin(suppress, EnsureAutomaticRuntimeBootstrap, RegisterRuntimeSceneCallback);
        }

        private static void OnRuntimeSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            automaticStartup.SceneLoaded(EnsureAutomaticRuntimeBootstrap);
        }

        private static void RegisterRuntimeSceneCallback()
        {
            SceneManager.sceneLoaded -= OnRuntimeSceneLoaded;
            SceneManager.sceneLoaded += OnRuntimeSceneLoaded;
        }

        private static void EnsureAutomaticRuntimeBootstrap()
        {
            EnsureRuntimeBootstrap();
        }

        /// <summary>Creates an explicitly driven instance for PlayMode automation.</summary>
        public static VerticalSliceBootstrap CreateForTests(int seed = VerticalSliceFactory.DefaultSeed)
        {
            return CreateForTests(seed, null, VerticalSliceFactory.Create);
        }

        public static VerticalSliceBootstrap CreateForTests(int seed, Camera gameplayCamera)
        {
            return CreateForTests(seed, gameplayCamera, VerticalSliceFactory.Create);
        }

        internal static VerticalSliceBootstrap CreateForTests(
            int seed,
            Func<int, VerticalSliceSample> sampleFactory)
        {
            return CreateForTests(seed, null, sampleFactory);
        }

        internal static VerticalSliceBootstrap CreateForTests(
            int seed,
            Camera gameplayCamera,
            Func<int, VerticalSliceSample> sampleFactory)
        {
            if (sampleFactory == null) throw new ArgumentNullException(nameof(sampleFactory));
            var root = new GameObject(RuntimeRootName + " (Test)");
            try
            {
                root.hideFlags = HideFlags.DontSave;
                var bootstrap = root.AddComponent<VerticalSliceBootstrap>();
                bootstrap.sampleFactory = sampleFactory;
                bootstrap.Compose(seed, true, gameplayCamera);
                return bootstrap;
            }
            catch
            {
                DestroyOwned(root);
                throw;
            }
        }

        /// <summary>
        /// Returns the existing bootstrap or creates the one persistent runtime-owned instance.
        /// Calling this method is an explicit request even while a test runner is active.
        /// </summary>
        public static VerticalSliceBootstrap EnsureRuntimeBootstrap()
        {
            return EnsureRuntimeBootstrap(VerticalSliceFactory.Create);
        }

        internal static VerticalSliceBootstrap EnsureRuntimeBootstrap(Func<int, VerticalSliceSample> sampleFactory)
        {
            if (sampleFactory == null) throw new ArgumentNullException(nameof(sampleFactory));
            var existing = FindExisting();
            if (existing != null)
            {
                existing.EnsureRuntimeComposed(sampleFactory);
                return existing;
            }

            var root = new GameObject(RuntimeRootName);
            try
            {
                root.hideFlags = HideFlags.DontSave;
                DontDestroyOnLoad(root);
                var bootstrap = root.AddComponent<VerticalSliceBootstrap>();
                bootstrap.EnsureRuntimeComposed(sampleFactory);
                return bootstrap;
            }
            catch
            {
                DestroyOwned(root);
                throw;
            }
        }

        public void PressCell(BoardPosition position)
        {
            Vector2 center;
            if (TryCellCenter(position, true, out center)) RequireTestDriver().Press(center);
        }

        public void MoveToCell(BoardPosition position)
        {
            Vector2 center;
            if (TryCellCenter(position, false, out center)) RequireTestDriver().Move(center);
        }

        public void ReleaseCell(BoardPosition position)
        {
            Vector2 center;
            if (TryCellCenter(position, false, out center)) RequireTestDriver().Release(center);
        }

        public void AdvanceUnscaledTime(float seconds)
        {
            if (Controller == null) throw new InvalidOperationException("The vertical slice is not initialized.");
            Controller.AdvanceTime(seconds);
        }

        private void Start()
        {
            if (!initialized) EnsureRuntimeComposed(VerticalSliceFactory.Create);
        }

        private void EnsureRuntimeComposed(Func<int, VerticalSliceSample> factory)
        {
            if (initialized) return;
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            sampleFactory = factory;
            Compose(VerticalSliceFactory.DefaultSeed, false, null);
        }

        private void Compose(int seed, bool useDeterministicPointer, Camera gameplayCamera)
        {
            if (initialized) throw new InvalidOperationException("VerticalSliceBootstrap is already initialized.");
            var runtimeRoot = new GameObject("Owned Core Runtime");
            runtimeRoot.hideFlags = HideFlags.DontSave;
            runtimeRoot.transform.SetParent(transform, false);
            ownedRuntimeRoot = runtimeRoot;
            try
            {
                Sample = sampleFactory(seed);
                if (Sample == null)
                    throw new InvalidOperationException("The vertical-slice sample factory returned null.");
                View = runtimeRoot.AddComponent<BoardView>();
                Controller = runtimeRoot.AddComponent<PlayableBattleController>();
                IBoardPointerSource pointerSource;
                if (useDeterministicPointer)
                {
                    PointerDriver = new VerticalSlicePointerDriver();
                    pointerSource = PointerDriver;
                }
                else
                {
                    PointerInput = runtimeRoot.AddComponent<BoardPointerInput>();
                    pointerSource = PointerInput;
                }

                View.Initialize(Sample.CurrentBoard);
                injectedGameplayCamera = gameplayCamera;
                hasInjectedCameraPolicy = useDeterministicPointer;
                if (useDeterministicPointer && injectedGameplayCamera == null)
                    injectedGameplayCamera = CreateDeterministicGameplayCamera(runtimeRoot);
                Controller.Initialize(pointerSource, View, default(Rect), Sample.CurrentBoard, Sample.OrbSource,
                    Sample.Party, Sample.Enemy, Sample.StageSession, Sample.BattleContext, Sample.Stage,
                    TryGetCurrentBoardScreenRect);
                StartForwardingEvents();
                initialized = true;
            }
            catch
            {
                StopForwardingEvents();
                if (PointerDriver != null) PointerDriver.Dispose();
                DestroyOwned(runtimeRoot);
                ClearReferences();
                throw;
            }
        }

        private void Update()
        {
            if (!initialized || Controller == null || Controller.IsDragging || Controller.IsResolving) return;
            Controller.TryRefreshBoardScreenRect();
        }

        private void OnDestroy()
        {
            StopForwardingEvents();
            if (PointerDriver != null) PointerDriver.Dispose();
            var root = ownedRuntimeRoot;
            ClearReferences();
            DestroyOwned(root);
        }

        private void StartForwardingEvents()
        {
            if (forwardingEvents) return;
            Controller.TimerChanged += ForwardTimer;
            Controller.MatchGroupsResolved += ForwardGroups;
            Controller.CascadeLayerResolved += ForwardCascade;
            Controller.AttackResolved += ForwardAttack;
            Controller.HealResolved += ForwardHeal;
            Controller.BossMechanicTriggered += ForwardBossMechanic;
            Controller.EnemyCountdownChanged += ForwardCountdown;
            Controller.EnemyActionResolved += ForwardEnemyAction;
            Controller.BoardResolutionCountChanged += ForwardResolutionCount;
            Controller.StageCompleted += ForwardStageCompletion;
            Controller.StarResultsReady += ForwardStars;
            Sample.PackFlow.Transitioned += ForwardPackFlow;
            forwardingEvents = true;
        }

        private void StopForwardingEvents()
        {
            if (!forwardingEvents) return;
            if (Controller != null)
            {
                Controller.TimerChanged -= ForwardTimer;
                Controller.MatchGroupsResolved -= ForwardGroups;
                Controller.CascadeLayerResolved -= ForwardCascade;
                Controller.AttackResolved -= ForwardAttack;
                Controller.HealResolved -= ForwardHeal;
                Controller.BossMechanicTriggered -= ForwardBossMechanic;
                Controller.EnemyCountdownChanged -= ForwardCountdown;
                Controller.EnemyActionResolved -= ForwardEnemyAction;
                Controller.BoardResolutionCountChanged -= ForwardResolutionCount;
                Controller.StageCompleted -= ForwardStageCompletion;
                Controller.StarResultsReady -= ForwardStars;
            }
            if (Sample != null) Sample.PackFlow.Transitioned -= ForwardPackFlow;
            forwardingEvents = false;
        }

        private void ForwardTimer(MoveTimerEvent value) { var handler = TimerChanged; if (handler != null) handler(value); }
        private void ForwardGroups(MatchGroupsEvent value) { var handler = MatchGroupsResolved; if (handler != null) handler(value); }
        private void ForwardCascade(CascadeLayerEvent value) { var handler = CascadeLayerResolved; if (handler != null) handler(value); }
        private void ForwardAttack(AttackEvent value) { var handler = AttackResolved; if (handler != null) handler(value); }
        private void ForwardHeal(HealEvent value) { var handler = HealResolved; if (handler != null) handler(value); }
        private void ForwardBossMechanic(EnemyEffectSnapshot value) { var handler = BossMechanicTriggered; if (handler != null) handler(value); }
        private void ForwardCountdown(EnemyCountdownEvent value) { var handler = EnemyCountdownChanged; if (handler != null) handler(value); }
        private void ForwardEnemyAction(EnemyActionSnapshot value) { var handler = EnemyActionResolved; if (handler != null) handler(value); }
        private void ForwardResolutionCount(int value) { var handler = BoardResolutionCountChanged; if (handler != null) handler(value); }
        private void ForwardStageCompletion(StageCompletionEvent value) { var handler = StageCompleted; if (handler != null) handler(value); }
        private void ForwardStars(IReadOnlyList<StarResult> value) { var handler = StarResultsReady; if (handler != null) handler(value); }
        private void ForwardPackFlow(PackSummonFlowEvent value) { var handler = PackFlowTransitioned; if (handler != null) handler(value); }

        private VerticalSlicePointerDriver RequireTestDriver()
        {
            if (PointerDriver == null)
                throw new InvalidOperationException("Deterministic pointer commands are available only on explicitly test-created bootstraps.");
            return PointerDriver;
        }

        private bool TryCellCenter(BoardPosition position, bool refreshIdleMapping, out Vector2 center)
        {
            if (Sample == null) throw new InvalidOperationException("The vertical slice is not initialized.");
            Sample.CurrentBoard.Get(position);
            center = default(Vector2);
            if (Controller == null) return false;
            if (refreshIdleMapping && !Controller.IsDragging && !Controller.TryRefreshBoardScreenRect()) return false;
            var accepted = Controller.AcceptedBoardScreenRect;
            if (!accepted.HasValue) return false;
            var rect = accepted.Value;
            center = new Vector2(
                rect.xMin + (position.X + .5f) * rect.width / BoardState.Columns,
                rect.yMin + (position.Y + .5f) * rect.height / BoardState.Rows);
            return true;
        }

        private static Camera CreateDeterministicGameplayCamera(GameObject runtimeRoot)
        {
            var cameraObject = new GameObject("Deterministic Gameplay Camera");
            cameraObject.hideFlags = HideFlags.DontSave;
            cameraObject.transform.SetParent(runtimeRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            return camera;
        }

        private Rect? TryGetCurrentBoardScreenRect()
        {
            var camera = ResolveCurrentGameplayCamera();
            GameplayCamera = camera;
            if (camera == null || View == null) return null;
            Rect result;
            return TryProjectWorldBoundsToScreen(camera, View.WorldBounds, out result) ? result : (Rect?)null;
        }

        private Camera ResolveCurrentGameplayCamera()
        {
            if (hasInjectedCameraPolicy)
                return IsUsableCamera(injectedGameplayCamera) ? injectedGameplayCamera : null;

            var count = Camera.allCamerasCount;
            if (cameraBuffer == null || cameraBuffer.Length < count)
                cameraBuffer = new Camera[Mathf.Max(count, cameraBuffer == null ? 4 : cameraBuffer.Length * 2)];
            var written = Camera.GetAllCameras(cameraBuffer);
            Camera selected = null;
            var ambiguous = false;
            for (var index = 0; index < written; index++)
            {
                var candidate = cameraBuffer[index];
                cameraBuffer[index] = null;
                if (!IsUsableCamera(candidate) || !candidate.CompareTag("MainCamera")) continue;
                if (selected == null) selected = candidate;
                else ambiguous = true;
            }
            return ambiguous ? null : selected;
        }

        private static bool IsUsableCamera(Camera camera)
        {
            return camera != null && camera.isActiveAndEnabled && camera.targetDisplay == 0;
        }

        private static bool TryProjectWorldBoundsToScreen(Camera camera, Bounds bounds, out Rect result)
        {
            result = default(Rect);
            if (!IsUsableCamera(camera)) return false;
            if (!IsFinite(bounds.min.x) || !IsFinite(bounds.min.y) || !IsFinite(bounds.max.x) || !IsFinite(bounds.max.y) ||
                bounds.size.x <= 0f || bounds.size.y <= 0f)
                return false;

            var first = camera.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.center.z));
            var second = camera.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.center.z));
            var third = camera.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.center.z));
            var fourth = camera.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.center.z));
            if (!IsVisibleProjectedCorner(first) || !IsVisibleProjectedCorner(second) ||
                !IsVisibleProjectedCorner(third) || !IsVisibleProjectedCorner(fourth)) return false;
            var minX = Mathf.Min(Mathf.Min(first.x, second.x), Mathf.Min(third.x, fourth.x));
            var maxX = Mathf.Max(Mathf.Max(first.x, second.x), Mathf.Max(third.x, fourth.x));
            var minY = Mathf.Min(Mathf.Min(first.y, second.y), Mathf.Min(third.y, fourth.y));
            var maxY = Mathf.Max(Mathf.Max(first.y, second.y), Mathf.Max(third.y, fourth.y));
            result = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return BoardLayout.IsValidRect(result);
        }

        private static bool IsVisibleProjectedCorner(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && value.z > 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static VerticalSliceBootstrap FindExisting()
        {
            // FindObjectsByType intentionally omits DontSave objects. Runtime roots use that flag so
            // they survive scene reloads without ever becoming serialized scene dependencies.
            var values = Resources.FindObjectsOfTypeAll<VerticalSliceBootstrap>();
            return values.Length == 0 ? null : values[0];
        }

        private void ClearReferences()
        {
            ownedRuntimeRoot = null;
            Sample = null;
            Controller = null;
            View = null;
            PointerInput = null;
            PointerDriver = null;
            GameplayCamera = null;
            injectedGameplayCamera = null;
            hasInjectedCameraPolicy = false;
            sampleFactory = null;
            initialized = false;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }

    /// <summary>Keeps one automatic-startup decision until subsystem registration resets it.</summary>
    internal sealed class AutomaticBootstrapStartup
    {
        private bool decisionMade;

        internal bool IsSuppressed { get; private set; }
        internal bool CallbackRegistered { get; private set; }

        internal static bool ShouldSuppress(IReadOnlyList<string> arguments, string initialSceneName)
        {
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            for (var index = 0; index < arguments.Count; index++)
                if (string.Equals(arguments[index], "-runTests", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arguments[index], "-testPlatform", StringComparison.OrdinalIgnoreCase)) return true;
            if (initialSceneName != null &&
                initialSceneName.StartsWith("InitTestScene", StringComparison.Ordinal)) return true;
            // Integration: the sample vertical slice only self-starts in its
            // own scene; the shipped game ("Game") and the presentation demo
            // ("PresentationDemo") compose their own bootstraps.
            return !string.Equals(initialSceneName, "SampleScene", StringComparison.Ordinal);
        }

        internal void Begin(bool suppress, Action autoBootstrap, Action registerSceneCallback)
        {
            if (autoBootstrap == null) throw new ArgumentNullException(nameof(autoBootstrap));
            if (registerSceneCallback == null) throw new ArgumentNullException(nameof(registerSceneCallback));
            if (decisionMade) return;

            decisionMade = true;
            IsSuppressed = suppress;
            if (suppress) return;

            autoBootstrap();
            registerSceneCallback();
            CallbackRegistered = true;
        }

        internal void SceneLoaded(Action autoBootstrap)
        {
            if (autoBootstrap == null) throw new ArgumentNullException(nameof(autoBootstrap));
            if (!decisionMade || IsSuppressed || !CallbackRegistered) return;
            autoBootstrap();
        }

        internal void Reset()
        {
            decisionMade = false;
            IsSuppressed = false;
            CallbackRegistered = false;
        }
    }

    /// <summary>Allocation-free event source used for deterministic pointer automation and smoke tests.</summary>
    public sealed class VerticalSlicePointerDriver : IBoardPointerSource, IDisposable
    {
        private Action<Vector2> pressed;
        private Action<Vector2> moved;
        private Action<Vector2> released;
        private bool disposed;

        public event Action<Vector2> PointerPressed { add { ThrowIfDisposed(); pressed += value; } remove { pressed -= value; } }
        public event Action<Vector2> PointerMoved { add { ThrowIfDisposed(); moved += value; } remove { moved -= value; } }
        public event Action<Vector2> PointerReleased { add { ThrowIfDisposed(); released += value; } remove { released -= value; } }

        public int SubscriptionCount { get { return Count(pressed) + Count(moved) + Count(released); } }

        public void Press(Vector2 position) { ThrowIfDisposed(); var handler = pressed; if (handler != null) handler(position); }
        public void Move(Vector2 position) { ThrowIfDisposed(); var handler = moved; if (handler != null) handler(position); }
        public void Release(Vector2 position) { ThrowIfDisposed(); var handler = released; if (handler != null) handler(position); }

        public void Dispose()
        {
            disposed = true;
            pressed = null;
            moved = null;
            released = null;
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException("VerticalSlicePointerDriver");
        }

        private static int Count(Delegate value) { return value == null ? 0 : value.GetInvocationList().Length; }
    }
}
