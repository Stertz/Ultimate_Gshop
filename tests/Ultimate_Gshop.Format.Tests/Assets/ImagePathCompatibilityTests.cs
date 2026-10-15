using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

public sealed class ImagePathCompatibilityTests
{
    [Theory]
    [InlineData(@"surfaces\百宝阁\中文.dds")]
    [InlineData("surfaces/百宝阁/中文.dds")]
    [InlineData(@"\surfaces\百宝阁\中文.DDS")]
    [InlineData("/surfaces/百宝阁/中文.png")]
    public void GamePathsKeepNamesButResolveUsingTheActualResourceDirectory(string gamePath)
    {
        var expected = Path.Combine(AppContext.BaseDirectory, "Resources", "surfaces", "百宝阁", "中文.png");

        Assert.Equal(expected, ImageUtils.ResolveImagePath(gamePath));
    }
}
