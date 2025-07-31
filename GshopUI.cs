using Ultimate_Gshop.Categories.Controls;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
namespace Ultimate_Gshop
{
    public partial class GshopUI : Form
    {
        private Shop? _shop;

        public GshopUI()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            _shop = new Shop();
            _shop.CategoriesLoaded += OnCategoriesLoaded;

            LoadGshop.TryLoadGshop(_shop);

            foreach (var cat in _shop.Categories)
            {

                var catb = new CategoryButton();

                catb.SetTitle(cat.Title);

                flowLayoutPanel1.Controls.Add(catb);

            }

            dataGridView1.DataSource = _shop.Itens;

        }

        private void OnCategoriesLoaded(object? sender, EventArgs e)
        {
            //MessageBox.Show("teste");
        }


    }
}
