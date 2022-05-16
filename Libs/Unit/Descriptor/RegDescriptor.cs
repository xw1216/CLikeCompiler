using System.Collections.Generic;
using System.Text;
using CLikeCompiler.Libs.Unit.Reg;

namespace CLikeCompiler.Libs.Unit.Descriptor;

internal class RegDescriptor
{
    internal Regs Reg { get; }
    internal List<VarDescriptor> Vars { get; }

    internal RegDescriptor(Regs reg)
    {
        Reg = reg;
        Vars = new List<VarDescriptor>();
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Reg);
        if (Vars.Count <= 0) return builder.ToString();

        builder.Append(" with ");
        foreach (VarDescriptor descriptor in Vars)
        {
            builder.Append(descriptor.ToString());
            builder.Append(' ');
        }
        return builder.ToString();
    }
}