using System.Text;
using Ultimate_Gshop.Gshop.Misc;

namespace Ultimate_Gshop.Format.Tests;

public sealed class TextFieldTests
{
    [Theory]
    [InlineData("4100000042000000", true, false, "A")]
    [InlineData("4100000042000000", true, true, "A\0B")]
    [InlineData("3DD800DE0000", true, false, "😀")]
    [InlineData("2D4E", true, false, "中")]
    [InlineData("00000000", true, true, "")]
    [InlineData("410042", true, false, "A�")]
    [InlineData("D6D0CEC40000", false, false, "中文")]
    [InlineData("41004200", false, false, "A")]
    [InlineData("D6", false, false, "?")]
    public void SpanDecoderRetainsLegacyNullAndEncodingBehavior(string hex, bool unicode, bool trimEnd, string expected)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = unicode ? Encoding.Unicode : Encoding.GetEncoding(936);
        var field = Convert.FromHexString(hex);
        using var reader = new BinaryReader(new MemoryStream([.. field, 0xAB]));
        var actual = BinaryFormat.ReadTextField(reader, new byte[field.Length], new char[1025], encoding, trimEnd);
        Assert.Equal(expected, actual);
        Assert.Equal(0xAB, reader.ReadByte());
    }

    [Fact]
    public void SpanDecoderRejectsTruncationAndSupportsPartialStreamReads()
    {
        using var stream = new PartialReadStream(Encoding.Unicode.GetBytes("Hello\0"));
        using var reader = new BinaryReader(stream);
        Assert.Equal("Hello", BinaryFormat.ReadTextField(reader, new byte[12], new char[12], Encoding.Unicode));
        Assert.Throws<EndOfStreamException>(() => BinaryFormat.ReadTextField(reader, new byte[2], new char[2], Encoding.Unicode));
    }

    private sealed class PartialReadStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(1, buffer.Length)]);
    }
}
