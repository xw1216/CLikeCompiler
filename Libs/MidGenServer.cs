using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class MidGenServer
    {
        private AnalyStack stack;

        internal MidGenServer(GramServer gram)
        {
            stack = gram.GetStack();
        }

        internal void DeriveHandler(PredictTableItem item, LexUnit unit)
        {
            DefaultDeriveNoProp(item);
        }

        private void DefaultDeriveNoProp(PredictTableItem item)
        {
            stack.Pop();
            List<Symbols> list  = item.prod.GetRhs().First();
            for(int i = list.Count - 1; i >= 0; i--)
            {
                stack.Push(list[i], new DynamicProperty());
            }
        }
    }
}
