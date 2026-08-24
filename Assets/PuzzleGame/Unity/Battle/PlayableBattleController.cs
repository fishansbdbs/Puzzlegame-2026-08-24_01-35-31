using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Stages;
using PuzzleGame.Unity.Input;
using UnityEngine;

namespace PuzzleGame.Unity.Battle
{
    public struct MoveTimerEvent
    {
        public MoveTimerEvent(float remainingSeconds, float durationSeconds)
        {
            RemainingSeconds = remainingSeconds;
            DurationSeconds = durationSeconds;
        }

        public float RemainingSeconds { get; private set; }
        public float DurationSeconds { get; private set; }
    }

    public sealed class MatchGroupsEvent
    {
        internal MatchGroupsEvent(int cascadeLayerIndex, IReadOnlyList<MatchGroup> groups)
        {
            CascadeLayerIndex = cascadeLayerIndex;
            Groups = Snapshot(groups);
        }

        public int CascadeLayerIndex { get; private set; }
        public IReadOnlyList<MatchGroup> Groups { get; private set; }

        private static IReadOnlyList<MatchGroup> Snapshot(IReadOnlyList<MatchGroup> groups)
        {
            var result = new List<MatchGroup>(groups.Count);
            for (var index = 0; index < groups.Count; index++) result.Add(groups[index]);
            return result.AsReadOnly();
        }
    }

    public sealed class CascadeLayerEvent
    {
        internal CascadeLayerEvent(int cascadeLayerIndex, IReadOnlyList<MatchGroup> groups)
        {
            CascadeLayerIndex = cascadeLayerIndex;
            var result = new List<MatchGroup>(groups.Count);
            for (var index = 0; index < groups.Count; index++) result.Add(groups[index]);
            Groups = result.AsReadOnly();
        }

        public int CascadeLayerIndex { get; private set; }
        public IReadOnlyList<MatchGroup> Groups { get; private set; }
    }

    public struct EnemyCountdownEvent
    {
        public EnemyCountdownEvent(int countdown) { Countdown = countdown; }
        public int Countdown { get; private set; }
    }

    public struct StageCompletionEvent
    {
        public StageCompletionEvent(string stageId, int boardResolutionCount, float finalHpRatio)
        {
            StageId = stageId;
            BoardResolutionCount = boardResolutionCount;
            FinalHpRatio = finalHpRatio;
        }

        public string StageId { get; private set; }
        public int BoardResolutionCount { get; private set; }
        public float FinalHpRatio { get; private set; }
    }

    public sealed class PlayableBattleController : MonoBehaviour
    {
        private readonly BoardResolver boardResolver = new BoardResolver();
        private readonly BattleEngine battleEngine = new BattleEngine();
        private IBoardPointerSource pointerSource;
        private BoardView boardView;
        private Rect boardScreenRect;
        private BoardState currentBoard;
        private IOrbSource orbSource;
        private PartyState party;
        private EnemyRuntime enemy;
        private StageSession stageSession;
        private BattleContext battleContext;
        private StageData objectiveStage;
        private DragSession dragSession;
        private BoardPosition dragPosition;
        private float dragElapsedSeconds;
        private float dragDurationSeconds;
        private bool initialized;
        private bool subscribed;
        private bool resolving;

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

        public int CompletedBoardResolutions { get; private set; }
        public bool IsDragging { get { return dragSession != null; } }
        public bool IsResolving { get { return resolving; } }
        public BoardState CurrentBoard { get { return currentBoard; } }

        public void Initialize(IBoardPointerSource input, BoardView view, Rect screenRect, BoardState board,
            IOrbSource source, PartyState partyState, EnemyRuntime enemyState, StageSession session,
            BattleContext context, StageData stageData)
        {
            if (initialized) throw new InvalidOperationException("PlayableBattleController is already initialized.");
            if (input == null) throw new ArgumentNullException("input");
            if (view == null) throw new ArgumentNullException("view");
            if (board == null) throw new ArgumentNullException("board");
            if (source == null) throw new ArgumentNullException("source");
            if (partyState == null) throw new ArgumentNullException("partyState");
            if (enemyState == null) throw new ArgumentNullException("enemyState");
            if (session == null) throw new ArgumentNullException("session");
            if (context == null) throw new ArgumentNullException("context");
            if (stageData == null) throw new ArgumentNullException("stageData");
            if (screenRect.width <= 0f || screenRect.height <= 0f || !IsFinite(screenRect.width) || !IsFinite(screenRect.height))
                throw new ArgumentOutOfRangeException("screenRect");
            if (!object.ReferenceEquals(context.Board, board)) throw new ArgumentException("Battle context must reference the supplied board.", "context");
            if (!object.ReferenceEquals(context.Party, partyState)) throw new ArgumentException("Battle context must reference the supplied party.", "context");
            if (!object.ReferenceEquals(context.Enemy, enemyState)) throw new ArgumentException("Battle context must reference the supplied enemy.", "context");

            pointerSource = input;
            boardView = view;
            boardScreenRect = screenRect;
            currentBoard = board;
            orbSource = source;
            party = partyState;
            enemy = enemyState;
            stageSession = session;
            battleContext = context;
            objectiveStage = SnapshotObjectives(stageData);
            initialized = true;
            boardView.Initialize(currentBoard);
            if (isActiveAndEnabled) Subscribe();
        }

        public void AdvanceTime(float unscaledDeltaTime)
        {
            if (!IsFinite(unscaledDeltaTime) || unscaledDeltaTime < 0f)
                throw new ArgumentOutOfRangeException("unscaledDeltaTime");
            if (dragSession == null || resolving) return;
            dragElapsedSeconds = Mathf.Min(dragDurationSeconds, dragElapsedSeconds + unscaledDeltaTime);
            EmitTimer(Mathf.Max(0f, dragDurationSeconds - dragElapsedSeconds));
            if (dragElapsedSeconds >= dragDurationSeconds) CompleteDrag();
        }

        private void Update()
        {
            AdvanceTime(Time.unscaledDeltaTime);
        }

        private void OnEnable()
        {
            if (initialized) Subscribe();
        }

        private void OnDisable()
        {
            try
            {
                if (dragSession != null) CompleteDrag();
            }
            finally
            {
                Unsubscribe();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed) return;
            pointerSource.PointerPressed += OnPointerPressed;
            pointerSource.PointerMoved += OnPointerMoved;
            pointerSource.PointerReleased += OnPointerReleased;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || pointerSource == null) return;
            pointerSource.PointerPressed -= OnPointerPressed;
            pointerSource.PointerMoved -= OnPointerMoved;
            pointerSource.PointerReleased -= OnPointerReleased;
            subscribed = false;
        }

        private void OnPointerPressed(Vector2 screenPosition)
        {
            if (resolving || dragSession != null || party.CurrentHp == 0 || enemy.IsDefeated || stageSession.IsCompleted) return;
            var cell = BoardLayout.ScreenToCell(screenPosition, boardScreenRect);
            if (!cell.HasValue) return;
            currentBoard = battleContext.Board;
            dragPosition = cell.Value;
            dragDurationSeconds = battleContext.MoveTimeSeconds;
            dragElapsedSeconds = 0f;
            dragSession = new DragSession(currentBoard, dragPosition, dragDurationSeconds);
            EmitTimer(dragDurationSeconds);
            if (dragDurationSeconds <= 0f) CompleteDrag();
        }

        private void OnPointerMoved(Vector2 screenPosition)
        {
            if (resolving || dragSession == null) return;
            var cell = BoardLayout.ScreenToCell(screenPosition, boardScreenRect);
            if (!cell.HasValue || cell.Value == dragPosition) return;
            if (!dragSession.TryMove(cell.Value, dragElapsedSeconds)) return;
            dragPosition = cell.Value;
            boardView.Refresh(currentBoard);
        }

        private void OnPointerReleased(Vector2 screenPosition)
        {
            if (resolving || dragSession == null) return;
            OnPointerMoved(screenPosition);
            CompleteDrag();
        }

        private void CompleteDrag()
        {
            if (dragSession == null || resolving) return;
            var completedDrag = dragSession;
            dragSession = null;
            completedDrag.End();
            EmitTimer(0f);
            resolving = true;
            try
            {
                var boardResolution = boardResolver.Resolve(currentBoard, orbSource);
                var turnResolution = stageSession.CompleteBoardResolution(battleEngine, boardResolution, battleContext);
                currentBoard = battleContext.Board;
                boardView.Refresh(currentBoard);
                CompletedBoardResolutions++;

                var completedStage = false;
                IReadOnlyList<StarResult> starResults = null;
                if (enemy.IsDefeated)
                {
                    stageSession.TryAdvanceWave();
                    completedStage = stageSession.IsCompleted;
                    if (completedStage)
                    {
                        var stageResult = new StageResult(true, party.HpPercent, stageSession.BoardResolutionCount);
                        starResults = StageObjectiveEvaluator.Evaluate(objectiveStage, stageResult);
                    }
                }

                EmitResolutionEvents(boardResolution, turnResolution);
                var countHandler = BoardResolutionCountChanged;
                if (countHandler != null) countHandler(stageSession.BoardResolutionCount);
                if (completedStage)
                {
                    var completionHandler = StageCompleted;
                    if (completionHandler != null)
                        completionHandler(new StageCompletionEvent(objectiveStage.Id, stageSession.BoardResolutionCount, party.HpPercent));
                    var starHandler = StarResultsReady;
                    if (starHandler != null) starHandler(starResults);
                }
            }
            finally
            {
                resolving = false;
            }
        }

        private void EmitResolutionEvents(BoardResolution boardResolution, BattleTurnResolution turnResolution)
        {
            for (var layerIndex = 0; layerIndex < boardResolution.CascadeLayers.Count; layerIndex++)
            {
                var groups = boardResolution.CascadeLayers[layerIndex].Groups;
                var groupsHandler = MatchGroupsResolved;
                if (groupsHandler != null) groupsHandler(new MatchGroupsEvent(layerIndex, groups));
                var layerHandler = CascadeLayerResolved;
                if (layerHandler != null) layerHandler(new CascadeLayerEvent(layerIndex, groups));
            }

            for (var index = 0; index < turnResolution.Combat.Attacks.Count; index++)
            {
                var handler = AttackResolved;
                if (handler != null) handler(turnResolution.Combat.Attacks[index]);
            }
            for (var index = 0; index < turnResolution.Combat.Heals.Count; index++)
            {
                var handler = HealResolved;
                if (handler != null) handler(turnResolution.Combat.Heals[index]);
            }

            var countdownHandler = EnemyCountdownChanged;
            if (countdownHandler != null) countdownHandler(new EnemyCountdownEvent(enemy.Countdown));
            for (var index = 0; index < turnResolution.EnemyTurn.ExecutedActions.Count; index++)
            {
                var actionHandler = EnemyActionResolved;
                if (actionHandler != null) actionHandler(turnResolution.EnemyTurn.ExecutedActions[index]);
            }
        }

        private void EmitTimer(float remainingSeconds)
        {
            var handler = TimerChanged;
            if (handler != null) handler(new MoveTimerEvent(remainingSeconds, dragDurationSeconds));
        }

        private static StageData SnapshotObjectives(StageData source)
        {
            var objectives = source.StarObjectives == null ? null : new StarObjectiveData[source.StarObjectives.Length];
            if (objectives != null)
            {
                for (var index = 0; index < objectives.Length; index++)
                {
                    var item = source.StarObjectives[index];
                    objectives[index] = item == null ? null : new StarObjectiveData
                    {
                        Type = item.Type,
                        HpThresholdPercent = item.HpThresholdPercent,
                        MaximumBoardResolutionCount = item.MaximumBoardResolutionCount
                    };
                }
            }
            return new StageData { Id = source.Id, StarObjectives = objectives };
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
