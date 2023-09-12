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

namespace FTPFileManagament
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void sel_chng(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TabItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void TabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void on_close(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button but)) return;
            if (!(but.TemplatedParent is TabItem tbitem)) return;
            if (!(tbitem.Content is FTPScreen ftpscreen)) return;

            ftpscreen.Close();

            (tbitem.Parent as TabControl).Items.Remove(tbitem);
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {

        }
        public void AddFTPView(FTPConnectionHelper ftpconn)
        {
            FTPScreen ftpview = new FTPScreen(ftpconn);
            tbctrl.Items.Add(new TabItem() { Content = ftpview, IsSelected = true });

        }
        public void SwitchToFilesView()
        {
            FileExplorer fileexplorer = new FileExplorer();
            (tbctrl.Items[0] as TabItem).Content = fileexplorer;
        }
        public void SwitchToConnsVIew()
        {
            ConnectionsScreen connscr = new ConnectionsScreen();
            (tbctrl.Items[0] as TabItem).Content = connscr;
        }
    }
}
