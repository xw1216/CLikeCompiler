using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Reg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Record.DataRecord;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class CallRecord : IRecord
    {
        public string Name { get ; set; }

        internal FuncRecord Caller { get; set; }
        internal FuncRecord Callee { get; set; }
        internal List<Regs> SaveRegList { get; private set; }
        internal List<VarRecord> ArgsList { get; set; }

        internal int CallLength => SaveRegList.Count * FuncRecord.Dword + Callee.ArgLength;


        public CallRecord(FuncRecord caller, FuncRecord callee)
        {
            Caller = caller;
            Callee = callee;
        }

        internal bool CalcuCallerSaveRegs()
        {
            if(Caller == null || Callee == null) { return false; }

            SaveRegList = RegFiles.CalcuCallerSaveList(Caller, Callee);
            return true;
        }

        public override string ToString()
        {
            return Caller.Name + " : " + Callee.Name;
        }

        public RecordType GetRecordType()
        {
            return RecordType.CALL;
        }
    }
}
