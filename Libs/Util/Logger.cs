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
        private const int RecDisplayCnt = 15;
        private const int LogDisplayCnt = 30;

        public static ObservableCollection<LogMsgItem> LogDisplayed { get; } = new();

        public static ObservableCollection<LogAnalyItem> ActionDisplayed { get; } = new();

        private Logger()
        {
            semaphore = new SemaphoreSlim(1, 1);
        }

        public static void ActionRecordTest()
        {
            LexUnit unit = new()
            {
                name = "Test",
                cont = "Test"
            };
            NewActionRecord(Term.End, unit, "Action Record Test ................................");
        }

        internal static void NewActionRecord(Symbols sym, LexUnit unit, string msg)
        {
            LogAnalyItem action = new()
            {
                stackTop = sym,
                inputFirst = unit,
                msg = msg
            };
            if (ActionDisplayed.Count > RecDisplayCnt)
            {
                ActionDisplayed.RemoveAt(0);
            }
            ActionDisplayed.Add(action);
        }

        public static void ClearActionRecord()
        {
            ActionDisplayed.Clear();
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
            LogDisplayed.Add(item);
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
            string logLine = item.GetTimeStr() + " [" + item.GetSeverityStr() + "] : " + item.Content + "\n";
            await semaphore.WaitAsync();
            await FileIO.AppendTextAsync(logFile, logLine);
            semaphore.Release();
        }

        private void RemoveOverflowRecord()
        {
            if (LogDisplayed.Count > LogDisplayCnt)
            {
                LogDisplayed.Remove(LogDisplayed.First());
            }
        }

        public void ClearDisplayRecord()
        {
            LogDisplayed.Clear();
        }

        public static bool IsLogEmpty()
        {
            if (LogDisplayed == null || LogDisplayed.Count > 0)
            {
                return true;
            }
            return false;
        }
    }
}
