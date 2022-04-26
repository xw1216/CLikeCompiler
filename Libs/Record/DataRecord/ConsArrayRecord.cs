using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.DataRecord
{
    internal class ConsArrayRecord : ArrayRecord
    {
        internal readonly List<object> list = new();
        internal string OriginCont { get; set; }

        internal override bool IsCons() { return true; }
    }
}
