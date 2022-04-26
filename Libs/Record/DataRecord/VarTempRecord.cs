using CLikeCompiler.Libs.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.DataRecord
{
    internal class VarTempReocrd : VarRecord
    {
        private static long tmpCnt = 0;

        internal VarTempReocrd()
        {
            Name = "~Tmp" + tmpCnt;
            tmpCnt++;
            Pos = Enum.RecordPos.MEM;
        }

        internal VarTempReocrd(VarType type)
        {
            Name = "~Tmp" + tmpCnt;
            tmpCnt++;
            Pos = Enum.RecordPos.MEM;
            Type = type;
        }

        internal override bool IsTemp() { return true; }
    }
}
