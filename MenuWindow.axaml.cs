using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ultimate_Gshop
{
    public partial class MenuWindow : Window
    {
        public MenuWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
