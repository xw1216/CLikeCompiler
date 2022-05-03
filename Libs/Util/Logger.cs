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
    public sealed class Logger
    {
        private static Logger logger = new();

        private StorageFile logFile;
        private readonly SemaphoreSlim semaphore;
        public const int RecDisplayCnt = 300;
        public const int LogDisplayCnt = 150;
        public const int LogDisplayPageCnt = 15;

        public delegate void LogChangeHandler(LogMsgItem msg);
        public delegate void LogClearHandler();
        public event LogChangeHandler LogChange;
        public event LogClearHandler LogClear;

        public  ObservableCollection<LogMsgItem> LogDisplayed { get; } = new();

        public  ObservableCollection<LogAnalyItem> ActionDisplayed { get; } = new();

        public  int LogPages
        {
            get
            {
                int pages = LogDisplayed.Count / LogDisplayPageCnt;
                if (LogDisplayed.Count % LogDisplayCnt != 0)
                {
                    pages += 1;
                }
                return pages;
            }
        }

        private Logger()
        {
            semaphore = new SemaphoreSlim(1, 1);
        }

        public static ref Logger Instance()
        {
            return ref logger;
        }

        public void Initialize()
        {
            OpenLogHandle();
        }


        #region Analyze Action

        public  void ActionRecordTest()
        {
            LexUnit unit = new()
            {
                name = "Test",
                cont = "Test"
            };
            NewActionRecord(Term.End, unit, "Action Record Test ................................");
        }

        internal void NewActionRecord(Symbols sym, LexUnit unit, string msg)
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

        public  void ClearActionRecord()
        {
            ActionDisplayed.Clear();
        }

        #endregion

        #region compiler logs

        public void NewLogRecord(string msg, LogMsgItem.Type type)
        {
            RemoveOverflowLog();
            LogMsgItem item = new(msg, type);
            LogDisplayed.Add(item);
            ExportRecordToFile(item);
            OnLogChange(item);
        }

        private async void OpenLogHandle()
        {
            StorageFolder storageFolder =
                ApplicationData.Current.LocalFolder;
            logFile = await storageFolder.CreateFileAsync("Log.txt",
                CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(logFile, "");
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

        private void RemoveOverflowLog()
        {
            if (LogDisplayed.Count > LogDisplayCnt)
            {
                LogDisplayed.Remove(LogDisplayed.First());
            }
        }

        public void ClearDisplayLog()
        {
            LogDisplayed.Clear();
            OnLogClear();
        }

        public  bool IsLogEmpty()
        {
            if (LogDisplayed == null || LogDisplayed.Count > 0)
            {
                return true;
            }
            return false;
        }

        #endregion

        private void OnLogChange(LogMsgItem msg)
        {
            LogChange?.Invoke(msg);
        }

        private void OnLogClear()
        {
            LogClear?.Invoke();
        }
    }
}
