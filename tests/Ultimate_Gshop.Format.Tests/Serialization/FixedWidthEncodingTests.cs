using System.Text;
using Ultimate_Gshop.Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

public sealed class FixedWidthEncodingTests
{
    [Theory]
    [InlineData("", 4, "00000000")]
    [InlineData("AB", 6, "410042000000")]
    [InlineData("AB", 4, "41004200")]
    [InlineData("A\0B", 6, "410000000000")]
    [InlineData("中", 4, "2D4E0000")]
    [InlineData("😀", 4, "3DD800DE")]
    public void Utf16FieldsPreserveExactBytesAndNullPadding(string text, int width, string expectedHex)
    {
        AssertBothEncoders(text, width, Encoding.Unicode, expectedHex);
    }

    [Theory]
    [InlineData("", 4, "00000000")]
    [InlineData("AB", 4, "41420000")]
    [InlineData("A\0B", 4, "41000000")]
    [InlineData("中文", 6, "D6D0CEC40000")]
    [InlineData("surfaces\\百宝阁\\item.dds", 128, null)]
    public void Cp936FieldsPreserveNamesAndPadding(string text, int width, string? expectedHex)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(936);

        if (expectedHex is not null)
        {
            AssertBothEncoders(text, width, encoding, expectedHex);
            return;
        }

        var bytes = BytesHelper.GetBytes(text, width, encoding);
        Assert.Equal(width, bytes.Length);
        Assert.Equal(text, encoding.GetString(bytes).TrimEnd('\0'));
        Assert.Equal(bytes, text.GetBytesFromString(width, encoding));
    }

    [Fact]
    public void LegacyTruncationUsesBytesRatherThanCharacters()
    {
        // Characterize the existing behavior; changing truncation needs a separate format decision.
        AssertBothEncoders("AB", 3, Encoding.Unicode, "410042");
    }

    private static void AssertBothEncoders(string text, int width, Encoding encoding, string expectedHex)
    {
        var expected = Convert.FromHexString(expectedHex);
        Assert.Equal(expected, BytesHelper.GetBytes(text, width, encoding));
        Assert.Equal(expected, text.GetBytesFromString(width, encoding));
    }
}
