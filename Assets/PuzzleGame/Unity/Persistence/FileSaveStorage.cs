using System;
using System.Globalization;
using System.IO;
using System.Text;
using PuzzleGame.Core.Persistence;

namespace PuzzleGame.Unity.Persistence
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private const long MaximumUtf8Bytes = (long)SaveSerializer.MaximumInputCharacters * 4L;
        private readonly string filePath;
        private readonly string parentPath;

        public FileSaveStorage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Save file path is required.", "path");
            string resolved;
            try { resolved = Path.GetFullPath(path); }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                throw new ArgumentException("Save file path is invalid.", "path", exception);
            }

            var root = Path.GetPathRoot(resolved);
            if (string.Equals(resolved, root, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(Path.GetFileName(resolved)))
                throw new ArgumentException("Save file path must identify a file, not a broad root.", "path");
            if (Directory.Exists(resolved)) throw new ArgumentException("Save file path cannot identify a directory.", "path");
            var parent = Path.GetDirectoryName(resolved);
            if (string.IsNullOrEmpty(parent)) throw new ArgumentException("Save file path must have a parent directory.", "path");

            filePath = resolved;
            parentPath = parent;
        }

        public SaveStorageReadResult Read()
        {
            if (!File.Exists(filePath)) return SaveStorageReadResult.Missing;
            byte[] bytes;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > MaximumUtf8Bytes) return SaveStorageReadResult.InvalidText;
                bytes = new byte[(int)stream.Length];
                var offset = 0;
                while (offset < bytes.Length)
                {
                    var read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read == 0) throw new EndOfStreamException("Save file ended before its reported length.");
                    offset += read;
                }
            }
            try { return SaveStorageReadResult.FromContent(StrictUtf8.GetString(bytes)); }
            catch (DecoderFallbackException) { return SaveStorageReadResult.InvalidText; }
        }

        public void WriteAtomic(string content)
        {
            if (content == null) throw new ArgumentNullException("content");
            Directory.CreateDirectory(parentPath);
            var temporaryPath = filePath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream, StrictUtf8, 4096, true))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(filePath)) File.Replace(temporaryPath, filePath, null);
                else File.Move(temporaryPath, filePath);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        public void BackupCorrupt()
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("The corrupt save source no longer exists.", filePath);
            Directory.CreateDirectory(parentPath);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
            var backupPath = filePath + ".corrupt-" + timestamp + "-" + Guid.NewGuid().ToString("N") + ".bak";
            try
            {
                using (var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var backup = new FileStream(backupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    source.CopyTo(backup);
                    backup.Flush(true);
                }
            }
            catch
            {
                if (File.Exists(backupPath)) File.Delete(backupPath);
                throw;
            }
        }
    }
}
