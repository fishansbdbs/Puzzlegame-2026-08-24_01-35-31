using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Gacha;
using PuzzleGame.Core.Persistence;
using PuzzleGame.Core.Sample;
using PuzzleGame.Core.Progression;

namespace PuzzleGame.Tests.EditMode.Integration
{
    public sealed class CoreVerticalSliceTests
    {
        [Test]
        public void Factory_exercises_board_battle_skill_leader_passive_enemy_and_stage_rules()
        {
            var sample = VerticalSliceFactory.Create(24082026);

            {
                Assert.That(sample.CharacterCatalog, Has.Count.EqualTo(5));
                Assert.That(sample.CharacterCatalog.Select(item => item.Id).Distinct().Count(), Is.EqualTo(5));
                Assert.That(sample.Party.Members, Has.Count.EqualTo(5));
                Assert.That(sample.Party.Leader.Data.Id, Is.EqualTo(sample.CharacterCatalog[0].Id));
                Assert.That(sample.ActiveSkillExample.Succeeded, Is.True);
                Assert.That(sample.ActiveSkillExample.Events.Select(item => item.Kind), Contains.Item(BattleEffectKind.AttackBoost));
                Assert.That(sample.OpeningCascadeLayers, Has.Count.EqualTo(2));
                Assert.That(sample.OpeningCascadeLayers[0].Groups.Count(item => item.OrbType == OrbType.Fire), Is.EqualTo(2));
                Assert.That(sample.OpeningTurn.Combat.Attacks.Count(item => item.Element == ElementType.Fire), Is.EqualTo(2));
                Assert.That(sample.OpeningTurn.Combat.Attacks.Where(item => item.Element == ElementType.Fire).Select(item => item.GroupId).Distinct().Count(), Is.EqualTo(2));
                Assert.That(sample.OpeningTurn.Combat.TotalHealing, Is.GreaterThan(0));
                Assert.That(sample.OpeningTurn.EnemyTurn.ExecutedActions.Select(item => item.Id), Is.EqualTo(new[] { "boss-strike" }));
                Assert.That(sample.OpeningTurn.Combat.Modifiers.Select(item => item.Source), Contains.Item(CombatModifierSource.Leader));
                Assert.That(sample.OpeningTurn.Combat.Modifiers.Select(item => item.Source), Contains.Item(CombatModifierSource.Passive));
                Assert.That(sample.OpeningTurn.Combat.Modifiers.Select(item => item.Source), Contains.Item(CombatModifierSource.ActiveSkill));
                Assert.That(sample.StageSession.ConsumesStamina, Is.False);
                Assert.That(sample.StageSession.AuthoredStructure.WaveCount, Is.EqualTo(1));
                Assert.That(sample.StageSession.AuthoredStructure.GetEnemyCount(0), Is.EqualTo(1));
                Assert.That(sample.StageObjectiveExample.All(item => item.Earned), Is.True);
            }

            foreach (var character in sample.CharacterCatalog)
                Assert.That(ContractValidation.Validate(character), Is.Empty, character.Id);
            foreach (var skill in sample.SkillCatalog)
                Assert.That(ContractValidation.Validate(skill), Is.Empty, skill.Id);
            Assert.That(ContractValidation.Validate(sample.LeaderSkill), Is.Empty);
            Assert.That(ContractValidation.Validate(sample.EnemyDefinition), Is.Empty);
            Assert.That(ContractValidation.Validate(sample.Stage), Is.Empty);
            Assert.That(ContractValidation.Validate(sample.StandardBanner), Is.Empty);
            Assert.That(ContractValidation.Validate(sample.GatherInRotationOneBanner), Is.Empty);
            Assert.That(ContractValidation.Validate(sample.GatherInRotationTwoBanner), Is.Empty);
            Assert.That(ContractValidation.Validate(sample.EventDefinition), Is.Empty);
            foreach (var schedule in sample.ScheduleDefinitions)
                Assert.That(ContractValidation.Validate(schedule), Is.Empty, schedule.Id);
        }

        [Test]
        public void Factory_keeps_the_opening_board_immutable_and_tracks_the_authoritative_board_after_resolution()
        {
            var sample = VerticalSliceFactory.Create(24082026);
            var opening = sample.OpeningBoard;
            var previousCurrent = sample.CurrentBoard;
            AssertSnapshotEqualsBoard(opening, previousCurrent);

            var resolution = new BoardResolver().Resolve(previousCurrent, sample.OrbSource);
            sample.StageSession.CompleteBoardResolution(new BattleEngine(), resolution, sample.BattleContext);

            Assert.That(sample.CurrentBoard, Is.SameAs(sample.BattleContext.Board));
            Assert.That(sample.CurrentBoard, Is.Not.SameAs(previousCurrent));
            AssertSnapshotEqualsBoard(new BoardSnapshot(resolution.FinalBoard), sample.CurrentBoard);
            AssertSnapshotEqualsBoard(opening, previousCurrent);

            var catalogSession = sample.StageCatalog.CreateSession(sample.Stage.Id);
            Assert.That(catalogSession, Is.Not.SameAs(sample.StageSession));
            Assert.That(catalogSession.AuthoredStructure.WaveCount, Is.EqualTo(1));
            Assert.That(catalogSession.AuthoredStructure.GetEnemyId(0, 0), Is.EqualTo(sample.EnemyDefinition.Id));
        }

        [Test]
        public void Factory_demonstrates_duplicate_ascension_and_nonduplicate_six_star_awakening()
        {
            var sample = VerticalSliceFactory.Create(24082026);

            {
                Assert.That(sample.AscensionExample.PreviousAscension, Is.Zero);
                Assert.That(sample.AscensionExample.NewAscension, Is.EqualTo(1));
                Assert.That(sample.AscensionProgress.EffectiveActiveSkill.ChargeRequired, Is.LessThan(sample.SkillCatalog[0].ChargeRequired));
                Assert.That(sample.AwakeningProgress.Ascension, Is.Zero, "Awakening must not require duplicates.");
                Assert.That(sample.AwakeningExample.Succeeded, Is.True);
                Assert.That(sample.AwakeningProgress.EffectiveRarity, Is.EqualTo((int)Rarity.Awakened));
                Assert.That(sample.AwakeningProgress.CurrentVisuals.PortraitKey, Is.Not.EqualTo(sample.CharacterCatalog[0].BaseVisuals.PortraitKey));
                Assert.That(sample.AwakeningProgress.CurrentVisuals.CardArtKey, Is.Not.EqualTo(sample.CharacterCatalog[0].BaseVisuals.CardArtKey));
                Assert.That(sample.AwakeningProgress.CurrentVisuals.ModelKey, Is.Not.EqualTo(sample.CharacterCatalog[0].BaseVisuals.ModelKey));
                Assert.That(sample.AwakeningProgress.CurrentVisuals.VfxKey, Is.Not.EqualTo(sample.CharacterCatalog[0].BaseVisuals.VfxKey));
                Assert.That(sample.BossMechanicExample, Has.Count.EqualTo(1));
                Assert.That(sample.BossMechanicExample[0].Type, Is.EqualTo(EnemyEffectType.Enrage));
            }
        }

        [Test]
        public void Sample_materializes_the_exact_owned_progression_into_damage_and_charge_behavior()
        {
            var sample = VerticalSliceFactory.Create(24082026);
            var owned = sample.OwnedCharacters.GetProgress(sample.FeaturedCharacterId);
            var skill = sample.SkillCatalog[0];
            var leader = sample.LeaderSkill;
            var passive = sample.Passive;
            var progressed = CharacterBattleFactory.Create(owned, skill, leader, passive);
            var unprogressed = CharacterBattleFactory.Create(new CharacterProgress(sample.CharacterCatalog[0], skill), skill, leader, passive);

            Assert.That(owned.Level, Is.EqualTo(2));
            Assert.That(owned.Ascension, Is.EqualTo(1));
            Assert.That(sample.Party.Leader.Data.BaseStats.Attack, Is.EqualTo(owned.CurrentStats.Attack));
            Assert.That(sample.Party.Leader.ActiveSkill.ChargeRequired, Is.EqualTo(owned.EffectiveActiveSkill.ChargeRequired));

            var group = PuzzleGame.Tests.EditMode.Battle.BattleFixtures.OneGroup(OrbType.Fire, 3);
            var progressedMembers = PuzzleGame.Tests.EditMode.Battle.BattleFixtures.StandardMembers();
            var unprogressedMembers = PuzzleGame.Tests.EditMode.Battle.BattleFixtures.StandardMembers();
            progressedMembers[0] = progressed;
            unprogressedMembers[0] = unprogressed;
            var progressedParty = new PartyState(progressedMembers);
            var unprogressedParty = new PartyState(unprogressedMembers);
            var progressedDamage = new CombatCalculator().Resolve(group, progressedParty, PuzzleGame.Tests.EditMode.Battle.BattleFixtures.Enemy(ElementType.Fire)).Attacks[0].CalculatedDamage;
            var unprogressedDamage = new CombatCalculator().Resolve(group, unprogressedParty, PuzzleGame.Tests.EditMode.Battle.BattleFixtures.Enemy(ElementType.Fire)).Attacks[0].CalculatedDamage;

            var engine = new SkillEngine();
            var progressedResolutions = ChargeUntilReady(engine, group, progressedParty, progressed);
            var unprogressedResolutions = ChargeUntilReady(engine, group, unprogressedParty, unprogressed);
            Assert.That(progressedDamage, Is.GreaterThan(unprogressedDamage));
            Assert.That(progressedResolutions, Is.LessThan(unprogressedResolutions));
        }

        [Test]
        public void Factory_purchases_standard_and_both_final_gather_in_steps_before_pack_reveal()
        {
            var sample = VerticalSliceFactory.Create(24082026);

            {
                Assert.That(sample.StandardBanner.Type, Is.EqualTo(BannerType.Standard));
                Assert.That(sample.StandardSummonExample.GemCost, Is.EqualTo(150));
                Assert.That(sample.StandardSummonExample.Results, Has.Count.EqualTo(1));
                Assert.That(sample.GatherInRotationOneBanner.Steps.Last().PullCount, Is.EqualTo(10));
                Assert.That(sample.GatherInRotationOneBanner.Steps.Last().GuaranteedFiveStarFeaturedBoost, Is.True);
                Assert.That(sample.GatherInRotationOneSummonExample.Results, Has.Count.EqualTo(10));
                Assert.That(sample.GatherInRotationOneSummonExample.Results.Last().Rarity, Is.EqualTo(5));
                Assert.That(sample.GatherInRotationOneState.IsComplete, Is.True);
                Assert.That(sample.GatherInRotationTwoBanner.Steps.Last().GuaranteedFeaturedFiveStar, Is.True);
                Assert.That(sample.GatherInRotationTwoSummonExample.Results.Last().CharacterId, Is.EqualTo(sample.FeaturedCharacterId));
                Assert.That(sample.GatherInRotationTwoState.IsComplete, Is.True);
                Assert.That(sample.PackFlow.State, Is.EqualTo(PackSummonState.PreStart));
                Assert.That(sample.PackFlow.Batch.Results, Has.Count.EqualTo(10));
            }

            var states = new System.Collections.Generic.List<PackSummonState>();
            sample.PackFlow.Transitioned += item => states.Add(item.State);
            sample.PackFlow.Begin();
            sample.PackFlow.PresentPack();
            sample.PackFlow.StartPackRip();
            sample.PackFlow.OpenPack();
            sample.PackFlow.RevealAll();

            Assert.That(states.First(), Is.EqualTo(PackSummonState.PurchaseValidated));
            Assert.That(states, Contains.Item(PackSummonState.PackOpened));
            Assert.That(states.Count(item => item == PackSummonState.CardReady), Is.EqualTo(10));
            Assert.That(states.Count(item => item == PackSummonState.CardRevealed), Is.EqualTo(10));
            Assert.That(states.Last(), Is.EqualTo(PackSummonState.ResultsComplete));
        }

        [Test]
        public void Factory_roundtrips_a_complete_current_save_deterministically()
        {
            var sample = VerticalSliceFactory.Create(24082026);
            var profile = sample.SaveProfile;

            {
                Assert.That(sample.SaveRoundTrip.Status, Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(profile.Version, Is.EqualTo(SaveData.CurrentVersion));
                Assert.That(profile.Wallet.Gold, Is.GreaterThan(0));
                Assert.That(profile.Wallet.Gems, Is.GreaterThan(0));
                Assert.That(profile.Wallet.Tickets, Is.GreaterThan(0));
                Assert.That(profile.Wallet.UniversalDuplicateResource, Is.GreaterThan(0));
                Assert.That(profile.Wallet.EventCurrencies, Has.Length.EqualTo(1));
                Assert.That(profile.Characters, Has.Length.EqualTo(5));
                Assert.That(profile.Materials, Has.Length.GreaterThanOrEqualTo(1));
                Assert.That(profile.PartyCharacterIds, Has.Length.EqualTo(5));
                Assert.That(profile.Banners, Has.Length.EqualTo(3));
                Assert.That(profile.Stages, Has.Length.EqualTo(1));
                Assert.That(profile.Stages[0].Stars, Is.EqualTo(new[] { true, true, true }));
                Assert.That(new SaveSerializer().Serialize(sample.SaveRoundTrip.Data), Is.EqualTo(sample.SerializedSave));
                Assert.That(VerticalSliceFactory.Create(24082026).SerializedSave, Is.EqualTo(sample.SerializedSave));
            }

            profile.Wallet.Gold = 0;
            profile.Characters[0].CharacterId = "tampered";
            Assert.That(sample.SaveProfile.Wallet.Gold, Is.GreaterThan(0));
            Assert.That(sample.SaveProfile.Characters[0].CharacterId, Is.Not.EqualTo("tampered"));
        }

        [Test]
        public void Factory_schedules_every_supported_category_with_an_injected_fixed_clock()
        {
            var sample = VerticalSliceFactory.Create(24082026);

            Assert.That(sample.ScheduleDefinitions.Select(item => item.ContentType).Distinct(),
                Is.EquivalentTo(System.Enum.GetValues(typeof(ContentType)).Cast<ContentType>()));
            Assert.That(sample.ActiveScheduledContent.Select(item => item.ContentType).Distinct(),
                Is.EquivalentTo(System.Enum.GetValues(typeof(ContentType)).Cast<ContentType>()));
            Assert.That(sample.ActiveScheduledContent.Any(item => item.ContentType == ContentType.EventChapter), Is.True);
            Assert.That(sample.ActiveScheduledContent.All(item => item.ActiveStart <= sample.ScheduleNow && sample.ScheduleNow < item.ActiveEnd), Is.True);

            var schedules = sample.ScheduleDefinitions;
            schedules[0].ContentId = "tampered";
            Assert.That(sample.ScheduleDefinitions[0].ContentId, Is.Not.EqualTo("tampered"));
        }

        [Test]
        public void Repeated_factory_calls_are_deterministic_isolated_and_do_not_alias_authored_data()
        {
            var first = VerticalSliceFactory.Create(24082026);
            var second = VerticalSliceFactory.Create(24082026);

            AssertBoardsEqual(first.GeneratedBoard, second.GeneratedBoard);
            Assert.That(first.StandardSummonExample.Results.Select(item => item.CharacterId),
                Is.EqualTo(second.StandardSummonExample.Results.Select(item => item.CharacterId)));

            var originalSecondOrb = second.CurrentBoard.Get(0, 0);
            first.CurrentBoard.Set(0, 0, originalSecondOrb == OrbType.Fire ? OrbType.Water : OrbType.Fire);
            first.Party.ApplyDamage(1);
            var returnedCharacters = first.CharacterCatalog;
            returnedCharacters[0].DisplayName = "tampered";

            Assert.That(second.CurrentBoard.Get(0, 0), Is.EqualTo(originalSecondOrb));
            Assert.That(second.Party.CurrentHp, Is.Not.EqualTo(first.Party.CurrentHp));
            Assert.That(first.CharacterCatalog[0].DisplayName, Is.Not.EqualTo("tampered"));
            Assert.That(VerticalSliceFactory.Create(24082027).CharacterCatalog.Select(item => item.Id),
                Is.EqualTo(first.CharacterCatalog.Select(item => item.Id)));
        }

        private static void AssertBoardsEqual(BoardSnapshot first, BoardSnapshot second)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                Assert.That(second.Get(x, y), Is.EqualTo(first.Get(x, y)), "cell " + x + "," + y);
        }

        private static int ChargeUntilReady(SkillEngine engine, BoardResolution resolution, PartyState party, CharacterRuntime character)
        {
            var count = 0;
            while (!engine.CanActivate(character))
            {
                engine.ChargeFrom(resolution, party);
                count++;
                if (count > 10) Assert.Fail("Matching resolutions did not charge the active skill.");
            }
            return count;
        }

        private static void AssertSnapshotEqualsBoard(BoardSnapshot snapshot, BoardState board)
        {
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                Assert.That(board.Get(x, y), Is.EqualTo(snapshot.Get(x, y)), "cell " + x + "," + y);
        }
    }
}
