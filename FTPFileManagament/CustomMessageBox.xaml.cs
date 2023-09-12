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
    /// Interaction logic for CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox()
        {
            InitializeComponent();
        }
        public string Header { get; set; }
        public string Message { get; set; }
        public List<Tuple<string, RoutedEventHandler>> customButtons { get; set; } = new List<Tuple<string, RoutedEventHandler>>();

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var handler = ((sender as Button).DataContext as Tuple<string, RoutedEventHandler>).Item2;
            handler?.Invoke(sender, e);
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
