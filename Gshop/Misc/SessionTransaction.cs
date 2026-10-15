using System.Text.Json;

namespace Ultimate_Gshop.Gshop.Misc;

internal static class SessionTransaction
{
    private const string JournalFolder = ".session-transaction";
    private static readonly HashSet<string> Names = ["gshop.data", "gshop_temp.data", "gshop_before.data", "tracking.json", "settings.json"];
    private static readonly object Gate = new();

    internal sealed record Entry(string Name, bool Existed);

    internal static T Read<T>(string directory, Func<T> read)
    {
        var root = Path.GetFullPath(directory);
        lock (Gate)
        {
            Directory.CreateDirectory(root);
            // A second process must not recover or replace this session while it is in use.
            using var sessionLock = new FileStream(Path.Combine(root, ".session.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Recover(root);
            return read();
        }
    }

    internal static void Write(string directory, IReadOnlyDictionary<string, Action<Stream>> writes,
        Action<int>? afterReplacement = null)
    {
        Read(directory, () =>
        {
            var root = Path.GetFullPath(directory);
            if (writes.Count == 0 || writes.Keys.Any(name => !Names.Contains(name)))
                throw new ArgumentException("A session transaction requires known session filenames.", nameof(writes));
            foreach (var name in writes.Keys)
            {
                var destination = Path.Combine(root, name);
                if (Directory.Exists(destination))
                    throw new IOException($"A directory blocks the session file: {name}");
                RejectLink(destination);
            }

            var journal = Path.Combine(root, JournalFolder);
            Directory.CreateDirectory(journal);
            try
            {
                var entries = new List<Entry>();
                foreach (var (name, write) in writes)
                {
                    WritePayload(Path.Combine(journal, name + ".next"), write);
                    var destination = Path.Combine(root, name);
                    var existed = File.Exists(destination);
                    if (existed)
                        CopyPayload(destination, Path.Combine(journal, name + ".previous"));
                    entries.Add(new Entry(name, existed));
                }

                // The journal becomes recoverable only after every new payload and backup has been flushed.
                WritePayload(Path.Combine(journal, "prepared.json"), stream => JsonSerializer.Serialize(stream, entries));
                File.Move(Path.Combine(journal, "prepared.json"), Path.Combine(journal, "pending.json"));
                var replacements = 0;
                foreach (var entry in entries)
                {
                    File.Move(Path.Combine(journal, entry.Name + ".next"), Path.Combine(root, entry.Name), overwrite: true);
                    afterReplacement?.Invoke(++replacements);
                }
                WritePayload(Path.Combine(journal, "resolved"), stream => stream.WriteByte(1));
            }
            catch
            {
                // If rollback itself fails, retain the journal for recovery on the next access.
                Recover(root);
                throw;
            }
            Cleanup(journal);
            return true;
        });
    }

    private static void Recover(string root)
    {
        var journal = Path.Combine(root, JournalFolder);
        if (!Directory.Exists(journal)) return;
        RejectLink(journal);
        RejectLink(Path.Combine(journal, "pending.json"));
        RejectLink(Path.Combine(journal, "resolved"));
        if (!File.Exists(Path.Combine(journal, "resolved")) && File.Exists(Path.Combine(journal, "pending.json")))
        {
            var entries = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(Path.Combine(journal, "pending.json")))
                ?? throw new InvalidDataException("The session recovery journal is empty.");
            if (entries.Count == 0 || entries.Count > Names.Count ||
                entries.Any(entry => entry is null || !Names.Contains(entry.Name)) ||
                entries.Select(entry => entry.Name).Distinct(StringComparer.Ordinal).Count() != entries.Count)
                throw new InvalidDataException("The session recovery journal contains invalid filenames.");

            // Check every backup before restoring any destination; never discard incomplete recovery evidence.
            foreach (var entry in entries.Where(entry => entry.Existed))
            {
                var previous = Path.Combine(journal, entry.Name + ".previous");
                RejectLink(previous);
                if (!File.Exists(previous))
                    throw new InvalidDataException($"Missing session recovery backup: {entry.Name}");
            }
            foreach (var entry in entries)
            {
                var destination = Path.Combine(root, entry.Name);
                RejectLink(destination);
                if (entry.Existed)
                    ProtectedFile.Write(destination, stream =>
                    {
                        using var input = File.OpenRead(Path.Combine(journal, entry.Name + ".previous"));
                        input.CopyTo(stream);
                    });
                else
                    File.Delete(destination);
            }
            // Interrupted cleanup must not attempt rollback after its backups have been deleted.
            WritePayload(Path.Combine(journal, "resolved"), stream => stream.WriteByte(1));
        }
        Cleanup(journal);
    }

    private static void Cleanup(string journal)
    {
        // Only remove this protocol's files, and never recursively delete a computed path.
        foreach (var name in Names)
        {
            File.Delete(Path.Combine(journal, name + ".next"));
            File.Delete(Path.Combine(journal, name + ".previous"));
        }
        File.Delete(Path.Combine(journal, "pending.json"));
        File.Delete(Path.Combine(journal, "prepared.json"));
        // Keep the commit marker until rollback evidence is gone, including across interrupted cleanup.
        File.Delete(Path.Combine(journal, "resolved"));
        Directory.Delete(journal);
    }

    private static void RejectLink(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Session files and recovery folders must not be symbolic links.");
    }

    private static void WritePayload(string path, Action<Stream> write)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        write(stream);
        stream.Flush(flushToDisk: true);
    }

    private static void CopyPayload(string source, string destination) => WritePayload(destination, stream =>
    {
        using var input = File.OpenRead(source);
        input.CopyTo(stream);
    });
}
