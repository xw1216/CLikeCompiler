using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Util.LogItem
{
    public class LogReportArgs : EventArgs
    {
        public readonly string msg;
        public readonly int lineNo;
        public readonly LogMsgItem.MsgType msgType;

        public LogReportArgs(LogMsgItem.MsgType msgType, string msg, int lineNo = 0)
        {
            this.msg = msg;
            this.lineNo = lineNo;
            this.msgType = msgType;
        }
    }
}
