using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;
using CLikeCompiler.Libs.Unit.Analy;
using CLikeCompiler.Libs.Unit.Symbol;
using CLikeCompiler.Libs.Util.LogItem;

namespace CLikeCompiler.Libs.Util
{
    public class Logger
    {
        private static Logger logger = new();

        private StorageFile logFile;
        private readonly SemaphoreSlim semaphore;
        private static readonly int recDispCnt = 15;
        private static readonly int logDispCnt = 30;

        private static readonly ObservableCollection<LogMsgItem> logDisplayed = new();
        public static ObservableCollection<LogMsgItem> LogDisplayed { get { return logDisplayed; } }

        private static readonly ObservableCollection<LogAnalyItem> actionDisplayed = new();
        public static ObservableCollection<LogAnalyItem> ActionDisplayed { get { return actionDisplayed; } }

        public Logger()
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
            LogAnalyItem action = new();
            action.stackTop = sym;
            action.inputFirst = unit;
            action.msg = msg;
            if (actionDisplayed.Count > recDispCnt)
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

        public static ref Logger Instance()
        {
            return ref logger;
        }

        private async void OpenLogHandle()
        {
            StorageFolder storageFolder =
                ApplicationData.Current.LocalFolder;
            logFile = await storageFolder.CreateFileAsync("Log.txt",
                CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(logFile, "");
        }

        public void NewLogRecord(string msg, LogMsgItem.Type type)
        {
            RemoveOverflowRecord();
            LogMsgItem item = new(msg, type);
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

        private async void ExportRecordToFile(LogMsgItem item)
        {
            string logLine = item.GetTimeStr() + " [" + item.GetServerityStr() + "] : " + item.Content + "\n";
            await semaphore.WaitAsync();
            await FileIO.AppendTextAsync(logFile, logLine);
            semaphore.Release();
        }

        private void RemoveOverflowRecord()
        {
            if (logDisplayed.Count > logDispCnt)
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
            if (logDisplayed == null || logDisplayed.Count > 0)
            {
                return true;
            }
            return false;
        }
    }
}
