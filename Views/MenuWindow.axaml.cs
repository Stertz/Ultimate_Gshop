using Avalonia.Controls;
using Avalonia.Interactivity;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop
{
    public partial class MenuWindow : Window
    {
        private readonly Shop? _shop;
        private readonly GshopSettings? _settings;

        public MenuWindow()
        {
            InitializeComponent();
            _shop = null;
            _settings = null;
            btnExportServer.IsEnabled = false;
            btnExportClient.IsEnabled = false;
            btnContinue.IsEnabled = ApplicationSettings.GshopExists;
        }

        public MenuWindow(Shop shop, GshopSettings settings)
        {
            InitializeComponent();
            _shop = shop;
            _settings = settings;
            this.Opacity = 0;

            this.KeyDown += MenuWindow_KeyDown;

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

        private void MenuWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                this.Close(_shop);
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close(_shop);
        }

        private void OnExportServerClicked(object sender, RoutedEventArgs e)
        {
            using var bw = new BinaryWriter(File.Create(Path.Combine(AppContext.BaseDirectory, "gshopsev.data")));

            if (_shop == null)
                throw new InvalidOperationException("Shop is not initialized.");

            ExportGshopServer.Handle(bw, _shop);

            bw.Close();

        }

        private void OnExportClientClicked(object sender, RoutedEventArgs e)
        {
            using var bw = new BinaryWriter(File.Create(Path.Combine(Variables.GshopClient)));
            if (_shop == null)
                throw new InvalidOperationException("Shop is not initialized.");
            
            ExportGshopClient.Handle(bw, _shop);
            bw.Close();
        }

        private async void OnImportGshopClicked(object sender, RoutedEventArgs e)
        {
            var gshop = await ImportGshop.Handle(this);
            Close(gshop);
        }

        private async void OnContinueGshopClicked(object sender, RoutedEventArgs e)
        {
            var gshop = await ContinueGshop.Handle(this);
            Close(gshop);
        }

        private void OnNewGshopClicked(object sender, RoutedEventArgs e)
        {
            var gshop = NewEmptyGshop.Handle();
            Close(gshop);
        }

        private void OnOptionsClicked(object sender, RoutedEventArgs e)
        {

        }

    }
}
