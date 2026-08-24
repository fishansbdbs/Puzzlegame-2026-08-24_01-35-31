using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PuzzleGame.Core.Persistence;
using PuzzleGame.Unity.Persistence;

namespace PuzzleGame.Tests.EditMode.Persistence
{
    public sealed class SaveServiceTests
    {
        [Test]
        public void Missing_storage_creates_an_unsaved_current_version_default()
        {
            var storage = new MemorySaveStorage();

            var result = new SaveService(storage).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.CreatedNew));
            Assert.That(result.Data.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(result.Data.PartyCharacterIds, Has.Length.EqualTo(5));
            Assert.That(result.Data.Settings.MusicVolumePercent, Is.EqualTo(100));
            Assert.That(result.Data.Settings.EffectsVolumePercent, Is.EqualTo(100));
            Assert.That(result.Data.Settings.VibrationEnabled, Is.True);
            Assert.That(storage.WriteCalls, Is.EqualTo(0));
            Assert.That(storage.BackupCalls, Is.EqualTo(0));
        }

        [Test]
        public void Complete_state_roundtrips_escaped_non_ascii_ids_and_every_persisted_subsystem()
        {
            var serializer = new SaveSerializer();
            var source = CompleteSave();
            var text = serializer.Serialize(source);
            var storage = new MemorySaveStorage(text);

            var result = new SaveService(storage, serializer).LoadOrCreate();
            var data = result.Data;

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(data.Wallet.Gold, Is.EqualTo(101));
            Assert.That(data.Wallet.Gems, Is.EqualTo(202));
            Assert.That(data.Wallet.Tickets, Is.EqualTo(3));
            Assert.That(data.Wallet.UniversalDuplicateResource, Is.EqualTo(4));
            Assert.That(data.Wallet.EventCurrencies.Single().CurrencyId, Is.EqualTo("event:\"ete\\夏"));
            Assert.That(data.Wallet.EventCurrencies.Single().Balance, Is.EqualTo(5));
            Assert.That(data.Characters.Single().CharacterId, Is.EqualTo("unit-雪\\\""));
            Assert.That(data.Characters.Single().TotalExperience, Is.EqualTo(250));
            Assert.That(data.Characters.Single().Ascension, Is.EqualTo(5));
            Assert.That(data.Characters.Single().Awakened, Is.True);
            Assert.That(data.Materials.Single().MaterialId, Is.EqualTo("crystal-Ω"));
            Assert.That(data.Materials.Single().Balance, Is.EqualTo(7));
            Assert.That(data.PartyCharacterIds[0], Is.EqualTo("unit-雪\\\""));
            Assert.That(data.PartyCharacterIds.Skip(1), Is.All.Empty);
            Assert.That(data.Banners.Single().BannerId, Is.EqualTo("banner-α"));
            Assert.That(data.Banners.Single().RotationId, Is.EqualTo("rotation-2"));
            Assert.That(data.Banners.Single().AuthoredStepCount, Is.EqualTo(10));
            Assert.That(data.Banners.Single().NextStepIndex, Is.EqualTo(9));
            Assert.That(data.Stages.Single().StageId, Is.EqualTo("stage-終"));
            CollectionAssert.AreEqual(new[] { true, true, false }, data.Stages.Single().Stars);
            Assert.That(data.Stages.Single().BestFinalHpBasisPoints, Is.EqualTo(7250));
            Assert.That(data.Stages.Single().BestBoardResolutionCount, Is.EqualTo(8));
            Assert.That(data.Settings.MusicVolumePercent, Is.EqualTo(81));
            Assert.That(data.Settings.EffectsVolumePercent, Is.EqualTo(62));
            Assert.That(data.Settings.ReducedMotion, Is.True);
            Assert.That(data.Settings.VibrationEnabled, Is.False);
        }

        [Test]
        public void Serialization_is_byte_deterministic_for_equivalent_reordered_state()
        {
            var serializer = new SaveSerializer();
            var first = CompleteSaveWithTwoEntries();
            var reordered = CompleteSaveWithTwoEntries();
            Array.Reverse(reordered.Wallet.EventCurrencies);
            Array.Reverse(reordered.Characters);
            Array.Reverse(reordered.Materials);
            Array.Reverse(reordered.Banners);
            Array.Reverse(reordered.Stages);

            var firstBytes = Encoding.UTF8.GetBytes(serializer.Serialize(first));
            var secondBytes = Encoding.UTF8.GetBytes(serializer.Serialize(reordered));

            CollectionAssert.AreEqual(firstBytes, secondBytes);
            StringAssert.StartsWith("{\"Version\":2,\"Wallet\":", Encoding.UTF8.GetString(firstBytes));
        }

        [Test]
        public void Missing_v2_optional_fields_and_arrays_receive_safe_defaults()
        {
            var result = new SaveService(new MemorySaveStorage("{\"Version\":2}")).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.Wallet.Gold, Is.EqualTo(0));
            Assert.That(result.Data.Wallet.EventCurrencies, Is.Empty);
            Assert.That(result.Data.Characters, Is.Empty);
            Assert.That(result.Data.Materials, Is.Empty);
            CollectionAssert.AreEqual(new[] { "", "", "", "" , "" }, result.Data.PartyCharacterIds);
            Assert.That(result.Data.Banners, Is.Empty);
            Assert.That(result.Data.Stages, Is.Empty);
            Assert.That(result.Data.Settings.MusicVolumePercent, Is.EqualTo(100));
        }

        [Test]
        public void Version_one_migrates_real_legacy_fields_and_defaults_v2_additions()
        {
            const string legacy = "{\"Version\":1,\"Gold\":11,\"Gems\":22,\"Characters\":[{\"CharacterId\":\"legacy-旧\",\"TotalExperience\":250,\"Ascension\":5,\"Awakened\":true}],\"PartyCharacterIds\":[\"legacy-旧\",\"\",\"\",\"\",\"\"],\"FutureAddition\":true}";

            var result = new SaveService(new MemorySaveStorage(legacy)).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Migrated));
            Assert.That(result.Data.Version, Is.EqualTo(2));
            Assert.That(result.Data.Wallet.Gold, Is.EqualTo(11));
            Assert.That(result.Data.Wallet.Gems, Is.EqualTo(22));
            Assert.That(result.Data.Wallet.Tickets, Is.EqualTo(0));
            Assert.That(result.Data.Wallet.UniversalDuplicateResource, Is.EqualTo(0));
            Assert.That(result.Data.Characters.Single().CharacterId, Is.EqualTo("legacy-旧"));
            Assert.That(result.Data.Characters.Single().Awakened, Is.True);
            Assert.That(result.Data.Materials, Is.Empty);
            Assert.That(result.Data.Banners, Is.Empty);
            Assert.That(result.Data.Stages, Is.Empty);
            Assert.That(result.Data.Settings.MusicVolumePercent, Is.EqualTo(100));
        }

        [Test]
        public void Unknown_same_version_fields_are_ignored_after_the_complete_value_is_bounded_and_parsed()
        {
            var result = new SaveService(new MemorySaveStorage("{\"Version\":2,\"Future\":{\"Nested\":[true,7.5,\"ok\"]}}")).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.Version, Is.EqualTo(2));
        }

        [Test]
        public void Valid_future_json_is_classified_unsupported_before_v2_semantic_mapping()
        {
            const string future = "{\"Version\":3,\"NewNumber\":1.5,\"Wallet\":\"not-a-v2-wallet\"}";
            var storage = new MemorySaveStorage(future);

            var result = new SaveService(storage).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.UnsupportedVersion));
            Assert.That(storage.BackupCalls, Is.EqualTo(0));
            Assert.That(storage.WriteCalls, Is.EqualTo(0));
        }

        [TestCase("")]
        [TestCase("{broken")]
        [TestCase("{\"Version\":2} trailing")]
        [TestCase("{\"Version\":2,\"Version\":2}")]
        [TestCase("{\"Version\":2,\"Wallet\":{\"Gold\":1,\"Gold\":2}}")]
        [TestCase("{\"Version\":2,\"Wallet\":{\"Gold\":1.0}}")]
        [TestCase("{\"Version\":2147483648}")]
        [TestCase("{\"Version\":2,\"Characters\":[{\"CharacterId\":\"\\uD800\",\"TotalExperience\":0,\"Ascension\":0,\"Awakened\":false}]}")]
        [TestCase("{\"Version\":2,\"Characters\":[{\"CharacterId\":\"\\q\",\"TotalExperience\":0,\"Ascension\":0,\"Awakened\":false}]}")]
        public void Corrupt_syntax_is_backed_up_once_and_returns_fresh_state_without_writing(string corrupt)
        {
            AssertRecoveredFromCorrupt(corrupt);
        }

        [TestCase("{\"Version\":0}")]
        [TestCase("{\"Version\":2,\"Wallet\":{\"Gold\":-1}}")]
        [TestCase("{\"Version\":2,\"Wallet\":{\"EventCurrencies\":[{\"CurrencyId\":\"event\",\"Balance\":1},{\"CurrencyId\":\"event\",\"Balance\":2}]}}")]
        [TestCase("{\"Version\":2,\"Characters\":[{\"CharacterId\":\"unit\",\"TotalExperience\":-1,\"Ascension\":0,\"Awakened\":false}]}")]
        [TestCase("{\"Version\":2,\"Characters\":[{\"CharacterId\":\"unit\",\"TotalExperience\":0,\"Ascension\":6,\"Awakened\":false}]}")]
        [TestCase("{\"Version\":2,\"Characters\":[{\"CharacterId\":\"unit\",\"TotalExperience\":0,\"Ascension\":0,\"Awakened\":false},{\"CharacterId\":\"unit\",\"TotalExperience\":0,\"Ascension\":0,\"Awakened\":false}]}")]
        [TestCase("{\"Version\":2,\"PartyCharacterIds\":[\"\",\"\",\"\",\"\"]}")]
        [TestCase("{\"Version\":2,\"PartyCharacterIds\":[\"missing\",\"\",\"\",\"\",\"\"]}")]
        [TestCase("{\"Version\":2,\"Materials\":[{\"MaterialId\":\"crystal\",\"Balance\":-1}]}")]
        [TestCase("{\"Version\":2,\"Materials\":[{\"MaterialId\":\"crystal\",\"Balance\":1},{\"MaterialId\":\"crystal\",\"Balance\":2}]}")]
        [TestCase("{\"Version\":2,\"Banners\":[{\"BannerId\":\"banner\",\"RotationId\":\"rotation\",\"AuthoredStepCount\":1,\"NextStepIndex\":2}]}")]
        [TestCase("{\"Version\":2,\"Banners\":[{\"BannerId\":\"banner\",\"RotationId\":\"rotation\",\"AuthoredStepCount\":-1,\"NextStepIndex\":0}]}")]
        [TestCase("{\"Version\":2,\"Banners\":[{\"BannerId\":\"banner\",\"RotationId\":\"rotation-a\",\"AuthoredStepCount\":1,\"NextStepIndex\":0},{\"BannerId\":\"banner\",\"RotationId\":\"rotation-b\",\"AuthoredStepCount\":1,\"NextStepIndex\":0}]}")]
        [TestCase("{\"Version\":2,\"Stages\":[{\"StageId\":\"stage\",\"IsCleared\":true,\"Stars\":[true,false],\"BestFinalHpBasisPoints\":0,\"BestBoardResolutionCount\":0}]}")]
        [TestCase("{\"Version\":2,\"Stages\":[{\"StageId\":\"stage\",\"IsCleared\":false,\"Stars\":[true,false,false],\"BestFinalHpBasisPoints\":0,\"BestBoardResolutionCount\":0}]}")]
        [TestCase("{\"Version\":2,\"Stages\":[{\"StageId\":\"stage\",\"IsCleared\":true,\"Stars\":[true,false,false],\"BestFinalHpBasisPoints\":10001,\"BestBoardResolutionCount\":0}]}")]
        [TestCase("{\"Version\":2,\"Settings\":{\"MusicVolumePercent\":101}}")]
        public void Impossible_supported_version_state_is_corrupt_instead_of_silently_normalized(string corrupt)
        {
            AssertRecoveredFromCorrupt(corrupt);
        }

        [Test]
        public void Excessive_nesting_and_input_size_are_rejected_before_mapping()
        {
            var nested = "0";
            for (var index = 0; index < SaveSerializer.MaximumNestingDepth + 1; index++) nested = "[" + nested + "]";
            AssertRecoveredFromCorrupt("{\"Version\":2,\"Future\":" + nested + "}");

            var oversized = "{\"Version\":2,\"Future\":\"" + new string('x', SaveSerializer.MaximumInputCharacters) + "\"}";
            AssertRecoveredFromCorrupt(oversized);
        }

        [Test]
        public void Parser_accepts_exact_array_element_limit_and_rejects_one_more()
        {
            var acceptedStorage = new MemorySaveStorage(BuildUnknownArraySave(10000));

            var accepted = new SaveService(acceptedStorage).LoadOrCreate();

            Assert.That(accepted.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(acceptedStorage.BackupCalls, Is.EqualTo(0));
            AssertRecoveredFromCorrupt(BuildUnknownArraySave(10001));
        }

        [Test]
        public void Parser_accepts_exact_object_member_limit_and_rejects_one_more()
        {
            var acceptedStorage = new MemorySaveStorage(BuildUnknownObjectSave(256));

            var accepted = new SaveService(acceptedStorage).LoadOrCreate();

            Assert.That(accepted.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(acceptedStorage.BackupCalls, Is.EqualTo(0));
            AssertRecoveredFromCorrupt(BuildUnknownObjectSave(257));
        }

        [Test]
        public void Empty_present_storage_and_invalid_text_are_corrupt_not_missing()
        {
            AssertRecoveredFromCorrupt("");
            var invalidText = new MemorySaveStorage(null) { HasInvalidText = true };

            var result = new SaveService(invalidText).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromCorrupt));
            Assert.That(invalidText.BackupCalls, Is.EqualTo(1));
        }

        [Test]
        public void Future_version_is_unsupported_not_corrupt_and_is_never_backed_up_or_overwritten()
        {
            const string future = "{\"Version\":3,\"Wallet\":\"not-a-v2-wallet\"}";
            var storage = new MemorySaveStorage(future);

            var result = new SaveService(storage).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.UnsupportedVersion));
            Assert.That(result.Data, Is.Null);
            Assert.That(storage.BackupCalls, Is.EqualTo(0));
            Assert.That(storage.WriteCalls, Is.EqualTo(0));
            Assert.That(storage.OriginalText, Is.EqualTo(future));
        }

        [Test]
        public void Save_validates_and_bounds_a_snapshot_before_calling_storage()
        {
            var invalid = CompleteSave();
            invalid.Wallet.Gems = -1;
            var invalidStorage = new MemorySaveStorage();
            Assert.That(() => new SaveService(invalidStorage).Save(invalid), Throws.TypeOf<ArgumentException>());
            Assert.That(invalidStorage.WriteCalls, Is.EqualTo(0));

            var oversized = CompleteSave();
            oversized.Wallet.EventCurrencies[0].CurrencyId = new string('x', SaveSerializer.MaximumInputCharacters);
            var oversizedStorage = new MemorySaveStorage();
            Assert.That(() => new SaveService(oversizedStorage).Save(oversized), Throws.InstanceOf<FormatException>());
            Assert.That(oversizedStorage.WriteCalls, Is.EqualTo(0));
        }

        [Test]
        public void Storage_read_write_and_backup_failures_propagate_without_false_success()
        {
            var readFailure = new MemorySaveStorage { ThrowOnRead = true };
            Assert.That(() => new SaveService(readFailure).LoadOrCreate(), Throws.TypeOf<IOException>());

            var writeFailure = new MemorySaveStorage { ThrowOnWrite = true };
            Assert.That(() => new SaveService(writeFailure).Save(CompleteSave()), Throws.TypeOf<IOException>());
            Assert.That(writeFailure.WriteCalls, Is.EqualTo(1));

            var backupFailure = new MemorySaveStorage("{broken") { ThrowOnBackup = true };
            Assert.That(() => new SaveService(backupFailure).LoadOrCreate(), Throws.TypeOf<IOException>());
            Assert.That(backupFailure.WriteCalls, Is.EqualTo(0));
        }

        [Test]
        public void Load_results_return_detached_snapshots_on_every_access()
        {
            var serializer = new SaveSerializer();
            var result = new SaveService(new MemorySaveStorage(serializer.Serialize(CompleteSave())), serializer).LoadOrCreate();
            var exposed = result.Data;
            exposed.Wallet.Gold = 999;
            exposed.Characters[0].CharacterId = "mutated";
            exposed.PartyCharacterIds[0] = "mutated";
            exposed.Stages[0].Stars[0] = false;

            var fresh = result.Data;
            Assert.That(fresh.Wallet.Gold, Is.EqualTo(101));
            Assert.That(fresh.Characters[0].CharacterId, Is.EqualTo("unit-雪\\\""));
            Assert.That(fresh.PartyCharacterIds[0], Is.EqualTo("unit-雪\\\""));
            Assert.That(fresh.Stages[0].Stars[0], Is.True);
        }

        private static void AssertRecoveredFromCorrupt(string corrupt)
        {
            var storage = new MemorySaveStorage(corrupt);

            var result = new SaveService(storage).LoadOrCreate();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromCorrupt));
            Assert.That(result.Data.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(storage.BackupCalls, Is.EqualTo(1));
            Assert.That(storage.Backups, Has.Count.EqualTo(1));
            Assert.That(storage.Backups[0], Is.EqualTo(corrupt));
            Assert.That(storage.WriteCalls, Is.EqualTo(0));
            Assert.That(storage.OriginalText, Is.EqualTo(corrupt));
        }

        private static string BuildUnknownArraySave(int elementCount)
        {
            var builder = new StringBuilder("{\"Version\":2,\"Future\":[");
            for (var index = 0; index < elementCount; index++)
            {
                if (index > 0) builder.Append(',');
                builder.Append('0');
            }
            return builder.Append("]}").ToString();
        }

        private static string BuildUnknownObjectSave(int memberCount)
        {
            var builder = new StringBuilder("{\"Version\":2,\"Future\":{");
            for (var index = 0; index < memberCount; index++)
            {
                if (index > 0) builder.Append(',');
                builder.Append('"').Append('p').Append(index).Append("\":0");
            }
            return builder.Append("}}").ToString();
        }

        private static SaveData CompleteSave()
        {
            return new SaveData
            {
                Version = SaveData.CurrentVersion,
                Wallet = new WalletSaveData
                {
                    Gold = 101,
                    Gems = 202,
                    Tickets = 3,
                    UniversalDuplicateResource = 4,
                    EventCurrencies = new[] { new CurrencyBalanceSaveData { CurrencyId = "event:\"ete\\夏", Balance = 5 } }
                },
                Characters = new[]
                {
                    new CharacterProgressSaveData { CharacterId = "unit-雪\\\"", TotalExperience = 250, Ascension = 5, Awakened = true }
                },
                Materials = new[] { new MaterialBalanceSaveData { MaterialId = "crystal-Ω", Balance = 7 } },
                PartyCharacterIds = new[] { "unit-雪\\\"", "", "", "", "" },
                Banners = new[]
                {
                    new BannerRuntimeSaveData { BannerId = "banner-α", RotationId = "rotation-2", AuthoredStepCount = 10, NextStepIndex = 9 }
                },
                Stages = new[]
                {
                    new StageProgressSaveData
                    {
                        StageId = "stage-終", IsCleared = true, Stars = new[] { true, true, false },
                        BestFinalHpBasisPoints = 7250, BestBoardResolutionCount = 8
                    }
                },
                Settings = new SettingsSaveData
                {
                    MusicVolumePercent = 81, EffectsVolumePercent = 62, ReducedMotion = true, VibrationEnabled = false
                }
            };
        }

        private static SaveData CompleteSaveWithTwoEntries()
        {
            var data = CompleteSave();
            data.Wallet.EventCurrencies = new[]
            {
                new CurrencyBalanceSaveData { CurrencyId = "event:z", Balance = 9 },
                new CurrencyBalanceSaveData { CurrencyId = "event:a", Balance = 8 }
            };
            data.Characters = new[]
            {
                new CharacterProgressSaveData { CharacterId = "unit-z", TotalExperience = 2 },
                new CharacterProgressSaveData { CharacterId = "unit-a", TotalExperience = 1 }
            };
            data.Materials = new[]
            {
                new MaterialBalanceSaveData { MaterialId = "z", Balance = 2 },
                new MaterialBalanceSaveData { MaterialId = "a", Balance = 1 }
            };
            data.PartyCharacterIds = new[] { "unit-z", "unit-a", "", "", "" };
            data.Banners = new[]
            {
                new BannerRuntimeSaveData { BannerId = "z", RotationId = "2", AuthoredStepCount = 2, NextStepIndex = 1 },
                new BannerRuntimeSaveData { BannerId = "a", RotationId = "1", AuthoredStepCount = 1, NextStepIndex = 0 }
            };
            data.Stages = new[]
            {
                new StageProgressSaveData { StageId = "z", IsCleared = true, Stars = new[] { true, false, false }, BestFinalHpBasisPoints = 1, BestBoardResolutionCount = 2 },
                new StageProgressSaveData { StageId = "a", IsCleared = false, Stars = new[] { false, false, false }, BestFinalHpBasisPoints = 0, BestBoardResolutionCount = 0 }
            };
            return data;
        }
    }

    public sealed class FileSaveStorageTests
    {
        [Test]
        public void Missing_read_does_not_create_the_target_parent()
        {
            using (var fixture = new TaskOwnedTempDirectory(create: false))
            {
                var target = Path.Combine(fixture.Root, "nested", "save.json");
                var result = new FileSaveStorage(target).Read();

                Assert.That(result.Exists, Is.False);
                Assert.That(Directory.Exists(Path.GetDirectoryName(target)), Is.False);
            }
        }

        [Test]
        public void Atomic_write_creates_only_the_parent_and_replaces_exact_target_with_utf8_without_bom()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                var target = Path.Combine(fixture.Root, "profile", "save.json");
                var storage = new FileSaveStorage(target);
                storage.WriteAtomic("first");
                storage.WriteAtomic("{\"id\":\"雪\"}");

                var read = storage.Read();
                var bytes = File.ReadAllBytes(target);
                Assert.That(read.Exists, Is.True);
                Assert.That(read.IsValidText, Is.True);
                Assert.That(read.Content, Is.EqualTo("{\"id\":\"雪\"}"));
                Assert.That(bytes.Take(3).ToArray(), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(target)), Has.Length.EqualTo(1));
            }
        }

        [Test]
        public void Corrupt_backups_are_timestamped_unique_and_never_overwrite_or_delete_the_source()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                var target = Path.Combine(fixture.Root, "save.json");
                File.WriteAllText(target, "{broken", new UTF8Encoding(false));
                var storage = new FileSaveStorage(target);

                storage.BackupCorrupt();
                storage.BackupCorrupt();

                Assert.That(File.ReadAllText(target), Is.EqualTo("{broken"));
                var backups = Directory.GetFiles(fixture.Root, "save.json.corrupt-*.bak");
                Assert.That(backups, Has.Length.EqualTo(2));
                Assert.That(backups[0], Is.Not.EqualTo(backups[1]));
                Assert.That(backups.Select(File.ReadAllText), Is.All.EqualTo("{broken"));
            }
        }

        [Test]
        public void Invalid_utf8_is_reported_as_present_invalid_text_and_can_be_backed_up_byte_for_byte()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                var target = Path.Combine(fixture.Root, "save.json");
                var bytes = new byte[] { 0x7B, 0x22, 0x78, 0x22, 0x3A, 0x22, 0xFF, 0x22, 0x7D };
                File.WriteAllBytes(target, bytes);
                var storage = new FileSaveStorage(target);

                var read = storage.Read();
                storage.BackupCorrupt();

                Assert.That(read.Exists, Is.True);
                Assert.That(read.IsValidText, Is.False);
                Assert.That(read.Content, Is.Null);
                var backup = Directory.GetFiles(fixture.Root, "save.json.corrupt-*.bak").Single();
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(backup));
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(target));
            }
        }

        [Test]
        public void Oversized_file_is_classified_invalid_before_allocating_unbounded_text()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                var target = Path.Combine(fixture.Root, "save.json");
                using (var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    stream.SetLength((long)SaveSerializer.MaximumInputCharacters * 4L + 1L);

                var read = new FileSaveStorage(target).Read();

                Assert.That(read.Exists, Is.True);
                Assert.That(read.IsValidText, Is.False);
                Assert.That(read.Content, Is.Null);
                Assert.That(new FileInfo(target).Length, Is.EqualTo((long)SaveSerializer.MaximumInputCharacters * 4L + 1L));
            }
        }

        [Test]
        public void Failed_replace_cleans_only_its_unique_temp_sibling()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                var target = Path.Combine(fixture.Root, "save.json");
                var storage = new FileSaveStorage(target);
                Directory.CreateDirectory(target);
                var unrelated = Path.Combine(fixture.Root, "save.json.tmp-unrelated");
                File.WriteAllText(unrelated, "keep");

                Assert.That(() => storage.WriteAtomic("new"), Throws.InstanceOf<IOException>());
                Assert.That(Directory.Exists(target), Is.True);
                Assert.That(File.ReadAllText(unrelated), Is.EqualTo("keep"));
                CollectionAssert.AreEqual(new[] { unrelated }, Directory.GetFiles(fixture.Root, "save.json.tmp-*"));
            }
        }

        [Test]
        public void Caller_paths_with_directory_terminal_markers_are_rejected_without_creating_anything()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                var terminalCurrentDirectory = Path.Combine(fixture.Root, "nonexistent-directory", ".");
                var terminalParentDirectory = Path.Combine(fixture.Root, "sub", "..");
                var trailingSeparator = Path.Combine(fixture.Root, "trailing") + Path.DirectorySeparatorChar;

                Assert.That(() => new FileSaveStorage(terminalCurrentDirectory), Throws.TypeOf<ArgumentException>());
                Assert.That(() => new FileSaveStorage(terminalParentDirectory), Throws.TypeOf<ArgumentException>());
                Assert.That(() => new FileSaveStorage(trailingSeparator), Throws.TypeOf<ArgumentException>());
                Assert.That(Directory.GetFileSystemEntries(fixture.Root), Is.Empty);
            }
        }

        [Test]
        public void Atomic_write_collision_preserves_unowned_sentinel_and_retries_with_a_fresh_name()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                const string collidedToken = "11111111111111111111111111111111";
                const string freshToken = "22222222222222222222222222222222";
                var target = Path.Combine(fixture.Root, "save.json");
                var collidedPath = target + ".tmp-" + collidedToken;
                var sentinel = new byte[] { 4, 2, 4, 2 };
                File.WriteAllBytes(collidedPath, sentinel);
                var names = new SequenceFileSaveNameSource(DateTime.UtcNow, collidedToken, freshToken);
                var storage = new FileSaveStorage(target, names);

                storage.WriteAtomic("new-save");

                CollectionAssert.AreEqual(sentinel, File.ReadAllBytes(collidedPath));
                Assert.That(File.ReadAllText(target), Is.EqualTo("new-save"));
                CollectionAssert.AreEquivalent(
                    new[] { collidedPath },
                    Directory.GetFiles(fixture.Root, "save.json.tmp-*"));
            }
        }

        [Test]
        public void Backup_collision_preserves_unowned_sentinel_and_retries_without_changing_source()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                const string collidedToken = "33333333333333333333333333333333";
                const string freshToken = "44444444444444444444444444444444";
                var timestamp = new DateTime(2031, 2, 3, 4, 5, 6, 7, DateTimeKind.Utc);
                var target = Path.Combine(fixture.Root, "save.json");
                var source = new byte[] { 0x7B, 0xFF, 0x7D };
                var sentinel = new byte[] { 9, 8, 7 };
                var collidedPath = target + ".corrupt-20310203-040506007-" + collidedToken + ".bak";
                var freshPath = target + ".corrupt-20310203-040506007-" + freshToken + ".bak";
                File.WriteAllBytes(target, source);
                File.WriteAllBytes(collidedPath, sentinel);
                var storage = new FileSaveStorage(
                    target,
                    new SequenceFileSaveNameSource(timestamp, collidedToken, freshToken));

                storage.BackupCorrupt();

                CollectionAssert.AreEqual(source, File.ReadAllBytes(target));
                CollectionAssert.AreEqual(sentinel, File.ReadAllBytes(collidedPath));
                CollectionAssert.AreEqual(source, File.ReadAllBytes(freshPath));
                CollectionAssert.AreEquivalent(
                    new[] { collidedPath, freshPath },
                    Directory.GetFiles(fixture.Root, "save.json.corrupt-*.bak"));
            }
        }

        [Test]
        public void Failed_existing_file_commit_preserves_source_bytes_and_cleans_only_owned_temp()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                const string freshToken = "55555555555555555555555555555555";
                var target = Path.Combine(fixture.Root, "save.json");
                var source = new byte[] { 0x7B, 0x22, 0x78, 0x22, 0x3A, 0xFF, 0x7D };
                File.WriteAllBytes(target, source);
                var committer = new ThrowingFileSaveCommitter();
                var storage = new FileSaveStorage(
                    target,
                    new SequenceFileSaveNameSource(DateTime.UtcNow, freshToken),
                    committer);

                Assert.That(() => storage.WriteAtomic("replacement"), Throws.TypeOf<IOException>());
                Assert.That(committer.Calls, Is.EqualTo(1));
                Assert.That(committer.TargetExisted, Is.True);
                Assert.That(committer.TemporaryExistedAtCommit, Is.True);
                CollectionAssert.AreEqual(source, File.ReadAllBytes(target));
                Assert.That(Directory.GetFiles(fixture.Root, "save.json.tmp-*"), Is.Empty);
            }
        }

        [Test]
        public void Constructor_rejects_blank_broad_and_existing_directory_paths_without_creating_anything()
        {
            using (var fixture = new TaskOwnedTempDirectory())
            {
                Assert.That(() => new FileSaveStorage(" "), Throws.TypeOf<ArgumentException>());
                Assert.That(() => new FileSaveStorage(Path.GetPathRoot(fixture.Root)), Throws.TypeOf<ArgumentException>());
                Assert.That(() => new FileSaveStorage(fixture.Root), Throws.TypeOf<ArgumentException>());
                Assert.That(Directory.GetFileSystemEntries(fixture.Root), Is.Empty);
            }
        }
    }

    internal sealed class MemorySaveStorage : ISaveStorage
    {
        internal MemorySaveStorage(string text = null)
        {
            Exists = text != null;
            OriginalText = text;
        }

        internal bool Exists;
        internal bool HasInvalidText;
        internal bool ThrowOnRead;
        internal bool ThrowOnWrite;
        internal bool ThrowOnBackup;
        internal int WriteCalls;
        internal int BackupCalls;
        internal string OriginalText;
        internal readonly List<string> Backups = new List<string>();

        public SaveStorageReadResult Read()
        {
            if (ThrowOnRead) throw new IOException("read failed");
            if (HasInvalidText) return SaveStorageReadResult.InvalidText;
            return Exists ? SaveStorageReadResult.FromContent(OriginalText) : SaveStorageReadResult.Missing;
        }

        public void WriteAtomic(string content)
        {
            WriteCalls++;
            if (ThrowOnWrite) throw new IOException("write failed");
            OriginalText = content;
            Exists = true;
        }

        public void BackupCorrupt()
        {
            BackupCalls++;
            if (ThrowOnBackup) throw new IOException("backup failed");
            Backups.Add(OriginalText);
        }
    }

    internal sealed class SequenceFileSaveNameSource : IFileSaveNameSource
    {
        private readonly Queue<string> tokens;

        internal SequenceFileSaveNameSource(DateTime utcNow, params string[] tokens)
        {
            UtcNow = utcNow;
            this.tokens = new Queue<string>(tokens);
        }

        public DateTime UtcNow { get; private set; }

        public string NextToken()
        {
            if (tokens.Count == 0) throw new InvalidOperationException("The deterministic name sequence was exhausted.");
            return tokens.Dequeue();
        }
    }

    internal sealed class ThrowingFileSaveCommitter : IFileSaveCommitter
    {
        internal int Calls;
        internal bool TargetExisted;
        internal bool TemporaryExistedAtCommit;

        public void Commit(string temporaryPath, string targetPath, bool targetExists)
        {
            Calls++;
            TargetExisted = targetExists;
            TemporaryExistedAtCommit = File.Exists(temporaryPath);
            throw new IOException("Injected atomic commit failure.");
        }
    }

    internal sealed class TaskOwnedTempDirectory : IDisposable
    {
        private const string Prefix = "PuzzleGame-Task9-";
        private readonly string tempRoot;

        internal TaskOwnedTempDirectory(bool create = true)
        {
            tempRoot = EnsureTrailingSeparator(Path.GetFullPath(Path.GetTempPath()));
            Root = Path.GetFullPath(Path.Combine(tempRoot, Prefix + Guid.NewGuid().ToString("N")));
            ValidateOwnedPath();
            if (create) Directory.CreateDirectory(Root);
        }

        internal string Root { get; private set; }

        public void Dispose()
        {
            ValidateOwnedPath();
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        private void ValidateOwnedPath()
        {
            if (!Root.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(Root).StartsWith(Prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Task-owned temporary path escaped its validated root.");
        }

        private static string EnsureTrailingSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }
}
