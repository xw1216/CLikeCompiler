using System.Collections.Generic;
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

}