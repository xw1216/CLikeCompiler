namespace CLikeCompiler.Libs.Unit.Descriptor
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

        public override string ToString()
        {
            return IsActive ? "(Y)" : "(^)";
        }
    }
}
