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
        }


        internal override bool IsCons() { return true; }
    }
}
