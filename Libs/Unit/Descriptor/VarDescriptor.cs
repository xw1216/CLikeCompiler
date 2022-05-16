using CLikeCompiler.Libs.Record.DataRecord;

namespace CLikeCompiler.Libs.Unit.Descriptor
{
    internal class VarDescriptor
    {
        internal VarRecord Var { get; }
        internal AddrDescriptor Addr { get; }
        internal ActiveDescriptor Active { get; set; }
        internal bool IsTemp { get; }
        internal bool IsCon { get; }
        internal bool IsNeedSpace { get; set; }
        internal bool IsGlobal { get; set; }

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
            IsTemp = var.IsTemp();
            IsCon = var.IsCons();
            IsNeedSpace = !IsTemp;
            IsGlobal = var.IsGlobal;
        }

        public override string ToString()
        {
            return Var.Name;
        }
    }
}
