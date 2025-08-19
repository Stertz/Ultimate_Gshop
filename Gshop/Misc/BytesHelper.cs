using System.Text;

namespace Ultimate_Gshop.Gshop.Misc
{
    internal static class BytesHelper
    {
        internal static byte[] GetBytes(string str, int l, Encoding e)
        {
            str = str.Split('\0')[0];
            byte[] data = new byte[l];
            if (e.GetByteCount(str) > l)
            {
                Array.Copy(e.GetBytes(str), 0, data, 0, l);
            }
            else
            {
                Array.Copy(e.GetBytes(str), data, e.GetByteCount(str));
            }
            return data;
        }

        internal static byte[] GetBytesFromString(this string str, int l, Encoding e)
        {
            str = str.Split(default(char))[0];
            byte[] array = new byte[l];
            if (e.GetByteCount(str) > l)
            {
                Array.Copy(e.GetBytes(str), 0, array, 0, l);
            }
            else
            {
                Array.Copy(e.GetBytes(str), array, e.GetByteCount(str));
            }

            return array;
        }
    }
}
