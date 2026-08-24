using System;
using PuzzleGame.Core.Board;

namespace PuzzleGame.Core.Battle
{
    public sealed class BoardEffectState
    {
        private readonly int[] lockTurns = new int[6];

        public int PoisonCount { get; private set; }
        public int PoisonTurns { get; private set; }
        public int HazardCount { get; private set; }
        public int HazardTurns { get; private set; }
        public int BlockerCount { get; private set; }
        public int BlockerTurns { get; private set; }

        public bool IsLocked(Contracts.OrbType orbType)
        {
            ValidateOrb(orbType);
            return lockTurns[(int)orbType] > 0;
        }

        public void Lock(Contracts.OrbType orbType, int turns)
        {
            ValidateOrb(orbType);
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            lockTurns[(int)orbType] = Math.Max(lockTurns[(int)orbType], turns);
        }

        public void AddPoison(int count, int turns)
        {
            PoisonCount = SaturatingAdd(PoisonCount, count);
            PoisonTurns = Math.Max(PoisonTurns, ValidateTurns(turns));
        }

        public void AddHazard(int count, int turns)
        {
            HazardCount = SaturatingAdd(HazardCount, count);
            HazardTurns = Math.Max(HazardTurns, ValidateTurns(turns));
        }

        public void AddBlocker(int count, int turns)
        {
            BlockerCount = SaturatingAdd(BlockerCount, count);
            BlockerTurns = Math.Max(BlockerTurns, ValidateTurns(turns));
        }

        private static int SaturatingAdd(int current, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            return (int)Math.Min(int.MaxValue, (long)current + amount);
        }

        private static int ValidateTurns(int turns)
        {
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            return turns;
        }

        private static void ValidateOrb(Contracts.OrbType orbType)
        {
            if (orbType < Contracts.OrbType.Fire || orbType > Contracts.OrbType.Heart)
                throw new ArgumentOutOfRangeException("orbType");
        }
    }

    public sealed class BattleContext
    {
        public BattleContext(BoardState board, PartyState party, EnemyRuntime enemy, float moveTimeSeconds = BoardState.DefaultDragDurationSeconds)
        {
            if (board == null) throw new ArgumentNullException("board");
            if (party == null) throw new ArgumentNullException("party");
            if (enemy == null) throw new ArgumentNullException("enemy");
            if (float.IsNaN(moveTimeSeconds) || float.IsInfinity(moveTimeSeconds) || moveTimeSeconds < 0f)
                throw new ArgumentOutOfRangeException("moveTimeSeconds");
            Board = board;
            Party = party;
            Enemy = enemy;
            MoveTimeSeconds = moveTimeSeconds;
            BoardEffects = new BoardEffectState();
        }

        public BoardState Board { get; private set; }
        public PartyState Party { get; private set; }
        public EnemyRuntime Enemy { get; private set; }
        public BoardEffectState BoardEffects { get; private set; }
        public float MoveTimeSeconds { get; private set; }
        public int MoveTimeModifierTurns { get; private set; }

        public void ModifyMoveTime(float deltaSeconds, int turns)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) throw new ArgumentOutOfRangeException("deltaSeconds");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            var value = MoveTimeSeconds + deltaSeconds;
            MoveTimeSeconds = value <= 0f ? 0f : float.IsInfinity(value) ? float.MaxValue : value;
            MoveTimeModifierTurns = Math.Max(MoveTimeModifierTurns, turns);
        }
    }

    public sealed class BattleTurnResolution
    {
        internal BattleTurnResolution(CombatResolution combat, SkillChargeResolution charge, EnemyTurnResolution enemyTurn)
        {
            Combat = combat;
            Charge = charge;
            EnemyTurn = enemyTurn;
        }

        public CombatResolution Combat { get; private set; }
        public SkillChargeResolution Charge { get; private set; }
        public EnemyTurnResolution EnemyTurn { get; private set; }
    }

    public sealed class BattleEngine
    {
        private readonly CombatCalculator combatCalculator = new CombatCalculator();
        private readonly SkillEngine skillEngine = new SkillEngine();
        private readonly EnemyActionEngine enemyActionEngine = new EnemyActionEngine();

        public BattleTurnResolution CompleteBoardResolution(BoardResolution resolution, BattleContext context)
        {
            if (resolution == null) throw new ArgumentNullException("resolution");
            if (context == null) throw new ArgumentNullException("context");
            var combat = combatCalculator.Resolve(resolution, context.Party, context.Enemy);
            var charge = skillEngine.ChargeFrom(resolution, context.Party);
            var enemyTurn = enemyActionEngine.AdvanceAfterBoardResolution(context.Enemy, context);
            return new BattleTurnResolution(combat, charge, enemyTurn);
        }
    }
}
