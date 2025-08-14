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
            InitializeDesign();
        }

        public MenuWindow(Shop shop, GshopSettings settings, ShopTracking tracking)
        {
            InitializeComponent();
            Cursor = ApplicationSettings.InitializeCustomCursor();
            _shop = shop;
            _settings = settings;
            _tracking = tracking;
            InitializeDesign();
        }

        private void InitializeDesign()
        {
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

        private async void OnExportServerClicked(object sender, RoutedEventArgs e)
        {
            var export_location = await SetGshopFileFromOS.Handle(this);

            if (export_location is null) return;

            var export_filepath = Path.Combine(export_location, "gshopsev.data");

            if (File.Exists(export_filepath))
            {
                var result =
                    await new CustomMessageBox("Substituir arquivos?", "Já existe um arquivo gshopsev na pasta, esta ação irá substituí-lo", "Ok", "Cancelar")
                    .ShowDialogAsync(this);

                if (result != "Ok") return;
            }

            using var bw = new BinaryWriter(File.Create(export_filepath));

            if (_shop == null)
                throw new InvalidOperationException("Shop is not initialized.");

            SaveGshop.Handle(_shop, _tracking!, _settings!);

            bw.Close();

        }

        private async void OnExportClientClicked(object sender, RoutedEventArgs e)
        {

            var export_location = await SetGshopFileFromOS.Handle(this);

            if (export_location is null) return;

            var export_filepath = Path.Combine(export_location, "gshop.data");

            if (File.Exists(export_filepath))
            {
                var result = 
                    await new CustomMessageBox("Substituir arquivos?", "Já existe um arquivo gshop na pasta, esta ação irá substituí-lo", "Ok", "Cancelar")
                    .ShowDialogAsync(this);

                if (result != "Ok") return;
            }

            using var bw = new BinaryWriter(File.Create(export_filepath));
            if (_shop == null)
                throw new InvalidOperationException("Shop is not initialized.");

            SaveGshop.Handle(_shop, _tracking!, _settings!);
            
            bw.Close();
        }

        private async void OnImportGshopClicked(object sender, RoutedEventArgs e)
        {
            SaveGshopIfExists();
            var gshop = await ImportGshop.Handle(this);
            if (gshop == null) return;
            Close(gshop);
        }

        private void OnContinueGshopClicked(object sender, RoutedEventArgs e)
        {
            Close(_shop);
        }

        private void OnNewGshopClicked(object sender, RoutedEventArgs e)
        {
            SaveGshopIfExists();
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

        private void SaveGshopIfExists()
        {
            if (ApplicationSettings.GshopExists && _shop is not null)
            {
                Directory.CreateDirectory(ApplicationSettings.BackupFolder);
                var bkp = Path.Combine(ApplicationSettings.BackupFolder, $"gshop_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}");
                Directory.CreateDirectory(bkp);
                SaveGshop.Handle(_shop, _tracking!, _settings!, bkp);
                _settings!.LastBackupFrom = bkp;
            }
        }

    }
}
