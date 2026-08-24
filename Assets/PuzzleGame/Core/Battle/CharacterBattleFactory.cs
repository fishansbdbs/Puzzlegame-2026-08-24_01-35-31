using System;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Progression;

namespace PuzzleGame.Core.Battle
{
    public static class CharacterBattleFactory
    {
        public static CharacterRuntime Create(CharacterProgress progress, SkillData activeSkill,
            LeaderSkillData leaderSkill = null, PassiveData passive = null)
        {
            if (progress == null) throw new ArgumentNullException("progress");
            if (activeSkill == null) throw new ArgumentNullException("activeSkill");
            if (!string.Equals(progress.EffectiveActiveSkill.Id, activeSkill.Id, StringComparison.Ordinal))
                throw new ArgumentException("Active skill ID must match the progressed character.", "activeSkill");
            Validate(activeSkill, "activeSkill");
            Validate(leaderSkill, "leaderSkill");
            Validate(passive, "passive");
            var authored = progress.CreateAuthoredSnapshot();
            if (string.IsNullOrWhiteSpace(authored.LeaderSkillId) != (leaderSkill == null) ||
                leaderSkill != null && !string.Equals(authored.LeaderSkillId, leaderSkill.Id, StringComparison.Ordinal))
                throw new ArgumentException("Leader skill ID must match the progressed character.", "leaderSkill");
            if (string.IsNullOrWhiteSpace(authored.PassiveId) != (passive == null) ||
                passive != null && !string.Equals(authored.PassiveId, passive.Id, StringComparison.Ordinal))
                throw new ArgumentException("Passive ID must match the progressed character.", "passive");
            var stats = progress.CurrentStats;
            authored.BaseStats = new StatBlock { Hp = stats.Hp, Attack = stats.Attack, Recovery = stats.Recovery };
            var visuals = progress.CurrentVisuals;
            var visualSet = new VisualReferenceSet { PortraitKey = visuals.PortraitKey, CardArtKey = visuals.CardArtKey, ModelKey = visuals.ModelKey, VfxKey = visuals.VfxKey };
            return new CharacterRuntime(authored, progress.CreateEffectiveActiveSkillData(), leaderSkill, passive,
                progress.EffectiveRarity, visualSet);
        }

        private static void Validate(IIdentifiedData data, string parameterName)
        {
            if (data == null) return;
            var errors = ContractValidation.Validate(data);
            if (errors.Count > 0) throw new ArgumentException(errors[0], parameterName);
        }
    }
}
