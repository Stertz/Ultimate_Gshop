using Ultimate_Gshop.Misc;
using System.IO;
namespace Ultimate_Gshop.Categories.Services
{
    public static class GetCategories
    {
        public static IEnumerable<Category> Handle(BinaryReader br, FileStream fs, int itemAmount)
        {

            var categories = new List<Category>();

            //Número de bytes de cada item = 1464 + 8 bytes de intervalo

            fs.Seek(itemAmount * Variables.BytesPerItem + 8, SeekOrigin.Begin);

            for (byte x = 0; x < 8; x++) {

                var title = GetCategoryTitle.Handle(br);

                var category = new Category() { 
                    Title = title
                };

                var subCount = br.ReadInt32();

                for (byte y = 0; y < subCount; y++)
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
