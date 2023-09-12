using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
    /// Interaction logic for FTPScreen.xaml
    /// </summary>
    public partial class FTPScreen : UserControl, INotifyPropertyChanged,ITabContent
    {
        public Window OwnerWnd { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string TabName { get => Connection.connInfo.ConnIP; }

        public Brush Foregnd => Brushes.Black;

        public Brush Backgnd => Brushes.LightGray;
        public string ConnName { get => Connection.connInfo.ConnName + " (FTP)"; }

        FileDescription CurrentDirLocal;

        FileDescription CurrentDirRemote;

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<FileDescription> FilesLocal { get; set; } = new ObservableCollection<FileDescription>();
        public ObservableCollection<FileDescription> FilesRemote { get; set; } = new ObservableCollection<FileDescription>();
        public FTPConnectionHelper Connection { get; set; }
        public List<FileDescription> LocalDirsTree { get; set; } = new List<FileDescription>();
        public List<FileDescription> RemoteDirsTree { get; set; } = new List<FileDescription>();
        public int Progress { get => progress; set { progress = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Progress")); } }
        int progress = 0;
        public bool TrasferExecuting { get => transfer_executing; set { CommandExecuting = transfer_executing = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrasferExecuting")); } }
        bool transfer_executing = false;
        public bool CommandExecuting { get => command_executing; set { command_executing = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CommandExecuting")); } }
        bool command_executing = false;
        public string InfoStr { get => Info.ToString(); set { Info.AppendLine(value); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InfoStr")); } }
        public bool HasError = false;
        public StringBuilder Info = new StringBuilder();

        public BackgroundWorker worker;
        bool Connected = false;
        public FTPScreen(FTPConnectionHelper fconn)
        {
            Connection = fconn;
            InitRemoteFiles();
            InitLocalFiles();
            InitializeComponent();
        }
        void InitLocalFiles()
        {
            LocalDirsTree.Clear();

            var currentDinfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var parentDinfo = currentDinfo.Parent;
            var CurrentDir = CurrentDirLocal = new FileDescription(currentDinfo.Name, currentDinfo.FullName, "Directory", currentDinfo.LastWriteTime.ToString(), null);
            LocalDirsTree.Add(CurrentDir);

            while (parentDinfo != null)
            {
                var ParentDir = new FileDescription(parentDinfo.Name, parentDinfo.FullName, "Directory", parentDinfo.LastWriteTime.ToString(), null);
                CurrentDir.Parent = ParentDir;
                LocalDirsTree.Add(ParentDir);

                CurrentDir = ParentDir;
                parentDinfo = parentDinfo.Parent;
            }

            LocalDirsTree.Reverse();
            CollectionViewSource.GetDefaultView(LocalDirsTree).Refresh();
            GetLocalFiles();

        }
        void GetLocalFiles()
        {
            var currentDinfo = new DirectoryInfo(CurrentDirLocal.FullName);
            FileInfo[] files = currentDinfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            DirectoryInfo[] dirs = currentDinfo.GetDirectories();

            FilesLocal.Clear();
            foreach (FileInfo file in files)
                FilesLocal.Add(new FileDescription(file.Name, file.FullName, file.Extension, file.LastWriteTime.ToString(), CurrentDirLocal, file.Length));
            foreach (var dir in dirs)
                FilesLocal.Add(new FileDescription(dir.Name, dir.FullName, "Directory", dir.LastWriteTime.ToString(), CurrentDirLocal));
        }

        void InitRemoteFiles()
        {
            InfoStr = $"Connecting to {Connection.connInfo.ConnIP}:{Connection.connInfo.ConnPort}...";
            Action<string> OnError = new Action<string>((error) => { InfoStr = error; });
            Action<FileDescription> OnComplete = new Action<FileDescription>((working_dir) =>
            {
                CurrentDirRemote = working_dir;
            });

            worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                CommandExecuting = true;
                try
                {
                    Connection.GetWorkingDir(OnComplete, OnError);
                }
                catch (Exception ex)
                {
                    e.Result = ex.Message;
                }

            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                CommandExecuting = false;
                if (e.Result == null)
                {
                    Connected = true;
                    InfoStr = "Connected";
                    RemoteDirsTree.Add(CurrentDirRemote);
                    CollectionViewSource.GetDefaultView(RemoteDirsTree).Refresh();
                    GetRemoteFiles(CurrentDirRemote);
                }
            };
            worker.RunWorkerAsync();

        }
        void GetRemoteFiles(FileDescription working_dir, Action OnSuccess = null)
        {
            FilesRemote.Clear();

            Action<string> OnError = new Action<string>((error) =>
            {
                Connected = false;
                InfoStr = error;
            });
            Action<FluentFTP.FtpListItem[]> OnComplete = new Action<FluentFTP.FtpListItem[]>((FTPFiles) =>
            {
                if (!Connected)
                {
                    Connected = true;
                    InfoStr = "Connected";
                }
                InfoStr = $"Directory listing of \"{working_dir.Name}\" successful";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (working_dir != CurrentDirRemote)
                        CurrentDirRemote = working_dir;

                    foreach (var item in FTPFiles)
                        FilesRemote.Add(new FileDescription(item.Name, item.FullName, item.Type.ToString(), item.Modified.ToString(), CurrentDirRemote, item.Size));

                    if (OnSuccess != null)
                        OnSuccess();
                });

            });

            if (!Connected) InfoStr = $"Connecting to {Connection.connInfo.ConnIP}:{Connection.connInfo.ConnPort}...";
            worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                CommandExecuting = true;
                Connection.GetNameListing(working_dir, OnComplete, OnError);
            };
            worker.RunWorkerCompleted += (sender, e) => { CommandExecuting = false; };
            worker.RunWorkerAsync();


        }

        CancellationTokenSource cancel;

        private async void btndonwload_Click(object sender, RoutedEventArgs e)
        {
            if (CommandExecuting)
                return;

            if (CurrentDirLocal == null || !(lvRemoteDirs.SelectedItem is FileDescription fdesc) || fdesc.IsDirectory)
            {
                InfoStr = "No file selected";
                return;
            }
            var dir = CurrentDirLocal.FullName;
            var file = (lvRemoteDirs.SelectedItem as FileDescription).Name;

            InfoStr = "Download in progress...";

            cancel = new CancellationTokenSource();
            TrasferExecuting = true;
            await Connection.DownloadFile(dir, file, cancel, UpdateProgress, DownloadCompleted, TransferError, CurrentDirRemote.FullName);
        }

        private async void btnupload_Click(object sender, RoutedEventArgs e)
        {
            if (CommandExecuting)
                return;
            if (CurrentDirLocal == null || !(lvLocalDirs.SelectedItem is FileDescription fdesc) || fdesc.IsDirectory)
            {
                InfoStr = "No file selected";
                return;
            }
            //System.Diagnostics.Trace.WriteLine(worker.IsBusy);
            var dir = CurrentDirLocal.FullName;
            var file = fdesc.Name;


            cancel = new CancellationTokenSource();
            TrasferExecuting = true;
            InfoStr = "Upload in progress...";

            await Connection.UploadFile(dir, file, cancel, UpdateProgress, UploadCompleted, TransferError, CurrentDirRemote.FullName);
        }
        void UpdateProgress(int progress)
        {
            Progress = progress;
        }
        void UploadCompleted()
        {
            TrasferExecuting = false;
            Progress = 0;
            InfoStr = "Upload Completed!";
            Application.Current.Dispatcher.Invoke(() => { GetRemoteFiles(CurrentDirRemote); });
        }
        void DownloadCompleted()
        {
            TrasferExecuting = false;
            Progress = 0;
            InfoStr = "Download Completed!";
            Application.Current.Dispatcher.Invoke(() => { GetLocalFiles(); });
        }
        void TransferError(string error)
        {
            TrasferExecuting = false;
            Progress = 0;
            InfoStr = error;
            HasError = true;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (Connected) InfoStr = $"Retreiving directory listing of \"{ CurrentDirRemote.Name}\"";
            GetLocalFiles();
            if (CurrentDirRemote == null)
                InitRemoteFiles();
            else
                GetRemoteFiles(CurrentDirRemote);
        }

        private void btnLocDirChange_Click(object sender, RoutedEventArgs e)
        {
            var seldir = ((Button)sender).Tag as FileDescription;
            if (CurrentDirLocal == seldir)
                return;


            while (CurrentDirLocal != seldir)
            {
                LocalDirsTree.Remove(CurrentDirLocal);
                CurrentDirLocal = CurrentDirLocal.Parent;
            }

            GetLocalFiles();
            CollectionViewSource.GetDefaultView(LocalDirsTree).Refresh();
        }
        private void btnRemDirChange_Click(object sender, RoutedEventArgs e)
        {
            var seldir = ((Button)sender).Tag as FileDescription;
            if (CurrentDirRemote == seldir)
                return;

            InfoStr = $"Retreiving directory listing of \"{ seldir.Name}\"";

            while (CurrentDirRemote != seldir)
            {
                RemoteDirsTree.Remove(CurrentDirRemote);
                CurrentDirRemote = CurrentDirRemote.Parent;
            }
            CollectionViewSource.GetDefaultView(RemoteDirsTree).Refresh();

            GetRemoteFiles(CurrentDirRemote);

        }

        private void lvLocalDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var sel = ((ContentControl)sender).Content as FileDescription;
            if (!sel.IsDirectory)
                return;

            CurrentDirLocal = sel;
            LocalDirsTree.Add(CurrentDirLocal);
            CollectionViewSource.GetDefaultView(LocalDirsTree).Refresh();
            GetLocalFiles();
        }
        private void lvRemoteDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var sel = ((ContentControl)sender).Content as FileDescription;
            if (!sel.IsDirectory)
                return;

            InfoStr = $"Retreiving directory listing of \"{ sel.Name}\"";

            CurrentDirRemote = sel;
            RemoteDirsTree.Add(sel);
            CollectionViewSource.GetDefaultView(RemoteDirsTree).Refresh();
            GetRemoteFiles(((ContentControl)sender).Content as FileDescription);
        }

        private void txtInfo_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtInfo.ScrollToEnd();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cancel.Cancel();
        }

        public void Close()
        {
            if (transfer_executing)
                cancel.Cancel();
        }

        private async void btnAddDB_Click(object sender, RoutedEventArgs e)
        {
            if (!(lvRemoteDirs.SelectedItem is FileDescription fdesc) || fdesc.IsDirectory)
                return;

            var dir = CurrentDirLocal.FullName;
            var file = fdesc.Name;
            cancel = new CancellationTokenSource();

            await Connection.DownloadFile(dir, file, cancel, UpdateProgress, AddDBDownloadCompleted, TransferError, CurrentDirRemote.FullName);
            if (HasError) return;

           
        }
        void AddDBDownloadCompleted()
        {
            if (!(lvRemoteDirs.SelectedItem is FileDescription fdesc) || fdesc.IsDirectory)
                return;

            var dir = CurrentDirLocal.FullName;
            var file = fdesc.Name;

            byte[] fileHash1;
            using (FileStream fileStream1 = new FileStream($"{dir}\\{file}", FileMode.Open))
            {
                fileHash1 = System.Security.Cryptography.SHA256.Create().ComputeHash(fileStream1);
            }
            string ext = System.IO.Path.GetExtension($"{dir}\\{file}").Remove(0, 1);
            string name = System.IO.Path.GetFileNameWithoutExtension($"{dir}\\{file}");

            var selected_conn = ConnectionsConfig.Instance.Connections.Where(x => x.ConnName == Connection.connInfo.ConnName).FirstOrDefault();

            var fi = new FTPFileInfo() { FName = name, FileFormat = ext, ConnId = selected_conn.Id, RemotePath = "\\RemoteFTPFiles" };
            FilesConfig.Instance.AddFile(fi);

            var fv = new FTPFileVersion() { FileId = fi.Id, Size = (long)fdesc.dSize, FileVersion = 1, LastModified = DateTime.Now.Ticks, HashCode = fileHash1, RemotePath = fdesc.GetPathName() };
            FilesConfig.Instance.AddVersion(fv);
            FilesConfig.Instance.GetFiles();
            TrasferExecuting = false;
            Progress = 0;
            //InfoStr = "Download Completed!";
            Application.Current.Dispatcher.Invoke(() => { GetLocalFiles(); });
            File.Delete($"{dir}\\{file}");
        }
     
    }

    public class FileDescription
    {
        public FileDescription() { }
        public FileDescription(string name, string fullname, string type, string lastmoddate, FileDescription parent, double size = -1)
        {

            Name = name;
            FullName = fullname;
            Type = type;
            LastModDate = lastmoddate;
            Parent = parent;
            dSize = size;
            if (size > 0)
                Size = Math.Round((double)(size) / 1024).ToString() + " KB";
            //83807930
            //11010048
        }

        public string Name { get; set; }
        public string FullName { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public double dSize { get; set; }
        public string LastModDate { get; set; }
        public FileDescription Parent { get; set; }
        public bool IsDirectory => Type == "Directory";
        public string GetPathName()
        {
            var par_name = "";
            if (Parent != null)
            {
                return Parent.GetPathName() + (IsDirectory ? "\\" + Name : "");
            }
            return "";
        }
    }


}
