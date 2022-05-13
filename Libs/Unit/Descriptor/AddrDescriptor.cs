namespace CLikeCompiler.Libs.Unit.Descriptor
{
    internal class AddrDescriptor
    {
        internal VarDescriptor Var { get; set; } = null;
        internal bool InMem { get; set; } = true;
        internal bool InReg { get; set; } = false;
        internal RegDescriptor RegAt { get; set; } = null;
    }
}
