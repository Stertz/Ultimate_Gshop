using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Text;
using Application = Avalonia.Application;

namespace Ultimate_Gshop
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.Exit += (_, _) => Misc.ImageUtils.ReleaseResources();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
