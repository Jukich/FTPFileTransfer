using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using FluentFTP;

namespace FTPFileManagament
{
    public interface ITabContent
    {
        Window OwnerWnd { get; set; }
        string TabName { get; }

        Brush Foregnd { get; }

        Brush Backgnd { get; }
        //EConnStatus ConnectionStatus { get; }

    }

    public partial class ConnectionsScreen : UserControl, ITabContent, INotifyPropertyChanged
    {

        public ConnectionsScreen()
        {
            InitializeComponent();
        }

        public Window OwnerWnd { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string TabName => "Connections";

        public Brush Foregnd => Brushes.Black;

        public Brush Backgnd => Brushes.LightGray;
        public FTPConnectionInfo currentConn { get; set; } = new FTPConnectionInfo();

        public MainWindow mainwnd = null;

        public event PropertyChangedEventHandler PropertyChanged;


        private void tvConns_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(tvConns.SelectedItem is FTPConnectionInfo ci))
                return;

            txtConnName.Text = ci.ConnName;
            txtUserName.Text = ci.ConnUsername;
            txtIP.Text = ci.ConnIP;
            txtPass.Password = ci.ConnPassword;
            txtPort.Text = ci.ConnPort.ToString();
            currentConn = ci;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("currentConn"));

        }

        private void tvConns_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

     

        private void tvConns_Loaded(object sender, RoutedEventArgs e)
        {
            if (tvConns.SelectedItem != null)
                return;
            ConnectionsConfig.Instance.GetConnections();
            SelectConnection(ConnectionsConfig.Instance.Connections.First());
        }
        void SelectConnection(FTPConnectionInfo conn)
        {
            var tbconn = ContainerFromItem(tvConns.ItemContainerGenerator, conn, true);
            tbconn.IsSelected = true;
            tbconn.Focus();
            currentConn = conn;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("currentConn"));
        }
        private static TreeViewItem ContainerFromItem(ItemContainerGenerator containerGenerator, object item, bool get_parent)
        {
            TreeViewItem container = (TreeViewItem)containerGenerator.ContainerFromItem(item);
            if (container != null)
                return container;

            foreach (object childItem in containerGenerator.Items)
            {
                TreeViewItem parent = containerGenerator.ContainerFromItem(childItem) as TreeViewItem;
                if (parent == null)
                    continue;

                container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container != null)
                    return get_parent ? parent : container;

                container = ContainerFromItem(parent.ItemContainerGenerator, item, get_parent);
                if (container != null)
                    return container;
            }
            return null;
        }
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNewName.Text) || string.IsNullOrEmpty(txtNewHost.Text))
                return;

            var ci = new FTPConnectionInfo() { ConnName = txtNewName.Text, ConnIP = txtNewHost.Text };
            ConnectionsConfig.Instance.AddConnection(ci);
            SelectConnection(ci);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!(tvConns.SelectedItem is FTPConnectionInfo ci))
                return;

            if (!ci.CanUserDelete)
            {
                ci = new FTPConnectionInfo()
                {
                    ConnName = "New Connection",
                    ConnUsername = txtUserName.Text,
                    ConnIP = txtIP.Text,
                    ConnPassword = txtPass.Password,
                    ConnPort = int.TryParse(txtPort.Text, out int port) ? (int?)port : null,
                };
                ConnectionsConfig.Instance.AddConnection(ci);
            }
            else
            {
                ci.ConnName = txtConnName.Text;
                ci.ConnUsername = txtUserName.Text;
                ci.ConnPassword = txtPass.Password;
                ci.ConnIP = txtIP.Text;
                ci.ConnPort = int.TryParse(txtPort.Text, out int port) ? (int?)port : null;
                ConnectionsConfig.Instance.UpdateConnection(ci);
            }

            SelectConnection(ci);
        }
        private void Delete()
        {
            if (!(tvConns.SelectedItem is FTPConnectionInfo cinfo))
                return;

            var res = MessageBox.Show($"Do you want to delete \"{cinfo.ConnName}\"", "", MessageBoxButton.YesNo);
            if (res != MessageBoxResult.Yes)
                return;

            var parent = ContainerFromItem(tvConns.ItemContainerGenerator, cinfo, true);
            if (parent != null)
            {
                ConnectionsConfig.Instance.DeleteConnection(cinfo);
                parent.IsSelected = true;
            }
        }

        private void tvConns_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (!(tvConns.SelectedItem is FTPConnectionInfo cinfo))
                    return;

                var cmmb = new CustomMessageBox() { Header = "Delete connecton", Message = $"Do you want to delete connection {cinfo.FullName}?" };
                cmmb.customButtons.Add(new Tuple<string, RoutedEventHandler>("Delete", (sender_, e_) =>
                {
                    var parent = ContainerFromItem(tvConns.ItemContainerGenerator, cinfo, true);
                    if (parent != null)
                    {
                        ConnectionsConfig.Instance.DeleteConnection(cinfo);
                        parent.IsSelected = true;
                    }

                }));
                cmmb.customButtons.Add(new Tuple<string, RoutedEventHandler>("Delete with files", (sender_, e_) => 
                { 
                    int a = 2;
                }));
                cmmb.customButtons.Add(new Tuple<string, RoutedEventHandler>("Cancel", (sender_, e_) => { int a = 2; }));
                cmmb.ShowDialog();
                
                //var res = MessageBox.Show($"Do you want to delete \"{cinfo.ConnName}\"", "", MessageBoxButton.YesNo);
                //if (res != MessageBoxResult.Yes)
                //    return;

                

            }
        }
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            //currentConn.ConnName = txtConnName.Text;
            //currentConn.ConnPassword = txtPass.Password;

            var conn = new FTPConnectionInfo()
            {
                ConnName = txtConnName.Text,
                ConnUsername = txtUserName.Text,
                ConnPassword = txtPass.Password,
                ConnIP = txtIP.Text,
                ConnPort = int.TryParse(txtPort.Text, out int port) ? (int?)port : null
            };


            OpenConn(conn);
        }
        void OpenConn(FTPConnectionInfo ci)
        {
            //if (currentConn.Name.Length == 0)
            //    currentConn.Name = currentConn.IP;
            //currentConn.ConnPass = txtPass.Password;

            var ftpconn = new FTPConnectionHelper() { connInfo = ci };
            //ConnectionsConfig.Instance.ActiveConnections.Add(ftpconn);
            mainwnd?.AddFTPView(ftpconn);

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var uielm = Parent;
            int count = 0;
            while (true)
            {
                if (count++ == 20)
                    break;

                if (uielm is MainWindow mw)
                {
                    mainwnd = mw;
                    break;
                }
                uielm = (uielm as FrameworkElement).Parent;
            }
        }

        private void btnFiles_Click(object sender, RoutedEventArgs e)
        {
            mainwnd?.SwitchToFilesView();

        }

        
    }
}
