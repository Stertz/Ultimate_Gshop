using System.Text;
using System.IO;
namespace Ultimate_Gshop.Categories.Services
{

    internal static class GetCategoryTitle
    {

        internal static readonly string[] separator = ["\0"];
        internal static string Handle(BinaryReader br) =>
            Encoding.Unicode.GetString(br.ReadBytes(128)).TrimEnd('\0').Split(separator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    }
}
