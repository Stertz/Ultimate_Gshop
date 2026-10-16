using Ultimate_Gshop.Misc;
using System.Text;

namespace Ultimate_Gshop.Gshop.Misc;

internal static class BinaryFormat
{
    internal const int HeaderSize = 8;
    internal const int CategoryTitleSize = 128;
    internal const int CategoryCount = 8;

    internal static long GetCategoryOffset(Stream stream, int itemCount)
    {
        // Calculate in 64 bits so a hostile count cannot wrap into an earlier field.
        var offset = HeaderSize + (long)itemCount * Variables.BytesPerItem;
        if (itemCount < 0 || offset > stream.Length)
            throw new InvalidDataException("The item count exceeds the available client records.");
        return offset;
    }

    internal static byte[] ReadField(BinaryReader reader, int size)
    {
        var bytes = reader.ReadBytes(size);
        if (bytes.Length != size)
            throw new EndOfStreamException($"Expected a complete {size}-byte field.");
        return bytes;
    }

    internal static string ReadTextField(BinaryReader reader, Span<byte> bytes, Span<char> chars,
        Encoding encoding, bool trimEndOnly = false)
    {
        reader.BaseStream.ReadExactly(bytes);
        var length = encoding.GetChars(bytes, chars);
        var text = chars[..length];
        // Explanation retains embedded nulls; names/icons stop at the first null.
        if (trimEndOnly) text = text.TrimEnd('\0');
        else
        {
            var end = text.IndexOf('\0');
            if (end >= 0) text = text[..end];
        }
        return new string(text);
    }
}
