using System;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Tests.EditMode.Battle
{
    public sealed class CombatTests
    {
        [Test]
        public void Party_requires_exactly_five_non_null_characters()
        {
            Assert.That(() => new PartyState(new[] { BattleFixtures.Character("one", ElementType.Fire) }), Throws.TypeOf<ArgumentException>());

            var tooMany = BattleFixtures.StandardMembers().Concat(new[] { BattleFixtures.Character("six", ElementType.Fire) });
            Assert.That(() => new PartyState(tooMany), Throws.TypeOf<ArgumentException>());

            var members = BattleFixtures.StandardMembers();
            members[3] = null;
            Assert.That(() => new PartyState(members), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Party_aggregates_hp_and_clamps_damage_and_healing_at_state_boundaries()
        {
            var party = BattleFixtures.Party(hp: 10, attack: 1, recovery: 1);

            Assert.That(party.MaxHp, Is.EqualTo(50));
            Assert.That(party.ApplyDamage(int.MaxValue), Is.EqualTo(50));
            Assert.That(party.CurrentHp, Is.Zero);
            Assert.That(party.Heal(int.MaxValue), Is.EqualTo(50));
            Assert.That(party.CurrentHp, Is.EqualTo(50));
            Assert.That(() => party.ApplyDamage(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Character_runtime_rejects_heart_and_negative_stats()
        {
            Assert.That(() => BattleFixtures.Character("heart", ElementType.Heart), Throws.TypeOf<ArgumentException>());
            Assert.That(() => BattleFixtures.Character("bad", ElementType.Fire, hp: -1), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Two_separate_fire_groups_create_an_attack_for_each_eligible_character_per_group()
        {
            var party = BattleFixtures.PartyWithElements(ElementType.Fire, ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light);
            var result = BattleFixtures.Resolve(BattleFixtures.TwoFireGroups(), party, ElementType.Light);

            Assert.That(result.ComboCount, Is.EqualTo(2));
            Assert.That(result.Attacks, Has.Count.EqualTo(4));
            Assert.That(result.Attacks.Select(item => item.GroupId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(result.Attacks.All(item => item.ComboCount == 2), Is.True);
        }

        [Test]
        public void Cascade_layers_preserve_per_group_attacks_and_use_one_global_combo_count()
        {
            var party = BattleFixtures.PartyWithElements(ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light, ElementType.Dark, attack: 100);
            var result = BattleFixtures.Resolve(BattleFixtures.TwoLayerFireThenWater(), party, ElementType.Light);

            Assert.That(result.ComboCount, Is.EqualTo(2));
            Assert.That(result.Attacks, Has.Count.EqualTo(2));
            Assert.That(result.Attacks[0].CascadeLayerIndex, Is.EqualTo(0));
            Assert.That(result.Attacks[1].CascadeLayerIndex, Is.EqualTo(1));
            Assert.That(result.Attacks[0].Damage, Is.EqualTo(125));
            Assert.That(result.Attacks[1].Damage, Is.EqualTo(125));
        }

        [Test]
        public void Affinity_covers_advantage_disadvantage_mutual_light_dark_and_neutral()
        {
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Fire, ElementType.Nature), Is.EqualTo(2f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Nature, ElementType.Water), Is.EqualTo(2f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Water, ElementType.Fire), Is.EqualTo(2f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Nature, ElementType.Fire), Is.EqualTo(0.5f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Water, ElementType.Nature), Is.EqualTo(0.5f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Fire, ElementType.Water), Is.EqualTo(0.5f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Light, ElementType.Dark), Is.EqualTo(2f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Dark, ElementType.Light), Is.EqualTo(2f));
            Assert.That(CombatCalculator.GetAffinityMultiplier(ElementType.Fire, ElementType.Light), Is.EqualTo(1f));
            Assert.That(() => CombatCalculator.GetAffinityMultiplier(ElementType.Heart, ElementType.Fire), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Heart_match_heals_from_party_recovery_clamps_and_never_attacks()
        {
            var party = BattleFixtures.Party(hp: 10, attack: 100, recovery: 2);
            party.ApplyDamage(7);

            var result = BattleFixtures.Resolve(BattleFixtures.OneGroup(OrbType.Heart, 3), party, ElementType.Fire);

            Assert.That(result.Attacks, Is.Empty);
            Assert.That(result.Heals, Has.Count.EqualTo(1));
            Assert.That(result.TotalHealing, Is.EqualTo(7));
            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp));
        }

        [Test]
        public void Match_and_combo_scaling_round_half_away_from_zero()
        {
            var party = BattleFixtures.PartyWithElements(ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light, ElementType.Dark, attack: 2);

            var matchScaled = BattleFixtures.Resolve(BattleFixtures.OneGroup(OrbType.Fire, 4), party, ElementType.Light);
            Assert.That(matchScaled.Attacks[0].Damage, Is.EqualTo(3));

            var comboScaled = BattleFixtures.Resolve(BattleFixtures.FireAndHeartGroups(), party, ElementType.Light);
            Assert.That(comboScaled.Attacks[0].Damage, Is.EqualTo(3));
        }

        [Test]
        public void Only_slot_zero_can_supply_the_leader_multiplier()
        {
            var leader = BattleFixtures.Leader(3f);
            var members = BattleFixtures.StandardMembers(attack: 10);
            members[1] = BattleFixtures.Character("not-leader-slot", ElementType.Water, attack: 10, leader: leader);
            var nonLeaderSlot = BattleFixtures.Resolve(BattleFixtures.OneGroup(OrbType.Fire, 3), new PartyState(members), ElementType.Light);

            members = BattleFixtures.StandardMembers(attack: 10);
            members[0] = BattleFixtures.Character("leader-slot", ElementType.Fire, attack: 10, leader: leader);
            var leaderSlot = BattleFixtures.Resolve(BattleFixtures.OneGroup(OrbType.Fire, 3), new PartyState(members), ElementType.Light);

            Assert.That(nonLeaderSlot.Attacks[0].Damage, Is.EqualTo(10));
            Assert.That(leaderSlot.Attacks[0].Damage, Is.EqualTo(30));
            Assert.That(leaderSlot.Modifiers.Single().Source, Is.EqualTo(CombatModifierSource.Leader));
        }

        [Test]
        public void Bound_slot_zero_does_not_supply_a_leader_multiplier()
        {
            var members = BattleFixtures.StandardMembers(attack: 10);
            members[0] = BattleFixtures.Character("bound-leader", ElementType.Fire, attack: 10, leader: BattleFixtures.Leader(3f));
            members[0].Bind(1);

            var result = BattleFixtures.Resolve(BattleFixtures.OneGroup(OrbType.Fire, 3), new PartyState(members), ElementType.Light);

            Assert.That(result.Attacks, Is.Empty);
            Assert.That(result.Modifiers.Any(item => item.Source == CombatModifierSource.Leader), Is.False);
        }

        [Test]
        public void Passive_and_active_attack_boosts_emit_modifier_events_and_change_damage()
        {
            var passive = BattleFixtures.Effect(SkillEffectType.AttackBoost, multiplier: 2f);
            var members = BattleFixtures.StandardMembers(attack: 10);
            members[0] = BattleFixtures.Character("fire", ElementType.Fire, attack: 10, passive: new[] { passive });
            var party = new PartyState(members);
            party.ApplyAttackBoost(1.5f, 2, "active");

            var result = BattleFixtures.Resolve(BattleFixtures.OneGroup(OrbType.Fire, 3), party, ElementType.Light);

            Assert.That(result.Attacks[0].Damage, Is.EqualTo(30));
            Assert.That(result.Modifiers.Select(item => item.Source), Is.EquivalentTo(new[] { CombatModifierSource.ActiveSkill, CombatModifierSource.Passive }));
        }

        [Test]
        public void Passive_modifier_is_reported_once_even_when_it_modifies_multiple_attack_events()
        {
            var passive = BattleFixtures.Effect(SkillEffectType.AttackBoost, multiplier: 2f);
            var members = BattleFixtures.StandardMembers(attack: 10);
            members[0] = BattleFixtures.Character("fire", ElementType.Fire, attack: 10, passive: new[] { passive });

            var result = BattleFixtures.Resolve(BattleFixtures.TwoFireGroups(), new PartyState(members), ElementType.Light);

            Assert.That(result.Attacks, Has.Count.EqualTo(2));
            Assert.That(result.Attacks.All(item => item.Damage == 25), Is.True);
            Assert.That(result.Modifiers.Count(item => item.Source == CombatModifierSource.Passive), Is.EqualTo(1));
        }

        [Test]
        public void Combat_rejects_null_resolution_party_or_enemy()
        {
            var calculator = new CombatCalculator();
            var party = BattleFixtures.Party();
            var enemy = BattleFixtures.Enemy(ElementType.Fire);
            var resolution = BattleFixtures.OneGroup(OrbType.Fire, 3);

            Assert.That(() => calculator.Resolve(null, party, enemy), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => calculator.Resolve(resolution, null, enemy), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => calculator.Resolve(resolution, party, null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Extreme_finite_modifier_products_saturate_and_zero_attack_stays_zero()
        {
            var huge = BattleFixtures.Effect(SkillEffectType.AttackBoost, multiplier: float.MaxValue);
            var leader = BattleFixtures.Leader(float.MaxValue);
            var members = BattleFixtures.StandardMembers(attack: 1);
            members[0] = BattleFixtures.Character("huge", ElementType.Fire, attack: 1, leader: leader, passive: new[] { huge });
            var party = new PartyState(members);
            party.ApplyAttackBoost(float.MaxValue, 1, "huge-active");

            var nonzero = new CombatCalculator().Resolve(BattleFixtures.OneGroup(OrbType.Fire, 3), party,
                BattleFixtures.Enemy(ElementType.Light, hp: int.MaxValue));
            Assert.That(nonzero.Attacks.Single().CalculatedDamage, Is.EqualTo(int.MaxValue));

            members = BattleFixtures.StandardMembers(attack: 0);
            members[0] = BattleFixtures.Character("zero", ElementType.Fire, attack: 0, leader: leader, passive: new[] { huge });
            party = new PartyState(members);
            party.ApplyAttackBoost(float.MaxValue, 1, "huge-active");

            var zero = new CombatCalculator().Resolve(BattleFixtures.OneGroup(OrbType.Fire, 3), party,
                BattleFixtures.Enemy(ElementType.Light, hp: int.MaxValue));
            Assert.That(zero.Attacks.Single().CalculatedDamage, Is.Zero);
        }

        [Test]
        public void Character_runtime_snapshots_character_skill_leader_and_passive_authored_data()
        {
            var characterData = new CharacterData
            {
                Id = "snapshot",
                Element = ElementType.Fire,
                BaseRarity = 1,
                BaseStats = new StatBlock { Hp = 10, Attack = 10, Recovery = 2 }
            };
            var skill = new SkillData
            {
                Id = "heal",
                ChargeElement = ElementType.Fire,
                ChargeRequired = 0,
                Effects = new[] { BattleFixtures.Effect(SkillEffectType.Heal, amount: 5) }
            };
            var leader = BattleFixtures.Leader(2f);
            var passive = BattleFixtures.Effect(SkillEffectType.AttackBoost, multiplier: 3f);
            var runtime = new CharacterRuntime(characterData, skill, leader, new[] { passive });

            characterData.Element = ElementType.Water;
            characterData.BaseStats.Attack = 999;
            skill.Effects[0].Payload.Amount = 99;
            leader.Effects[0].Payload.Multiplier = 9f;
            passive.Payload.Multiplier = 10f;
            var exposed = runtime.Data;
            exposed.BaseStats.Attack = 777;
            runtime.ActiveSkill.Effects[0].Payload.Amount = 88;
            runtime.LeaderSkill.Effects[0].Payload.Multiplier = 8f;
            runtime.PassiveEffects[0].Payload.Multiplier = 7f;

            Assert.That(runtime.Data.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(runtime.Data.BaseStats.Attack, Is.EqualTo(10));
            Assert.That(runtime.ActiveSkill.Effects[0].Payload.Amount, Is.EqualTo(5));
            Assert.That(runtime.LeaderSkill.Effects[0].Payload.Multiplier, Is.EqualTo(2f));
            Assert.That(runtime.PassiveEffects[0].Payload.Multiplier, Is.EqualTo(3f));
        }
    }

    internal static class BattleFixtures
    {
        internal static CharacterRuntime[] StandardMembers(int hp = 10, int attack = 10, int recovery = 2)
        {
            return new[]
            {
                Character("fire", ElementType.Fire, hp, attack, recovery),
                Character("water", ElementType.Water, hp, attack, recovery),
                Character("nature", ElementType.Nature, hp, attack, recovery),
                Character("light", ElementType.Light, hp, attack, recovery),
                Character("dark", ElementType.Dark, hp, attack, recovery)
            };
        }

        internal static CharacterRuntime Character(string id, ElementType element, int hp = 10, int attack = 10, int recovery = 2,
            SkillData skill = null, LeaderSkillData leader = null, SkillEffectData[] passive = null)
        {
            return new CharacterRuntime(new CharacterData
            {
                Id = id,
                Element = element,
                BaseRarity = 1,
                BaseStats = new StatBlock { Hp = hp, Attack = attack, Recovery = recovery }
            }, skill, leader, passive);
        }

        internal static PartyState Party(int hp = 10, int attack = 10, int recovery = 2)
        {
            return new PartyState(StandardMembers(hp, attack, recovery));
        }

        internal static PartyState PartyWithElements(ElementType first, ElementType second, ElementType third, ElementType fourth, ElementType fifth, int attack = 10)
        {
            return new PartyState(new[]
            {
                Character("zero", first, attack: attack), Character("one", second, attack: attack),
                Character("two", third, attack: attack), Character("three", fourth, attack: attack),
                Character("four", fifth, attack: attack)
            });
        }

        internal static EnemyRuntime Enemy(ElementType element, int hp = 100000, int countdown = 3, EnemyActionData[] actions = null)
        {
            return new EnemyRuntime(new EnemyData
            {
                Id = "enemy",
                Element = element,
                BaseStats = new StatBlock { Hp = hp, Attack = 10, Recovery = 0 },
                InitialCountdown = countdown,
                Actions = actions ?? new[] { new EnemyActionData { Id = "wait", ResetCountdown = countdown, Effects = new EnemyEffectData[0] } }
            });
        }

        internal static LeaderSkillData Leader(float multiplier)
        {
            return new LeaderSkillData
            {
                Id = "leader",
                Effects = new[] { Effect(SkillEffectType.AttackBoost, multiplier: multiplier) }
            };
        }

        internal static SkillEffectData Effect(SkillEffectType type, int amount = 0, float multiplier = 0f, int turns = 0,
            OrbType source = OrbType.Fire, OrbType target = OrbType.Water)
        {
            return new SkillEffectData
            {
                Type = type,
                Payload = new EffectPayloadData
                {
                    Amount = amount,
                    Multiplier = multiplier,
                    TurnCount = turns,
                    SourceOrb = source,
                    TargetOrb = target
                }
            };
        }

        internal static CombatResolution Resolve(BoardResolution resolution, PartyState party, ElementType enemyElement)
        {
            return new CombatCalculator().Resolve(resolution, party, Enemy(enemyElement));
        }

        internal static BoardResolution OneGroup(OrbType type, int count)
        {
            var board = StableBoard();
            for (var x = 0; x < count; x++) board.Set(x, 0, type);
            return new BoardResolver().Resolve(board, new CyclingOrbSource());
        }

        internal static BoardResolution TwoFireGroups()
        {
            var board = StableBoard();
            for (var x = 0; x < 3; x++) board.Set(x, 0, OrbType.Fire);
            for (var x = 3; x < 6; x++) board.Set(x, 4, OrbType.Fire);
            return new BoardResolver().Resolve(board, new CyclingOrbSource());
        }

        internal static BoardResolution FireAndHeartGroups()
        {
            var board = StableBoard();
            for (var x = 0; x < 3; x++) board.Set(x, 0, OrbType.Fire);
            for (var x = 3; x < 6; x++) board.Set(x, 4, OrbType.Heart);
            return new BoardResolver().Resolve(board, new CyclingOrbSource());
        }

        internal static BoardResolution TwoLayerFireThenWater()
        {
            var board = StableBoard();
            board.Set(0, 0, OrbType.Fire);
            board.Set(0, 1, OrbType.Fire);
            board.Set(0, 2, OrbType.Fire);
            board.Set(0, 3, OrbType.Water);
            board.Set(1, 0, OrbType.Water);
            board.Set(2, 0, OrbType.Water);
            return new BoardResolver().Resolve(board, new CyclingOrbSource());
        }

        internal static BoardState StableBoard()
        {
            var board = new BoardState();
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                board.Set(x, y, (OrbType)((x + y + 1) % 6));
            return board;
        }
    }

    internal sealed class CyclingOrbSource : IOrbSource
    {
        private int next;

        public OrbType NextOrb()
        {
            var pattern = new[] { OrbType.Light, OrbType.Dark, OrbType.Heart, OrbType.Nature, OrbType.Water };
            return pattern[next++ % pattern.Length];
        }
    }
}
