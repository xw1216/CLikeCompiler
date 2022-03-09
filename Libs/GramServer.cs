using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class GramServer
    {
        private List<Prod> prods;
        private List<Term> terms;
        private List<NTerm> nTerms;

        bool IsGramReady = false;

        internal void GetParserResult()
        {
            Compiler.parser.GetSymbolRefs(ref prods, ref terms, ref nTerms);
        }

        private void RemoveLeftRecur()
        {
            for(int i = 0; i < prods.Count; i++)
            {
                for(int j = 0; j < i; j++)
                {
                    IndirectRecurReplace(prods[i], prods[j]);
                }
                DirectRecurReplace(prods[i]);
            }
        }

        private void IndirectRecurReplace(Prod dst, Prod src)
        {
            NTerm srcLhs = src.GetLhs();

            List<List<Symbols>> list = dst.GetRhs();
            for (int i = 0; i < list.Count; i++)
            {
                List<Symbols> dstSub = list[i];
                // Ai -> AjY    Aj -> a1 | a2 | ... | ak
                if (dstSub.Count != 0 && dstSub.First() == srcLhs)
                {
                    // Store Y and remove Ai -> AjY
                    List<Symbols> latter;
                    if (dstSub.Count == 1) { latter = new(); }
                    else { latter = dstSub.GetRange(1, dstSub.Count - 1); }
                    dst.GetRhs().RemoveAt(i);
                    i--;

                    // Construct Ai -> a1Y | a2Y | ... | akY
                    foreach (List<Symbols> former in src.GetRhs())
                    {
                        List<Symbols> tmp = new();
                        tmp.Concat(former);
                        tmp.Concat(latter);
                        dst.GetRhs().Add(tmp);
                    }
                }
            }
        }

        private void DirectRecurReplace(Prod src)
        {
            // A -> Aa | b
            FindDirectRecurIndex(ref src, out List<int> recur, out List<int> normal);
            if (recur.Count <= 0) { return; }

            // Create A'
            NTerm newNTerm = new NTerm();
            newNTerm.SetName(src.GetLhs() + "\'");
            // Add A -> bA'
            foreach (int i in normal)
            {
                src.GetRhs()[i].Add(newNTerm);
            }

            // Create A' Prod
            Prod newProd = new Prod();
            newProd.SetLhs(newNTerm);
            // Add epsilon
            newProd.NewSubProd();
            // Remove A -> Aa , Add A' -> aA'
            foreach(int i in recur)
            {
                List<Symbols> tmp = src.GetRhs()[i];
                src.GetRhs().RemoveAt(i);
                tmp.Add(newNTerm);
                tmp.RemoveAt(0);
                newProd.AddSubProd(tmp);
            }
        }

        private void FindDirectRecurIndex(ref Prod src, out List<int> recur, out List<int> normal)
        {
            recur = new();
            normal = new();
            for (int i = 0; i < src.GetRhs().Count; i++)
            {
                if (src.GetRhs()[i].Count != 0 &&
                    (src.GetRhs()[i].First() == src.GetLhs()))
                { recur.Add(i); }
                else { normal.Add(i); }
            }
        }

        private ref List<Term> CalcuFirst(Symbols sym)
        {
            List<Term> first = new();
            switch(sym.GetForm())
            {
                case Symbols.Form.BLANK:
                case Symbols.Form.ACTION:

                    break;
                case Symbols.Form.TERM:
                case Symbols.Form.NONTERM:
            }
            if(sym.GetForm() == Symbols.Form.TERM)
            {
                first.Add((Term)sym);
            }
        }

        private ref List<Term> CalcuFirst(List<Symbols> list)
        {

        }

        private ref List<Term> CalcuFollow(Symbols sym)
        {

        }

        private void PrefixFactoring()
        {

        }

    }
}
