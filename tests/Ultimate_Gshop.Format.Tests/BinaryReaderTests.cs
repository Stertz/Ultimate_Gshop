using System.Text;
using Ultimate_Gshop.Categories.Services;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

public sealed class BinaryReaderTests
{
    [Fact]
    public void HeaderReadsLittleEndianTimestampAndCountWithoutConsumingRecords()
    {
        using var stream = new MemoryStream([1, 0, 0, 0, 2, 0, 0, 0, 0xAA]);
        using var reader = new BinaryReader(stream);

        var (timestamp, count) = GetHeader.Handle(reader);

        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), timestamp);
        Assert.Equal(DateTimeKind.Unspecified, timestamp.Kind);
        Assert.Equal(2, count);
        Assert.Equal(8, stream.Position);
        Assert.Equal(0xAA, reader.ReadByte());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(7)]
    public void IncompleteHeadersFailInsteadOfReturningAPartialHeader(int length)
    {
        using var stream = new MemoryStream(new byte[length]);
        using var reader = new BinaryReader(stream);

        Assert.Throws<EndOfStreamException>(() => GetHeader.Handle(reader));
    }

    [Theory]
    [InlineData("Shop", "Shop")]
    [InlineData("百宝阁", "百宝阁")]
    [InlineData("Shop\0Other", "Shop")]
    [InlineData("", "")]
    public void CategoryTitlesReadFixedWidthUtf16Fields(string text, string expected)
    {
        var field = new byte[128];
        Encoding.Unicode.GetBytes(text).CopyTo(field, 0);
        using var stream = new MemoryStream([.. field, 0xAA]);
        using var reader = new BinaryReader(stream);

        Assert.Equal(expected, GetCategoryTitle.Handle(reader));
        Assert.Equal(128, stream.Position);
        Assert.Equal(0xAA, reader.ReadByte());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CategoriesStartAfterTheHeaderAndFixedSizeItemRecords(int itemCount)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ultimate-gshop-format-{Guid.NewGuid():N}.data");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite,
            FileShare.None, 4096, FileOptions.DeleteOnClose);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(new byte[8 + itemCount * 1464]);
        for (var index = 0; index < 8; index++)
        {
            WriteTitle(writer, $"Category {index}");
            writer.Write(2);
            WriteTitle(writer, "百宝阁");
            WriteTitle(writer, "Second");
        }
        writer.Flush();
        var expectedEnd = stream.Position;
        stream.Position = 0;
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var categories = GetCategories.Handle(reader, stream, itemCount).ToArray();

        Assert.Equal(8, categories.Length);
        for (var index = 0; index < categories.Length; index++)
        {
            Assert.Equal($"Category {index}", categories[index].Title);
            Assert.Equal(["百宝阁", "Second"], categories[index].SubCategories);
        }
        Assert.Equal(expectedEnd, stream.Position);
        Assert.Equal(1464, Variables.BytesPerItem);
        Assert.Equal(4, Variables.MaxSalesPerItem);
    }

    private static void WriteTitle(BinaryWriter writer, string title)
    {
        var field = new byte[128];
        Encoding.Unicode.GetBytes(title).CopyTo(field, 0);
        writer.Write(field);
    }
}
