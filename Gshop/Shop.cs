using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.Gshop
{
    public class Shop
    {
        public DateTime Timestamp { get; set; }
        public int ItemAmount { get; set; }
        public List<Category> Categories { get; set; } = [];
        public List<Item> Itens { get; set; } = [];

        public EventHandler? CategoriesLoaded;

        public EventHandler? ItensLoaded;

    }
}
