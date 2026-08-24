using PuzzleGame.Core.Persistence;

namespace PuzzleGame.Presentation.CoreIntegration
{
    /// <summary>In-memory ISaveStorage for tests and headless validation.</summary>
    public sealed class InMemorySaveStorage : ISaveStorage
    {
        public string Content { get; private set; }
        public string CorruptBackup { get; private set; }

        public InMemorySaveStorage(string initialContent = null)
        {
            Content = initialContent;
        }

        public SaveStorageReadResult Read()
        {
            return Content == null ? SaveStorageReadResult.Missing : SaveStorageReadResult.FromContent(Content);
        }

        public void WriteAtomic(string content)
        {
            Content = content;
        }

        public void BackupCorrupt()
        {
            CorruptBackup = Content;
            Content = null;
        }
    }
}
