using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Misc;

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
