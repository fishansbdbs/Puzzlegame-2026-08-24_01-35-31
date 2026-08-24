using System;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Tests.EditMode.Battle
{
    public sealed class EnemyAndSkillTests
    {
        [Test]
        public void Matching_groups_charge_skills_once_per_group_and_cap_at_requirement()
        {
            var skill = Skill("charged", OrbType.Fire, 2, BattleFixtures.Effect(SkillEffectType.Heal, amount: 1));
            var members = BattleFixtures.StandardMembers();
            members[0] = BattleFixtures.Character("caster", ElementType.Fire, skill: skill);
            var party = new PartyState(members);

            var result = new SkillEngine().ChargeFrom(BattleFixtures.TwoFireGroups(), party);

            Assert.That(members[0].CurrentCharge, Is.EqualTo(2));
            Assert.That(new SkillEngine().CanActivate(members[0]), Is.True);
            Assert.That(result.Events, Has.Count.EqualTo(1));
            Assert.That(result.Events[0].Amount, Is.EqualTo(2));
        }

        [Test]
        public void Skill_activation_fails_without_charge_and_preserves_state()
        {
            var skill = Skill("heal", OrbType.Fire, 2, BattleFixtures.Effect(SkillEffectType.Heal, amount: 20));
            var caster = BattleFixtures.Character("caster", ElementType.Fire, skill: skill);
            var party = PartyWithCaster(caster);
            party.ApplyDamage(10);
            var context = Context(party: party);

            var result = new SkillEngine().Activate(caster, context);

            Assert.That(new SkillEngine().CanActivate(caster), Is.False);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(SkillActivationFailure.NotCharged));
            Assert.That(result.Events, Is.Empty);
            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 10));
        }

        [Test]
        public void Successful_skill_activation_consumes_charge_and_emits_effects()
        {
            var skill = Skill("heal", OrbType.Fire, 1, BattleFixtures.Effect(SkillEffectType.Heal, amount: 8));
            var caster = BattleFixtures.Character("caster", ElementType.Fire, skill: skill);
            caster.AddCharge(10);
            var party = PartyWithCaster(caster);
            party.ApplyDamage(10);

            var result = new SkillEngine().Activate(caster, Context(party: party));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(caster.CurrentCharge, Is.Zero);
            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 2));
            Assert.That(result.Events.Single().Kind, Is.EqualTo(BattleEffectKind.Heal));
        }

        [Test]
        public void Orb_skill_effects_convert_create_and_remove_deterministically()
        {
            var effects = new[]
            {
                BattleFixtures.Effect(SkillEffectType.ConvertOrbs, source: OrbType.Fire, target: OrbType.Water),
                BattleFixtures.Effect(SkillEffectType.CreateOrbs, amount: 2, target: OrbType.Dark),
                BattleFixtures.Effect(SkillEffectType.RemoveOrbs, source: OrbType.Heart, target: OrbType.Nature)
            };
            var caster = ChargedCaster(effects);
            var board = BattleFixtures.StableBoard();
            // Keep the conversion/removal targets after the first two row-major cells used by CreateOrbs.
            board.Set(5, 4, OrbType.Fire);
            board.Set(4, 4, OrbType.Heart);

            var result = new SkillEngine().Activate(caster, Context(board: board, party: PartyWithCaster(caster)));

            Assert.That(result.Events.Select(item => item.Kind), Is.EqualTo(new[] { BattleEffectKind.ConvertOrbs, BattleEffectKind.CreateOrbs, BattleEffectKind.RemoveOrbs }));
            Assert.That(board.Get(5, 4), Is.EqualTo(OrbType.Water));
            Assert.That(board.Get(4, 4), Is.EqualTo(OrbType.Nature));
            Assert.That(result.Events[1].AffectedCount, Is.EqualTo(2));
        }

        [Test]
        public void Modifier_skill_effects_change_attack_move_time_countdown_and_shield()
        {
            var effects = new[]
            {
                BattleFixtures.Effect(SkillEffectType.AttackBoost, multiplier: 2f, turns: 2),
                BattleFixtures.Effect(SkillEffectType.ExtendMoveTime, amount: 3, turns: 2),
                BattleFixtures.Effect(SkillEffectType.DelayEnemies, amount: 2),
                BattleFixtures.Effect(SkillEffectType.ManipulateCountdown, amount: -1),
                BattleFixtures.Effect(SkillEffectType.Shield, multiplier: 0.5f, turns: 2)
            };
            var caster = ChargedCaster(effects);
            var party = PartyWithCaster(caster);
            var enemy = BattleFixtures.Enemy(ElementType.Fire, countdown: 3);
            var context = Context(party: party, enemy: enemy);

            var result = new SkillEngine().Activate(caster, context);

            Assert.That(result.Events, Has.Count.EqualTo(5));
            Assert.That(party.AttackMultiplier, Is.EqualTo(2f));
            Assert.That(context.MoveTimeSeconds, Is.EqualTo(13f));
            Assert.That(enemy.Countdown, Is.EqualTo(4));
            Assert.That(party.DamageTakenMultiplier, Is.EqualTo(0.5f));
        }

        [Test]
        public void Direct_damage_skill_clamps_enemy_hp_without_overflow()
        {
            var caster = ChargedCaster(new[] { BattleFixtures.Effect(SkillEffectType.DirectDamage, amount: int.MaxValue) });
            var enemy = BattleFixtures.Enemy(ElementType.Fire, hp: 20);

            var result = new SkillEngine().Activate(caster, Context(party: PartyWithCaster(caster), enemy: enemy));

            Assert.That(enemy.CurrentHp, Is.Zero);
            Assert.That(result.Events.Single().Amount, Is.EqualTo(20));
        }

        [Test]
        public void Skill_engine_rejects_null_inputs_and_malformed_effect_payloads()
        {
            var engine = new SkillEngine();
            Assert.That(() => engine.CanActivate(null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => engine.ChargeFrom(null, BattleFixtures.Party()), Throws.TypeOf<ArgumentNullException>());

            var malformed = Skill("bad", OrbType.Fire, 0, new SkillEffectData { Type = SkillEffectType.Heal, Payload = null });
            Assert.That(() => BattleFixtures.Character("bad", ElementType.Fire, skill: malformed), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Runtime_rejects_an_invalid_effect_before_it_can_enter_a_multi_effect_skill()
        {
            var effects = new[]
            {
                BattleFixtures.Effect(SkillEffectType.ConvertOrbs, source: OrbType.Fire, target: OrbType.Water),
                BattleFixtures.Effect(SkillEffectType.Heal, amount: 5),
                BattleFixtures.Effect(SkillEffectType.Shield, multiplier: 2f, turns: 1)
            };
            Assert.That(() => ChargedCaster(effects), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Zero_turn_effects_do_not_activate_and_one_turn_effects_clear_on_tick()
        {
            var party = BattleFixtures.Party();
            var character = party.Members[0];
            var enemy = BattleFixtures.Enemy(ElementType.Fire);
            var context = Context(party: party, enemy: enemy);

            party.ApplyAttackBoost(2f, 0, "zero");
            party.ApplyShield(0.5f, 0);
            character.Bind(0);
            enemy.SetDamageAbsorb(ElementType.Fire, 0);
            enemy.SetComboShield(3, 0);
            enemy.SetEnrage(2f, 0);
            context.BoardEffects.Lock(OrbType.Fire, 0);
            context.BoardEffects.AddPoison(2, 0);
            context.BoardEffects.AddHazard(2, 0);
            context.BoardEffects.AddBlocker(2, 0);
            context.ModifyMoveTime(3f, 0);

            Assert.That(party.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(party.DamageTakenMultiplier, Is.EqualTo(1f));
            Assert.That(character.IsBound, Is.False);
            Assert.That(enemy.AbsorbedElement, Is.Null);
            Assert.That(enemy.ComboShieldMinimum, Is.Zero);
            Assert.That(enemy.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(context.BoardEffects.IsLocked(OrbType.Fire), Is.False);
            Assert.That(context.BoardEffects.PoisonCount + context.BoardEffects.HazardCount + context.BoardEffects.BlockerCount, Is.Zero);
            Assert.That(context.MoveTimeSeconds, Is.EqualTo(10f));

            party.ApplyAttackBoost(2f, 1, "one");
            party.ApplyShield(0.5f, 1);
            character.Bind(1);
            enemy.SetDamageAbsorb(ElementType.Fire, 1);
            enemy.SetComboShield(3, 1);
            enemy.SetEnrage(2f, 1);
            context.BoardEffects.Lock(OrbType.Fire, 1);
            context.BoardEffects.AddPoison(2, 1);
            context.BoardEffects.AddHazard(2, 1);
            context.BoardEffects.AddBlocker(2, 1);
            context.ModifyMoveTime(3f, 1);

            party.TickTimedEffects();
            character.TickBind();
            enemy.TickTimedEffects();
            context.BoardEffects.TickTimedEffects();
            context.TickMoveTimeEffect();

            Assert.That(party.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(party.DamageTakenMultiplier, Is.EqualTo(1f));
            Assert.That(character.IsBound, Is.False);
            Assert.That(enemy.AbsorbedElement, Is.Null);
            Assert.That(enemy.ComboShieldMinimum, Is.Zero);
            Assert.That(enemy.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(context.BoardEffects.IsLocked(OrbType.Fire), Is.False);
            Assert.That(context.BoardEffects.PoisonCount + context.BoardEffects.HazardCount + context.BoardEffects.BlockerCount, Is.Zero);
            Assert.That(context.MoveTimeSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void Clear_apis_remove_all_timed_effects_immediately()
        {
            var party = BattleFixtures.Party();
            var character = party.Members[0];
            var enemy = BattleFixtures.Enemy(ElementType.Fire);
            var context = Context(party: party, enemy: enemy);
            party.ApplyAttackBoost(2f, 2, "clear");
            party.ApplyShield(0.5f, 2);
            character.Bind(2);
            enemy.SetDamageAbsorb(ElementType.Fire, 2);
            enemy.SetComboShield(3, 2);
            enemy.SetEnrage(2f, 2);
            context.BoardEffects.Lock(OrbType.Fire, 2);
            context.BoardEffects.AddPoison(2, 2);
            context.BoardEffects.AddHazard(2, 2);
            context.BoardEffects.AddBlocker(2, 2);
            context.ModifyMoveTime(3f, 2);

            party.ClearTimedEffects();
            character.ClearBind();
            enemy.ClearTimedEffects();
            context.BoardEffects.ClearTimedEffects();
            context.ClearMoveTimeEffect();

            Assert.That(party.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(party.DamageTakenMultiplier, Is.EqualTo(1f));
            Assert.That(character.IsBound, Is.False);
            Assert.That(enemy.AbsorbedElement, Is.Null);
            Assert.That(enemy.ComboShieldMinimum, Is.Zero);
            Assert.That(enemy.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(context.BoardEffects.PoisonCount + context.BoardEffects.HazardCount + context.BoardEffects.BlockerCount, Is.Zero);
            Assert.That(context.MoveTimeSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void Countdown_one_executes_then_resets_after_one_completed_resolution()
        {
            var action = Action("hit", 3, EnemyEffect(EnemyEffectType.Damage, amount: 7));
            var enemy = BattleFixtures.Enemy(ElementType.Fire, countdown: 1, actions: new[] { action });
            var party = BattleFixtures.Party();

            var outcome = new EnemyActionEngine().AdvanceAfterBoardResolution(enemy, Context(party: party, enemy: enemy));

            Assert.That(outcome.ExecutedActions, Has.Count.EqualTo(1));
            Assert.That(enemy.Countdown, Is.EqualTo(3));
            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 7));
        }

        [Test]
        public void Countdown_decrements_exactly_once_when_it_does_not_reach_zero()
        {
            var enemy = BattleFixtures.Enemy(ElementType.Fire, countdown: 3);
            var engine = new EnemyActionEngine();

            var outcome = engine.AdvanceAfterBoardResolution(enemy, Context(enemy: enemy));

            Assert.That(outcome.ExecutedActions, Is.Empty);
            Assert.That(enemy.Countdown, Is.EqualTo(2));
        }

        [Test]
        public void Countdown_manipulation_clamps_overshoot_at_zero_and_integer_maximum()
        {
            var enemy = BattleFixtures.Enemy(ElementType.Fire, countdown: 3);
            Assert.That(enemy.ModifyCountdown(-100), Is.EqualTo(0));
            Assert.That(enemy.ModifyCountdown(int.MaxValue), Is.EqualTo(int.MaxValue));
            Assert.That(enemy.ModifyCountdown(1), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Enemy_board_effects_are_observable_for_conversion_lock_poison_hazard_and_blocker()
        {
            var effects = new[]
            {
                EnemyEffect(EnemyEffectType.ConvertOrbs, source: OrbType.Fire, target: OrbType.Water),
                EnemyEffect(EnemyEffectType.LockOrbs, turns: 2, source: OrbType.Water),
                EnemyEffect(EnemyEffectType.Poison, amount: 2, turns: 3),
                EnemyEffect(EnemyEffectType.Hazard, amount: 3, turns: 2),
                EnemyEffect(EnemyEffectType.Blocker, amount: 4, turns: 1)
            };
            var enemy = EnemyWithEffects(effects);
            var board = BattleFixtures.StableBoard();
            board.Set(0, 0, OrbType.Fire);
            var context = Context(board: board, enemy: enemy);

            var result = new EnemyActionEngine().AdvanceAfterBoardResolution(enemy, context);

            Assert.That(result.Events, Has.Count.EqualTo(5));
            Assert.That(board.Get(0, 0), Is.EqualTo(OrbType.Water));
            Assert.That(context.BoardEffects.IsLocked(OrbType.Water), Is.True);
            Assert.That(context.BoardEffects.PoisonCount, Is.EqualTo(2));
            Assert.That(context.BoardEffects.HazardCount, Is.EqualTo(3));
            Assert.That(context.BoardEffects.BlockerCount, Is.EqualTo(4));
        }

        [Test]
        public void Enemy_player_effects_are_observable_for_damage_bind_and_timer_reduction()
        {
            var effects = new[]
            {
                EnemyEffect(EnemyEffectType.Damage, amount: 10),
                EnemyEffect(EnemyEffectType.Bind, amount: 2, turns: 3),
                EnemyEffect(EnemyEffectType.ReduceMoveTime, amount: 4, turns: 2)
            };
            var enemy = EnemyWithEffects(effects);
            var party = BattleFixtures.Party();
            var context = Context(party: party, enemy: enemy);

            new EnemyActionEngine().AdvanceAfterBoardResolution(enemy, context);

            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 10));
            Assert.That(party.Members.Count(item => item.BindTurns == 3), Is.EqualTo(2));
            Assert.That(context.MoveTimeSeconds, Is.EqualTo(6f));
        }

        [Test]
        public void Enemy_combat_effects_are_observable_for_absorb_combo_shield_and_enrage()
        {
            var effects = new[]
            {
                EnemyEffect(EnemyEffectType.DamageAbsorb, turns: 2, source: OrbType.Fire),
                EnemyEffect(EnemyEffectType.ComboShield, amount: 3, turns: 2),
                EnemyEffect(EnemyEffectType.Enrage, multiplier: 2f, turns: 3)
            };
            var enemy = EnemyWithEffects(effects);

            var result = new EnemyActionEngine().AdvanceAfterBoardResolution(enemy, Context(enemy: enemy));

            Assert.That(result.Events, Has.Count.EqualTo(3));
            Assert.That(enemy.AbsorbedElement, Is.EqualTo(ElementType.Fire));
            Assert.That(enemy.ComboShieldMinimum, Is.EqualTo(3));
            Assert.That(enemy.AttackMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void Enemy_countdown_effect_is_observable_and_can_force_zero()
        {
            var enemy = EnemyWithEffects(new[] { EnemyEffect(EnemyEffectType.ManipulateCountdown, amount: -5) }, reset: 3);

            var result = new EnemyActionEngine().AdvanceAfterBoardResolution(enemy, Context(enemy: enemy));

            Assert.That(result.Events.Single().Kind, Is.EqualTo(BattleEffectKind.ManipulateCountdown));
            Assert.That(enemy.Countdown, Is.Zero);
        }

        [Test]
        public void Enemy_runtime_rejects_semantically_malformed_actions_before_turn_execution()
        {
            var malformed = Action("malformed", 3,
                EnemyEffect(EnemyEffectType.Damage, amount: 7),
                EnemyEffect(EnemyEffectType.Enrage, multiplier: float.NaN, turns: 2));
            Assert.That(() => BattleFixtures.Enemy(ElementType.Fire, countdown: 1, actions: new[] { malformed }), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Enemy_runtime_rejects_malformed_actions_even_when_their_countdown_is_not_due()
        {
            var malformed = Action("malformed", 3,
                EnemyEffect(EnemyEffectType.Damage, amount: 7),
                EnemyEffect(EnemyEffectType.Enrage, multiplier: float.NaN, turns: 2));
            Assert.That(() => BattleFixtures.Enemy(ElementType.Fire, countdown: 3, actions: new[] { malformed }), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Enemy_runtime_and_action_results_do_not_alias_mutable_authored_data()
        {
            var action = Action("snapshot-action", 3, EnemyEffect(EnemyEffectType.Damage, amount: 7));
            var data = new EnemyData
            {
                Id = "snapshot-enemy",
                Element = ElementType.Fire,
                BaseStats = new StatBlock { Hp = 100, Attack = 10 },
                InitialCountdown = 1,
                Actions = new[] { action }
            };
            var enemy = new EnemyRuntime(data);
            data.Element = ElementType.Water;
            data.BaseStats.Hp = 1;
            action.Id = "mutated-before";
            action.ResetCountdown = 9;
            action.Effects[0].Payload.Amount = 99;
            enemy.Data.Actions[0].Effects[0].Payload.Amount = 88;
            var party = BattleFixtures.Party();

            var outcome = new EnemyActionEngine().AdvanceAfterBoardResolution(enemy, Context(party: party, enemy: enemy));
            action.Id = "mutated-after";
            action.Effects[0].Payload.Amount = 77;

            Assert.That(enemy.Data.Element, Is.EqualTo(ElementType.Fire));
            Assert.That(enemy.MaxHp, Is.EqualTo(100));
            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 7));
            Assert.That(enemy.Countdown, Is.EqualTo(3));
            Assert.That(outcome.ExecutedActions[0].Id, Is.EqualTo("snapshot-action"));
            Assert.That(outcome.ExecutedActions[0].Effects[0].Amount, Is.EqualTo(7));
        }

        [Test]
        public void Completed_resolution_ticks_old_effects_then_keeps_new_enemy_effects_at_full_duration()
        {
            var poison = EnemyEffect(EnemyEffectType.Poison, amount: 2, turns: 1);
            var enemy = BattleFixtures.Enemy(ElementType.Light, countdown: 1, actions: new[] { Action("poison", 3, poison) });
            enemy.SetEnrage(2f, 1);
            var party = BattleFixtures.Party();
            party.ApplyAttackBoost(2f, 1, "old");
            var context = Context(party: party, enemy: enemy);
            context.BoardEffects.AddHazard(2, 1);
            context.ModifyMoveTime(3f, 1);

            var result = new BattleEngine().CompleteBoardResolution(BattleFixtures.OneGroup(OrbType.Fire, 3), context);

            Assert.That(result.Combat.Attacks.Single().CalculatedDamage, Is.EqualTo(20));
            Assert.That(party.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(enemy.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(context.BoardEffects.HazardCount, Is.Zero);
            Assert.That(context.MoveTimeSeconds, Is.EqualTo(10f));
            Assert.That(context.BoardEffects.PoisonCount, Is.EqualTo(2));
            Assert.That(context.BoardEffects.PoisonTurns, Is.EqualTo(1));
        }

        [Test]
        public void One_turn_preexisting_shield_reduces_due_enemy_damage_then_expires()
        {
            var enemy = BattleFixtures.Enemy(ElementType.Light, countdown: 1,
                actions: new[] { Action("hit", 3, EnemyEffect(EnemyEffectType.Damage, amount: 10)) });
            var party = BattleFixtures.Party();
            party.ApplyShield(0.5f, 1);

            new BattleEngine().CompleteBoardResolution(BattleFixtures.OneGroup(OrbType.Fire, 3), Context(party: party, enemy: enemy));

            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 5));
            Assert.That(party.ShieldTurns, Is.Zero);
            Assert.That(party.DamageTakenMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void One_turn_preexisting_enrage_increases_due_enemy_damage_then_expires()
        {
            var enemy = BattleFixtures.Enemy(ElementType.Light, countdown: 1,
                actions: new[] { Action("hit", 3, EnemyEffect(EnemyEffectType.Damage, amount: 10)) });
            enemy.SetEnrage(2f, 1);
            var party = BattleFixtures.Party();

            new BattleEngine().CompleteBoardResolution(BattleFixtures.OneGroup(OrbType.Fire, 3), Context(party: party, enemy: enemy));

            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 20));
            Assert.That(enemy.EnrageTurns, Is.Zero);
            Assert.That(enemy.AttackMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Same_action_reapplication_replaces_captured_enrage_and_keeps_full_duration()
        {
            var enemy = BattleFixtures.Enemy(ElementType.Light, countdown: 1,
                actions: new[]
                {
                    Action("hit-and-reapply", 3,
                        EnemyEffect(EnemyEffectType.Damage, amount: 10),
                        EnemyEffect(EnemyEffectType.Enrage, multiplier: 3f, turns: 1))
                });
            enemy.SetEnrage(2f, 1);
            var party = BattleFixtures.Party();

            new BattleEngine().CompleteBoardResolution(BattleFixtures.OneGroup(OrbType.Fire, 3), Context(party: party, enemy: enemy));

            Assert.That(party.CurrentHp, Is.EqualTo(party.MaxHp - 20));
            Assert.That(enemy.EnrageTurns, Is.EqualTo(1));
            Assert.That(enemy.AttackMultiplier, Is.EqualTo(3f));
        }

        [Test]
        public void Enemy_conversion_uses_the_installed_final_board_not_the_stale_context_board()
        {
            var action = Action("convert", 3, EnemyEffect(EnemyEffectType.ConvertOrbs, source: OrbType.Light, target: OrbType.Dark));
            var enemy = BattleFixtures.Enemy(ElementType.Fire, countdown: 1, actions: new[] { action });
            var resolution = BattleFixtures.OneGroup(OrbType.Fire, 3);
            var stale = BattleFixtures.StableBoard();
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++) stale.Set(x, y, OrbType.Heart);
            var context = Context(stale, BattleFixtures.Party(), enemy);
            Assert.That(CountOrbs(resolution.FinalBoard, OrbType.Light), Is.GreaterThan(0));

            new BattleEngine().CompleteBoardResolution(resolution, context);

            Assert.That(CountOrbs(context.Board, OrbType.Light), Is.Zero);
            Assert.That(CountOrbs(context.Board, OrbType.Dark), Is.GreaterThan(0));
        }

        [Test]
        public void Defeated_party_skips_the_entire_completed_resolution()
        {
            var caster = BattleFixtures.Character("caster", ElementType.Fire,
                skill: Skill("charge", OrbType.Fire, 2, BattleFixtures.Effect(SkillEffectType.Heal, amount: 5)));
            var party = PartyWithCaster(caster);
            party.ApplyDamage(int.MaxValue);
            var enemy = BattleFixtures.Enemy(ElementType.Light, countdown: 2);
            var stale = BattleFixtures.StableBoard();
            var original = stale.Get(0, 0);
            var context = Context(stale, party, enemy);

            var result = new BattleEngine().CompleteBoardResolution(BattleFixtures.TwoFireGroups(), context);

            Assert.That(result.WasSkippedBecausePartyDefeated, Is.True);
            Assert.That(result.Combat.Attacks, Is.Empty);
            Assert.That(result.Combat.Heals, Is.Empty);
            Assert.That(result.Charge.Events, Is.Empty);
            Assert.That(result.EnemyTurn.ExecutedActions, Is.Empty);
            Assert.That(party.CurrentHp, Is.Zero);
            Assert.That(caster.CurrentCharge, Is.Zero);
            Assert.That(enemy.Countdown, Is.EqualTo(2));
            Assert.That(context.Board.Get(0, 0), Is.EqualTo(original));
        }

        [Test]
        public void Absorb_and_combo_shield_change_combat_results_without_negative_hp()
        {
            var enemy = BattleFixtures.Enemy(ElementType.Light, hp: 50);
            enemy.ApplyDamage(20);
            enemy.SetDamageAbsorb(ElementType.Fire, 2);
            var party = BattleFixtures.PartyWithElements(ElementType.Fire, ElementType.Water, ElementType.Nature,
                ElementType.Light, ElementType.Dark, attack: 50);

            var absorbed = new CombatCalculator().Resolve(BattleFixtures.OneGroup(OrbType.Fire, 3), party, enemy);
            Assert.That(absorbed.Attacks.Single().WasAbsorbed, Is.True);
            Assert.That(enemy.CurrentHp, Is.EqualTo(50));

            enemy.SetComboShield(2, 2);
            var blocked = new CombatCalculator().Resolve(BattleFixtures.OneGroup(OrbType.Water, 3), party, enemy);
            Assert.That(blocked.Attacks.Single().WasBlocked, Is.True);
            Assert.That(enemy.CurrentHp, Is.EqualTo(50));
        }

        [Test]
        public void Battle_engine_charges_skills_and_advances_enemy_once_per_turn()
        {
            var skill = Skill("charge", OrbType.Fire, 2, BattleFixtures.Effect(SkillEffectType.Heal, amount: 1));
            var caster = BattleFixtures.Character("caster", ElementType.Fire, skill: skill);
            var party = PartyWithCaster(caster);
            var enemy = BattleFixtures.Enemy(ElementType.Light, countdown: 3);
            var context = Context(party: party, enemy: enemy);

            var result = new BattleEngine().CompleteBoardResolution(BattleFixtures.TwoFireGroups(), context);

            Assert.That(result.Combat.Attacks, Has.Count.EqualTo(2));
            Assert.That(caster.CurrentCharge, Is.EqualTo(2));
            Assert.That(enemy.Countdown, Is.EqualTo(2));
        }

        [Test]
        public void Enemy_runtime_rejects_invalid_contract_state()
        {
            Assert.That(() => new EnemyRuntime(null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => BattleFixtures.Enemy(ElementType.Heart), Throws.TypeOf<ArgumentException>());
            Assert.That(() => BattleFixtures.Enemy(ElementType.Fire, countdown: 0), Throws.TypeOf<ArgumentException>());

            var malformed = Action("bad", 0, EnemyEffect(EnemyEffectType.Damage, amount: 1));
            Assert.That(() => BattleFixtures.Enemy(ElementType.Fire, actions: new[] { malformed }), Throws.TypeOf<ArgumentException>());
        }

        private static CharacterRuntime ChargedCaster(SkillEffectData[] effects)
        {
            var caster = BattleFixtures.Character("caster", ElementType.Fire, skill: Skill("all", OrbType.Fire, 1, effects));
            caster.AddCharge(1);
            return caster;
        }

        private static PartyState PartyWithCaster(CharacterRuntime caster)
        {
            var members = BattleFixtures.StandardMembers();
            members[0] = caster;
            return new PartyState(members);
        }

        private static SkillData Skill(string id, OrbType chargeOrb, int charge, params SkillEffectData[] effects)
        {
            return new SkillData { Id = id, ChargeElement = (ElementType)chargeOrb, ChargeRequired = charge, Effects = effects };
        }

        private static EnemyEffectData EnemyEffect(EnemyEffectType type, int amount = 0, float multiplier = 0f, int turns = 0,
            OrbType source = OrbType.Fire, OrbType target = OrbType.Water)
        {
            return new EnemyEffectData
            {
                Type = type,
                Payload = new EffectPayloadData { Amount = amount, Multiplier = multiplier, TurnCount = turns, SourceOrb = source, TargetOrb = target }
            };
        }

        private static EnemyActionData Action(string id, int reset, params EnemyEffectData[] effects)
        {
            return new EnemyActionData { Id = id, ResetCountdown = reset, Effects = effects };
        }

        private static EnemyRuntime EnemyWithEffects(EnemyEffectData[] effects, int reset = 3)
        {
            return BattleFixtures.Enemy(ElementType.Fire, countdown: 1, actions: new[] { Action("effects", reset, effects) });
        }

        private static BattleContext Context(BoardState board = null, PartyState party = null, EnemyRuntime enemy = null)
        {
            return new BattleContext(board ?? BattleFixtures.StableBoard(), party ?? BattleFixtures.Party(), enemy ?? BattleFixtures.Enemy(ElementType.Fire));
        }

        private static int CountOrbs(BoardState board, OrbType orbType)
        {
            var count = 0;
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                if (board.Get(x, y) == orbType) count++;
            return count;
        }
    }
}
