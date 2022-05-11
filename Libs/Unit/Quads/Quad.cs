using System.Collections.Generic;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.Interface;

namespace CLikeCompiler.Libs.Unit.Quads
{
    public class Quad
    {
        public static readonly List<string> QuadStdOp = new()
        {
            "add",
            "addi",
            "sub",
            "mul",
            "div",
            "itr",
            "mv"
        };

        public static readonly List<string> QuadJumpOp = new()
        {
            "j",
            "jal",
            "jr",
            "bnez",
            "beq",
            "bne",
            "blt",
            "bge"
        };

        public static readonly List<string> QuadAbbrOp = new()
        {
            "CalleeEntry",
            "CalleeSave",
            "CalleeRestore",
            "CalleeExit",
            "CallerEntry",
            "CallerSave",
            "CallerArgs",
            "CallerRestore",
            "CallerExit",
            "ArrayOffset",
            "ArrayLoad",
            "ArrayStore"
        };

        internal static bool IsLegalOp(string op)
        {
            return QuadStdOp.Contains(op) || QuadAbbrOp.Contains(op) || QuadJumpOp.Contains(op);
        }

        internal static bool IsJumpOp(string op)
        {
            return QuadJumpOp.Contains(op);
        }

        internal static bool IsAbbrOp(string op)
        {
            return QuadAbbrOp.Contains(op);
        }

        internal static bool IsArrayOp(string op)
        {
            return op.StartsWith("Array");
        }

        internal LabelRecord Label { get; set; }

        internal string Name { get; set; } = "";
        internal IRecord Lhs { get; set; }
        internal IRecord Rhs { get; set; }
        internal IRecord Dst { get; set; }

        internal string GetLhsName()
        {
            return Lhs == null ? "-" : Lhs.Name;
        }

        internal string GetRhsName()
        {
            return Rhs == null ? "-" : Rhs.Name;
        }

        internal string GetDstName()
        {
            return Dst == null ? "-" : Dst.Name;
        }

    }
}
