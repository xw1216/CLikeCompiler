using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Unit.Target
{
    internal class ActiveDescriptor
    {
        internal VarDescriptor Var { get; set; } = null;
        internal QuadDescriptor NextUseQuad { get; set; } = null;
        internal bool IsActive { get; set; } = false;

        internal ActiveDescriptor() {}

        internal ActiveDescriptor(VarDescriptor var)
        {
            Var = var;
        }

        internal ActiveDescriptor(ActiveDescriptor descriptor)
        {
            this.Var = descriptor.Var;
            this.NextUseQuad = descriptor.NextUseQuad;
            this.IsActive = descriptor.IsActive;
        }
    }
}
