using System;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Progression;

namespace PuzzleGame.Core.Battle
{
    public static class CharacterBattleFactory
    {
        public static CharacterRuntime Create(CharacterProgress progress, SkillData activeSkill,
            LeaderSkillData leaderSkill = null, SkillEffectData[] passiveEffects = null)
        {
            if (progress == null) throw new ArgumentNullException("progress");
            if (activeSkill == null) throw new ArgumentNullException("activeSkill");
            if (!string.Equals(progress.EffectiveActiveSkill.Id, activeSkill.Id, StringComparison.Ordinal))
                throw new ArgumentException("Active skill ID must match the progressed character.", "activeSkill");
            var authored = progress.Data;
            var stats = progress.CurrentStats;
            authored.BaseStats = new StatBlock { Hp = stats.Hp, Attack = stats.Attack, Recovery = stats.Recovery };
            var visuals = progress.CurrentVisuals;
            var visualSet = new VisualReferenceSet { PortraitKey = visuals.PortraitKey, CardArtKey = visuals.CardArtKey, ModelKey = visuals.ModelKey, VfxKey = visuals.VfxKey };
            return new CharacterRuntime(authored, progress.CreateEffectiveActiveSkillData(), leaderSkill, passiveEffects,
                progress.EffectiveRarity, visualSet);
        }
    }
}
