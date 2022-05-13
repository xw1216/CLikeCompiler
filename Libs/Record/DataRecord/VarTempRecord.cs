using CLikeCompiler.Libs.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.DataRecord
{
    internal class VarTempRecord : VarRecord
    {
        private static long tmpCnt = 0;

        internal VarTempRecord()
        {
            Name = "~Tmp" + tmpCnt;
            tmpCnt++;
            Pos = Enum.RecordPos.MEM;
            IsGlobal = false;
        }

        internal VarTempRecord(VarType type)
        {
            Name = "~Tmp" + tmpCnt;
            tmpCnt++;
            Pos = Enum.RecordPos.MEM;
            Type = type;
            IsGlobal = false;
        }

        internal override bool IsTemp() { return true; }
    }
}
