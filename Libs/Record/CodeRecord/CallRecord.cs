using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Reg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class CallRecord : IRecord
    {
        public string Name { get; set; }

        internal FuncRecord Caller { get; set; }
        internal FuncRecord Callee { get; set; }
        internal List<Regs> SaveRegList { get; private set; }

        internal bool CalcuCallerSaveRegs()
        {
            if(Caller == null || Callee == null) { return false; }

            SaveRegList = RegFiles.CalcuCallerSaveList(Caller, Callee);
            return true;
        }

        public RecordType GetRecordType()
        {
            return RecordType.CALL;
        }
    }
}
