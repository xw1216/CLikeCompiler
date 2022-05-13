using CLikeCompiler.Libs.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.DataRecord
{
    internal class ConsVarRecord : VarRecord
    {
        private static long conCnt = 0;

        internal object Val { get; set; }
        internal string OriginCont { get; set; }
        internal ConsVarRecord()
        {
            this.Name = "~Con" + conCnt;
            conCnt++;
            Pos = Enum.RecordPos.DATA;
            IsGlobal = true;
        }

        internal ConsVarRecord(VarType type)
        {
            this.Name = "~Con" + conCnt;
            conCnt++;
            Pos = Enum.RecordPos.DATA;
            Type = type;
            IsGlobal = true;
        }

        internal override bool IsCons() { return true; }
    }
}
