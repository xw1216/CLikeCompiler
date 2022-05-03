using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Quad
{
    public class Quad
    {
        public static readonly List<string> QuadOp = new();

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
            return Lhs == null ? "-" : Rhs.Name;
        }

        internal string GetDstName()
        {
            return Dst == null ? "-" : Dst.Name;
        }

    }
}
