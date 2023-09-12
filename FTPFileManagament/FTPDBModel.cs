using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPFileManagament
{
    public class FilesConfig
    {
        public ObservableCollection<FTPFileInfo> RemoteFiles { get; } = new ObservableCollection<FTPFileInfo>();
        public static FilesConfig Instance { get; set; } = new FilesConfig();
        public void GetFiles()
        {
            RemoteFiles.Clear();

            using (var context = new FTPDBContext())
            {
                foreach (var finfo in context.FTPFileInfoes)
                {
                    RemoteFiles.Add(finfo);
                    finfo.FileVersions = new ObservableCollection<FTPFileVersion>( finfo.FTPFileVersions.Reverse().ToList());
                }
            }
        }
        public void AddFile(FTPFileInfo ci)
        {
            using (var context = new FTPDBContext())
            {
                context.FTPFileInfoes.Add(ci);
                context.SaveChanges();
            }
            RemoteFiles.Add(ci);
        }
        public void UpdateFile(FTPFileInfo ci)
        {
            using (var context = new FTPDBContext())
            {
                var conn = context.FTPFileInfoes.Where(x => x.Id == ci.Id).FirstOrDefault();
                if (conn == null)
                    throw new KeyNotFoundException();

                conn.ConnId = ci.ConnId;
                conn.RemotePath= ci.RemotePath;
                conn.FileFormat = ci.FileFormat;
                context.SaveChanges();
            }
        }
        public void DeleteFile(FTPFileInfo ci)
        {
            if (ci == null)
                return;
            using (var context = new FTPDBContext())
            {
                var rem_obj = context.FTPFileInfoes.Where(x => x.Id == ci.Id).FirstOrDefault();
                if (rem_obj == null)
                    throw new KeyNotFoundException();

                context.FTPFileInfoes.Remove(rem_obj);
                context.SaveChanges();
            }
            RemoteFiles.Remove(ci);
        }

        public void AddVersion(FTPFileVersion vi)
        {
            using (var context = new FTPDBContext())
            {
                context.FTPFileVersions.Add(vi);
                context.SaveChanges();
            }
        }
    }
    public class ConnectionsConfig
    {
        public ObservableCollection<FTPConnectionInfo> Connections { get; } = new ObservableCollection<FTPConnectionInfo>();
        //public ObservableCollection<FTPConnectionInfo> Connections { get; } = new ObservableCollection<FTPConnectionInfo>() { new FTPConnectionInfo() { ConnName = "Quick Connect", CanUserDelete = false } };
        public static ConnectionsConfig Instance { get; set; } = new ConnectionsConfig();

        public void GetConnections()
        {

            //int cnt = Connections.Count;
            //for (int i = 0; i < cnt - 1; i++)
            //    Connections.RemoveAt(1);
            Connections.Clear();

            using (var context = new FTPDBContext())
            {
                foreach(var con in context.FTPConnectionInfoes)
                    Connections.Add(con);
            }
        }
        public void AddConnection(FTPConnectionInfo ci)
        {
            using (var context = new FTPDBContext())
            {
                context.FTPConnectionInfoes.Add(ci);
                context.SaveChanges();
            }
            Connections.Add(ci);
        }
        public void UpdateConnection(FTPConnectionInfo ci)
        {
            using (var context = new FTPDBContext())
            {
                var conn = context.FTPConnectionInfoes.Where(x => x.Id == ci.Id).FirstOrDefault();
                if (conn == null)
                    throw new KeyNotFoundException();

                conn.ConnName = ci.ConnName;
                conn.ConnUsername = ci.ConnUsername;
                conn.ConnIP = ci.ConnIP;
                conn.ConnPort = ci.ConnPort;
                conn.ConnPassword = ci.ConnPassword;

                context.SaveChanges();
            }
        }
        public void DeleteConnection(FTPConnectionInfo ci)
        {
            using (var context = new FTPDBContext())
            {
                var rem_obj = context.FTPConnectionInfoes.Where(x => x.Id == ci.Id).FirstOrDefault();
                if (rem_obj == null)
                    throw new KeyNotFoundException();

                context.FTPConnectionInfoes.Remove(rem_obj);
                context.SaveChanges();
            }
            Connections.Remove(ci);
        }
    }
}
