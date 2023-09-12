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

namespace FTPFileManagament
{
    /// <summary>
    /// Interaction logic for WindowButtons.xaml
    /// </summary>
    public partial class WindowButtons : UserControl
    {
        public WindowButtons()
        {
            InitializeComponent();
        }

        Window wnd;
        //public Window wnd { get; set; }
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            wnd.WindowState = WindowState.Minimized;
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (wnd.WindowState == WindowState.Normal)
                wnd.WindowState = WindowState.Maximized;
            else
                wnd.WindowState = WindowState.Normal;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            wnd?.Close();
        }

        private void uc_loaded(object sender, RoutedEventArgs e)
        {
            var uielm = Parent;
            int count = 0;
            while (true)
            {
                count++;
                if (count == 20)
                    break;
                if (uielm is FrameworkElement fw)
                {
                    uielm = fw.Parent;
                    if (uielm is Window)
                        break;
                }
            }

            if (uielm is Window w)
                wnd = w;
        }
        public Visibility VizRestBtn
        {
            get { return btnRestore.Visibility; }
            set { btnRestore.Visibility = value; }
        }

        public Visibility VizMinBtn
        {
            get { return btnMinimize.Visibility; }
            set { btnMinimize.Visibility = value; }
        }
    }
}
