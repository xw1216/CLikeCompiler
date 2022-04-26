using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.DataRecord
{
    internal class ImmRecord : IRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.IMM;
        }
        public string Name { get; set; }

        internal long Value { get; set; }
    }
}
