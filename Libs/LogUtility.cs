using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;

namespace CLikeCompiler.Libs
{ 
    public class ActionRecord
    {
        internal Symbols stackTop;
        internal LexUnit inputFirst;
        internal string msg;

        public string GetStackTopStr()
        {
            if (stackTop == null) { return ""; }
            return stackTop.GetName();
        }

        public string GetInputStr()
        {
            if(inputFirst == null) { return ""; }
            return inputFirst.name;
        }

        public string GetInputCont()
        {
            if(inputFirst == null) { return ""; }
            return inputFirst.cont;
        }

        public string GetMsg()
        {
            if(msg == null) { return ""; }
            return msg;
        }

    }
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

        public string GetTipStr()
        {
            return GetServerityStr() + "  " + Time.ToString("G");
        }

        public string GetServerityFont()
        {
            
            // {x:Bind GetServerityFont()}
            return Serverity switch
            {
                MsgType.INFO => "\uF13C",
                MsgType.WARN => "\uF142",
                MsgType.ERROR => "\uF13D",
                _ => "\uF13D;",
            };
        }

        public string GetServerityStr()
        {
            return Serverity switch
            {
                MsgType.INFO => "提示",
                MsgType.WARN => "警告",
                MsgType.ERROR => "错误",
                _ => "错误",
            };
        }

    }
    public class LogUtility
    {
        private static LogUtility logger = new();

        private SemaphoreSlim semaphore;
        private StorageFile logFile;
        private static readonly int recDispCnt = 15;
        private static readonly int logDispCnt = 30;
        private static ObservableCollection<LogItem> logDisplayed = new();
        public static ObservableCollection<LogItem> LogDisplayed { get { return logDisplayed; } }

        private static ObservableCollection<ActionRecord> actionDisplayed = new();
        public static ObservableCollection<ActionRecord> ActionDisplayed { get { return actionDisplayed; } }

        public LogUtility()
        {
            semaphore = new SemaphoreSlim(1, 1);
        }

        public static void ActionRecordTest()
        {
            LexUnit unit = new();
            unit.name = "Test";
            unit.cont = "Test";
            NewActionRecord(Term.end, unit, "Action Record Test ................................");
        }

        internal static void NewActionRecord(Symbols sym, LexUnit unit, string msg)
        {
            ActionRecord action = new();
            action.stackTop = sym;
            action.inputFirst = unit;
            action.msg = msg;
            if(actionDisplayed.Count > recDispCnt)
            {
                actionDisplayed.RemoveAt(0);
            }
            actionDisplayed.Add(action);
        }

        public static void ClearActionRecord()
        {
            actionDisplayed.Clear();
        }

        public void Initialize()
        {
            OpenLogHandle();
        }

        public static ref LogUtility GetInstance()
        {
            return ref logger;
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

        public static bool IsLogEmpty()
        {
            if(logDisplayed == null || logDisplayed.Count > 0)
            {
                return true;
            } 
            return false;
        }

    }


}
