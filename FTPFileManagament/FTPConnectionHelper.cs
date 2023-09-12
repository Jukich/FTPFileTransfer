using FluentFTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FTPFileManagament
{
    public class FTPConnectionHelper
    {
        public FTPConnectionInfo connInfo;

        FtpClient makeClient()
        {
            int port = connInfo.ConnPort.HasValue ? connInfo.ConnPort.Value : 21;
            if (string.IsNullOrEmpty(connInfo.ConnUsername) && string.IsNullOrEmpty(connInfo.ConnPassword))
                return new FtpClient(connInfo.ConnIP.ToString(), port);
            else
                return new FtpClient(connInfo.ConnIP.ToString(), connInfo.ConnUsername, connInfo.ConnPassword, port);
        }
        AsyncFtpClient makeClientAsync()
        {
            int port = connInfo.ConnPort.HasValue ? connInfo.ConnPort.Value : 21;

            if (string.IsNullOrEmpty(connInfo.ConnUsername) && string.IsNullOrEmpty(connInfo.ConnPassword))
                return new AsyncFtpClient(connInfo.ConnIP.ToString(), port);
            else
                return new AsyncFtpClient(connInfo.ConnIP.ToString(), connInfo.ConnUsername, connInfo.ConnPassword, port);

        }

        public void GetWorkingDir(Action<FileDescription> OnComplete, Action<string> OnError)
        {
            using (var conn = makeClient())
            {
                try
                {
                    conn.Connect();
                    var wdir = conn.GetWorkingDirectory();
                    OnComplete(new FileDescription(wdir, wdir, "Folder", "mod", null));
                }
                catch (Exception ex)
                {
                    OnError(ex.Message);
                    throw;
                }
            }
        }
        public void GetNameListing(FileDescription workingDir, Action<FtpListItem[]> OnComplete, Action<string> OnError)
        {
            using (var conn = makeClient())
            {
                try
                {
                    conn.Connect();
                    OnComplete(conn.GetListing(workingDir.FullName));
                }
                catch (Exception ex)
                {
                    OnError(ex.Message);
                }
            }
        }
        public async Task UploadFile(string path, string fileName, CancellationTokenSource cancel, Action<int> OnProgress, Action OnComplete, Action<string> OnError, string workingDir,bool createDir=false)
        {
            using (var conn = makeClientAsync())
            {
                Progress<FtpProgress> prg = new Progress<FtpProgress>(p => { OnProgress((int)p.Progress); });
                try
                {
                    conn.Config.NoopInterval = 1000;

                    await conn.Connect();
                    if (workingDir != conn.GetWorkingDirectory().Result)
                    {
                        try
                        {
                            await conn.SetWorkingDirectory(workingDir);
                        }
                        catch(Exception ex)
                        {
                            if (createDir)
                            {
                                await conn.CreateDirectory(workingDir);
                                await conn.SetWorkingDirectory(workingDir);
                            }
                            else
                                throw ex;
                        }
                    }
                    var res = await conn.UploadFile(System.IO.Path.Combine(path, fileName), fileName, FtpRemoteExists.Overwrite, false, FtpVerify.None, prg, cancel.Token);
                    if (res == FtpStatus.Success)
                        OnComplete();
                    else
                        OnError("Upload failed.");
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        OnError("Upload was canceled.");
                    else
                    {
                        if (ex.InnerException != null)
                            OnError(ex.InnerException.Message);
                        else
                            OnError(ex.Message);
                    }
                }
            }
        }

        public async Task DownloadFile(string path, string fileName, CancellationTokenSource cancel, Action<int> OnProgress, Action OnComplete, Action<string> OnError, string workingDir)
        {
            using (var conn = makeClientAsync())
            {
                Progress<FtpProgress> prg = new Progress<FtpProgress>(p => { OnProgress((int)p.Progress); });
                try
                {
                    conn.Config.NoopInterval = 1000;

                    await conn.Connect();
                    if (workingDir != conn.GetWorkingDirectory().Result)
                        await conn.SetWorkingDirectory(workingDir);
                    var res = await conn.DownloadFile(System.IO.Path.Combine(path, fileName), fileName, FtpLocalExists.Overwrite, FtpVerify.None, prg, cancel.Token);
                    if (res == FtpStatus.Success)
                        OnComplete();
                    //else
                    //    OnError(conn.ErrorMessage);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        OnError("Download was canceled.");
                    else
                    {
                        if (ex.InnerException != null)
                        {
                            OnError(ex.Message + "\n" + ex.InnerException.Message);
                        }
                        else
                            OnError(ex.Message);
                    }
                }
            }
        }
    }
}
