using System.Text;
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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(ToQuad.Name);
            builder.Append(' ');
            builder.Append(ToQuad.GetLhsName() + LhsDescriptor?.ToString() + ", ");
            builder.Append(ToQuad.GetRhsName() + RhsDescriptor?.ToString() + ", ");
            builder.Append(ToQuad.GetDstName() + DstDescriptor?.ToString());
            return builder.ToString();
        }
    }
}
