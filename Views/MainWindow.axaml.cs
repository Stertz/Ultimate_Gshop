using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Globalization;
using Ultimate_Gshop.Categories;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Itens.Services;
using Ultimate_Gshop.Misc;
using Ultimate_Gshop.Views;
using Ultimate_Gshop.Views.UserControls;

namespace Ultimate_Gshop
{
    public partial class MainWindow : Window
    {
        private bool _isClosingHandled = false;

        private DispatcherTimer? _saveTimer;

        private static readonly List<string> ItemClass = ["All", "Blademaster", "Wizard", "Psychic", "Venomancer", "Barbarian", "Assassin", "Archer", "Cleric", "Seeker", "Mystic", "Duskblade", "Stormbringer"];

        private Shop _shop = new();
        private ShopTracking _tracking = new();
        private GshopSettings _settings = new();

        private Border? _lastSelectedBorder;
        private Border? _lastSelectedCategory;
        private Border? _lastSelectedSubCategory;

        private IQueryable<Item>? _currentItems;
        private List<Item> _currentSelectedItems = [];
        private Item? _currentSelectedItem;
        private int _selectedCategory = -1;
        private int _selectedSubCategory = -1;

        /* Campos para Detail Items*/
        private enum DetailState { Viewing, Creating, Editing }
        private DetailState _currentState = DetailState.Viewing;

        public MainWindow()
        {
            InitializeComponent();
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

            Cursor = ApplicationSettings.InitializeCustomCursor();

            _shop = ApplicationSettings.GshopExists
                ? ContinueGshop.Handle()
                : await ReturnGshopFromMenu();

            if (_shop is null) { Close(); return; }

            // Routed events essenciais
            DetailGiftID.AddHandler(KeyDownEvent, DetailGiftID_KeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            DetailGiftID.AddHandler(LostFocusEvent, DetailGiftID_LostFocus, RoutingStrategies.Bubble);
            SearchBox.AddHandler(InputElement.KeyUpEvent, SearchBox_KeyUp, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

            _settings = GshopSettings.Load();

            UndoButton.IsEnabled = false;
            RedoButton.IsEnabled = false;

            _tracking = ShopTracking.Load();
            _tracking.PropertyChanged += OnTrackingChange;
            RefreshTracking();

            if (_settings.AutoSave)
            {
                _saveTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds((_settings.SaveFromSeconds ?? 30))
                };

                _saveTimer.Tick += SaveTimer_Tick;
                _saveTimer.Start();
            }

            InitializeShopSources();

            // ItemsList: copiar (clona e posiciona no próximo Place)
            ItemsList.CopyRequested += async (_, item) =>
            {
                if (!await ConfirmDiscardChanges() || item is null) return;

                var cloned = item.Clone();                     // clone
                var maxPlace = _shop.Itens.Count != 0 ? _shop.Itens.Max(i => i.Place) : 0;
                cloned.Place = maxPlace + 1;                   // próximo slot

                _currentState = DetailState.Creating;
                SetItemDetailsEnabled();
                FillItemDetails(cloned);
            };

            // ItemsList: editar
            ItemsList.EditRequested += async (_, item) =>
            {
                if (!await ConfirmDiscardChanges()) return;

                _currentState = DetailState.Editing;
                _currentSelectedItem = item;
                SetItemDetailsEnabled();
                FillItemDetails(item);
            };

            // ItemsList: excluir
            ItemsList.DeleteRequested += async (_, item) =>
            {
                if (!await ConfirmDiscardChanges()) return;

                FillItemDetails(item);
                if (await DeleteItem.Handle(item, _shop.Itens, _tracking, this))
                {
                    UpdateGrid();
                    _currentState = DetailState.Viewing;
                    SetItemDetailsEnabled();
                }
            };

            // ItemsList: seleção
            ItemsList.ItemPressed += async (_, item) =>
            {
                if (!await ConfirmDiscardChanges()) return;
                SelectItem(item);
            };

            // ItemsList: reorder (recalcula Place preservando base do escopo)
            ItemsList.ReorderRequested += (_, args) =>
            {
                var scope = _shop.Itens
                    .Where(i => i.CategoryIndex == _selectedCategory &&
                                (_selectedSubCategory < 0 || i.SubCategoryIndex == _selectedSubCategory))
                    .OrderBy(i => i.Place)
                    .ToList();

                if (scope.Count == 0) return;

                var basePlace = scope.Min(i => i.Place);
                var srcIdx = scope.FindIndex(i => i.Place == args.SourcePlace);
                if (srcIdx < 0) return;

                var moving = scope[srcIdx];
                scope.RemoveAt(srcIdx);

                var insertIdx = Math.Clamp(args.TargetIndex, 0, scope.Count);
                scope.Insert(insertIdx, moving);

                for (int i = 0; i < scope.Count; i++)
                    scope[i].Place = basePlace + i;

                UpdateGrid();
                ItemsList.SelectByPlace(moving.Place);
                SelectItem(moving.Clone());
            };

            ItemsList.MultipleCloneRequested += async (_, itens) => {

                if (!await ConfirmDiscardChanges()) return;
                if (itens is null || itens.Count == 0) return;

                foreach (var item in itens)
                {
                    if(item is null) continue;

                    var cloned = item.Clone();
                    var maxPlace = _shop.Itens.Count != 0 ? _shop.Itens.Max(i => i.Place) : 0;
                    cloned.Place = maxPlace + 1;

                    InsertItem.Handle(_shop.Itens, item, _tracking);
                    _shop.ItemAmount++;
                }

                UpdateGrid();

            };

            ItemsList.MultipleEditRequested += (_, args) => { };

            ItemsList.MultipleDeleteRequested += async (_, itens) => {

                if (!await ConfirmDiscardChanges()) return;
                if (itens is null || itens.Count == 0) return;

                foreach (var item in itens)
                {
                    if (item is null) continue;

                    var index = _shop.Itens.IndexOf(item);

                    if (_shop.Itens.Remove(item))
                    {
                        _tracking.ItemDeleted(index, item);
                    }
                }

                UpdateGrid();

            };

        }

        private async void InitializeShopSources()
        {
            CategoryList.ItemsSource = _shop.Categories;
            DetailClassComboBox.ItemsSource = ItemClass;
            DetailCategoryComboBox.ItemsSource = _shop.Categories.Select(c => c.Title).ToList();

            var categories = _shop.Categories;
            var SubCategoriesResolver = new Func<object?, IEnumerable<string>>(categoryTitle =>
            {
                if (categoryTitle is string title)
                {
                    var category = categories.FirstOrDefault(c => c.Title == title);
                    return category?.SubCategories ?? [];
                }
                return [];
            });

            // Itens do primeiro combo
            var combo1Items = categories.Select(c => c.Title).ToList();

            var dlg = new CustomMessageBox(
                title: "Trocar Categoria",
                message: "Escolha a categoria e a subcategoria",
                icon: CustomMessageBox.MessageBoxIcon.Information,
                combo1Label: "Category",
                combo1Items: combo1Items,
                selected1: combo1Items.FirstOrDefault(),
                combo2Label: "SubCategory",
                combo2Resolver: SubCategoriesResolver,
                selected2: null,
                buttons: ["OK", "Cancelar"]
            );

            var res = await dlg.ShowComboDialogAsync(this);

            if (res.Button == "OK")
            {
                var categoryTitle = res.Selected1 as string;
                var subcategory = res.Selected2 as string;

                // Exemplo de uso:
                // - recuperar a Category pelo título
                var category = categories.FirstOrDefault(c => c.Title == categoryTitle);
                // - aplicar sua lógica (ex.: salvar filtro, atualizar lista, etc.)
            }

            if (_shop.Categories.Count > 0) // Seleciona a primeira categoria e primeira subcategoria
                SelectCategoryAndSubCategory(0, 0);
        }

        private void SaveTimer_Tick(object? sender, EventArgs e) => SaveGshop.Handle(_shop, _tracking, _settings);
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            if (!_isClosingHandled)
            {
                e.Cancel = true;
                _ = HandleClosingAsync();
            }
        }

        private async Task<Shop> ReturnGshopFromMenu()
        {
            var menuWindow = new MenuWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                CanResize = false
            };
            return await menuWindow.ShowDialog<Shop>(this);
        }

        private async Task HandleClosingAsync()
        {
            if (_isClosingHandled)
                return;

            if (await ConfirmDiscardChanges())
            {
                _isClosingHandled = true;
                _saveTimer?.Stop();
                SaveGshop.Handle(_shop, _tracking, _settings);
                Close();
            }
        }

        #region Item Events
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

                item.Sales = await editorWindow.ShowDialog<List<Sale>>(this);
            }
        }
        #endregion

        #region Category Events

        private async void OnCategoryClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is Category category && grid.Parent is Border current)
            {
                if (!await ConfirmDiscardChanges()) return;
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && _lastSelectedCategory == current) return;
                // Toggle off se clicar na mesma categoria
                if (_lastSelectedCategory == current)
                {
                    SelectCategoryAndSubCategory();
                    return;
                }

                var categoryIndex = _shop!.Categories.IndexOf(category);
                SelectCategoryAndSubCategory(categoryIndex);
            }
        }

        private async void OnSubCategoryClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is string subCategory && grid.Parent is Border current)
            {
                if (!await ConfirmDiscardChanges()) return;
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && _lastSelectedSubCategory == current) return;

                if (_lastSelectedCategory?.Child is not Grid categoryGrid ||
                    categoryGrid.DataContext is not Category category)
                    return;

                int subCategoryIndex = category.SubCategories.IndexOf(subCategory);


                if (_lastSelectedSubCategory == current)
                {
                    SelectCategoryAndSubCategory(_selectedCategory);
                    return;
                }
                var categoryIndex = _shop!.Categories.IndexOf(category);
                SelectCategoryAndSubCategory(categoryIndex, subCategoryIndex);
            }
        }

        private async void OnRenameCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedCategory == -1) return;
            if (!await ConfirmDiscardChanges()) return;

            var dlg = new CustomMessageBox(
                title: "Renomear",
                message: "Informe o novo nome para Categoria:",
                icon: CustomMessageBox.MessageBoxIcon.Information,
                withInput: true,
                inputLabel: "Category", // aparece no FloatingLabelTextBox
                initialText: _shop.Categories[_selectedCategory].Title,
                buttons: ["OK", "Cancelar"]
            );

            var result = await dlg.ShowInputDialogAsync(this);

            if (result.Button == "OK" && !string.IsNullOrWhiteSpace(result.Text))
            {
                _shop.Categories[_selectedCategory].Title = result.Text;
                CategoryList.ItemsSource = _shop.Categories.ToList();
                SelectCategoryAndSubCategory(_selectedCategory);
            }
        }

        private async void OnResetCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedCategory == -1) return;
            if (!await ConfirmDiscardChanges()) return;

            var dlg = new CustomMessageBox(
                title: "Resetar Categoria?",
                message: "Isso vai excluir TODOS os itens desta categoria. Deseja continuar?",
                icon: CustomMessageBox.MessageBoxIcon.Question,
                buttons: ["Sim", "Cancelar"]
            );

            var choice = await dlg.ShowDialogAsync(this);
            if (choice != "Sim") return;

            // Remove in-place para manter a referência da lista
            var removed = _shop.Itens.RemoveAll(i => i.CategoryIndex == _selectedCategory);

            SearchBox.Text = string.Empty;
            SelectCategoryAndSubCategory(_selectedCategory);

            await new CustomMessageBox(
                title: "Categoria resetada",
                message: removed > 0 ? $"{removed} itens removidos." : "Nenhum item para remover.",
                icon: CustomMessageBox.MessageBoxIcon.Information,
                buttons: ["OK"]
            ).ShowDialogAsync(this);
        }


        private async void OnAddSubCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedCategory == -1) return;
            if (!await ConfirmDiscardChanges()) return;

            var category = _shop.Categories[_selectedCategory];

            if (category.SubCategories.Count >= 11)
            {
                await new CustomMessageBox("Subcategoria", "Máximo de subcategorias atingido (11)", "Ok").ShowDialogAsync(this);
                return;
            }

            var dlg = new CustomMessageBox(
                title: "Nova Subcategoria",
                message: "Informe o nome:",
                icon: CustomMessageBox.MessageBoxIcon.Information,
                withInput: true,
                inputLabel: "Subcategory",
                initialText: "",
                buttons: ["OK", "Cancelar"]
            );

            var result = await dlg.ShowInputDialogAsync(this);

            if (result.Button == "OK" && !string.IsNullOrWhiteSpace(result.Text))
            {
                category.SubCategories.Add(result.Text);
                CategoryList.ItemsSource = _shop.Categories.ToList();
                SelectCategoryAndSubCategory(_selectedCategory, category.SubCategories.Count - 1);
            }
        }

        private async void OnRenameSubCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedCategory == -1 || _selectedSubCategory == -1) return;
            if (!await ConfirmDiscardChanges()) return;

            var category = _shop.Categories[_selectedCategory];

            var currentName = category.SubCategories[_selectedSubCategory];

            var dlg = new CustomMessageBox(
                title: "Renomear Subcategoria",
                message: "Informe o novo nome:",
                icon: CustomMessageBox.MessageBoxIcon.Information,
                withInput: true,
                inputLabel: "Subcategory",
                initialText: currentName,
                buttons: ["OK", "Cancelar"]
            );

            var result = await dlg.ShowInputDialogAsync(this);
            if (result.Button != "OK" || string.IsNullOrWhiteSpace(result.Text)) return;

            var newName = result.Text.Trim();
            if (newName.Length == 0) return;

            if (category.SubCategories.Contains(newName, StringComparer.OrdinalIgnoreCase))
            {
                await new CustomMessageBox("Renomear Subcategoria", "Já existe uma subcategoria com esse nome.", CustomMessageBox.MessageBoxIcon.Error, "OK").ShowDialogAsync(this);
                return;
            }

            category.SubCategories[_selectedSubCategory] = newName;

            // Rebind das fontes UI
            SubCategoryList.ItemsSource = category.SubCategories.ToList();
            DetailSubCategoryComboBox.ItemsSource = category.SubCategories.ToList();

            // Mantém a sub renomeada selecionada
            SelectCategoryAndSubCategory(_selectedCategory, _selectedSubCategory);
        }


        private async void OnResetSubCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedCategory == -1 || _selectedSubCategory == -1) return;
            if (!await ConfirmDiscardChanges()) return;

            var category = _shop.Categories[_selectedCategory];

            var subName = category.SubCategories[_selectedSubCategory];

            var dlg = new CustomMessageBox(
                title: "Resetar Subcategoria?",
                message: $"Isso vai excluir TODOS os itens da subcategoria \"{subName}\". Deseja continuar?",
                icon: CustomMessageBox.MessageBoxIcon.Question,
                buttons: ["Sim", "Cancelar"]
            );

            var choice = await dlg.ShowDialogAsync(this);
            if (choice != "Sim") return;

            // Remove in-place
            var removed = _shop.Itens.RemoveAll(i =>
                i.CategoryIndex == _selectedCategory && i.SubCategoryIndex == _selectedSubCategory);

            // Rebind UI e mantém mesma sub selecionada
            SearchBox.Text = string.Empty;
            SubCategoryList.ItemsSource = category.SubCategories.ToList();
            DetailSubCategoryComboBox.ItemsSource = category.SubCategories.ToList();

            SelectCategoryAndSubCategory(_selectedCategory, _selectedSubCategory);

            await new CustomMessageBox(
                title: "Subcategoria resetada",
                message: removed > 0 ? $"{removed} itens removidos." : "Nenhum item para remover.",
                icon: CustomMessageBox.MessageBoxIcon.Information,
                buttons: ["OK"]
            ).ShowDialogAsync(this);
        }


        private async void OnDeleteSubCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedCategory == -1 || _selectedSubCategory == -1) return;
            if (!await ConfirmDiscardChanges()) return;

            var category = _shop.Categories[_selectedCategory];
            var subName = category.SubCategories[_selectedSubCategory];

            // Validação: não deixa apagar se houver itens na sub
            bool hasItems = _shop.Itens.Any(i =>
                i.CategoryIndex == _selectedCategory && i.SubCategoryIndex == _selectedSubCategory);

            if (hasItems)
            {
                await new CustomMessageBox(
                    title: "Não é possível excluir",
                    message: $"Existem itens vinculados à subcategoria \"{subName}\". Remova-os ou mova-os antes de excluir.",
                    icon: CustomMessageBox.MessageBoxIcon.Error,
                    buttons: ["OK"]
                ).ShowDialogAsync(this);
                return;
            }

            // Confirmação
            var dlg = new CustomMessageBox(
                title: "Excluir Subcategoria?",
                message: $"Tem certeza que deseja excluir \"{subName}\"?",
                icon: CustomMessageBox.MessageBoxIcon.Question,
                buttons: ["Excluir", "Cancelar"]
            );

            var choice = await dlg.ShowDialogAsync(this);
            if (choice != "Excluir") return;

            // Remove a subcategoria
            category.SubCategories.RemoveAt(_selectedSubCategory);

            // Limpa seleção de sub e rebind das listas
            _selectedSubCategory = -1;
            RemoveShadow(_lastSelectedSubCategory);
            _lastSelectedSubCategory = null;

            SubCategoryList.ItemsSource = category.SubCategories.ToList();
            DetailSubCategoryComboBox.ItemsSource = category.SubCategories.ToList();

            // Seleciona apenas a categoria
            SelectCategoryAndSubCategory(_selectedCategory, -1);
        }


        private void SelectCategoryAndSubCategory(int categoryIndex = -1, int subCategoryIndex = -1)
        {
            if (_shop is null) return;
            if (categoryIndex < -1 || categoryIndex >= _shop.Categories.Count) return;

            // se não há categoria, não pode haver subcategoria
            if (categoryIndex == -1) subCategoryIndex = -1;

            // valida subcategoria
            if (categoryIndex > -1)
                if (subCategoryIndex < -1 || subCategoryIndex >= _shop.Categories[categoryIndex].SubCategories.Count) return;

            if (categoryIndex > -1)
            {
                // Atualiza fontes de subcategoria da categoria alvo
                var category = _shop.Categories[categoryIndex];
                SubCategoryList.ItemsSource = category.SubCategories.ToList();
                DetailSubCategoryComboBox.ItemsSource = category.SubCategories.ToList();
            }
            else
            {
                SubCategoryList.ItemsSource = null;
                DetailSubCategoryComboBox.ItemsSource = null;
            }
            _currentState = DetailState.Viewing;
            SetItemDetailsEnabled();
            SearchBox.Text = string.Empty;

            // limpa seleção visual anterior
            RemoveShadow(_lastSelectedSubCategory); _lastSelectedSubCategory = null;
            RemoveShadow(_lastSelectedCategory); _lastSelectedCategory = null;

            // atualiza seleção lógica
            _selectedCategory = categoryIndex;
            _selectedSubCategory = subCategoryIndex;

            // Aplica shadow da categoria (agarra o Border do item do CategoryList)
            CategoryList.ApplyTemplate();
            Dispatcher.UIThread.Post(() =>
            {
                var cp = CategoryList.ContainerFromIndex(categoryIndex) as ContentPresenter;
                Border? catBorder = null;

                if (cp?.Child is Border b1) catBorder = b1;
                else if (cp?.Child is Grid g1 && g1.Parent is Border b2) catBorder = b2;

                if (catBorder is not null)
                {
                    ApplyGoldShadow(catBorder);
                    _lastSelectedCategory = catBorder;
                }

                // Se houver subcategoria selecionada, aplica shadow nela também
                if (_selectedSubCategory >= 0)
                {
                    SubCategoryList.ApplyTemplate();
                    var scp = SubCategoryList.ContainerFromIndex(_selectedSubCategory) as ContentPresenter;
                    Border? subBorder = null;

                    if (scp?.Child is Border sb1) subBorder = sb1;
                    else if (scp?.Child is Grid sg1 && sg1.Parent is Border sb2) subBorder = sb2;

                    if (subBorder is not null)
                    {
                        ApplyGoldShadow(subBorder);
                        _lastSelectedSubCategory = subBorder;
                    }
                }
            }, DispatcherPriority.Render);

            // Atualiza grid conforme os campos selecionados
            UpdateGrid();
        }

        #endregion

        #region Search
        private async void SearchBox_KeyUp(object? sender, KeyEventArgs e)
        {
            await ExecuteSearchAsync();
        }

        private async Task ExecuteSearchAsync()
        {
            if (!await ConfirmDiscardChanges()) return;

            _currentState = DetailState.Viewing;
            SetItemDetailsEnabled();
            FillItemDetails(null);

            var query = SearchBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
            {
                UpdateGrid(); // reset quando vazio
                return;
            }

            // termos para busca por nome (like por partes)
            var searchTerms = query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // só considerar ID se o usuário digitou só números (permite parcial)
            bool isDigitsOnly = query.All(char.IsDigit);

            var filtered = _currentItems?
                .AsEnumerable()
                .Where(i =>
                {
                    var name = i.Name ?? string.Empty;

                    bool nameMatch = searchTerms.All(term =>
                    name.Contains(term, StringComparison.OrdinalIgnoreCase));

                    bool idMatch = isDigitsOnly && i.ID.ToString().Contains(query);

                    return nameMatch || idMatch;
                })
                .ToList();

            if (filtered != null)
            {
                _currentItems = filtered.AsQueryable();
                ItemsList.ItemsSource = _currentItems;
                ItemsList.FillTable();
            }
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

        private async void DetailSave_Click(object? sender, RoutedEventArgs e)
        {
            var msgBox = new CustomMessageBox(
                (_currentState == DetailState.Creating) ? "Criar Novo Item?" : "Editar Item?",
                "Confirme apenas se voce tiver certeza!",
                CustomMessageBox.MessageBoxIcon.Question,
                "Sim", "Cancelar");
            var result = await msgBox.ShowDialogAsync(this);

            if (result != "Sim") return;

            // Limpa SearchBox
            SearchBox.Text = string.Empty;

            // Fonte de verdade para Sales e OwnerNpcs é o item do painel
            var tagItem = DetailPanel.Tag as Item;

            // Permite vírgula ou ponto como separador decimal
            var giftPriceText = DetailGiftPrice.Text?.Replace(',', '.') ?? "0";
            var giftPrice = decimal.TryParse(giftPriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? (int)(parsed * 100)
                : 0;
            static int ParseInt(string? text) => int.TryParse(text, out var value) ? value : 0;

            // Sempre cria lista a partir do existente ou vazia
            var sales = (tagItem?.Sales?.Count > 0)
                ? [.. tagItem.Sales]
                : new List<Sale>();

            // Ajusta para ter exatamente 4 itens
            while (sales.Count < 4)
                sales.Add(new Sale()); // zerado

            if (sales.Count > 4)
                sales = [.. sales
                    .OrderByDescending(s => s.Price) // maiores preços primeiro
                    .Take(4)];

            // Normaliza OwnerNpcs: >0, distintos, no máximo 8, e preenche com zeros.
            int[] owners;
            {
                var list = (tagItem?.OwnerNpcs ?? [])
                    .Where(id => id > 0)
                    .Distinct()
                    .Take(8)
                    .ToList();
                while (list.Count < 8) list.Add(0);
                owners = [.. list];
            }

            // garantir PLACE único quando Creating
            var isCreating = _currentState == DetailState.Creating;
            var ensuredPlace = isCreating
                ? (_shop.Itens.Count != 0 ? _shop.Itens.Max(i => i.Place) + 1 : 0)      // novo place
                : (_currentSelectedItem?.Place ?? tagItem?.Place                        // mantém o place ao editar
                    ?? (_shop.Itens.Count != 0 ? _shop.Itens.Max(i => i.Place) + 1 : 0));

            var item = new Item()
            {
                ID = ParseInt(DetailID.Text),
                Name = DetailName.Text ?? string.Empty,
                Amount = ParseInt(DetailAmount.Text),
                Explanation = ExplanationEditorControl.ExportText(),
                Place = ensuredPlace,
                CategoryIndex = DetailCategoryComboBox.SelectedIndex,
                SubCategoryIndex = DetailSubCategoryComboBox.SelectedIndex,
                Icon = DetailIcon.Tag as string ?? string.Empty,

                Sales = sales,
                OwnerNpcs = owners,

                GiftID = ParseInt(DetailGiftID.Text),
                GiftAmount = ParseInt(DetailGiftAmount.Text),
                GiftTime = ParseInt(DetailGiftTime.Text),
                ILogPrice = giftPrice,
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
                _shop.ItemAmount++;
            }
            else
            {
                UpdateItem.Handle(_shop.Itens, item, _currentSelectedItem!, _tracking);
            }

            _currentState = DetailState.Viewing;
            SetItemDetailsEnabled();
            UpdateGrid();

            Dispatcher.UIThread.Post(() =>
            {
                // Re-seleciona e destaca o item salvo pelo Place
                ItemsList.SelectByPlace(item.Place);  // destaca e dá BringIntoView no card

                // Só leva pro fim se for novo (ou se estiver no último place)
                var lastPlace = _shop.Itens.Count > 0 ? _shop.Itens.Max(i => i.Place) : item.Place;
                if (item.Place == lastPlace)
                    ItemsList.ScrollToEnd();
            }, DispatcherPriority.Render);

        }

        private void UpdateGiftPreview()
        {
            // Limpa se inválido
            if (!int.TryParse(DetailGiftID.Text, out var giftId) || giftId <= 0)
            {
                DetailGiftIcon.Source = null;
                DetailGiftName.Text = string.Empty;
                return;
            }

            // Busca no shop
            var gift = _shop.Itens.FirstOrDefault(i => i.ID == giftId);

            if (gift is null)
            {
                DetailGiftIcon.Source = null;
                DetailGiftName.Text = "Not found";

                return;
            }

            // Atualiza preview
            DetailGiftIcon.Source = gift.LoadImage();
            DetailGiftName.Text = gift.Name;
        }

        private void DetailGiftID_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                UpdateGiftPreview();

                // Opcional: com Enter, avança o foco para o próximo campo
                if (e.Key == Key.Enter)
                {
                    DetailGiftAmount.Focus();
                    e.Handled = true; // evita comportamento inesperado
                }
            }
        }

        private void DetailGiftID_LostFocus(object? sender, RoutedEventArgs e)
        {
            UpdateGiftPreview();
        }

        private void DetailUndo_Click(object? sender, RoutedEventArgs e)
        {
            // Se estava criando, limpa o painel
            if (_currentState == DetailState.Creating)
            {
                FillItemDetails(null);
            }
            else
            {
                // Se estava editando, volta para o item original selecionado
                if (_currentSelectedItem != null)
                    FillItemDetails(_currentSelectedItem);
                else
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
            ExplanationEditorControl.IsEditing = !isViewing;
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
                ExplanationEditorControl.ImportTagged(string.Empty);
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
            item = item.Clone();

            DetailPanel.Tag = item;
            DetailIcon.Source = item.LoadImage();
            DetailIcon.Tag = item.Icon;
            DetailID.Text = item.ID.ToString();
            DetailName.Text = item.Name;
            DetailLimitPeriod.Text = item.LimitPeriod.ToString();
            DetailAvailFrequency.Text = item.AvailFrequency.ToString();
            DetailAmount.Text = item.Amount.ToString();
            ExplanationEditorControl.ImportTagged(item.Explanation);
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

        private void SelectItem(Item item)
        {
            if (item == null) return;
            _currentState = DetailState.Viewing;
            SetItemDetailsEnabled();
            FillItemDetails(item);
            _currentSelectedItem = item;
        }

        private void UpdateGrid()
        {
            if (_shop == null) return;

            var query = _shop.Itens.AsQueryable();

            if (_selectedCategory > -1)
                query = query.Where(i => i.CategoryIndex == _selectedCategory);

            if (_selectedSubCategory > -1 && _selectedCategory > -1)
                query = query.Where(i => i.SubCategoryIndex == _selectedSubCategory);

            RemoveShadow(_lastSelectedBorder);
            _lastSelectedBorder = null;
            FillItemDetails(null);

            _currentItems = query;
            ItemsList.ItemsSource = _currentItems;
            ItemsList.FillTable();
        }

        private async void NavButton_Click(object? sender, RoutedEventArgs e)
        {
            var menuWindow = new MenuWindow(_shop, _settings, _tracking)
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

            _shop = await menuWindow.ShowDialog<Shop>(this);

            InitializeShopSources();
            UpdateGrid();

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
            RefreshTracking();
        }

        private void RefreshTracking()
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