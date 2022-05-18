using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class TypeRecord : IRecord
    {

        public string Name { get; set; }

        internal VarType VarType { get; }

        internal TypeRecord(VarType type)
        {
            VarType = type;
            Name = VarType.ToString();
        }

        public RecordType GetRecordType()
        {
            return RecordType.TYPE;
        }

        public override string ToString()
        {
            return VarType.ToString();
        }
    }
}
