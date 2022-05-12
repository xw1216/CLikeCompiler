using System.Collections.Generic;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Unit.Reg;

namespace CLikeCompiler.Libs.Unit.Target;

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