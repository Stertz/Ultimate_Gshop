using Avalonia.Controls;
using Avalonia.Input;
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

        private readonly List<string> _controlOptions = ["Client", "Server"];

        private readonly Dictionary<string, int> _timeUnits = new()
        {
            ["Minute"] = 60,
            ["Hour"] = 3600,
            ["Day"] = 86400,
            ["Week"] = 604800,
            ["Month"] = 2592000,
            ["Year"] = 31536000
        };

        public SaleEditorWindow()
        {
            InitializeComponent();
            SetComboBoxes();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            _item = null;
            FillSales();
            this.Opacity = 0;
            this.Opened += async (_, __) =>
            {
                for (double i = 0; i <= 1; i += 0.05)
                {
                    this.Opacity = i;
                    await Task.Delay(7);
                }
                this.Opacity = 1;
            };
        }

        public SaleEditorWindow(Item item)
        {
            InitializeComponent();
            SetComboBoxes();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            _item = item;
            FillSales();
            this.Opacity = 0;

            this.Opened += async (_, __) =>
            {
                for (double i = 0; i <= 1; i += 0.05)
                {
                    this.Opacity = i;
                    await Task.Delay(7);
                }
                this.Opacity = 1;
            };
        }

        private void FillSales()
        {
            if (_item == null || _item.Sales.Count == 0)
            {
                SalesList.ItemsSource = new List<Sale> { new() };
                SaleValidPrice.Text = (0).ToString("F2", CultureInfo.InvariantCulture);
                return;
            }

            SaleValidPrice.Text = _item.GetValidPrice().ToString("F2", CultureInfo.InvariantCulture);

            var rows = _item.Sales
                .Select(s =>
                {
                    var match = _timeUnits.FirstOrDefault(pair => s.During % pair.Value == 0);
                    var time = match.Value > 0 ? s.During / match.Value : 0;
                    var setTime = match.Key ?? "";

                    return new
                    {
                        Price = (s.Price / 100.0).ToString("F2", CultureInfo.InvariantCulture),
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
            var msgBox = new CustomMessageBox(
                "Remover?",
                $"ID: {_item?.Sales.Count}\nName: Unknown",
                CustomMessageBox.MessageBoxIcon.Question,
                "Sim", "Não");
            var result = await msgBox.ShowDialogAsync(this);

            if (result == "Sim")
            {
                // lógica para remover
            }
        }

        private void AddSales_Click(object? sender, RoutedEventArgs e)
        {
            FillSales();
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close(_item?.Sales);
        }

        private void SetComboBoxes()
        {
            StatusComboBox.ItemsSource = _statusOptions;
            ControlComboBox.ItemsSource = _controlOptions;
            SetTimeComboBox.ItemsSource = _timeUnits.Keys.ToList();
            StatusComboBox.SelectedItem = "Default";
            ControlComboBox.SelectedItem = "Client";
            SetTimeComboBox.SelectedItem = "Minute";
        }
    }
}
