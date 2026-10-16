using Avalonia.Controls;

namespace Ultimate_Gshop.Format.Tests;

internal sealed class HeadlessHost : IDisposable
{
    internal Window Window { get; }

    internal HeadlessHost(Control content)
    {
        Window = new Window { Content = content, Width = 800, Height = 600 };
        Window.Show();
        Window.UpdateLayout();
    }

    public void Dispose() => Window.Close();
}
