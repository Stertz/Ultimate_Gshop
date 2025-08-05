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

            shop.Itens = [ new Item() { Id = 1, Name = "Item1",  Amount = 1, CategoryIndex = 0},
                           new Item() { Id = 2, Name = "Item2",  Amount = 1, CategoryIndex = 1},
                           new Item() { Id = 3, Name = "Item3",  Amount = 1, CategoryIndex = 2},
                           new Item() { Id = 4, Name = "Item4",  Amount = 1, CategoryIndex = 3},
                           new Item() { Id = 5, Name = "Item5",  Amount = 1, CategoryIndex = 4},
                           new Item() { Id = 6, Name = "Item6",  Amount = 1, CategoryIndex = 5},
                           new Item() { Id = 7, Name = "Item7",  Amount = 1, CategoryIndex = 6},
                           new Item() { Id = 8, Name = "Item8",  Amount = 1, CategoryIndex = 7}
            ];

            return shop;

        }
    }
}