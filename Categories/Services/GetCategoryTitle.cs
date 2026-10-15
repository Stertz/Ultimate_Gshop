using System.Text;
namespace Ultimate_Gshop.Categories.Services
{
    using Ultimate_Gshop.Gshop.Misc;

    internal static class GetCategoryTitle
    {

        internal static readonly string[] separator = ["\0"];
        internal static string Handle(BinaryReader br) =>
            Encoding.Unicode.GetString(BinaryFormat.ReadField(br, BinaryFormat.CategoryTitleSize)).TrimEnd('\0').Split(separator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    }
}
