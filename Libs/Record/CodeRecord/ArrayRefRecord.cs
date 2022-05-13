using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class ArrayRefRecord
    {
        public string Name { get; set; }

        internal ArrayRecord RefArray { get; set; }
        internal VarRecord RefIndex { get; set; }

        public ArrayRefRecord(ArrayRecord refArray, VarRecord refIndex)
        {
            this.RefArray = refArray;
            this.RefIndex = refIndex;
            this.Name = refArray.Name + "[" + refIndex.Name + "]";
        }

        public RecordType GetRecordType()
        {
            return RecordType.ARRAYREF;
        }
    }
}
