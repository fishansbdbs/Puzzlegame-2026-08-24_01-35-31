using System;
using System.Globalization;
using System.IO;
using System.Text;
using PuzzleGame.Core.Persistence;

namespace PuzzleGame.Unity.Persistence
{
    internal interface IFileSaveNameSource
    {
        DateTime UtcNow { get; }
        string NextToken();
    }

    internal interface IFileSaveCommitter
    {
        void Commit(string temporaryPath, string targetPath, bool targetExists);
    }

    public sealed class FileSaveStorage : ISaveStorage
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private const long MaximumUtf8Bytes = (long)SaveSerializer.MaximumInputCharacters * 4L;
        private const int MaximumSiblingNameAttempts = 16;
        private readonly string filePath;
        private readonly string parentPath;
        private readonly IFileSaveNameSource nameSource;
        private readonly IFileSaveCommitter committer;

        public FileSaveStorage(string path)
            : this(path, new GuidFileSaveNameSource(), new AtomicFileSaveCommitter())
        {
        }

        internal FileSaveStorage(string path, IFileSaveNameSource nameSource)
            : this(path, nameSource, new AtomicFileSaveCommitter())
        {
        }

        internal FileSaveStorage(string path, IFileSaveNameSource nameSource, IFileSaveCommitter committer)
        {
            ValidateCallerPathIntent(path);
            if (nameSource == null) throw new ArgumentNullException("nameSource");
            if (committer == null) throw new ArgumentNullException("committer");
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
            this.nameSource = nameSource;
            this.committer = committer;
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
            for (var attempt = 0; attempt < MaximumSiblingNameAttempts; attempt++)
            {
                var temporaryPath = filePath + ".tmp-" + NextValidatedToken();
                FileStream stream;
                if (!TryCreateOwnedSibling(temporaryPath, out stream)) continue;
                var ownsTemporaryPath = true;
                try
                {
                    using (stream)
                    using (var writer = new StreamWriter(stream, StrictUtf8, 4096, true))
                    {
                        writer.Write(content);
                        writer.Flush();
                        stream.Flush(true);
                    }

                    var targetExists = File.Exists(filePath);
                    committer.Commit(temporaryPath, filePath, targetExists);
                    ownsTemporaryPath = false;
                    return;
                }
                finally
                {
                    if (ownsTemporaryPath && File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
            }

            throw new IOException("Could not allocate a unique temporary save sibling.");
        }

        public void BackupCorrupt()
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("The corrupt save source no longer exists.", filePath);
            Directory.CreateDirectory(parentPath);
            var timestamp = GetUtcTimestamp().ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
            for (var attempt = 0; attempt < MaximumSiblingNameAttempts; attempt++)
            {
                var backupPath = filePath + ".corrupt-" + timestamp + "-" + NextValidatedToken() + ".bak";
                FileStream backup;
                if (!TryCreateOwnedSibling(backupPath, out backup)) continue;
                var ownsBackupPath = true;
                try
                {
                    using (backup)
                    using (var source = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        source.CopyTo(backup);
                        backup.Flush(true);
                    }

                    ownsBackupPath = false;
                    return;
                }
                finally
                {
                    if (ownsBackupPath && File.Exists(backupPath)) File.Delete(backupPath);
                }
            }

            throw new IOException("Could not allocate a unique corrupt-save backup sibling.");
        }

        private static void ValidateCallerPathIntent(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Save file path is required.", "path");
            if (IsDirectorySeparator(path[path.Length - 1]))
                throw new ArgumentException("Save file path cannot end with a directory separator.", "path");

            var separatorIndex = Math.Max(path.LastIndexOf(Path.DirectorySeparatorChar), path.LastIndexOf(Path.AltDirectorySeparatorChar));
            var terminalComponent = path.Substring(separatorIndex + 1);
            if (terminalComponent == "." || terminalComponent == "..")
                throw new ArgumentException("Save file path cannot end with a directory marker.", "path");

            try
            {
                var callerRoot = Path.GetPathRoot(path);
                if (!string.IsNullOrEmpty(callerRoot) && string.Equals(path, callerRoot, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Save file path must identify a file, not a broad root.", "path");
            }
            catch (Exception exception) when (exception is NotSupportedException || exception is PathTooLongException)
            {
                throw new ArgumentException("Save file path is invalid.", "path", exception);
            }
        }

        private static bool IsDirectorySeparator(char character)
        {
            return character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar;
        }

        private static bool TryCreateOwnedSibling(string path, out FileStream stream)
        {
            try
            {
                stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
                return true;
            }
            catch (IOException)
            {
                stream = null;
                if (File.Exists(path) || Directory.Exists(path)) return false;
                throw;
            }
        }

        private string NextValidatedToken()
        {
            var token = nameSource.NextToken();
            Guid parsed;
            if (string.IsNullOrEmpty(token) || !Guid.TryParseExact(token, "N", out parsed))
                throw new InvalidOperationException("The save sibling name source returned an invalid token.");
            return token;
        }

        private DateTime GetUtcTimestamp()
        {
            var timestamp = nameSource.UtcNow;
            if (timestamp.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("The save sibling name source must return a UTC timestamp.");
            return timestamp;
        }

        private sealed class GuidFileSaveNameSource : IFileSaveNameSource
        {
            public DateTime UtcNow { get { return DateTime.UtcNow; } }

            public string NextToken()
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        private sealed class AtomicFileSaveCommitter : IFileSaveCommitter
        {
            public void Commit(string temporaryPath, string targetPath, bool targetExists)
            {
                if (targetExists) File.Replace(temporaryPath, targetPath, null);
                else File.Move(temporaryPath, targetPath);
            }
        }
    }
}
