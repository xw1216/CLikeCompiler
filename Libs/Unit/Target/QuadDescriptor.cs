using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Unit.Target
{
    internal class QuadDescriptor
    {
        internal Quad ToQuad { get; set; }
        internal ActiveDescriptor LhsDescriptor { get; set; }
        internal ActiveDescriptor RhsDescriptor { get; set; }
        internal ActiveDescriptor DstDescriptor { get; set; }

        internal QuadDescriptor() {}

        internal QuadDescriptor(Quad quad)
        {
            ToQuad = quad;
        }

    }
}
