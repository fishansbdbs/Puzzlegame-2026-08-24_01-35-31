using System;
using System.Collections.Generic;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public enum CombatModifierSource
    {
        Leader,
        Passive,
        ActiveSkill
    }

    public sealed class CombatModifierEvent
    {
        internal CombatModifierEvent(CombatModifierSource source, string sourceId, float multiplier)
        {
            Source = source;
            SourceId = sourceId ?? string.Empty;
            Multiplier = multiplier;
        }

        public CombatModifierSource Source { get; private set; }
        public string SourceId { get; private set; }
        public float Multiplier { get; private set; }
    }

    public sealed class AttackEvent
    {
        internal AttackEvent(int characterSlot, string characterId, ElementType element, int cascadeLayerIndex, int groupId,
            int orbCount, int comboCount, int calculatedDamage, int damage, bool wasAbsorbed, bool wasBlocked, int absorbedHealing)
        {
            CharacterSlot = characterSlot;
            CharacterId = characterId;
            Element = element;
            CascadeLayerIndex = cascadeLayerIndex;
            GroupId = groupId;
            OrbCount = orbCount;
            ComboCount = comboCount;
            CalculatedDamage = calculatedDamage;
            Damage = damage;
            WasAbsorbed = wasAbsorbed;
            WasBlocked = wasBlocked;
            AbsorbedHealing = absorbedHealing;
        }

        public int CharacterSlot { get; private set; }
        public string CharacterId { get; private set; }
        public ElementType Element { get; private set; }
        public int CascadeLayerIndex { get; private set; }
        public int GroupId { get; private set; }
        public int OrbCount { get; private set; }
        public int ComboCount { get; private set; }
        public int CalculatedDamage { get; private set; }
        public int Damage { get; private set; }
        public bool WasAbsorbed { get; private set; }
        public bool WasBlocked { get; private set; }
        public int AbsorbedHealing { get; private set; }
    }

    public sealed class HealEvent
    {
        internal HealEvent(int cascadeLayerIndex, int groupId, int orbCount, int comboCount, int calculatedHealing, int healing)
        {
            CascadeLayerIndex = cascadeLayerIndex;
            GroupId = groupId;
            OrbCount = orbCount;
            ComboCount = comboCount;
            CalculatedHealing = calculatedHealing;
            Healing = healing;
        }

        public int CascadeLayerIndex { get; private set; }
        public int GroupId { get; private set; }
        public int OrbCount { get; private set; }
        public int ComboCount { get; private set; }
        public int CalculatedHealing { get; private set; }
        public int Healing { get; private set; }
    }

    public sealed class CombatResolution
    {
        internal CombatResolution(int comboCount, List<AttackEvent> attacks, List<HealEvent> heals, List<CombatModifierEvent> modifiers)
        {
            ComboCount = comboCount;
            Attacks = attacks.AsReadOnly();
            Heals = heals.AsReadOnly();
            Modifiers = modifiers.AsReadOnly();
            long totalHealing = 0;
            for (var index = 0; index < heals.Count; index++) totalHealing += heals[index].Healing;
            TotalHealing = totalHealing >= int.MaxValue ? int.MaxValue : (int)totalHealing;
        }

        public int ComboCount { get; private set; }
        public IReadOnlyList<AttackEvent> Attacks { get; private set; }
        public IReadOnlyList<HealEvent> Heals { get; private set; }
        public IReadOnlyList<CombatModifierEvent> Modifiers { get; private set; }
        public int TotalHealing { get; private set; }
    }

    public sealed class CombatCalculator
    {
        public CombatResolution Resolve(BoardResolution boardResolution, PartyState party, EnemyRuntime enemy)
        {
            if (boardResolution == null) throw new ArgumentNullException("boardResolution");
            if (party == null) throw new ArgumentNullException("party");
            if (enemy == null) throw new ArgumentNullException("enemy");

            var comboCount = CountGroups(boardResolution);
            var attacks = new List<AttackEvent>();
            var heals = new List<HealEvent>();
            var modifiers = new List<CombatModifierEvent>();
            var passiveMultipliers = new double[party.Members.Count];
            var passiveEvaluated = new bool[party.Members.Count];
            var leaderMultiplier = GetLeaderMultiplier(party, comboCount, modifiers);
            if (party.AttackMultiplier != 1f)
                modifiers.Add(new CombatModifierEvent(CombatModifierSource.ActiveSkill, party.AttackBoostSourceId, party.AttackMultiplier));

            for (var layerIndex = 0; layerIndex < boardResolution.CascadeLayers.Count; layerIndex++)
            {
                var groups = boardResolution.CascadeLayers[layerIndex].Groups;
                for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    var group = groups[groupIndex];
                    if (group.OrbType == OrbType.Heart)
                    {
                        var calculated = CalculateScaled(party.TotalRecovery, group.Cells.Count, comboCount, 1f);
                        heals.Add(new HealEvent(layerIndex, group.Id, group.Cells.Count, comboCount, calculated, party.Heal(calculated)));
                        continue;
                    }

                    var matchElement = (ElementType)group.OrbType;
                    for (var slot = 0; slot < party.Members.Count; slot++)
                    {
                        var character = party.Members[slot];
                        if (character.IsBound || character.Element != matchElement) continue;
                        if (!passiveEvaluated[slot])
                        {
                            passiveMultipliers[slot] = GetPassiveMultiplier(character, modifiers);
                            passiveEvaluated[slot] = true;
                        }
                        var passiveMultiplier = passiveMultipliers[slot];
                        var multiplier = MultiplySaturating(leaderMultiplier, party.AttackMultiplier);
                        multiplier = MultiplySaturating(multiplier, passiveMultiplier);
                        multiplier = MultiplySaturating(multiplier, GetAffinityMultiplier(character.Element, enemy.Element));
                        var calculated = CalculateScaled(character.Attack, group.Cells.Count, comboCount, multiplier);
                        var blocked = enemy.ComboShieldMinimum > 0 && comboCount <= enemy.ComboShieldMinimum;
                        var absorbed = !blocked && enemy.AbsorbedElement.HasValue && enemy.AbsorbedElement.Value == character.Element;
                        var damage = 0;
                        var absorbedHealing = 0;
                        if (absorbed) absorbedHealing = enemy.Heal(calculated);
                        else if (!blocked) damage = enemy.ApplyDamage(calculated);
                        attacks.Add(new AttackEvent(slot, character.Id, character.Element, layerIndex, group.Id,
                            group.Cells.Count, comboCount, calculated, damage, absorbed, blocked, absorbedHealing));
                    }
                }
            }

            return new CombatResolution(comboCount, attacks, heals, modifiers);
        }

        public static float GetAffinityMultiplier(ElementType attacker, ElementType defender)
        {
            EnemyRuntime.ValidateCombatElement(attacker, "attacker");
            EnemyRuntime.ValidateCombatElement(defender, "defender");
            if ((attacker == ElementType.Fire && defender == ElementType.Nature) ||
                (attacker == ElementType.Nature && defender == ElementType.Water) ||
                (attacker == ElementType.Water && defender == ElementType.Fire) ||
                (attacker == ElementType.Light && defender == ElementType.Dark) ||
                (attacker == ElementType.Dark && defender == ElementType.Light)) return 2f;
            if ((attacker == ElementType.Nature && defender == ElementType.Fire) ||
                (attacker == ElementType.Water && defender == ElementType.Nature) ||
                (attacker == ElementType.Fire && defender == ElementType.Water)) return 0.5f;
            return 1f;
        }

        // All positive combat quantities use midpoint-away-from-zero rounding, then saturate to Int32.MaxValue.
        public static int RoundToStateInt(double value)
        {
            if (double.IsNaN(value) || value < 0d) throw new ArgumentOutOfRangeException("value");
            if (double.IsPositiveInfinity(value) || value >= int.MaxValue) return int.MaxValue;
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static int CountGroups(BoardResolution resolution)
        {
            long total = 0;
            for (var index = 0; index < resolution.CascadeLayers.Count; index++) total += resolution.CascadeLayers[index].Groups.Count;
            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        private static int CalculateScaled(int baseValue, int orbCount, int comboCount, double additionalMultiplier)
        {
            if (baseValue == 0 || additionalMultiplier == 0d) return 0;
            var matchScale = 1d + 0.25d * Math.Max(0, orbCount - BoardState.MinimumMatchSize);
            var comboScale = 1d + 0.25d * Math.Max(0, comboCount - 1);
            var value = MultiplySaturating(baseValue, matchScale);
            value = MultiplySaturating(value, comboScale);
            value = MultiplySaturating(value, additionalMultiplier);
            return RoundToStateInt(value);
        }

        private static double GetLeaderMultiplier(PartyState party, int comboCount, List<CombatModifierEvent> events)
        {
            var leader = party.Leader;
            if (leader.IsBound) return 1d;
            var data = leader.LeaderSkillState;
            if (data == null || !LeaderRequirementsMet(data, party, comboCount)) return 1f;
            var multiplier = 1d;
            for (var index = 0; index < data.Effects.Length; index++)
            {
                var effect = data.Effects[index];
                if (effect.Type != SkillEffectType.AttackBoost) continue;
                var value = RequirePositiveMultiplier(effect.Payload.Multiplier);
                multiplier = MultiplySaturating(multiplier, value);
                events.Add(new CombatModifierEvent(CombatModifierSource.Leader, data.Id, value));
            }
            return multiplier;
        }

        private static bool LeaderRequirementsMet(LeaderSkillData data, PartyState party, int comboCount)
        {
            if (comboCount < data.MinimumComboCount || party.HpPercent < data.MinimumHpPercent) return false;
            var requiredElements = data.RequiredElements ?? new ElementType[0];
            for (var requiredIndex = 0; requiredIndex < requiredElements.Length; requiredIndex++)
            {
                var found = false;
                for (var memberIndex = 0; memberIndex < party.Members.Count; memberIndex++)
                    if (party.Members[memberIndex].Element == requiredElements[requiredIndex]) found = true;
                if (!found) return false;
            }
            var tags = data.RequiredTags ?? new string[0];
            for (var requiredIndex = 0; requiredIndex < tags.Length; requiredIndex++)
            {
                var found = false;
                for (var memberIndex = 0; memberIndex < party.Members.Count; memberIndex++)
                {
                    if (party.Members[memberIndex].HasTag(tags[requiredIndex])) found = true;
                }
                if (!found) return false;
            }
            return true;
        }

        private static double GetPassiveMultiplier(CharacterRuntime character, List<CombatModifierEvent> events)
        {
            var multiplier = 1d;
            for (var index = 0; index < character.PassiveEffectState.Length; index++)
            {
                var effect = character.PassiveEffectState[index];
                if (effect.Type != SkillEffectType.AttackBoost) continue;
                var value = RequirePositiveMultiplier(effect.Payload.Multiplier);
                multiplier = MultiplySaturating(multiplier, value);
                events.Add(new CombatModifierEvent(CombatModifierSource.Passive, character.PassiveId ?? character.Id, value));
            }
            return multiplier;
        }

        private static float RequirePositiveMultiplier(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
                throw new InvalidOperationException("Combat multiplier must be finite and non-negative.");
            return multiplier;
        }

        private static double MultiplySaturating(double left, double right)
        {
            if (left == 0d || right == 0d) return 0d;
            var product = left * right;
            return double.IsPositiveInfinity(product) || product >= double.MaxValue ? double.MaxValue : product;
        }
    }
}
