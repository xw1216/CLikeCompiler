using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Unit.Descriptor
{
    internal class QuadDescriptor
    {
        internal Quad ToQuad { get; set; }
        internal ActiveDescriptor LhsDescriptor { get; set; }
        internal ActiveDescriptor RhsDescriptor { get; set; }
        internal ActiveDescriptor DstDescriptor { get; set; }

        internal int VarNum { get; set; } = 0;

        internal QuadDescriptor() {}

        internal QuadDescriptor(Quad quad)
        {
            ToQuad = quad;
        }

    }
}
