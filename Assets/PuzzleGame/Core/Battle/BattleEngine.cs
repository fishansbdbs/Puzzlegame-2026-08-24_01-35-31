using System;
using System.Collections.Generic;
using PuzzleGame.Core.Board;

namespace PuzzleGame.Core.Battle
{
    public sealed class BoardEffectState
    {
        private readonly int[] lockTurns = new int[6];
        private readonly object[] lockIdentities = CreateIdentities(6);
        private object poisonIdentity = new object();
        private object hazardIdentity = new object();
        private object blockerIdentity = new object();

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
            if (turns > 0)
            {
                lockTurns[(int)orbType] = Math.Max(lockTurns[(int)orbType], turns);
                lockIdentities[(int)orbType] = new object();
            }
        }

        public void AddPoison(int count, int turns)
        {
            if (ValidateTurns(turns) == 0) return;
            PoisonCount = SaturatingAdd(PoisonCount, count);
            PoisonTurns = Math.Max(PoisonTurns, turns);
            poisonIdentity = new object();
        }

        public void AddHazard(int count, int turns)
        {
            if (ValidateTurns(turns) == 0) return;
            HazardCount = SaturatingAdd(HazardCount, count);
            HazardTurns = Math.Max(HazardTurns, turns);
            hazardIdentity = new object();
        }

        public void AddBlocker(int count, int turns)
        {
            if (ValidateTurns(turns) == 0) return;
            BlockerCount = SaturatingAdd(BlockerCount, count);
            BlockerTurns = Math.Max(BlockerTurns, turns);
            blockerIdentity = new object();
        }

        public void TickTimedEffects()
        {
            for (var index = 0; index < lockTurns.Length; index++) TickLock(index);
            TickPoison();
            TickHazard();
            TickBlocker();
        }

        public void ClearTimedEffects()
        {
            Array.Clear(lockTurns, 0, lockTurns.Length);
            PoisonCount = 0;
            PoisonTurns = 0;
            HazardCount = 0;
            HazardTurns = 0;
            BlockerCount = 0;
            BlockerTurns = 0;
            for (var index = 0; index < lockIdentities.Length; index++) lockIdentities[index] = new object();
            poisonIdentity = new object();
            hazardIdentity = new object();
            blockerIdentity = new object();
        }

        internal BoardTimedEffectCapture CaptureTimedEffects()
        {
            return new BoardTimedEffectCapture((object[])lockIdentities.Clone(), poisonIdentity, hazardIdentity, blockerIdentity);
        }

        internal void TickCapturedTimedEffects(BoardTimedEffectCapture capture)
        {
            for (var index = 0; index < lockTurns.Length; index++)
                if (object.ReferenceEquals(lockIdentities[index], capture.LockIdentities[index])) TickLock(index);
            if (object.ReferenceEquals(poisonIdentity, capture.PoisonIdentity)) TickPoison();
            if (object.ReferenceEquals(hazardIdentity, capture.HazardIdentity)) TickHazard();
            if (object.ReferenceEquals(blockerIdentity, capture.BlockerIdentity)) TickBlocker();
        }

        private void TickLock(int index)
        {
            if (lockTurns[index] > 0 && --lockTurns[index] == 0) lockIdentities[index] = new object();
        }

        private void TickPoison()
        {
            if (PoisonTurns > 0 && --PoisonTurns == 0) { PoisonCount = 0; poisonIdentity = new object(); }
        }

        private void TickHazard()
        {
            if (HazardTurns > 0 && --HazardTurns == 0) { HazardCount = 0; hazardIdentity = new object(); }
        }

        private void TickBlocker()
        {
            if (BlockerTurns > 0 && --BlockerTurns == 0) { BlockerCount = 0; blockerIdentity = new object(); }
        }

        private static object[] CreateIdentities(int count)
        {
            var identities = new object[count];
            for (var index = 0; index < count; index++) identities[index] = new object();
            return identities;
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
        private readonly float baseMoveTimeSeconds;
        private object moveTimeIdentity = new object();
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
            baseMoveTimeSeconds = moveTimeSeconds;
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
            if (turns == 0) return;
            var value = MoveTimeSeconds + deltaSeconds;
            MoveTimeSeconds = value <= 0f ? 0f : float.IsInfinity(value) ? float.MaxValue : value;
            MoveTimeModifierTurns = Math.Max(MoveTimeModifierTurns, turns);
            moveTimeIdentity = new object();
        }

        public void TickMoveTimeEffect()
        {
            if (MoveTimeModifierTurns > 0 && --MoveTimeModifierTurns == 0)
            {
                MoveTimeSeconds = baseMoveTimeSeconds;
                moveTimeIdentity = new object();
            }
        }

        public void ClearMoveTimeEffect()
        {
            MoveTimeSeconds = baseMoveTimeSeconds;
            MoveTimeModifierTurns = 0;
            moveTimeIdentity = new object();
        }

        public void InstallBoard(BoardState board)
        {
            if (board == null) throw new ArgumentNullException("board");
            Board = board.Clone();
        }

        internal object CaptureMoveTimeIdentity() { return moveTimeIdentity; }
        internal void TickCapturedMoveTimeEffect(object capturedIdentity)
        {
            if (object.ReferenceEquals(moveTimeIdentity, capturedIdentity)) TickMoveTimeEffect();
        }
    }

    internal struct BoardTimedEffectCapture
    {
        internal BoardTimedEffectCapture(object[] lockIdentities, object poisonIdentity, object hazardIdentity, object blockerIdentity)
        {
            LockIdentities = lockIdentities;
            PoisonIdentity = poisonIdentity;
            HazardIdentity = hazardIdentity;
            BlockerIdentity = blockerIdentity;
        }

        internal readonly object[] LockIdentities;
        internal readonly object PoisonIdentity;
        internal readonly object HazardIdentity;
        internal readonly object BlockerIdentity;
    }

    public sealed class BattleTurnResolution
    {
        internal BattleTurnResolution(CombatResolution combat, SkillChargeResolution charge, EnemyTurnResolution enemyTurn,
            IReadOnlyList<EnemyEffectSnapshot> mechanics, bool wasSkippedBecausePartyDefeated = false)
        {
            Combat = combat;
            Charge = charge;
            EnemyTurn = enemyTurn;
            Mechanics = Copy(mechanics);
            WasSkippedBecausePartyDefeated = wasSkippedBecausePartyDefeated;
        }

        public CombatResolution Combat { get; private set; }
        public SkillChargeResolution Charge { get; private set; }
        public EnemyTurnResolution EnemyTurn { get; private set; }
        public IReadOnlyList<EnemyEffectSnapshot> Mechanics { get; private set; }
        public bool WasSkippedBecausePartyDefeated { get; private set; }

        private static IReadOnlyList<EnemyEffectSnapshot> Copy(IReadOnlyList<EnemyEffectSnapshot> source)
        {
            var result = new List<EnemyEffectSnapshot>(source.Count);
            for (var index = 0; index < source.Count; index++) result.Add(source[index]);
            return result.AsReadOnly();
        }
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
            if (context.Party.CurrentHp == 0)
            {
                return new BattleTurnResolution(
                    new CombatResolution(0, new List<AttackEvent>(), new List<HealEvent>(), new List<CombatModifierEvent>()),
                    new SkillChargeResolution(new List<SkillChargeEvent>()),
                    new EnemyTurnResolution(new List<EnemyActionSnapshot>(), new List<BattleEffectEvent>()),
                    new List<EnemyEffectSnapshot>(), true);
            }
            var partyEffects = context.Party.CaptureTimedEffects();
            var bindEffects = new object[context.Party.Members.Count];
            for (var index = 0; index < context.Party.Members.Count; index++)
                bindEffects[index] = context.Party.Members[index].CaptureBindIdentity();
            var enemyEffects = context.Enemy.CaptureTimedEffects();
            var boardEffects = context.BoardEffects.CaptureTimedEffects();
            var moveTimeEffect = context.CaptureMoveTimeIdentity();
            var combat = combatCalculator.Resolve(resolution, context.Party, context.Enemy);
            var charge = skillEngine.ChargeFrom(resolution, context.Party);
            context.InstallBoard(resolution.FinalBoard);
            var mechanics = context.Enemy.EvaluateThresholds();
            var enemyTurn = enemyActionEngine.AdvanceAfterBoardResolution(context.Enemy, context);
            context.Party.TickCapturedTimedEffects(partyEffects);
            for (var index = 0; index < context.Party.Members.Count; index++)
                context.Party.Members[index].TickCapturedBind(bindEffects[index]);
            context.Enemy.TickCapturedTimedEffects(enemyEffects);
            context.BoardEffects.TickCapturedTimedEffects(boardEffects);
            context.TickCapturedMoveTimeEffect(moveTimeEffect);
            return new BattleTurnResolution(combat, charge, enemyTurn, mechanics);
        }
    }
}
