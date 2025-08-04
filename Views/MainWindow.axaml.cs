using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Item = Ultimate_Gshop.Itens.Item;

namespace Ultimate_Gshop
{
    public partial class MainWindow : Window
    {
        private readonly Shop _shop;
        private Border? _lastSelectedBorder;
        private Border? _lastSelectedCategory;
        private Border? _lastSelectedSubCategory;

        private IQueryable<Item>? _currentItems;
        private int _selectedCategory = -1;
        private int _selectedSubCategory = -1;

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
                var item = (grid.Tag as Item);
                if (item == null) return;
                FillItemDetails(item);

                if (grid.Parent is Border current)
                {
                    RemoveShadow(_lastSelectedBorder);

                    ApplyGoldShadow(current);
                    _lastSelectedBorder = current;
                }
            }
        }

        private void CopyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Item item)
            {
                if (item == null) return;
                FillItemDetails(item);

                RemoveShadow(_lastSelectedBorder);
                _lastSelectedBorder = null;

                // habilitar campos para novo item
            }
        }

        private void EditButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Item item)
            {
                if (item == null) return;
                FillItemDetails(item);

                if (btn.Parent is Grid grid && grid.Parent is Border current)
                {
                    RemoveShadow(_lastSelectedBorder);

                    ApplyGoldShadow(current);
                    _lastSelectedBorder = current;
                }

                // habilitar campos de edição
            }
        }

        private async void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Item item)
            {
                if (item == null) return;

                FillItemDetails(null);

                if (btn.Parent is Grid grid && grid.Parent is Border current)
                {
                    RemoveShadow(_lastSelectedBorder);

                    ApplyGoldShadow(current);
                    _lastSelectedBorder = current;
                }

                var msgBox = new GameMessageBox(
                            "Confirmação",
                            $"Deseja realmente excluir o item \"{item.Name}\"?",
                            "Sim", "Não");

                var result = await msgBox.ShowDialogAsync(this);

                if (result == "Sim")
                {
                    // Excluir item aqui

                    RemoveShadow(_lastSelectedBorder);
                    _lastSelectedBorder = null;
                }
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
                    _selectedCategory = -1;
                    _selectedSubCategory = -1;
                    UpdateGrid();
                    return;
                }

                RemoveShadow(_lastSelectedCategory);

                ApplyGoldShadow(current);
                _lastSelectedCategory = current;

                SubCategoryList.ItemsSource = category.SubCategories;

                CategoryToggled(categoryIndex);

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

                int subCategoryIndex = category.SubCategories.IndexOf(subCategory);

                if (_lastSelectedSubCategory == current)
                {
                    RemoveShadow(current);
                    _lastSelectedSubCategory = null;
                    _selectedSubCategory = -1;
                    UpdateGrid();
                    return;
                }

                RemoveShadow(_lastSelectedSubCategory);

                ApplyGoldShadow(current);
                _lastSelectedSubCategory = current;

                SubCategoryToggled(subCategoryIndex);
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

            if (string.IsNullOrWhiteSpace(query))
            {
                // Se o campo estiver vazio, apenas recarrega a lista normal
                UpdateGrid();
                return;
            }

            var filtered = _currentItems?
                .Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered != null)
            {
                _currentItems = filtered.AsQueryable();
                FillTable();
            }

            RemoveShadow(_lastSelectedBorder);
            _lastSelectedBorder = null;
            FillItemDetails(null);
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

        private static void RemoveShadow(Border? border)
        {
            if (border == null) return;

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

        private void FillTable()
        {
            var convertedList = _currentItems?.Select(item => new
            {
                Tag = item,
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

        private void FillItemDetails(Item? item)
        {
            if (item == null)
            {
                DetailIcon.Source = null;
                DetailName.Text = string.Empty;
                return;
            }

            DetailIcon.Source = LoadImage(item.Icon);
            DetailName.Text = item.Name;
        }

        private void SelectFirstCategory()
        {
            if (_shop == null) return;
            var firstCategory = _shop.Categories.FirstOrDefault();
            if (firstCategory == null) return;

            SubCategoryList.ItemsSource = firstCategory.SubCategories;
            int categoryIndex = _shop.Categories.IndexOf(firstCategory);

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

            CategoryToggled(categoryIndex);

        }

        private void CategoryToggled(int index)
        {
            _selectedCategory = index;
            _selectedSubCategory = -1;
            UpdateGrid();
        }

        private void SubCategoryToggled(int subcategoryIndex)
        {
            _selectedSubCategory = subcategoryIndex;
            UpdateGrid();
        }

        private void UpdateGrid()
        {
            if (_shop == null) return;

            var query = _shop.Itens.AsQueryable();
            query = query.Where(i => i.CategoryIndex == _selectedCategory);
            if (_selectedSubCategory > -1) query = query.Where(i => i.SubCategoryIndex == _selectedSubCategory);

            RemoveShadow(_lastSelectedBorder);
            _lastSelectedBorder = null;
            FillItemDetails(null);

            _currentItems = query;
            FillTable();

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
            var menuWindow = new MenuWindow(_shop)
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

            await menuWindow.ShowDialog<Shop>(this);
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
