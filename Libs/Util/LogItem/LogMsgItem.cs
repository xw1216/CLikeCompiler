using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Util.LogItem
{
    public class LogMsgItem
    {
        public enum Type
        {
            INFO,
            WARN,
            ERROR
        }

        public Type Serverity { get; set; }
        public string Content { get; set; }
        public DateTime Time { get; set; }

        public LogMsgItem()
        {
            Time = DateTime.Now;
        }

        public LogMsgItem(string content, Type type = Type.INFO)
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
                Type.INFO => "\uF13C",
                Type.WARN => "\uF142",
                Type.ERROR => "\uF13D",
                _ => "\uF13D;",
            };
        }

        public string GetServerityStr()
        {
            return Serverity switch
            {
                Type.INFO => "提示",
                Type.WARN => "警告",
                Type.ERROR => "错误",
                _ => "错误",
            };
        }

    }
}
