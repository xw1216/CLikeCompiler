using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Util.LogItem
{
    public class LogMsgItem
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

        public LogMsgItem()
        {
            Time = DateTime.Now;
        }

        public LogMsgItem(string content, MsgType type = MsgType.INFO)
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
}
