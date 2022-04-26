using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Unit.Reg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.Interface
{
    public interface IDataRecord : IRecord
    {
        VarType Type { get; set; }
        int Width { get; }

        RecordPos Pos { get; set; } // 实际存储的位置
        int Offset { get; set; }    // 栈内相对于 fp 的限制
        Regs Reg { get; set; }   // 如果存储在寄存器中 则具体位置

        static int GetWidth(VarType type)
        {
            return type switch
            {
                VarType.INT => 4,
                VarType.LONG => 8,
                VarType.BOOL => 1,
                VarType.CHAR => 1,
                VarType.VOID => 0,
                _ => 0,
            };
        }
    }
}
