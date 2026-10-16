using System.Buffers.Binary;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

[CollectionDefinition("Image rendering", DisableParallelization = true)]
public sealed class ImageRenderingCollection : ICollectionFixture<ImageRenderingFixture>;

public sealed class ImageRenderingFixture : IDisposable
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Thread _uiThread;

    public ImageRenderingFixture()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _uiThread = new Thread(() =>
        {
            try
            {
                AppBuilder.Configure<App>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                    .UseSkia().UseHarfBuzz().SetupWithoutStarting();
                ready.SetResult();
                Dispatcher.UIThread.MainLoop(_shutdown.Token);
            }
            catch (Exception error) { ready.TrySetException(error); }
        }) { IsBackground = true, Name = "Avalonia headless tests" };
        _uiThread.Start();
        ready.Task.GetAwaiter().GetResult();
    }

    public async Task Run(Action test) => await Dispatcher.UIThread.InvokeAsync(test);
    public async Task Run(Func<Task> test) => await Dispatcher.UIThread.InvokeAsync(test);

    public void Dispose()
    {
        _shutdown.Cancel();
        if (!_uiThread.Join(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("The headless UI thread did not stop.");
        _shutdown.Dispose();
    }
}

[Collection("Image rendering")]
public sealed class ImageLoadingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingGameImagesLoadFromCopiedResourcesAndRetainTheirDimensions(bool windowsSeparators)
    {
        var fallbackSize = ImageUtils.LoadImage($"missing-{Guid.NewGuid():N}.png").PixelSize;
        var resourceRoot = Path.Combine(AppContext.BaseDirectory, "Resources");
        var sample = Directory.EnumerateFiles(Path.Combine(resourceRoot, "surfaces"), "*.png", SearchOption.AllDirectories)
            .Select(path => (Path: path, Size: ReadPngSize(path)))
            .First(entry => entry.Size != fallbackSize);
        var input = Path.ChangeExtension(Path.GetRelativePath(resourceRoot, sample.Path), ".dds").Replace('\\', '/');
        if (windowsSeparators)
            input = input.Replace('/', '\\');

        var image = ImageUtils.LoadImage(input);
        var reduced = ImageUtils.LoadImageReduced(input, 17, 23);

        Assert.Equal(sample.Size, image.PixelSize);
        Assert.Equal(new PixelSize(17, 23), reduced.PixelSize);
        Assert.Same(image, ImageUtils.LoadImage(input));
        Assert.Same(reduced, ImageUtils.LoadImageReduced(input, 17, 23));
    }

    [Fact]
    public void MissingImagesUseTheExistingEmbeddedFallback()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png"));
        using var expected = new Bitmap(stream);

        var actual = ImageUtils.LoadImage($"missing-{Guid.NewGuid():N}.dds");

        Assert.Equal(expected.PixelSize, actual.PixelSize);
    }

    private static PixelSize ReadPngSize(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        stream.ReadExactly(header);
        return new PixelSize(BinaryPrimitives.ReadInt32BigEndian(header[16..20]),
            BinaryPrimitives.ReadInt32BigEndian(header[20..24]));
    }
}
