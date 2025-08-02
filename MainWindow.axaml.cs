using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using System.IO;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Button = Avalonia.Controls.Button;
namespace Ultimate_Gshop
{
    public partial class MainWindow : Window
    {
        private Shop? _shop;
        private Border? _lastSelectedBorder;

        public MainWindow()
        {
            InitializeComponent();

            // Define cursor personalizado
            using (var stream = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/cursor.png")))
            {
                var bitmap = new Bitmap(stream);
                this.Cursor = new Avalonia.Input.Cursor(bitmap, new PixelPoint(0, 0));
            }

            _shop = new Shop();

            LoadGshop.TryLoadGshop(_shop);

            CategoryList.ItemsSource = _shop.Categories;

            SubCategoryList.ItemsSource = _shop.Categories.FirstOrDefault()?.SubCategories;


            // Carrega os itens na tabela
            FillTable(_shop.Itens.Take(20).ToList());
        }

        private void OnItemCard_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var item = grid.DataContext as dynamic;
                if (item == null)
                    return;

                DetailIcon.Source = item.IconBitmap;
                DetailName.Text = item.Name;

                // aplica destaque no card clicado:
                if (grid.Parent is Border current)
                {
                    // Remove sombra anterior
                    if (_lastSelectedBorder is not null)
                    {
                        _lastSelectedBorder.BoxShadow = new BoxShadows(
                             new BoxShadow
                             {
                                 Color = Colors.Transparent,
                                 Blur = 10,
                                 OffsetX = 0,
                                 OffsetY = 0
                             }
                         );
                    }

                    // Aplica sombra dourada
                    current.BoxShadow = new BoxShadows(
                        new BoxShadow
                        {
                            Color = Avalonia.Media.Color.Parse("#ebb935"),
                            Blur = 15,
                            OffsetX = 0,
                            OffsetY = 0
                        }
                    );
                    _lastSelectedBorder = current;
                }
            }
        }

        private void CopyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var item = grid.DataContext as dynamic;
                if (item == null)
                    return;

                Console.WriteLine($"Copiando item: {item.Name} (ID: {item.Id})");
            }
        }

        private void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var item = grid.DataContext as dynamic;
                if (item == null)
                    return;

                Console.WriteLine($"Editando item: {item.Name} (ID: {item.Id})");
            }
        }

        private void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var item = grid.DataContext as dynamic;
                if (item == null)
                    return;

                Console.WriteLine($"Deletando item: {item.Name} (ID: {item.Id})");
            }
        }

        private void FillTable(List<Item> items)
        {
            var convertedList = items.Select(item => new
            {
                item.Id,
                item.Name,
                item.Amount,
                LimitPeriod = item.LimitPeriod == 0 ? "Perm." : $"{item.LimitPeriod} dias",
                IconBitmap = LoadImage(item.Icon)
            }).ToList();

            ItemList.ItemsSource = convertedList;
        }


        private static Bitmap LoadImage(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Empty image path.");

                // GAMBIARRA -> Troca temporária de .dds para .png
                if (path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                    path = Path.ChangeExtension(path, ".png");

                var fullPath = Path.Combine(AppContext.BaseDirectory, "resources", path.TrimStart('/', '\\'));

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Image not found: {fullPath}");

                using var fileStream = File.OpenRead(fullPath);
                return new Bitmap(fileStream);
            }
            catch
            {
                using var fallback = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png"));
                return new Bitmap(fallback);
            }
        }

        private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private void TopDrag_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }
    }
}