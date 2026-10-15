using Avalonia;
using ReactiveUI.Avalonia;
using System.Text;

namespace Ultimate_Gshop
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static int Main(string[] args)
        {
            // Registra suporte a encodings
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args is ["--verify-installation"])
            {
                try
                {
                    Misc.InstallationCheck.Run();
                    return 0;
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    return 1;
                }
            }

            return BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI(static _ => { })
                .LogToTrace();
    }
}
