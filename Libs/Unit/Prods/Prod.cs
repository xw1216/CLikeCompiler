using CLikeCompiler.Libs.Unit.Symbol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Prods
{
    internal class Prod
    {
        private NTerm lhs;
        private readonly List<List<Symbols>> rhs = new();

        internal void SetLhs(NTerm term)
        {
            lhs = term;
        }

        internal List<List<Symbols>> GetRhs()
        {
            return rhs;
        }

        internal NTerm GetLhs()
        {
            return lhs;
        }

        internal void NewSubProd()
        {
            rhs.Add(new List<Symbols>());
        }

        internal void AddSubProdUnit(Symbols sym)
        {
            if (sym == null) { return; }
            rhs.Last().Add(sym);
        }

        internal void AddSubProd(List<Symbols> sub)
        {
            rhs.Add(sub);
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append(lhs.GetName() + " -> ");
            for (int i = 0; i < rhs.Count; i++)
            {
                List<Symbols> list = rhs[i];
                if (list.Count == 0) { builder.Append("blank "); }
                for (int j = 0; j < list.Count; j++)
                {
                    builder.Append(list[j].GetName() + " ");
                }
                if (i < rhs.Count - 1)
                {
                    builder.Append("| ");
                }
            }
            return builder.ToString();
        }
    }
}
