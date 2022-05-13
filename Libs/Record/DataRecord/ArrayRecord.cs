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
    internal class ArrayRecord : IDataRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.ARRAY;
        }

        private List<int> dimList = new();

        public string Name { get; set; } = "";
        public int Width => IDataRecord.GetWidth(Type);
        public VarType Type { get; set; }
        public RecordPos Pos { get; set; }
        public int Offset { get; set; }
        public Regs Reg { get; set; }
        public bool IsGlobal { get; set; }

        internal int Dim
        {
            get
            {
                if (dimList == null) { return 0; }
                else { return dimList.Count; }
            }
        }

        internal int Length
        {
            get
            {
                int len = 0;
                for (int i = 0; i < dimList.Count; i++) { len += dimList[i]; }
                len *= this.Width;
                return len;
            }
        }

        internal virtual bool IsCons() { return false; }

        internal void SetDimList(List<int> list) { dimList = list; }

        internal int GetDimLen(int index)
        {
            if (dimList == null || index < 0 || index > dimList.Count - 1) { return 0; }
            else { return dimList[index]; }
        }

    }
}
