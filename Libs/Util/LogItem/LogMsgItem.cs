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

        private Type Severity { get; set; }
        public string Content { get; set; }
        private DateTime Time { get; set; }

        public LogMsgItem()
        {
            Time = DateTime.Now;
        }

        public LogMsgItem(string content, Type type = Type.INFO)
        {
            Content = content;
            Severity = type;
            Time = DateTime.Now;
        }

        public string GetTimeStr()
        {
            return Time.ToString("G");
        }

        public string GetTipStr()
        {
            return GetSeverityStr() + "  " + Time.ToString("G");
        }

        public string GetSeverityFont()
        {

            // {x:Bind GetSeverityFont()}
            return Severity switch
            {
                Type.INFO => "\uF13C",
                Type.WARN => "\uF142",
                Type.ERROR => "\uF13D",
                _ => "\uF13D;",
            };
        }

        public string GetSeverityStr()
        {
            return Severity switch
            {
                Type.INFO => "提示",
                Type.WARN => "警告",
                Type.ERROR => "错误",
                _ => "错误",
            };
        }

    }
}
