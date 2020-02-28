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

namespace ProAppVisibilityModule.Views
{
    /// <summary>
    /// Interaction logic for RLOSView.xaml
    /// </summary>
    public partial class VisibilityRLOSView : UserControl
    {
        public VisibilityRLOSView()
        {
            InitializeComponent();
        }
    
        private void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // TRICKY: right mouse click selects item in list box, 
            // avoid this when multiple items are selected by setting e.Handled to true
            if ((listBoxObservers.SelectedItems != null) && (listBoxObservers.SelectedItems.Count >= 1))
                e.Handled = true;
        }
    }
}
