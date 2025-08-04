using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Globalization;
using System.Text.RegularExpressions;
using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;
using static System.Net.Mime.MediaTypeNames;

namespace Ultimate_Gshop
{
    public partial class MainWindow : Window
    {

        private enum DetailState { Viewing, Creating, Editing }
        private DetailState _currentState = DetailState.Viewing;

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
                this.Cursor = new Cursor(bitmap, new PixelPoint(0, 0));
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

        private void ItemCopyButton_Click(object? sender, RoutedEventArgs e)
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

        private void ItemEditButton_Click(object? sender, RoutedEventArgs e)
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

        private async void ItemDeleteButton_Click(object? sender, RoutedEventArgs e)
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

                var msgBox = new GameMessageBox(
                            "Confirmação",
                            $"Deseja realmente excluir o item \"{item.Name}\" (ID: {item.Id})?\n\n" +
                            "Esta ação é permanente e não poderá ser desfeita.",
                            "Sim", "Não");


                var result = await msgBox.ShowDialogAsync(this);

                if (result == "Sim")
                {
                    _shop.Itens.Remove(item);
                    UpdateGrid();
                }
            }
        }

        private async void DetailNew_Click(object? sender, RoutedEventArgs e)
        {
            _currentState = DetailState.Creating;

            var msg = new GameMessageBox(
                "Estado alterado",
                "Criando novo item.",
                "OK");
            await msg.ShowDialogAsync(this);
        }

        private async void DetailEdit_Click(object? sender, RoutedEventArgs e)
        {
            _currentState = DetailState.Editing;

            var msg = new GameMessageBox(
                "Estado alterado",
                "Editando item existente.",
                "OK");
            await msg.ShowDialogAsync(this);
        }

        private async void DetailDelete_Click(object? sender, RoutedEventArgs e)
        {
            _currentState = DetailState.Viewing;

            if (DetailPanel.Tag is Item item)
            {
                var msgBox = new GameMessageBox(
                            "Confirmação",
                            $"Deseja realmente excluir o item \"{item.Name}\" (ID: {item.Id})?\n\n" +
                            "Esta ação é permanente e não poderá ser desfeita.",
                            "Sim", "Não");


                var result = await msgBox.ShowDialogAsync(this);

                if (result == "Sim")
                {
                    _shop.Itens.Remove(item);
                    UpdateGrid();
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
                Color = Color.Parse("#ebb935"),
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
                Price = GetValidPrice(item.Sales).ToString("F2", CultureInfo.InvariantCulture),
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
                DetailPanel.Tag = null;
                DetailIcon.Source = null;
                DetailName.Text = string.Empty;
                SetExplanationFormatted(string.Empty);
                return;
            }

            DetailPanel.Tag = item;
            DetailIcon.Source = LoadImage(item.Icon);
            DetailName.Text = item.Name;
            SetExplanationFormatted(item.Explanation);
        }

        private void SetExplanationFormatted(string text)
        {
            if (DetailExplanation.Inlines == null)
                return;

            DetailExplanation.Inlines.Clear();

            if (string.IsNullOrWhiteSpace(text))
                return;

            // Expressão regular: captura ^ seguido de 6 hex + texto até próxima cor ou fim
            var regex = new Regex(@"\^(?<color>[0-9A-Fa-f]{6})(?<content>.*?)(?=(\^[0-9A-Fa-f]{6})|$)", RegexOptions.Singleline);

            foreach (Match match in regex.Matches(text))
            {
                string hex = match.Groups["color"].Value;
                string content = match.Groups["content"].Value;

                // Substituir \r por quebra de linha
                content = content.Replace(@"\r", "\n");

                var run = new Run
                {
                    Text = content,
                    Foreground = new SolidColorBrush(Color.Parse("#" + hex))
                };

                DetailExplanation.Inlines.Add(run);
            }
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

        /// <summary>
        /// Retorna o preço válido de um item (convertido para decimal),
        /// baseado em regras de status e disponibilidade.
        /// - Prioriza Sales com Status ativo.
        /// - Verifica SellingEndTime (ignora vendas expiradas).
        /// - Considera Day como dia fixo de disponibilidade (se configurado).
        /// - Divide por 100 para retornar o valor monetário real.
        /// </summary>
        /// <param name="sales">Lista de opções de venda (Sales).</param>
        /// <returns>Preço válido como decimal ou 0 se nenhum for encontrado.</returns>
        public static decimal GetValidPrice(List<Sale> sales)
        {
            if (sales == null || sales.Count == 0)
                return 0m;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int today = (int)DateTime.UtcNow.DayOfWeek; // Caso 'Day' represente dia da semana

            // Filtra vendas válidas com base em status e tempo
            var validSales = sales.Where(s =>
                (s.SellingEndTime == 0 || s.SellingEndTime > now) && // Ainda disponível
                (s.Day == 0 || s.Day == today)                       // Se Day for usado, confere
            ).ToList();

            if (validSales.Count == 0)
                return 0m;

            // Prioriza vendas com status ativo > 0
            var activeSale = validSales.FirstOrDefault(s => s.Status > 0);
            var price = (activeSale ?? validSales.First()).Price;

            // Converte para decimal e divide por 100
            return price / 100m;
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
