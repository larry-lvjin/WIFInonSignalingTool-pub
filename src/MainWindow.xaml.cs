using System.Windows;
using System.Windows.Controls;

namespace WIFInonSignalingTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.ScrollToEnd();
        }

        private void ComboBox_SelectionChanged()
        {

        }
    }
}
