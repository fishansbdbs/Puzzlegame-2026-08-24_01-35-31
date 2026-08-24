using System.Collections.Generic;

namespace PuzzleGame.Presentation
{
    // ---------------------------------------------------------------------
    // Battle event payloads consumed by the HUD and VFX layers.
    // Core (Codex) systems raise these through IBattleEventSource; the
    // presentation layer never computes damage, matching or RNG itself.
    // ---------------------------------------------------------------------

    public struct BoardCell
    {
        public int Col;
        public int Row;

        public BoardCell(int col, int row)
        {
            Col = col;
            Row = row;
        }
    }

    /// <summary>One resolved color match (a single attack event source).</summary>
    public class MatchEvent
    {
        public OrbColor Color;
        public List<BoardCell> Cells = new List<BoardCell>();
        /// <summary>1-based index of this match within the current resolution.</summary>
        public int MatchIndex;
        /// <summary>Combo total after this match counted.</summary>
        public int ComboAfter;
        /// <summary>True when this match came from a cascade rather than the initial move.</summary>
        public bool FromCascade;
    }

    /// <summary>One character attack triggered by one match event.</summary>
    public class AttackEvent
    {
        public string CharacterId;
        public int PartySlot;               // 0..4
        public ElementId Element;
        public long Damage;
        public int TargetEnemyIndex;
        public bool Critical;
        public bool EffectiveHit;           // elemental advantage
        public bool ResistedHit;            // elemental disadvantage
        /// <summary>Index of this attack within a rapid sequence, for pacing.</summary>
        public int SequenceIndex;
        public int SequenceCount;
    }

    public class HealEvent
    {
        public long Amount;
        public bool FromHeartMatch;
    }

    public class EnemyActionEvent
    {
        public int EnemyIndex;
        public string ActionName;           // e.g. "Sludge Slam"
        public string Description;          // e.g. "Converts 3 orbs to Poison"
        public long DamageToParty;
        /// <summary>Cells affected by conversions/locks/hazards, if any.</summary>
        public List<BoardCell> AffectedCells = new List<BoardCell>();
        public OrbColor? ConvertTo;
        public bool IsTelegraphOnly;        // announce next action without acting
    }

    public class SkillCastEvent
    {
        public string CharacterId;
        public int PartySlot;
        public string SkillName;
        public ElementId Element;
        /// <summary>Show the near-full-screen cut-in treatment.</summary>
        public bool BigCutIn;
    }

    public enum BattleOutcome
    {
        Victory,
        Defeat,
        Retreat
    }

    public class BattleEndEvent
    {
        public BattleOutcome Outcome;
        public int StarsEarned;
        public List<string> RewardLines = new List<string>();
    }
}
