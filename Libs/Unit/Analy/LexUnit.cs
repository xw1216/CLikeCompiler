using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Analy
{
    internal class LexUnit
    {
        internal enum Type
        {
            ID, KEYWORD, OP,
            INT, DEC, STR,
            CH, END
        }

        internal Type type;
        internal string name;
        internal string cont;
        internal int line;
    }
}
