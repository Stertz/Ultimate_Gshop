using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Misc;
using Ultimate_Gshop.Views;
namespace Ultimate_Gshop
{
    public partial class MenuWindow : Window
    {
        private readonly Shop? _shop;
        private readonly GshopSettings? _settings;
        private readonly ShopTracking? _tracking;

        public MenuWindow()
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            _shop = null;
            _settings = null;
            _tracking = null;
            btnExportServer.IsEnabled = false;
            btnExportClient.IsEnabled = false;
            btnContinue.IsEnabled = ApplicationSettings.GshopExists;

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

        public MenuWindow(Shop shop, GshopSettings settings, ShopTracking tracking)
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            _shop = shop;
            _settings = settings;
            _tracking = tracking;

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

        private async void OnExportClientClicked(object sender, RoutedEventArgs e)
        {

            var export_location = await SetGshopFileFromOS.Handle(this);

            if (export_location is null) return;

            var export_filepath = Path.Combine(export_location, "gshop.data");

            using var bw = new BinaryWriter(File.Create(export_filepath));
            if (_shop == null)
                throw new InvalidOperationException("Shop is not initialized.");

            SaveGshop.Handle(_shop, _tracking!, _settings!);
            
            ExportGshopClient.Handle(bw, _shop);

            bw.Close();
        }

        private async void OnImportGshopClicked(object sender, RoutedEventArgs e)
        {
            if (ApplicationSettings.GshopExists && _shop is not null)
            {
                Directory.CreateDirectory(ApplicationSettings.BackupFolder);
                var bkp = Path.Combine(ApplicationSettings.BackupFolder, $"gshop_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}");
                Directory.CreateDirectory(bkp);
                using var bw = new BinaryWriter(File.Create(Path.Combine(bkp, "gshop.data")));
                ExportGshopClient.Handle(bw, _shop);
               _settings!.LastBackupFrom = bkp;
            }
            var gshop = await ImportGshop.Handle(this);
            if (gshop == null) return;
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
            var options_window = new OptionsWindow(_settings!)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                CanResize = false
            };
            options_window.Show();
        }

    }
}
