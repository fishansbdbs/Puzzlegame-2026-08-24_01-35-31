using System;
using NUnit.Framework;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Tests.EditMode.Contracts
{
    public sealed class ContractValidationTests
    {
        [Test]
        public void Character_rejects_heart_as_a_playable_element()
        {
            var data = CharacterData.CreateForTests("healer", ElementType.Heart, 3);

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Character element cannot be Heart.");
        }

        [Test]
        public void Stage_requires_at_least_one_wave_and_three_objectives()
        {
            var data = new StageData
            {
                Id = "stage-1",
                Waves = Array.Empty<WaveData>(),
                StarObjectives = Array.Empty<StarObjectiveData>()
            };

            var errors = ContractValidation.Validate(data);

            Assert.That(errors, Has.Count.GreaterThanOrEqualTo(2));
            CollectionAssert.Contains(errors, "Stage must define at least one wave.");
            CollectionAssert.Contains(errors, "Stage must define exactly three star objectives.");
        }
    }
}
