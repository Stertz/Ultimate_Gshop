using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.ViewModels
{
    public class GridViewModel
    {
        public GridViewModel(IEnumerable<Item> items)
        {
            Items = new ReadOnlyCollection<GridItemSnapshot>(items.Select(item => new GridItemSnapshot(
                item.Place, item.ID, item.Name, item.Amount, item.CategoryIndex, item.SubCategoryIndex)).ToList());
        }
        public ReadOnlyCollection<GridItemSnapshot> Items { get; }
        public int ItemCount => Items.Count;
    }

    public sealed record GridItemSnapshot(int Place, int ID, string Name, int Amount,
        int CategoryIndex, int SubCategoryIndex);
}
