using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;

namespace PuzzleGame.Core.Progression
{
    public static class ProgressionService
    {
        public const int MaximumAscension = 5;

        public static LevelApplicationResult ApplyExperience(CharacterProgress progress, int experience)
        {
            if (progress == null) throw new ArgumentNullException("progress");
            if (experience < 0) throw new ArgumentOutOfRangeException("experience", "Experience cannot be negative.");

            var curve = progress.Data.LevelCurve;
            var previousLevel = progress.Level;
            var cap = curve.MaxLevel == 1 ? 0 : curve.ExperienceRequiredByLevel[curve.ExperienceRequiredByLevel.Length - 1];
            var total = progress.TotalExperience;
            var accepted = Math.Min((long)experience, (long)cap - total);
            if (accepted < 0) accepted = 0;
            var nextTotal = checked(total + (int)accepted);
            var nextLevel = LevelForTotalExperience(curve, nextTotal);
            progress.SetExperience(nextTotal, nextLevel);
            return new LevelApplicationResult(previousLevel, nextLevel, (int)accepted, nextTotal, nextLevel == curve.MaxLevel, progress.CurrentStats);
        }

        public static DuplicateApplicationResult ApplyDuplicate(CharacterProgress progress, Wallet wallet)
        {
            if (progress == null) throw new ArgumentNullException("progress");
            if (wallet == null) throw new ArgumentNullException("wallet");
            var previous = progress.Ascension;
            if (previous == MaximumAscension)
            {
                var overflow = progress.Data.Ascension.OverflowUniversalResourceAmount;
                wallet.Add(WalletCurrencies.UniversalDuplicateResource, overflow);
                return new DuplicateApplicationResult(previous, previous, overflow, ProgressionCalculation.DeriveEffects(progress.Data, previous));
            }

            var next = previous + 1;
            progress.SetAscension(next);
            return new DuplicateApplicationResult(previous, next, 0, ProgressionCalculation.DeriveEffects(progress.Data, next));
        }

        public static bool CanAwaken(CharacterProgress progress, Wallet wallet, MaterialInventory materials)
        {
            return GetAwakeningFailure(progress, wallet, materials) == null;
        }

        public static AwakeningResult Awaken(CharacterProgress progress, Wallet wallet, MaterialInventory materials)
        {
            var failure = GetAwakeningFailure(progress, wallet, materials);
            if (failure != null) return new AwakeningResult(false, failure, progress.CurrentVisuals);

            var requirements = progress.Data.Awakening;
            if (!wallet.TrySpend(WalletCurrencies.Gold, requirements.GoldCost))
                throw new InvalidOperationException("Wallet changed during an awakening transaction.");
            if (!materials.TrySpend(requirements.Materials))
            {
                wallet.Add(WalletCurrencies.Gold, requirements.GoldCost);
                throw new InvalidOperationException("Material inventory changed during an awakening transaction.");
            }

            progress.SetAwakened();
            return new AwakeningResult(true, null, progress.CurrentVisuals);
        }

        private static string GetAwakeningFailure(CharacterProgress progress, Wallet wallet, MaterialInventory materials)
        {
            if (progress == null) throw new ArgumentNullException("progress");
            if (wallet == null) throw new ArgumentNullException("wallet");
            if (materials == null) throw new ArgumentNullException("materials");
            if (progress.IsAwakened) return "AlreadyAwakened";
            var requirements = progress.Data.Awakening;
            if (requirements.RequiredLevel == 0) return "NotConfigured";
            if (progress.Level < requirements.RequiredLevel) return "LevelTooLow";
            if (!wallet.CanAfford(WalletCurrencies.Gold, requirements.GoldCost)) return "InsufficientGold";
            if (!materials.CanAfford(requirements.Materials)) return "InsufficientMaterials";
            return null;
        }

        private static int LevelForTotalExperience(ProgressionCurveData curve, int totalExperience)
        {
            var level = 1;
            for (var index = 0; index < curve.ExperienceRequiredByLevel.Length; index++)
            {
                if (totalExperience < curve.ExperienceRequiredByLevel[index]) break;
                level++;
            }

            return level;
        }
    }

    public sealed class LevelApplicationResult
    {
        public LevelApplicationResult(int previousLevel, int newLevel, int appliedExperience, int totalExperience, bool isAtMaxLevel, StatSnapshot stats)
        {
            PreviousLevel = previousLevel;
            NewLevel = newLevel;
            AppliedExperience = appliedExperience;
            TotalExperience = totalExperience;
            IsAtMaxLevel = isAtMaxLevel;
            Stats = stats;
        }

        public int PreviousLevel { get; private set; }
        public int NewLevel { get; private set; }
        public int AppliedExperience { get; private set; }
        public int TotalExperience { get; private set; }
        public bool IsAtMaxLevel { get; private set; }
        public StatSnapshot Stats { get; private set; }
    }

    public sealed class DuplicateApplicationResult
    {
        public DuplicateApplicationResult(int previousAscension, int newAscension, int universalResourceGranted, AscensionEffectSnapshot effects)
        {
            PreviousAscension = previousAscension;
            NewAscension = newAscension;
            UniversalResourceGranted = universalResourceGranted;
            Effects = effects;
        }

        public int PreviousAscension { get; private set; }
        public int NewAscension { get; private set; }
        public int UniversalResourceGranted { get; private set; }
        public AscensionEffectSnapshot Effects { get; private set; }
    }

    public sealed class AwakeningResult
    {
        public AwakeningResult(bool succeeded, string failureCode, VisualSnapshot visuals)
        {
            Succeeded = succeeded;
            FailureCode = failureCode;
            Visuals = visuals;
        }

        public bool Succeeded { get; private set; }
        public string FailureCode { get; private set; }
        public VisualSnapshot Visuals { get; private set; }
    }

    public sealed class MaterialInventory
    {
        private readonly Dictionary<string, int> balances = new Dictionary<string, int>(StringComparer.Ordinal);

        public int GetBalance(string materialId)
        {
            ValidateMaterialId(materialId);
            int balance;
            return balances.TryGetValue(materialId, out balance) ? balance : 0;
        }

        public int Add(string materialId, int amount)
        {
            ValidateMaterialId(materialId);
            if (amount < 0) throw new ArgumentOutOfRangeException("amount", "Amount cannot be negative.");
            var current = GetBalance(materialId);
            int next;
            try { next = checked(current + amount); }
            catch (OverflowException) { throw new OverflowException("Material balance cannot exceed Int32.MaxValue."); }
            balances[materialId] = next;
            return next;
        }

        public bool CanAfford(IReadOnlyList<MaterialRequirementData> requirements)
        {
            ValidateRequirements(requirements);
            for (var index = 0; index < requirements.Count; index++)
                if (GetBalance(requirements[index].MaterialId) < requirements[index].Amount) return false;
            return true;
        }

        public bool TrySpend(IReadOnlyList<MaterialRequirementData> requirements)
        {
            if (!CanAfford(requirements)) return false;
            for (var index = 0; index < requirements.Count; index++)
            {
                var requirement = requirements[index];
                balances[requirement.MaterialId] = GetBalance(requirement.MaterialId) - requirement.Amount;
            }

            return true;
        }

        private static void ValidateRequirements(IReadOnlyList<MaterialRequirementData> requirements)
        {
            if (requirements == null) throw new ArgumentNullException("requirements");
            for (var index = 0; index < requirements.Count; index++)
            {
                var requirement = requirements[index];
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.MaterialId) || requirement.Amount <= 0)
                    throw new ArgumentException("Material requirements must have a nonblank ID and positive amount.", "requirements");
                for (var other = 0; other < index; other++)
                    if (string.Equals(requirements[other].MaterialId, requirement.MaterialId, StringComparison.Ordinal))
                        throw new ArgumentException("Material requirement IDs must be unique.", "requirements");
            }
        }

        private static void ValidateMaterialId(string materialId)
        {
            if (string.IsNullOrWhiteSpace(materialId)) throw new ArgumentException("Material ID is required.", "materialId");
        }
    }
}
