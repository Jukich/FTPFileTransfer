
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
    /// Interaction logic for CreateFileDialog.xaml
    /// </summary>
    public partial class CreateFileDialog : Window, INotifyPropertyChanged
    {
        public CreateFileDialog()
        {
            //conns = ConnectionsConfig.Instance.Connections.Skip(1).ToList();
            conns = ConnectionsConfig.Instance.Connections.ToList();
            InitializeComponent();
        }
        public List<string> FileExtentions { get; set; } = new List<string>() { "txt", "docx", "pdf" };       
        public List<FTPConnectionInfo> conns { get; set; }
        public int Progress { get => progress; set { progress = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Progress")); } }
        int progress = 0;
        public bool TrasferExecuting { get => transfer_executing; set { transfer_executing = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrasferExecuting")); } }
        bool transfer_executing = false;
        public string InfoStr { get => info_str; set { info_str = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InfoStr")); } }
        string info_str= "";

        public event PropertyChangedEventHandler PropertyChanged;
        public Action<string, string> OnComplete;
        FTPConnectionInfo selected_conn = new FTPConnectionInfo();
        //FileDescription currentFile;
        FileVersionDesc currentFile;
        private void btnAddEx_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|Word files (*.docx)|*.docx|PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (openFileDialog.ShowDialog() == false)
                return;

            txtFileName.Text = openFileDialog.FileName;
            currentFile = new FileVersionDesc();
            currentFile.dir = openFileDialog.InitialDirectory;
            currentFile.name = openFileDialog.SafeFileNames[0].Remove(openFileDialog.SafeFileNames[0].LastIndexOf('.'));
            currentFile.ext = openFileDialog.SafeFileNames[0].Split('.').Last();
            currentFile.size = new FileInfo(openFileDialog.FileName).Length;

            grAddFile.Visibility = Visibility.Visible;

            stckSelEx.Visibility = Visibility.Visible;
            stckCreateNew.Visibility = Visibility.Collapsed;
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            stckSelEx.Visibility = Visibility.Collapsed;
            stckCreateNew.Visibility = Visibility.Visible;
        }
        private void cmbConn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(cmbConn.SelectedItem is FTPConnectionInfo conn))
                return;

            //txtIP.Text = conn.ConnIP;
            txtUserName.Text = conn.ConnUsername;
            txtPass.Password = conn.ConnPassword;
            txtPort.Text = conn.ConnPort?.ToString();
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            selected_conn = ConnectionsConfig.Instance.Connections.Where(x => x.ConnName == (cmbConn.SelectedItem as FTPConnectionInfo)?.ConnName).FirstOrDefault();

            if (selected_conn == null || TrasferExecuting)  return;

            var conn = new FTPConnectionInfo()
            {
                ConnUsername = txtUserName.Text,
                ConnPassword = txtPass.Password,
                ConnIP = selected_conn.ConnIP,
                ConnPort = int.TryParse(txtPort.Text, out int port) ? (int?)port : null,
            };

            if (stckSelEx.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrEmpty(txtFileName.Text))
                {
                    InfoStr = "No file selected";
                    return; 
                }
                string fName = currentFile.GetFulName(-1);
                string vName = currentFile.GetFulName(1);
                File.Copy(fName, vName, true);

                var ftpconn = new FTPConnectionHelper() { connInfo = conn };
                var cancel = new CancellationTokenSource();

                InfoStr = "Uploading...";
                await ftpconn.UploadFile(currentFile.dir, currentFile.GetNameWithExt(-1), 
                    cancel, UpdateProgress, UploadCompleted, TransferError, "\\", true);
            }
            else if (stckCreateNew.Visibility == Visibility.Visible)
            {
                currentFile = new FileVersionDesc();
                currentFile.dir = System.IO.Directory.GetCurrentDirectory();
                currentFile.name = txtNewFileName.Text;
                currentFile.ext = cmbFileExt.SelectedItem.ToString();
                currentFile.size = 0;
                string fName = currentFile.GetNameWithExt(-1);
                //string fName = currentFile.Name + "_1" + '.' + currentFile.Type;

                try
                {
                    if (File.Exists(fName))
                        File.Delete(fName);

                    File.Create(fName).Close();

                    var ftpconn = new FTPConnectionHelper() { connInfo = conn };
                    var cancel = new CancellationTokenSource();

                    InfoStr = "Uploading...";
                    await ftpconn.UploadFile(System.IO.Directory.GetCurrentDirectory(), fName, 
                        cancel, UpdateProgress, UploadCompleted, TransferError, "\\", true);

                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.ToString());
                }
            }
        }
        void UpdateProgress(int progress)
        {
            Progress = progress;
        }
        void UploadCompleted()
        {
            TrasferExecuting = false;
            InfoStr = "Upload Completed!";

            var file_name = currentFile.GetFulName(-1);
            byte[] fileHash1;
            using (FileStream fileStream1 = new FileStream(file_name, FileMode.Open))
            {
                fileHash1 = System.Security.Cryptography.SHA256.Create().ComputeHash(fileStream1);
            }

            var fi = new FTPFileInfo() { FName = currentFile.name, FileFormat = currentFile.ext, ConnId = selected_conn.Id, RemotePath = "\\RemoteFTPFiles" };
            FilesConfig.Instance.AddFile(fi);

            var fv = new FTPFileVersion() { FileId = fi.Id, Size = currentFile.size, FileVersion = 1, LastModified = DateTime.Now.Ticks, HashCode = fileHash1, RemotePath = "\\" };
            FilesConfig.Instance.AddVersion(fv);
            FilesConfig.Instance.GetFiles();

            File.Delete(currentFile.GetNameWithExt(-1));


        }
        void TransferError(string error)
        {
            TrasferExecuting = false;
            Progress = 0;
            InfoStr = error;

            //if (stckCreateNew.Visibility == Visibility.Visible)
            File.Delete(currentFile.GetNameWithExt(1));
        }
    }
}
