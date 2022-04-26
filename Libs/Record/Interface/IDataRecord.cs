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

        RecordPos Pos { get; set; }
        int Offset { get; set; }
        Regs Reg { get; set; }

        static int GetWidth(VarType type)
        {
            switch (type)
            {
                case VarType.INT: return 4;
                case VarType.LONG: return 8;
                case VarType.BOOL: return 1;
                case VarType.CHAR: return 1;
                default: return 0;
            }
        }
    }
}
