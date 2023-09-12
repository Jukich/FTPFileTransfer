using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for FileExplorer.xaml
    /// </summary>
    public partial class FileExplorer : UserControl,ITabContent
    {
        public FileExplorer()
        {
            InitializeComponent();
        }
        public ObservableCollection <FileDescription> SavedFiles { get; set; } = new ObservableCollection<FileDescription>();
        bool loaded = false;
        FileVersionDesc fDesc;
        FTPFileInfo fInfo;
        byte[] fileHash;
        public string TabName { get => "Remote Files"; }
        public MainWindow mainwnd = null;
        public Brush Foregnd => Brushes.Black;
        public Brush Backgnd => Brushes.LightGray;
        public string ConnName { get => ""; }
        public Window OwnerWnd { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            if (loaded) return;
            loaded = true;

            FilesConfig.Instance.GetFiles();

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


        private void btnAddFile_Click(object sender, RoutedEventArgs e)
        {
            CreateFileDialog dialog = new CreateFileDialog();
            dialog.ShowDialog();
            lvFIlesDirs.UpdateLayout();
            lvFIlesDirs.Visibility = Visibility.Collapsed;
            lvFIlesDirs.Visibility = Visibility.Visible;

        }

        private void btnSwitchView_Click(object sender, RoutedEventArgs e)
        {
            mainwnd?.SwitchToConnsVIew();
        }

        private void lvVersions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
        private async void lvFIlesDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            fInfo = (sender as ListViewItem).Content as FTPFileInfo;
            var cn = ConnectionsConfig.Instance.Connections.Where(x => x.Id == fInfo.ConnId).FirstOrDefault();
            fDesc = new FileVersionDesc();
            fDesc.dir = System.IO.Directory.GetCurrentDirectory();
            fDesc.name = fInfo.FName;
            fDesc.ext = fInfo.FileFormat;
            int version = fInfo.VersionsCount - 1;

            var conn = new FTPConnectionInfo()
            {
                ConnUsername = cn.ConnUsername,
                ConnPassword = cn.ConnPassword,
                ConnIP = cn.ConnIP,
                ConnPort = cn.ConnPort
            };

            //InfoStr = "Download in progress...";

            var ftpconn = new FTPConnectionHelper() { connInfo = conn };
            var cancel = new CancellationTokenSource();

            //TrasferExecuting = true;
            await ftpconn.DownloadFile(fDesc.dir, fDesc.GetNameWithExt(version), cancel, UpdateProgress,
                DownloadCompleted, TransferError, fInfo.FTPFileVersions.ElementAt(version).RemotePath);

        }
        void DownloadCompleted()
        {
            if (fDesc.ext != "txt" && fDesc.ext != "docx" && fDesc.ext != "pdf")
            {
                CustomMessageBox msg = new CustomMessageBox() { Message = "File type not supported", Header = "Error opening file" };
                msg.customButtons.Add(new Tuple<string, RoutedEventHandler>("OK", null));
                msg.ShowDialog();
                return;
            }

            Process editProcess = new Process() { EnableRaisingEvents = true };
            editProcess.StartInfo.FileName = fDesc.GetNameWithExt(fInfo.VersionsCount - 1);
            editProcess.StartInfo.UseShellExecute = true;
            editProcess.Exited += EditProcess_Exited;
            editProcess.Start();
        }
        private async void EditProcess_Exited(object sender, EventArgs e)
        {
            var full_name = fDesc.GetFulName(fInfo.VersionsCount - 1);
            using (FileStream fileStream1 = new FileStream(full_name, FileMode.Open))
            {
                fileHash = SHA256.Create().ComputeHash(fileStream1);
            }

            if (fileHash.SequenceEqual(fInfo.LastVersion.HashCode))
            {
                File.Delete(full_name);
                return;
            }

            var cn = ConnectionsConfig.Instance.Connections.Where(x => x.Id == fInfo.ConnId).FirstOrDefault();
            var conn = new FTPConnectionInfo()
            {
                ConnUsername = cn.ConnUsername,
                ConnPassword = cn.ConnPassword,
                ConnIP = cn.ConnIP,
                ConnPort = cn.ConnPort
            };

            System.IO.File.Move(full_name, fDesc.GetFulName(fInfo.VersionsCount));

            var ftpconn = new FTPConnectionHelper() { connInfo = conn };
            var cancel = new CancellationTokenSource();
            await ftpconn.UploadFile(System.IO.Directory.GetCurrentDirectory(), fDesc.GetNameWithExt(fInfo.VersionsCount), cancel, 
                UpdateProgress, UploadCompleted, TransferError, fInfo.RemotePath, true);
        }
        void UpdateProgress(int progress)
        {
            //Progress = progress;
        }
 
        void UploadCompleted()
        {
            var file_name = fDesc.GetFulName(fInfo.VersionsCount);
            var fsize = new FileInfo(file_name).Length.ToString();
            var fv = new FTPFileVersion()
            {
                FileId = fInfo.Id,
                Size = long.Parse(fsize),
                FileVersion = fInfo.VersionsCount,
                LastModified = DateTime.Now.Ticks,
                HashCode = fileHash,
                RemotePath = "\\RemoteFTPFiles"
            };
            fileHash = null;
            FilesConfig.Instance.AddVersion(fv);
            Application.Current.Dispatcher.Invoke(() => { { FilesConfig.Instance.GetFiles(); } });
            File.Delete(file_name);
        }
        void TransferError(string error)
        {
            CustomMessageBox messageBox = new CustomMessageBox() { Header = "Error opening file", Message = error };
            messageBox.customButtons.Add(new Tuple<string, RoutedEventHandler>("OK", null));
            messageBox.ShowDialog();
        }

        private void lvFIlesDirs_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (lvFIlesDirs.SelectedItem == null)
                    return;

                var file = lvFIlesDirs.SelectedItem as FTPFileInfo;
                CustomMessageBox messageBox = new CustomMessageBox() { Header = "Delete File", Message = $"Do you want to delete file \"{file.FName}\"" };
                messageBox.customButtons.Add(new Tuple<string, RoutedEventHandler>("OK", (_sender, _e) =>
                {
                    FilesConfig.Instance.DeleteFile(file);
                }));
                messageBox.customButtons.Add(new Tuple<string, RoutedEventHandler>("Cancel", null));
                messageBox.ShowDialog();
            }
        }

        private void lvFIlesDirs_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            int a = 2;
        }

    }
    public class FileVersionDesc
    {
        public string name;
        public string dir;
        public string ext;
        public long size;

        public string GetFulName(int version)
        {
            if (version <= 0)
                return dir + "\\" + name + "." + ext;
            return dir + "\\" + name + $"_{version}" + "." + ext;
        }
        public string GetNameWithExt(int version)
        {
            if (version <= 0)
                return name + "." + ext;
            return name + $"_{version}" + "." + ext;
        }
    }
}
//TODO:
//Add description
//Edit old versions
//Remove files
//remove verisons from ftp
//find versions when adding