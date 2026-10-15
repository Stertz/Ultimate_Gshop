using System.Security.Cryptography;
using System.Text.Json;

namespace Ultimate_Gshop.Format.Tests;

public sealed class AssetCompatibilityTests
{
    [Fact]
    public void ResourceNamesPathsSizesAndBytesMatchTheOriginalInventory()
    {
        var root = FindRepositoryRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Docs", "baseline", "assets.json")));
        var baseline = manifest.RootElement;
        Assert.Equal(1, baseline.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("Resources", baseline.GetProperty("root").GetString());
        Assert.Contains("Resources", Directory.EnumerateDirectories(root).Select(Path.GetFileName), StringComparer.Ordinal);

        var expected = baseline.GetProperty("files").EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("path").GetString()!, entry => entry, StringComparer.Ordinal);
        var actual = Directory.EnumerateFiles(Path.Combine(root, "Resources"), "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(root, path).Replace('\\', '/'), path => path, StringComparer.Ordinal);

        Assert.Equal(baseline.GetProperty("fileCount").GetInt32(), expected.Count);
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), actual.Keys.Order(StringComparer.Ordinal));

        long totalBytes = 0;
        foreach (var (relativePath, entry) in expected)
        {
            var path = actual[relativePath];
            var length = new FileInfo(path).Length;
            using var stream = File.OpenRead(path);
            var hash = Convert.ToHexStringLower(SHA256.HashData(stream));
            if (length != entry.GetProperty("size").GetInt64() || hash != entry.GetProperty("sha256").GetString())
                Assert.Fail($"Asset changed: {relativePath}");
            totalBytes += length;
        }

        Assert.Equal(baseline.GetProperty("totalBytes").GetInt64(), totalBytes);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Ultimate_Gshop.csproj")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Run the asset compatibility test from a checkout of Ultimate Gshop.");
    }
}
