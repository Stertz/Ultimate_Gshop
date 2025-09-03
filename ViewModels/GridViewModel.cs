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
            Items = new ObservableCollection<Item>([.. items]);
        }
        public ObservableCollection<Item> Items { get; set; }
        public string? Teste => "Hello Word";
    }
}
