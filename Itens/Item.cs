using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultimate_Gshop.Itens
{
    public class Item
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public int Place { get; set; }
        public int CategoryIndex { get; set; }
        public int SubCategoryIndex { get; set; }
        public string Icon { get; set; } = string.Empty;
        public List<Sale> Sales { get; set; } = [];
        public int Status { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GiftID { get; set; }
        public int GiftAmount { get; set; }
        public int GiftTime { get; set; }
        public int ILogPrice { get; set; }
        public int[] OwnerNpcs { get; set; } = [];
        public int LimitPeriod { get; set; }
        public int AvailFrequency { get; set; }
        public int Class { get; set; }
        public int Reserve1 { get; set; } 
        public int Reserve2 { get; set; } 
        public int Reserve3 { get; set; } 
        public int Reserve4 { get; set; }
        public int Reserve5 { get; set; }
        public int Reserve6 { get; set; }
    }
}
