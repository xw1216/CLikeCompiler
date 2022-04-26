using CLikeCompiler.Libs.Unit.Analy;
using CLikeCompiler.Libs.Unit.Symbol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Util.LogItem
{
    public class LogAnalyItem
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
            if (inputFirst == null) { return ""; }
            return inputFirst.name;
        }

        public string GetInputCont()
        {
            if (inputFirst == null) { return ""; }
            return inputFirst.cont;
        }

        public string GetMsg()
        {
            if (msg == null) { return ""; }
            return msg;
        }
    }
}
