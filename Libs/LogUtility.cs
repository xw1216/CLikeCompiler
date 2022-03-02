using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public DateTime Time { get;  }

        LogItem()
        {
            Time = DateTime.Now;
        }

        LogItem(string content , MsgType type = MsgType.INFO)
        {
            Content = content;
            Serverity = type;
            Time = DateTime.Now;
        }

        public string GetTimeString()
        {
            return Time.ToString();
        }

        public string GetServerityFont()
        {
            return Serverity switch
            {
                MsgType.INFO => "&#xF13F;",
                MsgType.WARN => "&#xF13C;",
                MsgType.ERROR => "&#xF13D;",
                _ => "&#xF13D;",
            };
        }

    }
    public class LogUtility
    {
        private readonly int logDispCnt = 50;
        private ObservableCollection<LogItem> logDisplayed = new();
        public ObservableCollection<LogItem> LogDisplayed { get { return logDisplayed; } }

        public void newLogRecord(string msg, LogItem.MsgType type)
        {

        }

        private void removeOverflowRecord()
        {

        }



    }


}
