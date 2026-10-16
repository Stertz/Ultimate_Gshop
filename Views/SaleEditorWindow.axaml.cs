using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Globalization;
using Ultimate_Gshop.Itens;
using Ultimate_Gshop.Misc;
using Ultimate_Gshop.Views;

namespace Ultimate_Gshop
{
    public partial class SaleEditorWindow : Window
    {
        private readonly Item? _item;

        private readonly List<string> _statusOptions =
        [
            "Default", "New", "Popular", "Often selling",
            "Sale 10%", "Sale 20%", "Sale 30%", "Sale 40%", "Sale 50%",
            "Sale 60%", "Sale 70%", "Sale 80%", "Sale 90%",
            "Vip0", "Vip1", "Vip2", "Vip3", "Vip4", "Vip5", "Vip6"
        ];

        private readonly List<string> _controlOptions = ["", "Client", "Server"];

        private readonly Dictionary<string, int> _timeUnits = new()
        {
            ["Minute"] = 60,
            ["Hour"] = 3600,
            ["Day"] = 86400,
            ["Week"] = 604800,
            ["Month"] = 2592000,
            ["Year"] = 31536000
        };

        // Opções do Type/Value
        private readonly List<string> _typeValueOptions =
        [
            "Price",     // define o preço
            "Add %",     // soma % ao preço
            "Remove %",  // subtrai % do preço
            "Multiply",  // multiplica pelo valor
            "Divide"     // divide pelo valor
        ];

        public SaleEditorWindow()
        {
            InitializeComponent();
            _item = null;
            Cursor = ApplicationSettings.InitializeCustomCursor();
            SetComboBoxes();
            FillSales();
        }

        public SaleEditorWindow(Item? item)
        {
            InitializeComponent();
            _item = item;
            SetComboBoxes();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            FillSales();
        }

        // Escolhe a MAIOR unidade que divide exatamente 'seconds'; fallback para Minute.
        private (int value, string unit) SplitDuration(int seconds)
        {
            if (seconds <= 0) return (0, "Minute");

            var match = _timeUnits
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault(kv => seconds % kv.Value == 0);

            if (match.Value > 0)
                return (seconds / match.Value, match.Key);

            return (seconds / 60, "Minute");
        }

        private void FillSales()
        {
            TypeValueComboBox.SelectedItem = null;
            ValueTextBox.Text = string.Empty;
            if (_item == null || _item.Sales.Count == 0)
            {
                SalesList.ItemsSource = new List<object>();
                SaleValidPrice.Text = "0.00";
                return;
            }

            SaleValidPrice.Text = _item.GetValidPrice().ToString("F2", CultureInfo.InvariantCulture);

            var rows = _item.Sales
                .Select((s, index) =>
                {
                    var (time, setTime) = SplitDuration(s.During);

                    return new
                    {
                        SalesIndex = index,
                        Price = s.Price / 100m,
                        Days = s.Day,
                        Time = time,
                        SetTime = setTime,
                        s.Flags,
                        VipLevel = s.VipLevelRequired,
                        Status = s.Status >= 0 && s.Status < _statusOptions.Count ? _statusOptions[s.Status] : "",
                        Control = s.Control >= 0 && s.Control < _controlOptions.Count ? _controlOptions[s.Control] : "",
                        SellingStartTime = FormatUtils.ToDate(s.SellingStartTime).ToString("dd/MM/yy"),
                        SellingEndTime = FormatUtils.ToDate(s.SellingEndTime).ToString("dd/MM/yy")
                    };
                })
                .ToList();

            SalesList.ItemsSource = rows;
        }

        private async void RemoveSales_Click(object? sender, RoutedEventArgs e)
        {
            if (_item == null || sender is not Button btn || btn.Tag is not int index || index < 0 || index >= _item.Sales.Count)
                return;

            var result = await new CustomMessageBox(
                "Remover?",
                $"Deseja remover a sale #{index}?",
                CustomMessageBox.MessageBoxIcon.Question,
                "Sim", "Não").ShowDialogAsync(this);

            if (result == "Sim")
            {
                _item.Sales.RemoveAt(index);
                FillSales();
            }
        }

        private void AddSales_Click(object? sender, RoutedEventArgs e)
        {
            if (_item == null) return;

            // 1) Se Type/Value estiverem preenchidos, manipula somente sale[0].Price e retorna.
            if (ApplyTypeValueToFirstSale())
            {
                FillSales();
                return;
            }

            // 2) Caso contrário, segue o fluxo normal de adicionar uma nova sale
            static int ParseInt(string? s) => int.TryParse(s, out var v) ? v : 0;
            static int NonNeg(int v) => v < 0 ? 0 : v;

            var start = SalesStartPicker.DateAsInt ?? 0;
            var end = SalesEndPicker.DateAsInt ?? 0;

            var priceCents = PriceUtils.ToCents(PriceUtils.Parse(PriceTextBox.Text));

            var unitName = SetTimeComboBox.SelectedItem as string ?? "Minute";
            var unitSeconds = _timeUnits.TryGetValue(unitName, out var sec) ? sec : 60;
            var during = ParseInt(TimeTextBox.Text) > 0
                ? checked(NonNeg(ParseInt(TimeTextBox.Text)) * unitSeconds)
                : 0;

            var statusIndex = StatusComboBox.SelectedIndex;
            if (statusIndex < 0 || statusIndex >= _statusOptions.Count) statusIndex = 0;

            var controlIndex = ControlComboBox.SelectedIndex;
            if (controlIndex < 0 || controlIndex >= _controlOptions.Count) controlIndex = 0;

            _item.Sales.Add(new Sale
            {
                Price = priceCents,
                Day = NonNeg(ParseInt(DayTextBox.Text)),
                During = during,
                SellingStartTime = start,
                SellingEndTime = end,
                Flags = NonNeg(ParseInt(FlagsTextBox.Text)),
                VipLevelRequired = NonNeg(ParseInt(VipLevelTextBox.Text)),
                Status = statusIndex,
                Control = controlIndex
            });

            FillSales();
        }

        private void SetComboBoxes()
        {
            StatusComboBox.ItemsSource = _statusOptions;
            ControlComboBox.ItemsSource = _controlOptions;
            SetTimeComboBox.ItemsSource = _timeUnits.Keys.ToList();

            // Type/Value (novos)
            TypeValueComboBox.ItemsSource = _typeValueOptions;
            TypeValueComboBox.SelectedItem = null; // começa vazio
            ValueTextBox.Text = string.Empty;

            StatusComboBox.SelectedItem = "Default";
            ControlComboBox.SelectedItem = "";
            SetTimeComboBox.SelectedItem = "Minute";
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close(_item?.Sales);
        }

        // -----------------------
        // Helpers de preço/parse:
        // -----------------------

        private bool ApplyTypeValueToFirstSale()
        {
            var typeSel = TypeValueComboBox.SelectedItem as string;
            var hasType = !string.IsNullOrWhiteSpace(typeSel);

            var valDec = PriceUtils.Parse(ValueTextBox.Text);
            var hasValue = valDec != 0m || (ValueTextBox.Text ?? "").Trim() == "0" || (ValueTextBox.Text ?? "").Trim() == "0.0";

            if (!hasType || !hasValue || _item == null)
                return false;

            // Garante que exista a sale[0]
            if (_item.Sales.Count == 0)
                _item.Sales.Add(new Sale { Price = 0 });

            if (!PriceUtils.TryApply(_item.Sales[0].Price, typeSel, valDec, out var newCents))
                return false;

            _item.Sales[0].Price = newCents;
            // Atualiza o label de preço válido
            SaleValidPrice.Text = _item.GetValidPrice().ToString("F2", CultureInfo.InvariantCulture);
            return true;
        }
    }
}
