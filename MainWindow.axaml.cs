using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Item = Ultimate_Gshop.Itens.Item;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

namespace Ultimate_Gshop
{
    public partial class MainWindow : Window
    {
        private Shop? _shop;
        private Border? _lastSelectedBorder;
        private Border? _lastSelectedCategory;
        private Border? _lastSelectedSubCategory;

        public MainWindow()
        {
            InitializeComponent();
            _shop = new Shop();
            LoadGshop.TryLoadGshop(_shop);

            CategoryList.ItemsSource = _shop.Categories;

            // Seleciona a primeira categoria ao abrir a janela
            this.Opened += (_, __) =>
            {
                SelectFirstCategory();
            };
        }

        #region Item Events

        private void OnItemCard_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var item = grid.DataContext as dynamic;
                if (item == null)
                    return;

                DetailIcon.Source = item.IconBitmap;
                DetailName.Text = item.Name;

                if (grid.Parent is Border current)
                {
                    if (_lastSelectedBorder != null)
                        RemoveShadow(_lastSelectedBorder);

                    ApplyGoldShadow(current);
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

        #endregion

        #region Category Events

        private void OnCategoryClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is Category category && grid.Parent is Border current)
            {
                int categoryIndex = _shop!.Categories.IndexOf(category);
                SearchBox.Text = string.Empty;

                if (_lastSelectedCategory == current)
                {
                    RemoveShadow(current);
                    _lastSelectedCategory = null;
                    SubCategoryList.ItemsSource = null;
                    FillTable([]);
                    return;
                }

                if (_lastSelectedCategory != null)
                    RemoveShadow(_lastSelectedCategory);

                ApplyGoldShadow(current);
                _lastSelectedCategory = current;

                SubCategoryList.ItemsSource = category.SubCategories;

                var items = _shop.Itens
                    .Where(i => i.CategoryIndex == categoryIndex)
                    .ToList();

                FillTable(items);
            }
        }

        private void OnSubCategoryClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is string subCategory && grid.Parent is Border current)
            {
                SearchBox.Text = string.Empty;

                if (_lastSelectedCategory?.Child is not Grid categoryGrid ||
                    categoryGrid.DataContext is not Category category)
                    return;

                int categoryIndex = _shop!.Categories.IndexOf(category);
                int subCategoryIndex = category.SubCategories.IndexOf(subCategory);

                if (_lastSelectedSubCategory == current)
                {
                    RemoveShadow(current);
                    _lastSelectedSubCategory = null;

                    var items = _shop.Itens
                        .Where(i => i.CategoryIndex == categoryIndex)
                        .ToList();

                    FillTable(items);
                    return;
                }

                if (_lastSelectedSubCategory != null)
                    RemoveShadow(_lastSelectedSubCategory);

                ApplyGoldShadow(current);
                _lastSelectedSubCategory = current;

                var filteredItems = _shop.Itens
                    .Where(i => i.CategoryIndex == categoryIndex &&
                                i.SubCategoryIndex == subCategoryIndex)
                    .ToList();

                FillTable(filteredItems);
            }
        }

        #endregion

        #region Search

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ExecuteSearch();
        }

        private void SearchIcon_Click(object? sender, RoutedEventArgs e)
        {
            ExecuteSearch();
        }

        private void ExecuteSearch()
        {
            string query = SearchBox.Text?.Trim() ?? string.Empty;
            IEnumerable<Item> items = _shop!.Itens;

            Category? activeCategory = null;
            int categoryIndex = -1;

            if (_lastSelectedCategory?.Child is Grid catGrid &&
                catGrid.DataContext is Category category)
            {
                activeCategory = category;
                categoryIndex = _shop.Categories.IndexOf(category);
                items = items.Where(i => i.CategoryIndex == categoryIndex);
            }

            if (activeCategory != null &&
                _lastSelectedSubCategory?.Child is Grid subGrid &&
                subGrid.DataContext is string subCategory)
            {
                int subCategoryIndex = activeCategory.SubCategories.IndexOf(subCategory);

                if (subCategoryIndex >= 0)
                {
                    items = items.Where(i => i.CategoryIndex == categoryIndex &&
                                             i.SubCategoryIndex == subCategoryIndex);
                }
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                FillTable(items);
                return;
            }

            items = items.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            FillTable(items);
        }

        #endregion

        #region Shadow Helpers

        private static void ApplyGoldShadow(Border border)
        {
            border.BoxShadow = new BoxShadows(new BoxShadow
            {
                Color = Avalonia.Media.Color.Parse("#ebb935"),
                Blur = 15,
                OffsetX = 0,
                OffsetY = 0
            });
        }

        private static void RemoveShadow(Border border)
        {
            border.BoxShadow = new BoxShadows(new BoxShadow
            {
                Color = Colors.Transparent,
                Blur = 10,
                OffsetX = 0,
                OffsetY = 0
            });
        }

        #endregion

        #region Utils

        private void FillTable(IEnumerable<Item> items)
        {
            var convertedList = items.Select(item => new
            {
                item.Id,
                item.Name,
                item.Amount,
                LimitPeriod = item.LimitPeriod == 0 ? "Perm." : $"{item.LimitPeriod} dias",
                IconBitmap = LoadImage(item.Icon)
            }).ToList();

            var repeater = this.FindControl<ItemsRepeater>("ItemList");
            if (repeater != null)
            {
                repeater.ItemsSource = convertedList;
            }            
        }

        private void SelectFirstCategory()
        {
            if (_shop == null) return;
            var firstCategory = _shop.Categories.FirstOrDefault();
            if (firstCategory == null) return;

            SubCategoryList.ItemsSource = firstCategory.SubCategories;
            int categoryIndex = _shop.Categories.IndexOf(firstCategory);

            var items = _shop.Itens
                .Where(i => i.CategoryIndex == categoryIndex)
                .ToList();
            FillTable(items);

            CategoryList.ApplyTemplate();

            if (CategoryList.ContainerFromIndex(categoryIndex) is ContentPresenter container)
            {
                if (container.Child is Border border)
                {
                    ApplyGoldShadow(border);
                    _lastSelectedCategory = border;
                }
                else if (container.Child is Grid grid && grid.Parent is Border b)
                {
                    ApplyGoldShadow(b);
                    _lastSelectedCategory = b;
                }
            }
        }

        private static Bitmap LoadImage(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new Exception("Empty image path.");

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

        private async void NavButton_Click(object? sender, RoutedEventArgs e)
        {
            var menuWindow = new MenuWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                SystemDecorations = SystemDecorations.None, // mantém estilo modal sem barra
                CanResize = false
            };

            // Pega tamanho do MainWindow e do MenuWindow
            var mainBounds = this.Bounds;
            menuWindow.Width = 400;
            menuWindow.Height = 300;

            menuWindow.Position = new PixelPoint(
                (int)(Position.X + (mainBounds.Width - menuWindow.Width) / 2),
                (int)(Position.Y + (mainBounds.Height - menuWindow.Height) / 2)
            );

            await menuWindow.ShowDialog(this);
        }

        private async void ItemEditorButton_Click(object? sender, RoutedEventArgs e)
        {
            var itemEditorWindow = new ItemEditorWindow
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                SystemDecorations = SystemDecorations.None, // mantém estilo modal sem barra
                CanResize = false
            };

            // Pega tamanho do MainWindow e do MenuWindow
            var mainBounds = this.Bounds;
            itemEditorWindow.Width = 400;
            itemEditorWindow.Height = 300;

            itemEditorWindow.Position = new PixelPoint(
                (int)(Position.X + (mainBounds.Width - itemEditorWindow.Width) / 2),
                (int)(Position.Y + (mainBounds.Height - itemEditorWindow.Height) / 2)
            );

            await itemEditorWindow.ShowDialog(this);
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TopDrag_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        #endregion
    }
}
