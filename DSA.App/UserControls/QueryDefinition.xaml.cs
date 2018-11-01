using DSA.Lib.Models;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DSA.App.UserControls
{
    /// <summary>
    /// Interaction logic for QueryDefinition.xaml
    /// </summary>
    public partial class QueryDefinition : UserControl
    {
        // Properties

        // Events
        public event RoutedEventHandler Remove;
        public event RoutedEventHandler Test;

        // Dependency Properties

        public QueryDefinition()
        {
            InitializeComponent();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Remove(DataContext, e);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Test(DataContext, e);
        }

        private void Grid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var query = DataContext as Query;
            if ( query != null && query.ProfileType != "custom")
            {
                PathLabel.Visibility = Visibility.Collapsed;
                PathInput.Visibility = Visibility.Collapsed;
            }
        }
    }
}
