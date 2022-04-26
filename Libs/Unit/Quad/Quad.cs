using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Quad
{
    internal class Quad
    {
        internal static readonly List<string> quadOp;

        internal LabelRecord Label { get; set; }

        internal string Name { get; set; }
        internal IRecord Lhs { get; set; }
        internal IRecord Rhs { get; set; }
        internal IRecord Dst { get; set; }
    }
}
