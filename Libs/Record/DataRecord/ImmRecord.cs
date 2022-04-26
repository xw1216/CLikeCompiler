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

        // 初始值
        internal int Initial { get; set; } = 0;
        // 若为栈帧值 则记录动态偏移量
        internal int Offset { get; set; } = 0;
        // 是否相对于 Sp 寻址
        internal bool IsSpRel { get; set; } = false;

        internal int Value { get { return Initial + Offset; } }
    }
}
