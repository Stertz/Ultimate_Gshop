using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DynamicData;
using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.Misc;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop
{
    public partial class MainWindow : Window
    {
        private bool _isClosingHandled = false;

        private static readonly List<string> ItemClass = ["All", "Blademaster", "Wizard", "Psychic", "Venomancer", "Barbarian", "Assassin", "Archer", "Cleric", "Seeker", "Mystic", "Duskblade", "Stormbringer"];

        private Shop _shop;
        private readonly ShopTracking _tracking = new();
        private readonly GshopSettings _settings;
        private Border? _lastSelectedBorder;
        private Border? _lastSelectedCategory;
        private Border? _lastSelectedSubCategory;

        private IQueryable<Item>? _currentItems;
        private Item? _currentSelectedItem;
        private int _selectedCategory = -1;
        private int _selectedSubCategory = -1;

        /* Campos para Detail Items*/
        private enum DetailState { Viewing, Creating, Editing }
        private DetailState _currentState = DetailState.Viewing;
        private bool _isInEditMode = false;
        [GeneratedRegex(@"\^(?<color>[0-9A-Fa-f]{6})(?<content>.*?)(?=(\^[0-9A-Fa-f]{6})|$)", RegexOptions.Singleline)]
        private static partial Regex ColorTagRegex();

        public MainWindow()
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            _settings = ApplicationSettings.Load();
            _shop = new Shop();
            this.Closing += (_, e) =>
            {
                if (!_isClosingHandled)
                {
                    e.Cancel = true;
                    _ = HandleClosingAsync();
                }
            };
        }

        /// <summary>
        /// Disparado ao abrir a janela
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (_settings.AlwaysFetchLastGshop)
            {
                _shop = await ContinueGshop.Handle(this);
            }
            else
            {
                var menuWindow = new MenuWindow()
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SystemDecorations = SystemDecorations.None, // mantém estilo modal sem barra
                    CanResize = false
                };
                _shop = await menuWindow.ShowDialog<Shop>(this);
            }

            if (_shop is null)
            {
                Close();
                return;
            }

            RedoButton.IsEnabled = false;
            UndoButton.IsEnabled = false;
            _tracking.PropertyChanged += OnTrackingChange;
            CategoryList.ItemsSource = _shop.Categories;
            DetailClassComboBox.ItemsSource = ItemClass;
            DetailCategoryComboBox.ItemsSource = _shop.Categories.Select(c => c.Title).ToList();
            SelectFirstCategory();
        }

        private async Task HandleClosingAsync()
        {
            if (_isClosingHandled)
                return;

            if (await ConfirmDiscardChanges())
            {
                _isClosingHandled = true;
                this.Close();
            }
        }

        #region Item Events

        private async void OnItemCard_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid)
            {
                if (!await ConfirmDiscardChanges()) return;
                var item = (grid.Tag as Item);
                if (item == null) return;
                SelectItem(item);
                if (grid.Parent is Border current)
                {
                    RemoveShadow(_lastSelectedBorder);
                    ApplyGoldShadow(current);
                    _lastSelectedBorder = current;
                }
            }
        }

        private async void ItemCopyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Item item)
            {
                if (!await ConfirmDiscardChanges()) return;
                if (item == null) return;

                _currentState = DetailState.Creating;
                SetItemDetailsEnabled();
                FillItemDetails(item);

                RemoveShadow(_lastSelectedBorder);
                _lastSelectedBorder = null;
            }
        }

        private async void ItemEditButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Item item)
            {
                if (!await ConfirmDiscardChanges()) return;
                if (item == null) return;

                _currentState = DetailState.Editing;
                SetItemDetailsEnabled();
                FillItemDetails(item);

                if (btn.Parent is Grid grid && grid.Parent is Border current)
                {
                    RemoveShadow(_lastSelectedBorder);

                    ApplyGoldShadow(current);
                    _lastSelectedBorder = current;
                }
            }
        }

        private async void ItemDeleteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Item item)
            {
                if (!await ConfirmDiscardChanges()) return;
                if (item == null) return;

                FillItemDetails(item);

                if (btn.Parent is Grid grid && grid.Parent is Border current)
                {
                    RemoveShadow(_lastSelectedBorder);

                    ApplyGoldShadow(current);
                    _lastSelectedBorder = current;
                }

                if (await DeleteItem.Handle(item, _shop.Itens, _tracking, this))
                {
                    UpdateGrid();
                    _currentState = DetailState.Viewing;
                    SetItemDetailsEnabled();
                }
            }
        }

        private async void DetailNew_Click(object? sender, RoutedEventArgs e)
        {
            if (!await ConfirmDiscardChanges()) return;

            _currentState = DetailState.Creating;

            FillItemDetails(new Item());
            DetailCategoryComboBox.SelectedIndex = _selectedCategory == -1 ? 0 : _selectedCategory;
            DetailSubCategoryComboBox.SelectedIndex = _selectedSubCategory == -1 ? 0 : _selectedSubCategory;
            SetItemDetailsEnabled();
        }

        private async void DetailEdit_Click(object? sender, RoutedEventArgs e)
        {
            if (DetailPanel.Tag is Item item)
            {
                if (!await ConfirmDiscardChanges()) return;
                _currentState = DetailState.Editing;
                FillItemDetails(item);
                SetItemDetailsEnabled();
            }
        }

        private async void DetailDelete_Click(object? sender, RoutedEventArgs e)
        {
            if (!await ConfirmDiscardChanges()) return;

            if (DetailPanel.Tag is Item item)
            {
                if (await DeleteItem.Handle(item, _shop.Itens, _tracking, this))
                {
                    UpdateGrid();
                    _currentState = DetailState.Viewing;
                    SetItemDetailsEnabled();
                }
            }
        }

        private void AddOwner_Click(object? sender, RoutedEventArgs e)
        {
            if (DetailPanel.Tag is Item item)
            {
                if (string.IsNullOrWhiteSpace(DetailOwnerAdd.Text)) return;

                if (int.TryParse(DetailOwnerAdd.Text, out int newNpcId))
                {
                    var list = item.OwnerNpcs.ToList();

                    if (!list.Contains(newNpcId))
                    {
                        list.Add(newNpcId);
                        item.OwnerNpcs = [.. list];

                        DetailOwnerAdd.Text = string.Empty;
                        FillOwnerList(item.OwnerNpcs);
                    }
                }
            }
        }

        private void DetailOwnerAdd_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddOwner_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void RemoveOwner_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int ownerId && DetailPanel.Tag is Item item)
            {
                var msgBox = new CustomMessageBox(
                "Remover Owner?",
                $"ID: {ownerId}\nName: Unknown", // Fazer pesquisa pelo nome do NPC
                CustomMessageBox.MessageBoxIcon.Question,
                "Sim", "Não");
                var result = await msgBox.ShowDialogAsync(this);

                if (result == "Sim")
                {
                    item.OwnerNpcs = [.. item.OwnerNpcs.Where(id => id != ownerId)];
                    FillOwnerList(item.OwnerNpcs);
                }
            }
        }

        private async void SaleEditor_Click(object? sender, RoutedEventArgs e)
        {
            if (DetailPanel.Tag is Item item)
            {
                var editorWindow = new SaleEditorWindow(item)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    SystemDecorations = SystemDecorations.None, // mantém estilo modal sem barra
                    CanResize = false
                };

                // Pega tamanho do MainWindow e do MenuWindow
                var mainBounds = this.Bounds;

                editorWindow.Position = new PixelPoint(
                    (int)(Position.X + (mainBounds.Width - editorWindow.Width) / 2),
                    (int)(Position.Y + (mainBounds.Height - editorWindow.Height) / 2)
                );

                item = await editorWindow.ShowDialog<Item>(this);
            }
        }
        #endregion

        #region Category Events

        private async void OnCategoryClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is Category category && grid.Parent is Border current)
            {
                if (!await ConfirmDiscardChanges()) return;
                _currentState = DetailState.Viewing;
                SetItemDetailsEnabled();

                int categoryIndex = _shop!.Categories.IndexOf(category);
                SearchBox.Text = string.Empty;

                if (_lastSelectedCategory == current)
                {
                    RemoveShadow(current);
                    _lastSelectedCategory = null;
                    SubCategoryList.ItemsSource = null;
                    DetailSubCategoryComboBox.ItemsSource = null;
                    _selectedCategory = -1;
                    _selectedSubCategory = -1;
                    UpdateGrid();
                    return;
                }

                RemoveShadow(_lastSelectedCategory);

                ApplyGoldShadow(current);
                _lastSelectedCategory = current;

                SubCategoryList.ItemsSource = category.SubCategories;
                DetailSubCategoryComboBox.ItemsSource = category.SubCategories;

                CategoryToggled(categoryIndex);

            }
        }

        private async void OnSubCategoryClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is string subCategory && grid.Parent is Border current)
            {
                if (!await ConfirmDiscardChanges()) return;
                _currentState = DetailState.Viewing;
                SetItemDetailsEnabled();
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
                item.ID,
                item.Name,
                item.Amount,
                LimitPeriod = item.LimitPeriod == 0 ? "Perm." : $"{item.LimitPeriod} dias",
                Price = item.GetValidPrice().ToString("F2", CultureInfo.InvariantCulture),
                IconBitmap = item.LoadImage()
            }).ToList();

            var repeater = this.FindControl<ItemsRepeater>("ItemList");
            if (repeater != null)
            {
                repeater.ItemsSource = convertedList;
            }
        }

        private async Task<bool> ConfirmDiscardChanges()
        {
            if (_currentState == DetailState.Viewing) return true;

            var msgBox = new CustomMessageBox(
                "Descartar Alterações?",
                "Você tem alterações não salvas. Deseja descartá-las?",
                CustomMessageBox.MessageBoxIcon.Information,
                "Sim", "Cancelar");
            var result = await msgBox.ShowDialogAsync(this);
            return result == "Sim";
        }

        private void ToggleExplanationMode()
        {
            _isInEditMode = !_isInEditMode;

            if (_isInEditMode && DetailExplanation.Inlines != null)
            {
                var formattedText = new StringBuilder();

                foreach (var inline in DetailExplanation.Inlines)
                {
                    if (inline is Run run)
                    {
                        // Pega a cor no formato "#RRGGBB"
                        var hex = run.Foreground is ISolidColorBrush brush
                            ? brush.Color.ToUInt32().ToString("x8")[2..]  // Remove alpha e deixa minúsculo
                            : "ffffff"; // cor padrão

                        // Texto com \n convertido para \r
                        var text = run.Text?.Replace("\n", @"\r") ?? string.Empty;

                        // Adiciona a tag de cor com o texto
                        formattedText.Append($"^{hex}{text}");
                    }
                }

                DetailExplanationEditor.Text = formattedText.ToString();
            }
            else
            {
                SetExplanationFormatted(DetailExplanationEditor.Text);
            }

            DetailExplanationPreview.IsVisible = !_isInEditMode;
            DetailExplanationEditor.IsVisible = _isInEditMode;
            DetailPaletteButton.IsVisible = _isInEditMode;

            var previewIcon = _isInEditMode
                ? new Uri("avares://Ultimate_Gshop/Resources/unpreview.png")
                : new Uri("avares://Ultimate_Gshop/Resources/preview.png");

            PreviewButtonIcon.Source = new Bitmap(AssetLoader.Open(previewIcon));
        }

        private void PreviewToggle_Click(object? sender, RoutedEventArgs e)
        {
            ToggleExplanationMode();
        }

        private async void DetailSave_Click(object? sender, RoutedEventArgs e)
        {
            var msgBox = new CustomMessageBox(
                (_currentState == DetailState.Creating) ? "Criar Novo Item?" : "Editar Item?",
                "Confirme apenas se voce tiver certeza!",
                CustomMessageBox.MessageBoxIcon.Question,
                "Sim", "Cancelar");
            var result = await msgBox.ShowDialogAsync(this);

            if (result == "Sim")
            {
                // Permite vírgula ou ponto como separador decimal
                var giftPriceText = DetailGiftPrice.Text?.Replace(',', '.') ?? "0";

                // Tenta converter para decimal e depois multiplica por 100
                var giftPrice = decimal.TryParse(giftPriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                    ? (int)(parsed * 100)
                    : 0;
                int ParseInt(string? text) => int.TryParse(text, out var value) ? value : 0;

                var item = new Item()
                {
                    ID = ParseInt(DetailID.Text),
                    Name = DetailName.Text ?? string.Empty,
                    Amount = ParseInt(DetailAmount.Text),
                    Explanation = DetailExplanationEditor.Text ?? string.Empty,
                    Place = _shop.Itens.FindIndex(i => i.ID == ParseInt(DetailID.Text)),
                    CategoryIndex = DetailCategoryComboBox.SelectedIndex,
                    SubCategoryIndex = DetailSubCategoryComboBox.SelectedIndex,
                    Icon = DetailIcon.Tag as string ?? string.Empty,
                    Sales = [new Sale()],
                    GiftID = ParseInt(DetailGiftID.Text),
                    GiftAmount = ParseInt(DetailGiftAmount.Text),
                    GiftTime = ParseInt(DetailGiftTime.Text),
                    ILogPrice = Convert.ToInt32(giftPrice),
                    OwnerNpcs = [],
                    LimitPeriod = ParseInt(DetailLimitPeriod.Text),
                    AvailFrequency = ParseInt(DetailAvailFrequency.Text),
                    Class = DetailClassComboBox.SelectedIndex - 1,
                    Reserve1 = ParseInt(DetailReserve1.Text),
                    Reserve2 = ParseInt(DetailReserve2.Text),
                    Reserve3 = ParseInt(DetailReserve3.Text),
                    Reserve4 = ParseInt(DetailReserve4.Text),
                    Reserve5 = ParseInt(DetailReserve5.Text),
                    Reserve6 = ParseInt(DetailReserve6.Text)
                };


                if (_currentState == DetailState.Creating)
                {
                    InsertItem.Handle(_shop.Itens, item, _tracking);
                }
                else
                {
                    UpdateItem.Handle(_shop.Itens, item, _currentSelectedItem!, _tracking);
                }

                _currentState = DetailState.Viewing;
                SetItemDetailsEnabled();

                _selectedCategory = item.CategoryIndex;
                _selectedSubCategory = item.SubCategoryIndex;
                UpdateGrid();
            }
        }

        private async void DetailUndo_Click(object? sender, RoutedEventArgs e)
        {
            if (!await ConfirmDiscardChanges()) return;


            if (DetailPanel.Tag is Item item && _currentState != DetailState.Creating)
            {
                FillItemDetails(item);
            }
            else
            {
                FillItemDetails(null);
            }
            _currentState = DetailState.Viewing;
            SetItemDetailsEnabled();
        }

        private void SetItemDetailsEnabled()
        {
            bool isViewing = _currentState == DetailState.Viewing;

            DetailID.IsEnabled = !isViewing;
            DetailName.IsEnabled = !isViewing;
            DetailLimitPeriod.IsEnabled = !isViewing;
            DetailAvailFrequency.IsEnabled = !isViewing;
            DetailAmount.IsEnabled = !isViewing;
            DetailExplanationEditor.IsEnabled = !isViewing;
            DetailPaletteButton.IsEnabled = !isViewing;
            DetailPaletteButton.Opacity = !isViewing ? 1 : 0.25;
            DetailSaleButton.IsEnabled = !isViewing;
            DetailSaleButton.Opacity = !isViewing ? 0.8 : 0.25;

            DetailClassComboBox.IsEnabled = !isViewing;
            DetailCategoryComboBox.IsEnabled = !isViewing;
            DetailSubCategoryComboBox.IsEnabled = !isViewing;
            
            // Gift
            DetailGiftID.IsEnabled = !isViewing;
            DetailGiftAmount.IsEnabled = !isViewing;
            DetailGiftPrice.IsEnabled = !isViewing;
            DetailGiftTime.IsEnabled = !isViewing;

            DetailReserve1.IsEnabled = !isViewing;
            DetailReserve2.IsEnabled = !isViewing;
            DetailReserve3.IsEnabled = !isViewing;
            DetailReserve4.IsEnabled = !isViewing;
            DetailReserve5.IsEnabled = !isViewing;
            DetailReserve6.IsEnabled = !isViewing;
            DetailOwnerAdd.IsEnabled = !isViewing;

            // Buttons
            DetailNewButton.IsVisible = isViewing;
            DetailEditButton.IsVisible = isViewing;
            DetailDeleteButton.IsVisible = isViewing;

            DetailSaveButton.IsVisible = !isViewing;
            DetailUndoButton.IsVisible = !isViewing;

            DetailOwnerAddButton.IsEnabled = !isViewing;
            DetailOwnerAddButton.Opacity = !isViewing ? 0.8 : 0.25;
            DetailOwnerList.Resources["IsEnableDetailOwnerRemoveButton"] = _currentState != DetailState.Viewing;
            DetailOwnerList.Resources["OpacityDetailOwnerRemoveButton"] = !isViewing ? 0.8 : 0.25;
        }


        private void FillItemDetails(Item? item)
        {
            // Ao preencher detalhes, sempre em modo de visualização
            _isInEditMode = true;
            ToggleExplanationMode();

            if (item == null)
            {
                DetailPanel.Tag = null;
                DetailIcon.Source = null;
                DetailIcon.Tag = null;
                DetailID.Text = string.Empty;
                DetailName.Text = string.Empty;
                DetailLimitPeriod.Text = string.Empty;
                DetailAvailFrequency.Text = string.Empty;
                DetailAmount.Text = string.Empty;
                SetExplanationFormatted(string.Empty);
                DetailCategoryComboBox.SelectedIndex = -1;
                DetailSubCategoryComboBox.SelectedIndex = -1;
                DetailClassComboBox.SelectedIndex = -1;
                // Gift
                DetailGiftIcon.Source = null;
                DetailGiftName.Text = string.Empty;
                DetailGiftID.Text = string.Empty;
                DetailGiftAmount.Text = string.Empty;
                DetailGiftPrice.Text = string.Empty;
                DetailGiftTime.Text = string.Empty;

                DetailReserve1.Text = string.Empty;
                DetailReserve2.Text = string.Empty;
                DetailReserve3.Text = string.Empty;
                DetailReserve4.Text = string.Empty;
                DetailReserve5.Text = string.Empty;
                DetailReserve6.Text = string.Empty;
                FillOwnerList([]);
                return;
            }

            DetailPanel.Tag = item;
            DetailIcon.Source = item.LoadImage();
            DetailIcon.Tag = item.Icon;
            DetailID.Text = item.ID.ToString();
            DetailName.Text = item.Name;
            DetailLimitPeriod.Text = item.LimitPeriod.ToString();
            DetailAvailFrequency.Text = item.AvailFrequency.ToString();
            DetailAmount.Text = item.Amount.ToString();
            SetExplanationFormatted(item.Explanation);
            DetailCategoryComboBox.SelectedIndex = item.CategoryIndex;
            DetailSubCategoryComboBox.SelectedIndex = item.SubCategoryIndex;
            DetailClassComboBox.SelectedIndex = item.Class + 1;
            // Gift
            DetailGiftIcon.Source = _shop.Itens
                .FirstOrDefault(i => i.ID == item.GiftID)?.LoadImage() ?? null;
            DetailGiftName.Text = _shop.Itens
                .FirstOrDefault(i => i.ID == item.GiftID)?.Name ?? "";
            DetailGiftID.Text = item.GiftID.ToString();
            DetailGiftAmount.Text = item.GiftAmount.ToString();
            DetailGiftPrice.Text = (item.ILogPrice / 100m).ToString("F2", CultureInfo.InvariantCulture);

            DetailGiftTime.Text = item.GiftTime.ToString();

            DetailReserve1.Text = item.Reserve1.ToString();
            DetailReserve2.Text = item.Reserve2.ToString();
            DetailReserve3.Text = item.Reserve3.ToString();
            DetailReserve4.Text = item.Reserve4.ToString();
            DetailReserve5.Text = item.Reserve5.ToString();
            DetailReserve6.Text = item.Reserve6.ToString();
            FillOwnerList(item.OwnerNpcs);
        }

        private void FillOwnerList(int[] ownerList)
        {
            if (ownerList == null || ownerList.Length == 0)
            {
                // mostre vazio
                DetailOwnerList.ItemsSource = Array.Empty<object>();
                return;
            }

            var rows = ownerList
                .Where(id => id > 0)
                .Distinct()
                .Select(id => new
                {
                    DetailOwnerID = id,
                    DetailOwnerName = "Unknown" // futuramente: lookup em tabela de NPCs
                })
                .ToList();

            DetailOwnerList.ItemsSource = rows;
        }

        private void SetExplanationFormatted(string? text)
        {
            if (DetailExplanation.Inlines == null) return;
            if (string.IsNullOrWhiteSpace(text))
            {
                DetailExplanation.Inlines.Clear();
                return;
            }

            DetailExplanation.Inlines.Clear();

            foreach (Match match in ColorTagRegex().Matches(text))
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

        private async void DetailPaletteButton_Click(object? sender, RoutedEventArgs e)
        {
            var color = await ColorPickerWindow.ShowAsync(this);
            if (!color.HasValue)
                return;

            if (!DetailExplanationEditor.IsVisible)
                return;

            if (DetailExplanationEditor.Text == null)
                return;

            var editor = DetailExplanationEditor;
            var caretIndex = editor.CaretIndex;
            var selectedText = editor.SelectedText;
            var selectionStart = editor.SelectionStart;
            var selectionEnd = editor.SelectionEnd;

            var hex = color.Value.ToUInt32().ToString("x8")[2..];

            if (string.IsNullOrEmpty(selectedText))
            {
                // Nenhuma seleção: apenas insere a cor
                editor.Text = editor.Text.Insert(caretIndex, $"^{hex}");
                editor.CaretIndex = caretIndex + hex.Length + 1; // posiciona depois do ^
            }
            else
            {
                // Com seleção: aplica a cor no texto selecionado
                editor.Text = editor.Text.Remove(selectionStart, selectionEnd - selectionStart)
                                         .Insert(selectionStart, $"^{hex}{selectedText}");

                editor.CaretIndex = selectionStart + hex.Length + 1 + selectedText.Length;
            }
        }


        private void SelectFirstCategory()
        {
            if (_shop == null) return;
            var firstCategory = _shop.Categories.FirstOrDefault();
            if (firstCategory == null) return;

            SubCategoryList.ItemsSource = firstCategory.SubCategories;
            DetailSubCategoryComboBox.ItemsSource = firstCategory.SubCategories;
            int categoryIndex = _shop.Categories.IndexOf(firstCategory);

            CategoryList.ApplyTemplate();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var container = CategoryList.ContainerFromIndex(categoryIndex) as ContentPresenter;
                if (container?.Child is Border border)
                {
                    ApplyGoldShadow(border);
                    _lastSelectedCategory = border;
                }
                else if (container?.Child is Grid grid && grid.Parent is Border b)
                {
                    ApplyGoldShadow(b);
                    _lastSelectedCategory = b;
                }

                CategoryToggled(categoryIndex);
            });
        }

        private void SelectItem(Item item)
        {
            if (item == null) return;
            _currentState = DetailState.Viewing;
            SetItemDetailsEnabled();
            FillItemDetails(item);
            _currentSelectedItem = item;
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

        private async void NavButton_Click(object? sender, RoutedEventArgs e)
        {
            var menuWindow = new MenuWindow(_shop, _settings)
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                SystemDecorations = SystemDecorations.None, // mantém estilo modal sem barra
                CanResize = false
            };

            // Pega tamanho do MainWindow e do MenuWindow
            var mainBounds = this.Bounds;

            menuWindow.Position = new PixelPoint(
                (int)(Position.X + (mainBounds.Width - menuWindow.Width) / 2),
                (int)(Position.Y + (mainBounds.Height - menuWindow.Height) / 2)
            );

            await menuWindow.ShowDialog<Shop>(this);
        }

        private void Undo_Click(object? sender, RoutedEventArgs e)
        {
            _tracking.Undo(_shop);
            UpdateGrid();
        }
        private void Redo_Click(object? sender, RoutedEventArgs e)
        {
            _tracking.Redo(_shop);
            UpdateGrid();
        }

        private void OnTrackingChange(object? sender, PropertyChangedEventArgs e)
        {
            UndoButton.IsEnabled = _tracking.CanUndo;
            RedoButton.IsEnabled = _tracking.CanRedo;

            UndoButton.Opacity = _tracking.CanUndo ? 0.95 : 0.25;
            RedoButton.Opacity = _tracking.CanRedo ? 0.95 : 0.25;
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