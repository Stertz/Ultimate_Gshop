using System.Text;

namespace Ultimate_Gshop.Gshop.Misc
{
    internal static class BytesHelper
    {
        internal static byte[] GetBytes(string str, int l, Encoding e)
        {
            var terminator = str.IndexOf('\0');
            if (terminator >= 0)
                str = str[..terminator];
            byte[] data = new byte[l];
            var encoded = e.GetBytes(str);
            // Preserve legacy byte truncation, including partial encoded characters.
            Array.Copy(encoded, data, Math.Min(encoded.Length, l));
            return data;
        }

        internal static byte[] GetBytesFromString(this string str, int l, Encoding e)
            => GetBytes(str, l, e);
    }
}
