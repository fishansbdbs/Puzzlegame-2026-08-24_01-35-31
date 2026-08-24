using System;

namespace PuzzleGame.Core.Persistence
{
    public enum SaveLoadStatus
    {
        CreatedNew,
        Loaded,
        Migrated,
        RecoveredFromCorrupt,
        UnsupportedVersion
    }

    public sealed class SaveLoadResult
    {
        private readonly SaveData data;

        internal SaveLoadResult(SaveLoadStatus status, SaveData data)
        {
            Status = status;
            this.data = data == null ? null : SaveDataSnapshot.CreateValidated(data);
        }

        public SaveLoadStatus Status { get; private set; }
        public SaveData Data { get { return data == null ? null : SaveDataSnapshot.CreateValidated(data); } }
    }

    public sealed class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly SaveSerializer serializer;

        public SaveService(ISaveStorage storage)
            : this(storage, new SaveSerializer())
        {
        }

        public SaveService(ISaveStorage storage, SaveSerializer serializer)
        {
            this.storage = storage ?? throw new ArgumentNullException("storage");
            this.serializer = serializer ?? throw new ArgumentNullException("serializer");
        }

        public SaveLoadResult LoadOrCreate()
        {
            var read = storage.Read();
            if (read == null) throw new InvalidOperationException("Save storage returned no read result.");
            if (!read.Exists) return new SaveLoadResult(SaveLoadStatus.CreatedNew, SaveData.CreateDefault());
            if (!read.IsValidText) return RecoverFromCorrupt();

            try
            {
                var deserialized = serializer.Deserialize(read.Content);
                switch (deserialized.Status)
                {
                    case SaveDeserializationStatus.Loaded:
                        return new SaveLoadResult(SaveLoadStatus.Loaded, deserialized.Data);
                    case SaveDeserializationStatus.Migrated:
                        return new SaveLoadResult(SaveLoadStatus.Migrated, deserialized.Data);
                    case SaveDeserializationStatus.UnsupportedVersion:
                        return new SaveLoadResult(SaveLoadStatus.UnsupportedVersion, null);
                    default:
                        throw new InvalidOperationException("Save serializer returned an unknown status.");
                }
            }
            catch (FormatException)
            {
                return RecoverFromCorrupt();
            }
            catch (ArgumentException)
            {
                return RecoverFromCorrupt();
            }
        }

        public void Save(SaveData data)
        {
            var content = serializer.Serialize(data);
            storage.WriteAtomic(content);
        }

        private SaveLoadResult RecoverFromCorrupt()
        {
            storage.BackupCorrupt();
            return new SaveLoadResult(SaveLoadStatus.RecoveredFromCorrupt, SaveData.CreateDefault());
        }
    }
}
