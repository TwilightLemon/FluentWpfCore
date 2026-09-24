using System.Windows;

namespace DataGridTest;

public partial class MainWindow : Window
{
    private const int ItemCount = 100_000;

    public MainWindow()
    {
        InitializeComponent();

        TestDataGrid.ItemsSource = MockDataItem.CreateMany(ItemCount);
        ItemCountText.Text = $"{ItemCount:N0} rows";
    }
}
