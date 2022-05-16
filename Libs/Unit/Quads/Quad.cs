using System.Collections.Generic;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.Interface;

namespace CLikeCompiler.Libs.Unit.Quads
{
    public class Quad
    {
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

        public override string ToString()
        {
            return Name + " \t" + GetLhsName() + ", " + GetRhsName() + ", " + GetDstName();
        }

        public static readonly List<string> QuadStdOp = new()
        {
            "add",
            "addi",
            "sub",
            "mul",
            "div",
            "lui",
            "mv",
            "itr",
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
            "bge",
        };

        public static readonly List<string> QuadCallOp = new()
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
        };

        public static readonly List<string> QuadMemOp = new()
        {
            "Load",
            "Store",
            "LoadAddr",
            "ArrayOffset",
            "ld",
            "st",
        };

        internal static bool IsLegalOp(string op)
        {
            return QuadStdOp.Contains(op) 
                   || QuadCallOp.Contains(op) 
                   || QuadJumpOp.Contains(op) 
                   || QuadMemOp.Contains(op);
        }

        internal static bool IsJumpOp(string op)
        {
            return QuadJumpOp.Contains(op);
        }

        internal static bool IsCallOp(string op)
        {
            return QuadCallOp.Contains(op);
        }

        internal static bool IsCopyOp(string op)
        {
            return op is "mv" or "itr";
        }

        internal static bool IsMemOp(string op)
        {
            return QuadMemOp.Contains(op);
        }

    }
}
