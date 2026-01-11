using FluentWpfCore.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var _ in Enumerable.Range(0, 80))
            {
                panel.Children.Add(new TextBlock()
                {
                    Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                           "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                           "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
                           "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
                           "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10)
                });
                panel.Children.Add(new Border()
                {
                    Background = Brushes.SkyBlue,
                    Height = 64,
                    Margin = new Thickness(10)
                });
            }
        }
    }
}