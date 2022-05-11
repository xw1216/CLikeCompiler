using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Record.DataRecord;

namespace CLikeCompiler.Libs.Unit.Target
{
    internal class AddrDescriptor
    {
        internal VarDescriptor Var { get; set; } = null;
        internal bool InMem { get; set; } = true;
        internal bool InReg { get; set; } = false;
    }
}
