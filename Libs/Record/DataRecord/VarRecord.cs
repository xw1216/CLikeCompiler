using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Reg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.DataRecord
{
    internal class VarRecord : IDataRecord
    {
        public string Name { get; set; } = "";
        public int Offset { get; set; }
        public int Width => IDataRecord.GetWidth(Type);
        public VarType Type { get; set; }
        public RecordPos Pos { get; set; }
        public Regs Reg { get; set; }

        public int Length => Width;

        public bool IsGlobal { get; set; }

        internal VarRecord() { }

        internal VarRecord(string name, VarType type)
        {
            this.Name = name;
            this.Type = type;
            this.IsGlobal = false;
        }

        internal virtual bool IsTemp() { return false; }
        internal virtual bool IsCons() { return false; }

        public RecordType GetRecordType()
        {
            return RecordType.VAR;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
