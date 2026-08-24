using System;
using System.Collections.Generic;
using PuzzleGame.Core.Economy;

namespace PuzzleGame.Core.Persistence
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public WalletSaveData Wallet = new WalletSaveData();
        public CharacterProgressSaveData[] Characters = Array.Empty<CharacterProgressSaveData>();
        public MaterialBalanceSaveData[] Materials = Array.Empty<MaterialBalanceSaveData>();
        public string[] PartyCharacterIds = { "", "", "", "", "" };
        public BannerRuntimeSaveData[] Banners = Array.Empty<BannerRuntimeSaveData>();
        public StageProgressSaveData[] Stages = Array.Empty<StageProgressSaveData>();
        public SettingsSaveData Settings = new SettingsSaveData();

        public static SaveData CreateDefault()
        {
            return new SaveData();
        }
    }

    [Serializable]
    public sealed class WalletSaveData
    {
        public int Gold;
        public int Gems;
        public int Tickets;
        public int UniversalDuplicateResource;
        public CurrencyBalanceSaveData[] EventCurrencies = Array.Empty<CurrencyBalanceSaveData>();
    }

    [Serializable]
    public sealed class CurrencyBalanceSaveData
    {
        public string CurrencyId = "";
        public int Balance;
    }

    [Serializable]
    public sealed class CharacterProgressSaveData
    {
        public string CharacterId = "";
        public int TotalExperience;
        public int Ascension;
        public bool Awakened;
    }

    [Serializable]
    public sealed class MaterialBalanceSaveData
    {
        public string MaterialId = "";
        public int Balance;
    }

    [Serializable]
    public sealed class BannerRuntimeSaveData
    {
        public string BannerId = "";
        public string RotationId = "";
        public int AuthoredStepCount;
        public int NextStepIndex;
    }

    [Serializable]
    public sealed class StageProgressSaveData
    {
        public string StageId = "";
        public bool IsCleared;
        public bool[] Stars = { false, false, false };
        public int BestFinalHpBasisPoints;
        public int BestBoardResolutionCount;
    }

    [Serializable]
    public sealed class SettingsSaveData
    {
        public int MusicVolumePercent = 100;
        public int EffectsVolumePercent = 100;
        public bool ReducedMotion;
        public bool VibrationEnabled = true;
    }

    internal static class SaveDataSnapshot
    {
        internal static SaveData CreateValidated(SaveData source)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (source.Version != SaveData.CurrentVersion)
                throw new ArgumentException("Only the current save version can be serialized.", "source");
            if (source.Wallet == null) throw new ArgumentException("Wallet state is required.", "source");
            if (source.Characters == null || source.Materials == null || source.PartyCharacterIds == null ||
                source.Banners == null || source.Stages == null || source.Settings == null)
                throw new ArgumentException("Save collections and settings cannot be null.", "source");

            ValidateCollectionSize(source.Wallet.EventCurrencies, "Event currency");
            ValidateCollectionSize(source.Characters, "Character");
            ValidateCollectionSize(source.Materials, "Material");
            ValidateCollectionSize(source.Banners, "Banner");
            ValidateCollectionSize(source.Stages, "Stage");
            if (source.PartyCharacterIds.Length != 5)
                throw new ArgumentException("Party state must contain exactly five slots.", "source");

            var result = new SaveData { Version = SaveData.CurrentVersion };
            result.Wallet = CloneWallet(source.Wallet);
            result.Characters = CloneCharacters(source.Characters);
            result.Materials = CloneMaterials(source.Materials);
            result.PartyCharacterIds = CloneParty(source.PartyCharacterIds, result.Characters);
            result.Banners = CloneBanners(source.Banners);
            result.Stages = CloneStages(source.Stages);
            result.Settings = CloneSettings(source.Settings);
            return result;
        }

        private static WalletSaveData CloneWallet(WalletSaveData source)
        {
            ValidateBalance(source.Gold, "Gold");
            ValidateBalance(source.Gems, "Gems");
            ValidateBalance(source.Tickets, "Tickets");
            ValidateBalance(source.UniversalDuplicateResource, "Universal duplicate resource");
            if (source.EventCurrencies == null) throw new ArgumentException("Event currencies cannot be null.", "source");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var eventCurrencies = new CurrencyBalanceSaveData[source.EventCurrencies.Length];
            for (var index = 0; index < eventCurrencies.Length; index++)
            {
                var entry = source.EventCurrencies[index];
                if (entry == null) throw new ArgumentException("Event currency entries cannot be null.", "source");
                RequireId(entry.CurrencyId, "Event currency ID");
                if (IsReservedCurrency(entry.CurrencyId))
                    throw new ArgumentException("Fixed wallet currencies cannot be duplicated as event currencies.", "source");
                if (!seen.Add(entry.CurrencyId)) throw new ArgumentException("Event currency IDs must be unique.", "source");
                ValidateBalance(entry.Balance, "Event currency");
                eventCurrencies[index] = new CurrencyBalanceSaveData { CurrencyId = entry.CurrencyId, Balance = entry.Balance };
            }
            Array.Sort(eventCurrencies, (left, right) => StringComparer.Ordinal.Compare(left.CurrencyId, right.CurrencyId));
            return new WalletSaveData
            {
                Gold = source.Gold,
                Gems = source.Gems,
                Tickets = source.Tickets,
                UniversalDuplicateResource = source.UniversalDuplicateResource,
                EventCurrencies = eventCurrencies
            };
        }

        private static CharacterProgressSaveData[] CloneCharacters(CharacterProgressSaveData[] source)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new CharacterProgressSaveData[source.Length];
            for (var index = 0; index < result.Length; index++)
            {
                var entry = source[index];
                if (entry == null) throw new ArgumentException("Character progress entries cannot be null.", "source");
                RequireId(entry.CharacterId, "Character ID");
                if (!seen.Add(entry.CharacterId)) throw new ArgumentException("Character IDs must be unique.", "source");
                if (entry.TotalExperience < 0) throw new ArgumentException("Character experience cannot be negative.", "source");
                if (entry.Ascension < 0 || entry.Ascension > 5) throw new ArgumentException("Character Ascension must be between zero and five.", "source");
                result[index] = new CharacterProgressSaveData
                {
                    CharacterId = entry.CharacterId,
                    TotalExperience = entry.TotalExperience,
                    Ascension = entry.Ascension,
                    Awakened = entry.Awakened
                };
            }
            Array.Sort(result, (left, right) => StringComparer.Ordinal.Compare(left.CharacterId, right.CharacterId));
            return result;
        }

        private static MaterialBalanceSaveData[] CloneMaterials(MaterialBalanceSaveData[] source)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new MaterialBalanceSaveData[source.Length];
            for (var index = 0; index < result.Length; index++)
            {
                var entry = source[index];
                if (entry == null) throw new ArgumentException("Material entries cannot be null.", "source");
                RequireId(entry.MaterialId, "Material ID");
                if (!seen.Add(entry.MaterialId)) throw new ArgumentException("Material IDs must be unique.", "source");
                ValidateBalance(entry.Balance, "Material");
                result[index] = new MaterialBalanceSaveData { MaterialId = entry.MaterialId, Balance = entry.Balance };
            }
            Array.Sort(result, (left, right) => StringComparer.Ordinal.Compare(left.MaterialId, right.MaterialId));
            return result;
        }

        private static string[] CloneParty(string[] source, CharacterProgressSaveData[] characters)
        {
            var owned = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < characters.Length; index++) owned.Add(characters[index].CharacterId);
            var assigned = new HashSet<string>(StringComparer.Ordinal);
            var result = new string[5];
            for (var index = 0; index < result.Length; index++)
            {
                var id = source[index];
                if (string.IsNullOrEmpty(id)) { result[index] = ""; continue; }
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Party character IDs cannot be whitespace.", "source");
                if (!owned.Contains(id)) throw new ArgumentException("Party slots must reference owned characters.", "source");
                if (!assigned.Add(id)) throw new ArgumentException("Party character IDs must be unique.", "source");
                result[index] = id;
            }
            return result;
        }

        private static BannerRuntimeSaveData[] CloneBanners(BannerRuntimeSaveData[] source)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new BannerRuntimeSaveData[source.Length];
            for (var index = 0; index < result.Length; index++)
            {
                var entry = source[index];
                if (entry == null) throw new ArgumentException("Banner state entries cannot be null.", "source");
                RequireId(entry.BannerId, "Banner ID");
                RequireId(entry.RotationId, "Rotation ID");
                if (!seen.Add(entry.BannerId)) throw new ArgumentException("Banner IDs must be unique.", "source");
                if (entry.AuthoredStepCount < 0) throw new ArgumentException("Banner step count cannot be negative.", "source");
                if (entry.NextStepIndex < 0 || entry.NextStepIndex > entry.AuthoredStepCount)
                    throw new ArgumentException("Banner next-step index is outside its authored shape.", "source");
                result[index] = new BannerRuntimeSaveData
                {
                    BannerId = entry.BannerId,
                    RotationId = entry.RotationId,
                    AuthoredStepCount = entry.AuthoredStepCount,
                    NextStepIndex = entry.NextStepIndex
                };
            }
            Array.Sort(result, (left, right) => StringComparer.Ordinal.Compare(left.BannerId, right.BannerId));
            return result;
        }

        private static StageProgressSaveData[] CloneStages(StageProgressSaveData[] source)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new StageProgressSaveData[source.Length];
            for (var index = 0; index < result.Length; index++)
            {
                var entry = source[index];
                if (entry == null) throw new ArgumentException("Stage progress entries cannot be null.", "source");
                RequireId(entry.StageId, "Stage ID");
                if (!seen.Add(entry.StageId)) throw new ArgumentException("Stage IDs must be unique.", "source");
                if (entry.Stars == null || entry.Stars.Length != 3) throw new ArgumentException("Stage progress must contain exactly three stars.", "source");
                if (!entry.IsCleared && (entry.Stars[0] || entry.Stars[1] || entry.Stars[2]))
                    throw new ArgumentException("An uncleared stage cannot have earned stars.", "source");
                if (entry.BestFinalHpBasisPoints < 0 || entry.BestFinalHpBasisPoints > 10000)
                    throw new ArgumentException("Best final HP must be between zero and 10000 basis points.", "source");
                if (entry.BestBoardResolutionCount < 0)
                    throw new ArgumentException("Best board-resolution count cannot be negative.", "source");
                result[index] = new StageProgressSaveData
                {
                    StageId = entry.StageId,
                    IsCleared = entry.IsCleared,
                    Stars = (bool[])entry.Stars.Clone(),
                    BestFinalHpBasisPoints = entry.BestFinalHpBasisPoints,
                    BestBoardResolutionCount = entry.BestBoardResolutionCount
                };
            }
            Array.Sort(result, (left, right) => StringComparer.Ordinal.Compare(left.StageId, right.StageId));
            return result;
        }

        private static SettingsSaveData CloneSettings(SettingsSaveData source)
        {
            if (source.MusicVolumePercent < 0 || source.MusicVolumePercent > 100 ||
                source.EffectsVolumePercent < 0 || source.EffectsVolumePercent > 100)
                throw new ArgumentException("Settings volume percentages must be between zero and 100.", "source");
            return new SettingsSaveData
            {
                MusicVolumePercent = source.MusicVolumePercent,
                EffectsVolumePercent = source.EffectsVolumePercent,
                ReducedMotion = source.ReducedMotion,
                VibrationEnabled = source.VibrationEnabled
            };
        }

        private static void ValidateCollectionSize<T>(T[] values, string name)
        {
            if (values == null) throw new ArgumentException(name + " collection cannot be null.", "source");
            if (values.Length > SaveSerializer.MaximumCollectionElements)
                throw new ArgumentException(name + " collection exceeds the supported entry limit.", "source");
        }

        private static void ValidateBalance(int value, string name)
        {
            if (value < 0) throw new ArgumentException(name + " balance cannot be negative.", "source");
        }

        private static void RequireId(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required.", "source");
        }

        private static bool IsReservedCurrency(string id)
        {
            return string.Equals(id, WalletCurrencies.Gold, StringComparison.Ordinal) ||
                   string.Equals(id, WalletCurrencies.Gems, StringComparison.Ordinal) ||
                   string.Equals(id, WalletCurrencies.Tickets, StringComparison.Ordinal) ||
                   string.Equals(id, WalletCurrencies.UniversalDuplicateResource, StringComparison.Ordinal);
        }
    }
}
