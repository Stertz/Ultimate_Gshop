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
            _shop.CategoriesLoaded += OnCategoriesLoaded;

            LoadGshop.TryLoadGshop(_shop);

            foreach (var cat in _shop.Categories)
            {
                Console.WriteLine($"Carregando categoria: {cat.Title}");

            }

            // Carrega os itens na tabela
            FillTable(_shop.Itens);
        }

        private void OnCategoriesLoaded(object? sender, EventArgs e)
        {
            //MessageBox.Show("teste");
        }

        private void OnItemCard_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is Item item)
            {
                DetailIcon.Source = LoadImage(item.Icon);
                //DetailTitle.Text = item.Name;

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
            if (sender is Button btn && btn.DataContext is Item item)
            {
                Console.WriteLine($"Copiando item: {item.Name} (ID: {item.Id})");
            }
        }

        private void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Item item)
            {
                Console.WriteLine($"Editando item: {item.Name} (ID: {item.Id})");
            }
        }

        private void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Item item)
            {
                Console.WriteLine($"Deletando item: {item.Name} (ID: {item.Id})");
            }
        }

        private void FillTable(List<Item> items)
        {
            var convertedList = new List<Item>();

            foreach (var item in items)
            {
                convertedList.Add(new Item
                {
                    Id = item.Id,
                    Name = item.Name,
                    Amount = item.Amount,
                    Icon = item.Icon,
                   // Price = 997.58 // Falta ajustar
                });
            }

            ItemList.ItemsSource = convertedList;
        }



        private Bitmap LoadImage(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Empty image path.");

                /*
                 * ALERTA GAMBIARRA
                 * não consegui converter .dds para .png
                 * então mudei os arquivos por enquanto
                 * até decidir se vamos ou não usar o 
                 * surface.pck
                 */
                // Troca temporária de .dds para .png
                if (path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                    path = Path.ChangeExtension(path, ".png");


                var fullPath = Path.Combine(AppContext.BaseDirectory, "Resources", path.TrimStart('/', '\\'));

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