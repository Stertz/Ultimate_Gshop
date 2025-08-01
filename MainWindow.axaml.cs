using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using System.IO;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Button = Avalonia.Controls.Button;
namespace Ultimate_Gshop
{
    public class ItemInfo
    {
        public Bitmap Icon { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public double Price { get; set; } = 0;
        public int Id { get; set; } = 0;
        public string Temp { get; set; } = "30 dias";
        public int Quantity { get; set; } = 0;
        public string Description { get; set; } = "Descrição do item não disponível.";
    }

    public partial class MainWindow : Window
    {
        private Border? _lastSelectedBorder;

        public MainWindow()
        {
            InitializeComponent();

            using (var stream = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/cursor.png")))
            {
                var bitmap = new Bitmap(stream);
                this.Cursor = new Avalonia.Input.Cursor(bitmap, new PixelPoint(0, 0));
            }

            var items = new List<ItemInfo>();

            for (int i = 100000; i <= 100033; i++)
            {
                var iconPath = i % 2 == 0 ? null : "/surfaces/百宝阁/100块帽子女128.dds";

                items.Add(new ItemInfo
                {
                    Icon = LoadImage(iconPath),
                    Title = $"Pedra Filosofal {i} do Har...",
                    Price = i * 1.33,
                    Id = i,
                    Quantity = i / 2
                });
            }

            ItemList.ItemsSource = items;
        }

        private void TopDrag_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void OnItemCard_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is ItemInfo item)
            {
                DetailIcon.Source = item.Icon;
                DetailTitle.Text = item.Title;
                /*DetailId.Text = $"ID: {item.Id}";
                DetailQuantity.Text = $"Qtd: {item.Quantity}";
                DetailPrice.Text = item.Price.ToString("C2") + " GOLD";
                DetailDescription.Text = item.Description;*/

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



        private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemInfo item)
            {
                Console.WriteLine($"Copiando item: {item.Title} (ID: {item.Id})");
            }
        }

        private void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemInfo item)
            {
                Console.WriteLine($"Editando item: {item.Title} (ID: {item.Id})");
            }
        }

        private void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemInfo item)
            {
                Console.WriteLine($"Deletando item: {item.Title} (ID: {item.Id})");
            }
        }


        private Bitmap LoadImage(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Empty image path.");

                var fullPath = Path.Combine(AppContext.BaseDirectory, "Resources", path.TrimStart('/', '\\'));

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Image not found: {fullPath}");

                var ext = Path.GetExtension(fullPath).ToLowerInvariant();

                if (ext == ".dds")
                {

                    // Converter DDS para Bitmap
                    using var fileStream = File.OpenRead(fullPath);
                    return new Bitmap(fileStream);
                }
                else
                {
                    using var fileStream = File.OpenRead(fullPath);
                    return new Bitmap(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar {path}: {ex.Message}");
                using var fallback = AssetLoader.Open(new Uri("avares://Ultimate_Gshop/Resources/question_mark.png"));
                return new Bitmap(fallback);
            }
        }
    }
}