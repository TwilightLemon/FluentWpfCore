using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Example
{
    /// <summary>
    /// Interaction logic for FluentListBoxTestWindow.xaml
    /// </summary>
    public partial class FluentListBoxTestWindow : Window
    {
        public IReadOnlyList<string> LargeItems { get; }

        public FluentListBoxTestWindow()
        {
            LargeItems = Enumerable.Range(1, 1000)
                .Select(i => $"Virtualized item #{i:D5}")
                .ToList();

            InitializeComponent();
            DataContext = this;
        }
    }
}
