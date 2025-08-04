using Avalonia.Controls;
using Avalonia.Interactivity;
using Ultimate_Gshop.Gshop;
using Ultimate_Gshop.Gshop.Services;
using Ultimate_Gshop.Misc;
using Avalonia.Platform.Storage;
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

            if (_settings is null) return;

            IStorageFile? file = await GetFileFromOS.ShowOpenFileDialog(this);

            if (file is null) return;

            var filepath = file.TryGetLocalPath();

            if (filepath is null) return;

            var gshop = ImportGshop.Handle(filepath, _settings);

            Close(gshop);
        }

    }
}
