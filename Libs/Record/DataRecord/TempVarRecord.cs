using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.DataRecord
{
    internal class TempVarReocrd : VarRecord
    {
        private static long tmpCnt = 0;

        internal TempVarReocrd()
        {
            this.Name = "~Tmp" + tmpCnt;
            tmpCnt++;
        }
        internal override bool IsTemp() { return true; }
    }
}
