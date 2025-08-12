using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Threading;
using Ultimate_Gshop.Itens;

namespace Ultimate_Gshop.Views.UserControls;

public partial class ItemsListControl : UserControl
{
    // Fonte de dados (bind da lista)
    public static readonly StyledProperty<IEnumerable<Item>?> ItemsSourceProperty =
        AvaloniaProperty.Register<ItemsListControl, IEnumerable<Item>?>(nameof(ItemsSource));

    // Place selecionado externamente (para re-enfatizar um item pós-salvar, por ex.)
    public static readonly StyledProperty<int?> SelectedPlaceProperty =
        AvaloniaProperty.Register<ItemsListControl, int?>(nameof(SelectedPlace));

    public IEnumerable<Item>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public int? SelectedPlace { get => GetValue(SelectedPlaceProperty); set => SetValue(SelectedPlaceProperty, value); }

    public event EventHandler<Item>? ItemPressed;
    public event EventHandler<Item>? CopyRequested;
    public event EventHandler<Item>? EditRequested;
    public event EventHandler<Item>? DeleteRequested;

    // Timer para auto‑scroll enquanto um item arrastado está perto das bordas
    private DispatcherTimer? _autoScrollTimer;
    private int _autoScrollDir = 0;       // -1 para cima, +1 para baixo, 0 parado
    private double _autoScrollSpeed = 10; // pixels por tick (ajustado dinamicamente)

    public sealed class ReorderRequestEventArgs : EventArgs
    {
        public int SourcePlace { get; init; }
        public int TargetIndex { get; init; }
    }
    public event EventHandler<ReorderRequestEventArgs>? ReorderRequested;

    private Border? _lastSelectedBorder;
    private List<ItemRow> _rows = [];

    // Parâmetros de "arming" do drag: só inicia o DnD se segurar/esperar ou mover o suficiente.
    private const int DragStartDelayMs = 150;     // tempo mínimo segurando antes de permitir arrastar
    private const double DragStartThreshold = 6.0; // distância mínima caso o tempo não tenha passado
    private bool _dragPending;   // estado armado, aguardando critério para começar
    private bool _dragActive;    // DnD em andamento
    private Point _pressPos;     // posição inicial do press
    private DateTime _pressTime; // timestamp do press
    private Grid? _pressGrid;    // grid/card que recebeu o press (para captura)

    public ItemsListControl()
    {
        InitializeComponent();

        // Habilita área de drop no ScrollViewer raiz e controla auto-scroll via DragOver
        DragDrop.SetAllowDrop(RootScroll, true);
        RootScroll.AddHandler(DragDrop.DragOverEvent, OnRoot_DragOver, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        RootScroll.AddHandler(DragDrop.DragLeaveEvent, (_, __) => StopAutoScroll(), RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        // Reconstroi a UI quando a fonte muda e re-aplica destaque quando o Place muda
        this.GetObservable(ItemsSourceProperty).Subscribe(_ => FillTable());
        this.GetObservable(SelectedPlaceProperty).Subscribe(HighlightByPlace);
    }

    public void FillTable()
    {
        StopAutoScroll();

        // Projeta Item -> ItemRow (modelo visual), já ordenado por Place
        _rows = (ItemsSource?
            .OrderBy(i => i.Place)
            .Select(i => new ItemRow
            {
                Tag = i,
                ID = i.ID,
                Name = i.Name,
                Amount = i.Amount,
                LimitPeriod = i.LimitPeriod == 0 ? "Perm." : $"{i.LimitPeriod} dias",
                Price = i.GetValidPrice().ToString("F2", CultureInfo.InvariantCulture),
                IconBitmap = i.LoadImage(),
                Place = i.Place
            })
            .ToList()) ?? [];

        ItemList.ItemsSource = _rows;

        // Aguarda layout/virtual tree estabilizar para anexar handlers de DnD nos cards
        Dispatcher.UIThread.Post(() =>
        {
            AttachDnDToCards();
            if (SelectedPlace is int sp) HighlightByPlace(sp); // visual já existe agora
        }, DispatcherPriority.Background);
    }

    private void AttachDnDToCards()
    {
        
        // Percorre os cards visuais e injeta handlers de DragOver/Drop e ponteiro
        foreach (var b in ItemList.GetVisualDescendants().OfType<Border>().Where(x => x.Name == "CardBorder"))
        {
            DragDrop.SetAllowDrop(b, true);
            b.RemoveHandler(DragDrop.DragOverEvent, OnItem_DragOver);
            b.RemoveHandler(DragDrop.DropEvent, OnItem_Drop);
            b.AddHandler(DragDrop.DragOverEvent, OnItem_DragOver, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            b.AddHandler(DragDrop.DropEvent, OnItem_Drop, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

            /*var g = b.GetVisualDescendants().OfType<Grid>().FirstOrDefault();
            if (g != null)
            {
                // Também habilita na Grid interna para melhor área hit‑test
                DragDrop.SetAllowDrop(g, true);
                g.RemoveHandler(DragDrop.DragOverEvent, OnItem_DragOver);
                g.RemoveHandler(DragDrop.DropEvent, OnItem_Drop);
                g.AddHandler(DragDrop.DragOverEvent, OnItem_DragOver, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                g.AddHandler(DragDrop.DropEvent, OnItem_Drop, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

                // Handlers de ponteiro responsáveis por "armar" e iniciar o DnD
                g.RemoveHandler(PointerPressedEvent, OnItemCard_PointerPressed);
                g.RemoveHandler(PointerMovedEvent, OnItemCard_PointerMoved);
                g.RemoveHandler(PointerReleasedEvent, OnItemCard_PointerReleased);

                g.AddHandler(PointerPressedEvent, OnItemCard_PointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                g.AddHandler(PointerMovedEvent, OnItemCard_PointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                g.AddHandler(PointerReleasedEvent, OnItemCard_PointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

                g.RemoveHandler(Button.ClickEvent, ItemCopyButton_Click);
                g.RemoveHandler(Button.ClickEvent, ItemEditButton_Click);
                g.RemoveHandler(Button.ClickEvent, ItemDeleteButton_Click);

                g.AddHandler(Button.ClickEvent, ItemCopyButton_Click, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                g.AddHandler(Button.ClickEvent, ItemEditButton_Click, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                g.AddHandler(Button.ClickEvent, ItemDeleteButton_Click, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            }*/
        }
    }

    public void SelectByPlace(int place) => SelectedPlace = place;

    private void HighlightByPlace(int? place)
    {
        RemoveShadow(_lastSelectedBorder);
        if (place is null) return;

        foreach (var border in ItemList.GetVisualDescendants().OfType<Border>().Where(b => b.Name == "CardBorder"))
        {
            var g = border.GetVisualDescendants().OfType<Grid>().FirstOrDefault();
            if (g?.Tag is Item it && it.Place == place)
            {
                ApplyGoldShadow(border);
                _lastSelectedBorder = border;
                border.BringIntoView();

                // Dispara para o host preencher os detalhes
                ItemPressed?.Invoke(this, it);
                break;
            }
        }
    }

    // ---------- Armado do DnD (segurar/mover) ----------
    private void OnItemCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 1) Se veio de um Button, não armar drag nem capturar:
        if (e.Source is Control src && (src is Button || src.GetVisualAncestors().OfType<Button>().Any()))
            return;

        // 2) Seleção “lógica”
        if (sender is Grid g && g.Tag is Item item)
        {
            ItemPressed?.Invoke(this, item);
            SelectByPlace(item.Place);
        }

        var pt = e.GetCurrentPoint(this);
        if (!pt.Properties.IsLeftButtonPressed) return;

        // 3) Arma drag normalmente
        _pressGrid = sender as Grid;
        _pressPos = e.GetPosition(this);
        _pressTime = DateTime.UtcNow;
        _dragPending = true;
        _dragActive = false;

        try { e.Pointer.Capture(_pressGrid); } catch { /* ignore */ }
    }

    private async void OnItemCard_PointerMoved(object? sender, PointerEventArgs e)
    {
        // Só processa se estamos armados e ainda não iniciamos o DnD
        if (!_dragPending || _dragActive) return;

        var pt = e.GetCurrentPoint(this);
        if (!pt.Properties.IsLeftButtonPressed) { CancelPendingDrag(); return; }

        // Verifica critérios para começar: tempo segurando OU distância arrastada
        var now = DateTime.UtcNow;
        var cur = e.GetPosition(this);
        var dx = _pressPos.X - cur.X;
        var dy = _pressPos.Y - cur.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);

        var canStart =
            (now - _pressTime).TotalMilliseconds >= DragStartDelayMs ||
            dist >= DragStartThreshold;

        if (!canStart) return;

        // Resolve o "place" do card origem (via Border com Name==CardBorder)
        var border = FindCardBorder(sender);
        if (border?.Tag is not int place) { CancelPendingDrag(); return; }

        _dragActive = true;
        _dragPending = false;

        // Cria payload do DnD com um formato interno (application/x-item-place)
        var data = new DataObject();
        data.Set("application/x-item-place", place);

        // Inicia a operação de arrastar‑mover; bloqueia até drop/cancel
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

        // Ao retornar, a operação terminou (drop ou cancel)
        _dragActive = false;
        ReleasePointerCaptureSafe(e);
    }

    private void OnItemCard_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Solta estado de armado caso o usuário só tenha clicado/soltado
        CancelPendingDrag();
        ReleasePointerCaptureSafe(e);
    }

    private void CancelPendingDrag()
    {
        _dragPending = false;
        _pressGrid = null;
    }

    private static void ReleasePointerCaptureSafe(PointerEventArgs e)
    {
        // Libera captura com try/catch para evitar exceções em estados de corrida
        try { e.Pointer.Capture(null); } catch { /* ignore */ }
    }

    // ---------- DnD targets / auto-scroll ----------
    private void OnItem_DragOver(object? sender, DragEventArgs e)
    {
        // Só aceitamos o nosso formato interno; caso contrário, cancela auto‑scroll
        if (e.Data.Contains("application/x-item-place"))
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;

            // Estratégia de auto‑scroll: se o cursor estiver próximo das bordas
            // superior/inferior do ScrollViewer, iniciamos um scroll proporcional
            if (RootScroll is not null)
            {
                var pos = e.GetPosition(RootScroll);
                var h = RootScroll.Bounds.Height;
                const double margin = 28; // zona "quente" perto das bordas

                if (pos.Y < margin)
                {
                    // Quanto mais perto da borda, maior o multiplicador
                    var mult = (margin - pos.Y) / margin;
                    StartAutoScroll(-1, 0.8 + mult * 1.2);
                }
                else if (pos.Y > h - margin)
                {
                    var mult = (pos.Y - (h - margin)) / margin;
                    StartAutoScroll(+1, 0.8 + mult * 1.2);
                }
                else
                {
                    StopAutoScroll();
                }
            }
        }
        else
        {
            StopAutoScroll();
        }
    }

    private void OnItem_Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains("application/x-item-place")) return;

        // Extrai o place de origem
        var sourcePlace = e.Data.Get("application/x-item-place") as int? ?? -1;
        if (sourcePlace < 0) return;

        // Índice padrão caso solte fora de um card específico: ao final da lista
        var targetIndex = _rows.Count;

        // Descobre o card alvo (Border "CardBorder") sob o ponteiro, se existir
        var border = FindCardBorder(sender);

        if (border != null && border.Tag is int targetPlace)
        {
            // Converte place do alvo em índice no _rows (pois a lista está ordenada)
            var idx = _rows.FindIndex(r => r.Place == targetPlace);
            if (idx >= 0)
            {
                // Heurística "antes ou depois" do card alvo:
                // se soltou na metade esquerda, insere antes; na direita, depois
                var pt = e.GetPosition(border);
                targetIndex = pt.X < border.Bounds.Width / 2.0 ? idx : idx + 1;
            }
        }

        // Garante que o índice está dentro dos limites
        targetIndex = Math.Clamp(targetIndex, 0, _rows.Count);

        // Dispara evento de reordenação para a camada de domínio/aplicação decidir
        ReorderRequested?.Invoke(this, new ReorderRequestEventArgs
        {
            SourcePlace = sourcePlace,
            TargetIndex = targetIndex
        });

        StopAutoScroll();
        e.Handled = true;
    }

    private static Border? FindCardBorder(object? sender)
    {
        // Sobe na árvore visual a partir do sender até encontrar o Border do card
        if (sender is Border b && b.Name == "CardBorder") return b;
        if (sender is Control c)
            return c.GetVisualAncestors().OfType<Border>().FirstOrDefault(x => x.Name == "CardBorder");
        return null;
    }

    private void ItemCopyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is Item item) CopyRequested?.Invoke(this, item);
    }
    private void ItemEditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is Item item)
        {
            EditRequested?.Invoke(this, item);
            SelectByPlace(item.Place);
        }
    }
    private void ItemDeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is Item item)
            DeleteRequested?.Invoke(this, item);
    }

    private static void ApplyGoldShadow(Border border) =>
        border.BoxShadow = new BoxShadows(new BoxShadow { Color = Color.Parse("#ebb935"), Blur = 15, OffsetX = 0, OffsetY = 0 });

    private static void RemoveShadow(Border? border)
    {
        if (border is null) return;
        border.BoxShadow = new BoxShadows(new BoxShadow { Color = Colors.Transparent, Blur = 10, OffsetX = 0, OffsetY = 0 });
    }

    private void StartAutoScroll(int direction, double speedMultiplier = 1.0)
    {
        // Define direção e velocidade (com clamp para não ficar lento/demais)
        _autoScrollDir = Math.Sign(direction);
        _autoScrollSpeed = Math.Clamp(10 * speedMultiplier, 4, 30);

        // Configura/reattacha o timer a cada início para evitar múltiplos handlers
        _autoScrollTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60 FPS
        _autoScrollTimer.Tick -= OnAutoScrollTick;
        _autoScrollTimer.Tick += OnAutoScrollTick;

        if (!_autoScrollTimer.IsEnabled) _autoScrollTimer.Start();
    }

    private void StopAutoScroll()
    {
        _autoScrollDir = 0;
        if (_autoScrollTimer is { IsEnabled: true }) _autoScrollTimer.Stop();
    }

    private void OnAutoScrollTick(object? sender, EventArgs e)
    {
        // Avança o offset vertical do ScrollViewer respeitando os limites
        if (_autoScrollDir == 0 || RootScroll is null) return;

        var sv = RootScroll;
        var extentH = sv.Extent.Height;
        var viewportH = sv.Viewport.Height;
        var maxY = Math.Max(0, extentH - viewportH);

        var newY = sv.Offset.Y + (_autoScrollDir * _autoScrollSpeed);
        newY = Math.Clamp(newY, 0, maxY);

        sv.Offset = new Vector(sv.Offset.X, newY);
    }

    public void ScrollToEnd()
    {
        if (RootScroll is null) return;
        var maxY = Math.Max(0, RootScroll.Extent.Height - RootScroll.Viewport.Height);
        RootScroll.Offset = new Vector(RootScroll.Offset.X, maxY);
    }

    private void OnRoot_DragOver(object? sender, DragEventArgs e)
    {
        // Versão para a área "vazia" do ScrollViewer (não sobre um card)
        if (!e.Data.Contains("application/x-item-place")) { StopAutoScroll(); return; }

        var pos = e.GetPosition(RootScroll);
        var h = RootScroll.Bounds.Height;
        const double margin = 28;

        if (pos.Y < margin)
        {
            var mult = (margin - pos.Y) / margin;
            StartAutoScroll(-1, 0.8 + mult * 1.2);
        }
        else if (pos.Y > h - margin)
        {
            var mult = (pos.Y - (h - margin)) / margin;
            StartAutoScroll(+1, 0.8 + mult * 1.2);
        }
        else
        {
            StopAutoScroll();
        }
    }

    // Modelo visual interno para a lista
    private sealed class ItemRow
    {
        public Item Tag { get; set; } = default!;
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public int Amount { get; set; }
        public string LimitPeriod { get; set; } = "";
        public IImage? IconBitmap { get; set; }
        public string Price { get; set; } = "";
        public int Place { get; set; }
    }
}
