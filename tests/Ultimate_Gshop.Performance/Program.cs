using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Ultimate_Gshop.Format.Tests;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.ViewModels;

#if DEBUG
throw new InvalidOperationException("Run performance measurements in Release configuration.");
#endif

const int warmups = 3;
const int iterations = 15;
var output = Path.GetFullPath(args.Length == 0 ? "TestResults/performance/latest.json" : args.Single());
var results = new List<Measurement>();
var memoryProfiles = new List<MemoryProfile>();
var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"ultimate-gshop-perf-{Guid.NewGuid():N}"));
Directory.CreateDirectory(tempRoot);
try
{
    foreach (var size in new[] { 1_000, 10_000, 50_000 })
    {
        var shop = ShopFixture.Create(size);
        var tracking = new ShopTracking();
        var settings = new GshopSettings();
        var root = Path.Combine(tempRoot, size.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var selection = new ItemSelection();
        var expectedNames = shop.Itens.Count(item => item.Name.Contains("Item", StringComparison.OrdinalIgnoreCase) && item.Name.Contains("999", StringComparison.OrdinalIgnoreCase));
        var expectedIds = shop.Itens.Count(item => item.Name.Contains("100", StringComparison.OrdinalIgnoreCase) || item.ID.ToString(System.Globalization.CultureInfo.InvariantCulture).Contains("100"));

        Measure("search-all", size, () => FilterItems.Handle(shop.Itens, -1, -1, ""),
            value => value.Count == size);
        Measure("search-name", size, () => FilterItems.Handle(shop.Itens, -1, -1, "Item 999"),
            value => value.Count == expectedNames);
        Measure("search-id", size, () => FilterItems.Handle(shop.Itens, -1, -1, "100"),
            value => value.Count == expectedIds);
        Measure("selection-full-range", size, () =>
        {
            selection.Reset();
            selection.Select(shop.Itens, shop.Itens[0], extend: false, toggle: false);
            selection.Select(shop.Itens, shop.Itens[^1], extend: true, toggle: false);
            return selection.Items;
        }, value => value.Count == size && ReferenceEquals(value[^1], shop.Itens[^1]));
        Measure("grid-snapshot", size, () => new GridViewModel(shop.Itens),
            value => value.ItemCount == size && value.Items[0].Name == shop.Itens[0].Name && value.Items[^1].ID == shop.Itens[^1].ID);
        MeasureSave("session-save-empty-history", size);
        // Fixed history size separates item-count scaling from history growth.
        for (var index = 0; index < 100; index++) tracking.ItemUpdated(index, shop.Itens[index]);
        Measure("session-snapshot-100-history", size, () => SessionSnapshot.Capture(shop, tracking, settings),
            value => value is not null);
        MeasureSave("session-save-100-history", size);
        Measure("session-load", size, () => ContinueGshop.Handle(root),
            value => value.Itens.Count == size && value.Itens[^1].ID == shop.Itens[^1].ID);

        var managedBefore = GC.GetTotalMemory(forceFullCollection: true);
        var heldSnapshot = SessionSnapshot.Capture(shop, tracking, settings);
        var managedWithSnapshot = GC.GetTotalMemory(forceFullCollection: true);
        var heldGrid = new GridViewModel(shop.Itens);
        var managedWithGrid = GC.GetTotalMemory(forceFullCollection: true);
        memoryProfiles.Add(new MemoryProfile(size, managedBefore, managedWithSnapshot, managedWithGrid));
        GC.KeepAlive(heldSnapshot);
        GC.KeepAlive(heldGrid);
        // A full-shop reset is the largest current history payload; keep it separate from I/O samples.
        tracking.ItemsReset(shop.Itens);
        Measure("snapshot-with-reset-history", size, () => SessionSnapshot.Capture(shop, tracking, settings),
            value => value is not null);
        GC.KeepAlive(shop);
        GC.KeepAlive(tracking);

        void MeasureSave(string name, int count)
        {
            Measure(name, count, () =>
            {
                SaveGshop.HandleSession(shop, tracking, settings, root);
                return true;
            }, value => value);
            // Verify outside the timed region, including the on-disk history.
            var reloaded = ContinueGshop.Handle(root);
            var history = JsonSerializer.Deserialize<ShopTracking>(File.ReadAllText(Path.Combine(root, "tracking.json")));
            if (reloaded.Itens.Count != size || history?.UndoChanges.Count != tracking.UndoChanges.Count)
                throw new InvalidDataException("Saved session did not round-trip.");
        }
    }

    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, JsonSerializer.Serialize(new
    {
        SchemaVersion = 1,
        CapturedUtc = DateTimeOffset.UtcNow,
        Runtime = RuntimeInformation.FrameworkDescription,
        OS = RuntimeInformation.OSDescription,
        Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        ProcessorCount = Environment.ProcessorCount,
        Warmups = warmups,
        Iterations = iterations,
        TieredCompilation = false,
        Fixture = "Synthetic ShopFixture: 8 categories, 4 sales and 8 owner IDs per item",
        ProcessPeakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64,
        MemoryProfiles = memoryProfiles,
        Results = results
    }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Report: {output}");
}
finally
{
    // Only delete this run's direct child of the OS temporary directory.
    var relative = Path.GetRelativePath(Path.GetFullPath(Path.GetTempPath()), tempRoot);
    if (relative != Path.GetFileName(tempRoot) || !relative.StartsWith("ultimate-gshop-perf-", StringComparison.Ordinal))
        throw new InvalidOperationException("Unexpected performance cleanup path.");
    Directory.Delete(tempRoot, recursive: true);
}

void Measure<T>(string operation, int size, Func<T> action, Func<T, bool> validate)
{
    for (var index = 0; index < warmups; index++)
        if (!validate(action())) throw new InvalidDataException($"Invalid warmup: {operation}");
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    var samples = new List<Sample>(iterations);
    for (var index = 0; index < iterations; index++)
    {
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var started = Stopwatch.GetTimestamp();
        var value = action();
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        if (!validate(value)) throw new InvalidDataException($"Invalid result: {operation}");
        GC.KeepAlive(value);
        samples.Add(new Sample(elapsed, bytes));
    }
    var times = samples.Select(sample => sample.ElapsedMs).Order().ToArray();
    var allocations = samples.Select(sample => sample.AllocatedBytes).Order().ToArray();
    var result = new Measurement(operation, size, times[iterations / 2],
        times[(int)Math.Ceiling(iterations * .95) - 1], allocations[iterations / 2], samples);
    results.Add(result);
    Console.WriteLine($"{size,6} {operation,-28} median {result.MedianMs,9:F3} ms  p95 {result.P95Ms,9:F3} ms  allocated {result.MedianAllocatedBytes,12:N0} B");
}

internal sealed record Sample(double ElapsedMs, long AllocatedBytes);
internal sealed record MemoryProfile(int ItemCount, long ManagedBytesBeforeSnapshot,
    long ManagedBytesWithSnapshot, long ManagedBytesWithSnapshotAndGrid);
internal sealed record Measurement(string Operation, int ItemCount, double MedianMs, double P95Ms,
    long MedianAllocatedBytes, IReadOnlyList<Sample> Samples);
