using System;

namespace PuzzleGame.Core.Persistence
{
    public interface ISaveStorage
    {
        SaveStorageReadResult Read();
        void WriteAtomic(string content);
        void BackupCorrupt();
    }

    public sealed class SaveStorageReadResult
    {
        private static readonly SaveStorageReadResult MissingResult = new SaveStorageReadResult(false, true, null);
        private static readonly SaveStorageReadResult InvalidTextResult = new SaveStorageReadResult(true, false, null);

        private SaveStorageReadResult(bool exists, bool isValidText, string content)
        {
            Exists = exists;
            IsValidText = isValidText;
            Content = content;
        }

        public bool Exists { get; private set; }
        public bool IsValidText { get; private set; }
        public string Content { get; private set; }

        public static SaveStorageReadResult Missing { get { return MissingResult; } }
        public static SaveStorageReadResult InvalidText { get { return InvalidTextResult; } }

        public static SaveStorageReadResult FromContent(string content)
        {
            if (content == null) throw new ArgumentNullException("content");
            return new SaveStorageReadResult(true, true, content);
        }
    }
}
