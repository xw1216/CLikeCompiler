using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class Prod
    {
        private NTerm lhs;
        private List<List<Symbols>> rhs = new();

        internal void SetLhs(NTerm term)
        {
            lhs = term;
        }

        internal List<List<Symbols>> GetRhs()
        {
            return rhs;
        }

        internal ref NTerm GetLhs()
        {
            return ref lhs;
        }

        internal void NewSubProd()
        {
            rhs.Add(new List<Symbols>());
        }

        internal void AddSubProdUnit(Symbols sym)
        {
            if(sym == null) { return; }
            rhs.Last().Add(sym);
        }

        internal void AddSubProd(List<Symbols> sub)
        {
            rhs.Add(sub);
        }
        
    }
}
