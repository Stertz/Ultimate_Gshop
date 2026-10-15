namespace Ultimate_Gshop.Gshop.Misc;

internal static class ProtectedFile
{
    internal static void Write(string path, Action<Stream> write)
    {
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        var temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        var ownsTemporaryFile = false;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                ownsTemporaryFile = true;
                write(stream);
                stream.Flush(flushToDisk: true);
            }

            // Replace only after the complete payload has been written and flushed.
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (ownsTemporaryFile && File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
