namespace Ultimate_Gshop.Format.Tests;

internal static class HeadlessWait
{
    internal static async Task UntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition()) await Task.Delay(5, timeout.Token);
    }
}
