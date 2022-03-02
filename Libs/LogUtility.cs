using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CLikeCompiler.Libs
{ 
    public class LogItem
    {
        public enum MsgType
        {
            INFO,
            WARN,
            ERROR
        }

        public MsgType Serverity { get; set; }
        public string Content { get; set; }
        public DateTime Time { get; set; }

        public LogItem()
        {
            Time = DateTime.Now;
        }

        public LogItem(string content , MsgType type = MsgType.INFO)
        {
            Content = content;
            Serverity = type;
            Time = DateTime.Now;
        }

        public string GetTimeStr()
        {
            return Time.ToString("G");
        }

        public string GetServerityFont()
        {
            
            // {x:Bind GetServerityFont()}
            return Serverity switch
            {
                MsgType.INFO => "\uF13F;",
                MsgType.WARN => "\uF13C;",
                MsgType.ERROR => "\uF13D;",
                _ => "\uF13D;",
            };
        }

        public string GetServerityStr()
        {
            return Serverity switch
            {
                MsgType.INFO => "Info",
                MsgType.WARN => "Warning",
                MsgType.ERROR => "Error",
                _ => "Error",
            };
        }

    }
    public class LogUtility
    {
        public static LogUtility logger;
        private static SemaphoreSlim semaphore;
        private Windows.Storage.StorageFile logFile;
        private readonly int logDispCnt = 30;
        public ObservableCollection<LogItem> logDisplayed = new();
        public ObservableCollection<LogItem> LogDisplayed { get { return this.logDisplayed; } }

        public LogUtility()
        {
            OpenLogHandle();
            semaphore = new SemaphoreSlim(1, 1);
        }

        private async void OpenLogHandle()
        {
            Windows.Storage.StorageFolder storageFolder = 
                Windows.Storage.ApplicationData.Current.LocalFolder;
            logFile = await storageFolder.CreateFileAsync("Log.txt", 
                Windows.Storage.CreationCollisionOption.OpenIfExists);
            await Windows.Storage.FileIO.WriteTextAsync(logFile, "");
        }

        public void NewLogRecord(string msg, LogItem.MsgType type)
        {
            RemoveOverflowRecord();
            LogItem item = new(msg, type);
            logDisplayed.Add(item);
            ExportRecordToFile(item);
        }

        public void OpenLogInNotepad()
        {
            System.Diagnostics.Process proc = new();
            proc.StartInfo.FileName = "notepad.exe";
            proc.StartInfo.Arguments = logFile.Path;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();
        }

        private async void ExportRecordToFile(LogItem item)
        {
            string logLine = item.GetTimeStr() + " [" + item.GetServerityStr() + "] : " + item.Content + "\n";
            await semaphore.WaitAsync();
            await Windows.Storage.FileIO.AppendTextAsync(logFile, logLine);
            semaphore.Release();
        }

        private void RemoveOverflowRecord()
        {
            if(logDisplayed.Count > logDispCnt)
            {
                logDisplayed.Remove(logDisplayed.First());
            }
        }

        public void ClearDisplayRecord()
        {
            logDisplayed.Clear();
        }

    }


}
