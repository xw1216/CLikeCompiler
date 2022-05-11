using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Record.DataRecord;

namespace CLikeCompiler.Libs.Unit.Target
{
    internal class VarDescriptor
    {
        internal VarRecord Var { get; }
        internal AddrDescriptor Addr { get; }
        internal ActiveDescriptor Active { get; }
        internal bool IsCon { get; }

        internal VarDescriptor(VarRecord var)
        {
            Var = var;
            Addr = new AddrDescriptor
            {
                Var = this,
                InMem = true,
                InReg = false,
            };
            Active = new ActiveDescriptor 
            {
                Var = this,
                IsActive = false,
                NextUseQuad = null,
            };
            IsCon = var.IsCons();
        }

    }
}
