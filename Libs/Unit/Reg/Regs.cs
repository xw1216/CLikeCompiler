using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Reg
{
    public class Regs : IRecord
    {
        public int Index { get; private set; }
        public string Name { get => RegFiles.GetRegName(Index); set {; } }
        public IRecord Cont { get; set; }

        internal Regs(int index)
        {
            Index = index;
        }

        public RecordType GetRecordType()
        {
            return RecordType.REGS;
        }
    }
}
