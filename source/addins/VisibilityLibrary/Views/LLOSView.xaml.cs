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

namespace VisibilityLibrary.Views
{
    /// <summary>
    /// Interaction logic for LLOSView.xaml
    /// </summary>
    public partial class VisibilityLLOSView : UserControl
    {
        public VisibilityLLOSView()
        {
            InitializeComponent();
        }

        private void listBoxObservers_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // TRICKY: right mouse click selects item in list box, 
            // avoid this when multiple items are selected by setting e.Handled to true
            if ((listBoxObservers.SelectedItems != null) && (listBoxObservers.SelectedItems.Count >= 1))
                e.Handled = true;
        }

        private void listBoxTargets_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // TRICKY: right mouse click selects item in list box, 
            // avoid this when multiple items are selected by setting e.Handled to true
            if ((listBoxTargets.SelectedItems != null) && (listBoxTargets.SelectedItems.Count >= 1))
                e.Handled = true;
        }


    }
}
