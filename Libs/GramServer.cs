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
            RemoveUnusedProd();
        }

        private void RemoveUnusedProd()
        {
            List<NTerm> findSet = FindUnreachNTerm();

            List<NTerm> unused = new List<NTerm>(nTerms.Except(findSet));
            foreach(NTerm nTerm in unused)
            {
                DecSymbolRef(nTerm);
                Prod prod = prods[nTerm.prodIndex];
                foreach(List<Symbols> list in prod.GetRhs())
                {
                    DecSeqRef(list);
                }
            }
            foreach(Prod prod in prods)
            {
                if(unused.Contains(prod.GetLhs()))
                {
                    prods.Remove(prod);
                }
            }
            ReNoteNTermIndex();
        }

        private void ReNoteNTermIndex()
        {
            for(int i = 0; i < prods.Count; i++)
            {
                prods[i].GetLhs().prodIndex = i;
            }
        }

        private List<NTerm> FindUnreachNTerm()
        {
            List<NTerm> findSet = new();
            Queue<NTerm> bfsQueue = new();
            bfsQueue.Enqueue(Compiler.parser.GetStartNTerm());

            while (bfsQueue.Count > 0)
            {
                NTerm nTerm = bfsQueue.Dequeue();
                findSet.Add(nTerm);
                Prod prod = prods[nTerm.prodIndex];
                foreach (List<Symbols> list in prod.GetRhs())
                {
                    foreach (Symbols sym in list)
                    {
                        if (sym.IsNTerm() && findSet.Contains(sym))
                        {
                            bfsQueue.Enqueue((NTerm)sym);
                        }
                    }
                }
            }
            return findSet;
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
                    else 
                    {
                        latter = dstSub.GetRange(1, dstSub.Count - 1); 
                    }
                    DecSeqRef(dst.GetRhs()[i]);
                    dst.GetRhs().RemoveAt(i);
                    i--;

                    // Construct Ai -> a1Y | a2Y | ... | akY
                    foreach (List<Symbols> former in src.GetRhs())
                    {
                        List<Symbols> tmp = new();
                        tmp.Concat(former);
                        tmp.Concat(latter);
                        IncSeqRef(tmp);
                        dst.GetRhs().Add(tmp);
                    }
                }
            }
        }

        private void IncSymbolRef(Symbols sym)
        {
            if(sym == null) { return; }
            sym.IncRef();
        }

        private void DecSymbolRef(Symbols sym)
        {
            if (sym == null) { return; }
            sym.DecRef();
            if(sym.IsTerm() && !(sym.BeRef())) { terms.Remove((Term)sym); }
            else if(sym.IsNTerm() && !(sym.BeRef())) { nTerms.Remove((NTerm)sym); }
        }

        private void IncSeqRef(List<Symbols> list)
        {
            foreach(Symbols sym in list)
            {
                IncSymbolRef(sym);
            }
        }

        private void DecSeqRef(List<Symbols> list)
        {
            foreach(Symbols sym in list)
            {
                DecSymbolRef(sym);
            }
        }

        private void AddFirstUnique(ref List<Term> first, Symbols sym)
        {
            if (sym.IsTerm())
            {
                first.Add((Term)sym);
                first.Distinct();
            }
        }

        private void AddFirstUnique(ref List<Term> first, ref List<Term> subFirst)
        {
            first.Concat(subFirst);
            first.Distinct();
        }

        private void DirectRecurReplace(Prod src)
        {
            // A -> Aa | b
            FindDirectRecurIndex(ref src, out List<int> recur, out List<int> normal);
            if (recur.Count <= 0) { return; }

            // Create A'
            NTerm newNTerm = new NTerm();
            newNTerm.SetName(src.GetLhs() + "\'");
            newNTerm.prodIndex = prods.Count;
            nTerms.Add(newNTerm);
            // Add A -> bA'
            foreach (int i in normal)
            {
                IncSymbolRef(newNTerm);
                src.GetRhs()[i].Add(newNTerm);
            }

            // Create A' Prod
            Prod newProd = new Prod();
            newProd.SetLhs(newNTerm);
            Compiler.parser.AddNewNTerm(newNTerm);
            // Add epsilon
            newProd.NewSubProd();
            // Remove A -> Aa , Add A' -> aA'
            for (int i = 0; i < src.GetRhs().Count; i++)
            {
                if(src.GetRhs()[i].Count != 0 &&
                    (src.GetRhs()[i].First() == src.GetLhs()))
                {
                    List<Symbols> tmp = src.GetRhs()[i];
                    src.GetRhs().RemoveAt(i);
                    tmp.Add(newNTerm);
                    tmp.RemoveAt(0);
                    newProd.AddSubProd(tmp);
                }
            }
            prods.Add(newProd);
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

        private List<Term> CalcuFirst(Symbols sym)
        {
            // If sym is a termimal
            if(sym.IsTerm())
            {
                List<Term> first = new();
                AddFirstUnique(ref first, sym);
                return first;
            } 
            // If sym is a nontermimal
            else if(sym.IsNTerm())
            {
                NTerm nTerm = (NTerm)sym;
                if (nTerm.first.Count != 0) { return nTerm.first; }
                // Find the corresponding Production
                Prod prod = prods[nTerm.prodIndex];
                // Check every sub production
                foreach (List<Symbols> sub in prod.GetRhs()) 
                { 
                    // sym -> epsilon then add epsilon to first
                    if(sub.Count == 0) 
                    { 
                        AddFirstUnique(ref nTerm.first, Term.blank); 
                    } 
                    else
                    {
                        // sym -> aA... add terminal a to first
                        if(sub.First().IsTerm()) 
                        { 
                            AddFirstUnique(ref nTerm.first ,sub.First());
                        } 
                        // sym -> AX... add First of sub production to First of sym
                        else if(sub.First().IsNTerm())
                        {
                            List<Term> subFirst = CalcuFirst(sub, nTerm);
                            AddFirstUnique(ref nTerm.first, ref subFirst);
                        }
                    }
                }
                return nTerm.first;
            } 
            // Unkonwn sym type , throw exception
            else
            {
                Compiler.GetInstance().ReportBackInfo(this,
                                        new CompilerReportArgs(LogItem.MsgType.ERROR,
                                        "无法计算 First 集，无效的语法符号类型"));
                throw new InvalidOperationException();
            }
        }

        private List<Term> CalcuFirst(List<Symbols> list, NTerm lhs)
        {
            List<Term> first = new();
            if(list.Count == 0) 
            { 
                AddFirstUnique(ref first , Term.blank); return first; 
            }
            CheckFirstRecurExcep(list.First(), lhs, true);

            foreach(Symbols sym in list)
            {
                CheckFirstRecurExcep(sym, lhs, false);
                // Check First of single symbol 
                List<Term> subFirst = CalcuFirst(sym);
                // If sub First do not contains epsilon , break and return
                if(!(subFirst.Contains(Term.blank)))
                {
                    first = subFirst;
                    break;
                } 
                // Else continue to calcu and add next symbol
                else
                {
                    // Caution: the the First of last symbol should not remove epsilon
                    if(sym != list.Last())
                    {
                        subFirst.Remove(Term.blank);
                    }
                    AddFirstUnique(ref first, ref subFirst);
                    continue;
                }
            }
            return first;
        }

        private void CheckFirstRecurExcep(Symbols sym, NTerm lhs, bool IsDierct)
        {
            string part = (IsDierct) ? "直接" : "间接";
            if (sym == lhs)
            {
                Compiler.GetInstance().ReportBackInfo(this,
                new CompilerReportArgs(LogItem.MsgType.ERROR, 
                                                        $"{part}左递归，无法计算 First 集：" + lhs.GetName()));
                throw new InvalidOperationException();
            }
        }

        private List<Term> CalcuFollow(NTerm sym)
        {
            if(sym.follow.Count > 0) { return sym.follow; }
            if(sym == Compiler.parser.GetStartNTerm())
            {
                sym.follow.Add(Term.end);
            }
            foreach(Prod prod in prods)
            {
                foreach(List<Symbols> list in prod.GetRhs())
                {
                    for(int i = 0; i < list.Count; i++)
                    {
                        if(list[i] == sym)
                        {
                            List<Term> seqFirst = CalcuFirst(GetSubProdRemain(list, i), prod.GetLhs());
                            // First(b) contains epsilon add First(b) - {epsilon} and Follow(lhs) to Follow(sym)
                            if (seqFirst.Contains(Term.blank))
                            {
                                seqFirst.Remove(Term.blank);
                                sym.follow.Union(seqFirst);
                                // Prevent right recurrsion
                                if(sym != prod.GetLhs())
                                {
                                    List<Term> lhsFirst = CalcuFollow(prod.GetLhs());
                                    sym.follow.Union(lhsFirst);
                                }
                            }
                            // First(b) do not contains epsilon, simply add First(b)
                            else
                            {
                                sym.follow.Union(seqFirst);
                            }
                        }
                    }
                }
            }
            return sym.follow;
        }

        private List<Symbols> GetSubProdRemain(List<Symbols> list, int index)
        {
            List<Symbols> remains;
            if(index == list.Count - 1)
            {
                remains = new();
                return remains;
            } else
            {
                remains = list.GetRange(index + 1, list.Count - index - 1);
                return remains;
            }
        }

        private void PrefixFactoring()
        {
            foreach(Prod prod in prods)
            {
                while(HasSharedPrefix(prod, out List<int> subIndexList))
                {
                    int shareLen = 0;
                    if(!SharedPrefixLength(prod, subIndexList, ref shareLen) || shareLen <= 0) { continue; } 
                    else
                    {

                    }
                }
            }
        }

        private bool HasSharedPrefix(Prod prod, out List<int> subIndexList)
        {
            subIndexList = new();
            if (prod.GetRhs().Count <= 1) {
                return false; 
            }
            List<List<Symbols>> rhs = prod.GetRhs();
            for (int i = 0; i < rhs.Count; i++)
            {
                subIndexList.Add(i);
               for( int j = i + 1; j < rhs.Count; j++) {
                    List < Symbols > intersect = new (CalcuFirst(rhs[i], prod.GetLhs()).Intersect(rhs[j]));
                    if (intersect.Count > 1)
                    {
                        subIndexList.Add(j);
                    }
                }
               if(subIndexList.Count > 1) 
                { 
                    subIndexList.Sort();
                    return true; 
                }
               else { subIndexList.Clear(); }
            }
            subIndexList.Clear();
            return false;
        } 

        private bool SharedPrefixLength(Prod prod, List<int> shared, ref int len)
        {
            List<List<Symbols>> rhs = prod.GetRhs();
            int minLen = MinLenOfSharedSubProd(prod, shared);
            // Shared prefix is epsilon
            if(minLen <= 0)
            {
                ReplacePrefix(prod, shared, minLen);
                return false;
            }

            for(len = 0; len < minLen ;len++)
            {
                Symbols sym = rhs[shared[0]][len];
                // Check if all subProd same at index 'len'
                for (int i = 0; i < shared.Count; i++)
                {
                    if(rhs[shared[i]][len] != sym) {
                        // Check is there any indirect equal
                        if (IsSharedPrefixAtIndex(prod, shared, len))
                        {
                            // Replace nontermimals at index for further equal
                            ReplacePrefix(prod, shared, len);
                            return false;
                        }
                        // End calcu and shared prefix length is 'len'
                        else { return true; }
                    }
                }
                len++;
            }
            Compiler.GetInstance().ReportBackInfo(this, new CompilerReportArgs(LogItem.MsgType.ERROR,
                                              "公因子长度计算错误：" + prod.GetLhs().GetName()));
            throw new Exception();
        }

        private int MinLenOfSharedSubProd(Prod prod, List<int> shared)
        {
            List<List<Symbols>> rhs = prod.GetRhs();
            int minLen = int.MaxValue;
            foreach (int i in shared)
            {
                if (rhs.Count < minLen) { minLen = rhs.Count; }
            }
            return minLen;
        }

        private bool IsSharedPrefixAtIndex(Prod prod, List<int> share, int index)
        {
            List<Term> intersect = null;
            List<List<Symbols>> rhs = prod.GetRhs();
            foreach (int i in share)
            {
                if(rhs[i][index] == prod.GetLhs()) { return false; }
                List<Term> subFirst = CalcuFirst(rhs[i].GetRange(index, rhs[i].Count - index), prod.GetLhs());
                if(intersect == null) { intersect = subFirst; }
                else { intersect.Intersect(subFirst); }
            }
            if (intersect == null || intersect.Count <= 0) { return false; }
            else { return true; }
        }

        private void ReplacePrefix(Prod prod, List<int> shared, int index)
        {
            List<List<Symbols>> rhs = prod.GetRhs();
            foreach (int i in shared)
            {
                if(rhs[i].Count > index && rhs[i][index].IsNTerm())
                {
                    NTerm replaceSym = (NTerm)rhs[i][index];
                    if(replaceSym == prod.GetLhs()) { continue; }
                    List<List<Symbols>> replaceRhs = prods[replaceSym.prodIndex].GetRhs();
                    List<Symbols> remain = rhs[i].GetRange(index, rhs[i].Count - index);
                    foreach(List<Symbols> replaceSeq in replaceRhs)
                    {
                        List<Symbols> list = replaceSeq.Concat(remain).ToList();
                        rhs.Add(list);
                    }
                }
            }
            for(int i = shared.Count - 1; i >= 0; i--)
            {
                rhs.RemoveAt(i);
            }
            RemoveDupSubProd(prod);
        }

        private void RemoveDupSubProd(Prod prod)
        {
            for(int i = 0; i < prod.GetRhs().Count;i++)
            {
                for(int j = i +1; j < prod.GetRhs().Count; j++)
                {
                    if(prod.GetRhs()[i].SequenceEqual(prod.GetRhs()[j]))
                    {
                        prod.GetRhs().RemoveAt(j);
                        j--;
                    }
                }
            }
        }



    }
}
