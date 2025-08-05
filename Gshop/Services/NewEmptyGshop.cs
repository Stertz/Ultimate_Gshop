using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Itens;
namespace Ultimate_Gshop.Gshop.Services
{
    public static class NewEmptyGshop
    {
        public static Shop Handle()
        {

            var shop = new Shop();

            shop.Timestamp = DateTime.Now;
            shop.ItemAmount = 8;
            shop.Categories = [ new Category() { Title = "CategoryA", SubCategories = [ "SubCategory"] },
                                new Category() { Title = "CategoryB", SubCategories = [ "SubCategory"] },
                                new Category() { Title = "CategoryC", SubCategories = [ "SubCategory"] },
                                new Category() { Title = "CategoryD", SubCategories = [ "SubCategory"] },
                                new Category() { Title = "CategoryE", SubCategories = [ "SubCategory"] },
                                new Category() { Title = "CategoryF", SubCategories = [ "SubCategory"] },
                                new Category() { Title = "CategoryG", SubCategories = [ "SubCategory"] },
                                new Category() { Title = "CategoryH", SubCategories = [ "SubCategory"] }
            ];

            shop.Itens = [ new Item() { ID = 1, Name = "Item1",  Amount = 1, CategoryIndex = 0},
                           new Item() { ID = 2, Name = "Item2",  Amount = 1, CategoryIndex = 1},
                           new Item() { ID = 3, Name = "Item3",  Amount = 1, CategoryIndex = 2},
                           new Item() { ID = 4, Name = "Item4",  Amount = 1, CategoryIndex = 3},
                           new Item() { ID = 5, Name = "Item5",  Amount = 1, CategoryIndex = 4},
                           new Item() { ID = 6, Name = "Item6",  Amount = 1, CategoryIndex = 5},
                           new Item() { ID = 7, Name = "Item7",  Amount = 1, CategoryIndex = 6},
                           new Item() { ID = 8, Name = "Item8",  Amount = 1, CategoryIndex = 7}
            ];

            return shop;

        }
    }
}