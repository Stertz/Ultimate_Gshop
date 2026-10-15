using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Categories.Services
{
    using Ultimate_Gshop.Gshop.Misc;
    public static class GetCategories
    {
        public static IEnumerable<Category> Handle(BinaryReader br, FileStream fs, int itemAmount)
        {

            var categories = new List<Category>();

            fs.Seek(BinaryFormat.GetCategoryOffset(fs, itemAmount), SeekOrigin.Begin);

            for (var x = 0; x < BinaryFormat.CategoryCount; x++)
            {

                var title = GetCategoryTitle.Handle(br);

                var category = new Category()
                {
                    Title = title
                };

                var subCount = br.ReadInt32();
                var remainingCategoryHeaders = (BinaryFormat.CategoryCount - x - 1) *
                    (BinaryFormat.CategoryTitleSize + sizeof(int));
                var available = fs.Length - fs.Position - remainingCategoryHeaders;
                if (subCount < 0 || (long)subCount * BinaryFormat.CategoryTitleSize > available)
                    throw new InvalidDataException("The subcategory count exceeds the available category data.");

                for (var y = 0; y < subCount; y++)
                {
                    var subtitle = GetCategoryTitle.Handle(br);
                    category.SubCategories.Add(subtitle);
                }

                categories.Add(category);

            }

            return categories;

        }
    }
}
