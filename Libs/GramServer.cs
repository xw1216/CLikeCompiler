using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{

    internal class PredictTableItem
    {
        internal Prod prod = null;
        internal Form form = Form.BLANK;
        internal int[] pos = new int[2] {0, 0};

        internal enum Form
        {
            BLANK,
            FILL,
            SYNCH
        }

        internal bool IsBlank()
        {
            return form == Form.BLANK;
        }
    }

    internal class GramServer
    {
        private List<Prod> prods;
        private List<Term> terms;
        private List<NTerm> nTerms;

        private List<string> termsName;

        private PredictTableItem[,] table;
        private AnalyStack stack = new();

        private LexUnit lastInput;

        internal bool IsGramReady { get; private set; } = false;

        private readonly string endStr = "end";

        internal void ResetGramServer()
        {
            prods = null;
            terms = null;
            nTerms = null;
            table = null;
            IsGramReady = false;
            lastInput = null;
            stack.ResetStack();
        }

        internal ref AnalyStack GetStack()
        {
            return ref stack;
        }

        internal void ResetAnalyStack()
        {
            stack.ResetStack();
        }
        internal LexUnit GetLastInput()
        {
            return lastInput;
        }

        internal void BuildGram()
        {
            if(!IsGramReady)
            {
                GetParserResult();
                RemoveLeftRecur();
                PrefixFactoring();
                CalcuAllFirstAndFollow();
                PrintGrammar();
                PrintFirstAndFollow();
                BuildPredictTable();
                IsGramReady = true;
            }
        }

        internal void PrintGrammar()
        {
            foreach(Prod prod in prods)
            {
                System.Diagnostics.Debug.WriteLine(prod.ToString());
            }
        }

        internal void PrintFirstAndFollow()
        {
            foreach(NTerm nTerm in nTerms)
            {
                StringBuilder builder = new();
                List<Term> first = CalcuFirst(nTerm);
                SetStrBuilder(first, builder);
                System.Diagnostics.Debug.WriteLine(nTerm.GetName() + " : First Set");
                System.Diagnostics.Debug.WriteLine(builder.ToString());

                List<Term> follow = CalcuFollow(nTerm);
                SetStrBuilder(follow, builder);
                System.Diagnostics.Debug.WriteLine(nTerm.GetName() + " : Last Set");
                System.Diagnostics.Debug.WriteLine(builder.ToString());
            }
        }

        internal void CalcuAllFirstAndFollow()
        {
            foreach (NTerm nTerm in nTerms)
            {
                CalcuFirst(nTerm);
                CalcuFollow(nTerm);
            }
        }

        private void SetStrBuilder(List<Term> set, StringBuilder builder)
        {
            builder.Clear();
            builder.Append('\t');
            for (int i = 0; i < set.Count; i++)
            {
                builder.Append(set[i].GetName());
                if (i < set.Count - 1) { builder.Append(' '); }
            }
        }

        private void GetParserResult()
        {
            if(Compiler.parser.IsBaseGramReady)
            {
                Compiler.parser.GetSymbolRefs(ref prods, ref terms, ref nTerms);
            }
            else
            {
                Compiler.GetInstance().ReportBackInfo(this,
                        new CompilerReportArgs(LogItem.MsgType.ERROR,
                        "未建立基础文法"));
                throw new InvalidOperationException();
            }
        }

        private void RecordTermsName()
        {
            termsName = new();
            foreach(Term term in terms)
            {
                termsName.Add(term.GetName());
            }
        }

        private void BuildPredictTable()
        {
            RecordTermsName();
            InitTableItem();
            for (int i = 0; i < prods.Count; i++)
            {
                Prod prod = prods[i];
                List<List<Symbols>> rhs = prod.GetRhs();
                for (int j = 0; j < rhs.Count; j++)
                {
                    List<Symbols> sub = rhs[j];
                    List<Term> first = CalcuFirst(sub, prod.GetLhs());
                    if(first.Contains(Term.blank))
                    {
                        List<Term> follow = CalcuFollow(prod.GetLhs());
                        AddSetUnique(ref first, ref follow);
                    }
                    first.Remove(Term.blank);
                    InsertTableItem(prod.GetLhs(), sub, first, i, j);
                }
                InsertSynchItem(prod.GetLhs());
            }
        }

        private void InsertSynchItem(NTerm lhs)
        {
            int x = nTerms.IndexOf(lhs);
            List<Term> follow = CalcuFollow(lhs);
            foreach(Term term in follow)
            {
                int y = terms.IndexOf(term);
                if(table[x, y].IsBlank())
                {
                    table[x, y].form = PredictTableItem.Form.SYNCH;
                }
            }
        }

        private void InsertTableItem(NTerm lhs, List<Symbols> rhs, List<Term> list, int prodIndex, int subIndex)
        {
            Prod prod = new();
            prod.SetLhs(lhs);
            prod.AddSubProd(rhs);
            int x = nTerms.IndexOf(lhs);

            for (int i = 0; i < list.Count; i++)
            {
                Term term = list[i];
                int y = terms.IndexOf(term);
                if (x < 0 || x >= nTerms.Count || y < 0 || y > terms.Count)
                {
                    continue;
                }

                if (table[x, y].IsBlank())
                {
                        table[x, y].form = PredictTableItem.Form.FILL;
                        table[x, y].prod = prod;
                        table[x, y].pos[0] = prodIndex;
                        table[x, y].pos[1] = subIndex;
                }
                else
                {
                    Compiler.GetInstance().ReportBackInfo(this,
                                        new CompilerReportArgs(LogItem.MsgType.ERROR,
                                        $"预测表入口冲突：非终结符 {lhs.GetName()} , 终结符 {term.GetName()}"));
                    throw new InvalidOperationException();
                }
            }
        }

        private void InitTableItem()
        {
            table = new PredictTableItem[nTerms.Count, terms.Count];
            for(int i = 0; i < table.GetLength(0); i++)
            {
                for(int j = 0; j < table.GetLength(1); j++)
                {
                    table[i, j] = new PredictTableItem();
                    table[i, j].form = PredictTableItem.Form.BLANK;
                }
            }
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

            List<NTerm> unused = new(nTerms.Except(findSet));
            foreach(NTerm nTerm in unused)
            {
                DecSymbolRef(nTerm);
                Prod prod = prods[nTerm.prodIndex];
                foreach(List<Symbols> list in prod.GetRhs())
                {
                    DecSeqRef(list);
                }
            }
            for (int i = 0; i < prods.Count; i++)
            {
                Prod prod = prods[i];
                if (unused.Contains(prod.GetLhs()))
                {
                    prods.Remove(prod);
                    i--;
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
                        if (sym.IsNTerm() && !(findSet.Contains(sym)))
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
                        tmp = tmp.Concat(former).ToList();
                        tmp = tmp.Concat(latter).ToList();
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

        private void AddSetUnique(ref List<Term> set, Symbols sym)
        {
            if (sym.IsTerm() || sym.IsBlank())
            {
                set.Add((Term)sym);
                set.Distinct();
            }
        }

        private void AddSetUnique(ref List<Term> set, ref List<Term> sub)
        {
            set = set.Union(sub).ToList();
        }

        private NTerm CreateRecurNTerm(string name)
        {
            NTerm newNTerm = new();
            newNTerm.SetName(name);
            newNTerm.prodIndex = prods.Count;
            nTerms.Add(newNTerm);
            IncSymbolRef(newNTerm);
            return newNTerm;
        }

        private void DirectRecurReplace(Prod src)
        {
            // A -> Aa | b
            FindDirectRecurIndex(ref src, out List<int> recur, out List<int> normal);
            if (recur.Count <= 0) { return; }
            
            // Create A'
            NTerm newNTerm = CreateRecurNTerm(src.GetLhs().GetName() + "\'");
            // Add A -> bA'
            foreach (int i in normal)
            {
                IncSymbolRef(newNTerm);
                src.GetRhs()[i].Add(newNTerm);
            }

            // Create A' Prod
            Prod newProd = new();
            newProd.SetLhs(newNTerm);
            
            // Add epsilon
            newProd.NewSubProd();
            // Remove A -> Aa , Add A' -> aA'
            for (int i = 0; i < src.GetRhs().Count; i++)
            {
                if(src.GetRhs()[i].Count != 0 &&
                    (src.GetRhs()[i].First() == src.GetLhs()))
                {
                    List<Symbols> tmp = new(src.GetRhs()[i]);
                    src.GetRhs().RemoveAt(i);
                    i--;
                    IncSymbolRef(newNTerm);
                    tmp.Add(newNTerm);
                    DecSymbolRef(tmp[0]);
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
                AddSetUnique(ref first, sym);
                return first;
            } 
            // If sym is a nontermimal
            else if(sym.IsNTerm())
            {
                NTerm nTerm = (NTerm)sym;
                if (nTerm.first.Count > 0) { return new(nTerm.first); }
                // Find the corresponding Production
                Prod prod = prods[nTerm.prodIndex];
                // Check every sub production
                foreach (List<Symbols> sub in prod.GetRhs()) 
                { 
                    // sym -> epsilon then add epsilon to first
                    if(sub.Count == 0) 
                    { 
                        AddSetUnique(ref nTerm.first, Term.blank); 
                    } 
                    else
                    {
                        // sym -> aA... add terminal a to first
                        if(sub.First().IsTerm()) 
                        { 
                            AddSetUnique(ref nTerm.first ,sub.First());
                        } 
                        // sym -> AX... add First of sub production to First of sym
                        else if(sub.First().IsNTerm())
                        {
                            List<Term> subFirst = CalcuFirst(sub, nTerm);
                            AddSetUnique(ref nTerm.first, ref subFirst);
                        }
                    }
                }
                return new(nTerm.first);
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
                AddSetUnique(ref first , Term.blank); 
                return first; 
            }

            foreach(Symbols sym in list)
            {
                // Check First of single symbol 
                // No left recurrsion so no infinity loop
                List<Term> subFirst = CalcuFirst(sym);
                // If sub First do not contains epsilon , break and return
                if(!(subFirst.Contains(Term.blank)))
                {
                    AddSetUnique(ref first, ref subFirst);
                    break;
                } 
                // Else continue to calcu and add next symbol
                else
                {
                    // Caution: the the First of last symbol should not remove epsilon
                    if(sym != list.Last()) { subFirst.Remove(Term.blank); }
                    AddSetUnique(ref first, ref subFirst);
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
            if(sym.follow.Count > 0) { return new(sym.follow); }
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
                                AddSetUnique(ref sym.follow , ref seqFirst);
                                // Prevent right recurrsion
                                if(sym != prod.GetLhs())
                                {
                                    List<Term> lhsFirst = CalcuFollow(prod.GetLhs());
                                    AddSetUnique(ref sym.follow, ref lhsFirst);
                                }
                            }
                            // First(b) do not contains epsilon, simply add First(b)
                            else
                            {
                                AddSetUnique(ref sym.follow, ref seqFirst);
                            }
                        }
                    }
                }
            }
            return new(sym.follow);
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
            for (int i = 0; i < prods.Count; i++)
            {
                Prod prod = prods[i];
                while (HasSharedPrefix(prod, out List<int> shared))
                {
                    int shareLen = 0;
                    if(!SharedPrefixLength(prod, shared, ref shareLen) || shareLen <= 0) { continue; } 
                    else
                    {
                        FactoringAndNewProd(prod, shared, shareLen);
                    }
                }
            }
            RemoveUnusedProd();
        }

        private void FactoringAndNewProd(Prod prod, List<int> shared, int len)
        {
            if(shared.Count == 0) { return; }
            NTerm newNTerm = new();
            newNTerm.SetName(prod.GetLhs().GetName() + "~");
            newNTerm.prodIndex = prods.Count;
            nTerms.Add(newNTerm);

            Prod newProd = new Prod();
            newProd.SetLhs(newNTerm);
            IncSymbolRef(newNTerm);
            prods.Add(newProd);

            List<List<Symbols>> rhs = prod.GetRhs();

            // Create new prod and add every remain sequence to new prod
            foreach (int i in shared)
            {
                List<Symbols> remain = rhs[i].GetRange(len, rhs[i].Count - len);
                newProd.AddSubProd(remain);
            }

            List<Symbols> former = rhs[shared[0]].GetRange(0, len);
            former.Add(newNTerm);
            for(int i = shared.Count - 1; i >= 0; i--)
            {
                rhs.RemoveAt(shared[i]);
            }
            rhs.Add(former);
        }

        private bool HasSharedPrefix(Prod prod, out List<int> shared)
        {
            shared = new();
            // Has only one or no subprod , no shared prefix
            if (prod.GetRhs().Count <= 1) {
                return false; 
            }
            List<List<Symbols>> rhs = prod.GetRhs();
            for (int i = 0; i < rhs.Count; i++)
            {
                // Select one as baseline
                shared.Add(i);
               for( int j = i + 1; j < rhs.Count; j++) {
                    // First of Sequence after intersect is no empty set indicates shared prefix 
                    List < Symbols > intersect = new (
                        CalcuFirst(rhs[i], prod.GetLhs()).Intersect(CalcuFirst(rhs[j], prod.GetLhs())));
                    if (intersect.Count >= 1)
                    {
                        shared.Add(j);
                    }
                }
               // Sort the index of shared prefix
               if(shared.Count > 1) 
                { 
                    // Send the nearest shared prefix out and wait for next call
                    shared.Sort();
                    return true; 
                }
               else { shared.Clear(); }
            }
            shared.Clear();
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
                for (int i = 1; i < shared.Count; i++)
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
                if (rhs[i].Count < minLen) { minLen = rhs[i].Count; }
            }
            return minLen;
        }

        private bool IsSharedPrefixAtIndex(Prod prod, List<int> shared, int index)
        {
            List<Term> intersect = null;
            List<List<Symbols>> rhs = prod.GetRhs();
            foreach (int i in shared)
            {
                // Prevent replace recurssion
                if(rhs[i][index] == prod.GetLhs()) { return false; }
                List<Term> subFirst = CalcuFirst(rhs[i].GetRange(index, rhs[i].Count - index), prod.GetLhs());
                if(intersect == null) { intersect = subFirst; }
                else { intersect = intersect.Intersect(subFirst).ToList(); }
            }
            if (intersect == null || intersect.Count <= 0) { return false; }
            else { return true; }
        }

        private void ReplacePrefix(Prod prod, List<int> shared, int index)
        {
            List<List<Symbols>> rhs = prod.GetRhs();
            List<int> changed = new();
            foreach (int i in shared)
            {
                if(rhs[i].Count > index && rhs[i][index].IsNTerm())
                {
                    changed.Add(i);
                    NTerm replaceSym = (NTerm)rhs[i][index];

                    // Prevent replace recurssion
                    if (replaceSym == prod.GetLhs()) { continue; }
                    List<List<Symbols>> replaceRhs = prods[replaceSym.prodIndex].GetRhs();
                    List<Symbols> remain = rhs[i].GetRange(index + 1, rhs[i].Count - index - 1);

                    // Concat two seq
                    for (int j = 0; j < replaceRhs.Count; j++)
                    {
                        List<Symbols> replaceSeq = replaceRhs[j];
                        List<Symbols> list = replaceSeq.Concat(remain).ToList();
                        rhs.Add(list);
                    }
                }
            }
            for(int i = changed.Count - 1; i >= 0; i--)
            {
                rhs.RemoveAt(changed[i]);
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

        private void InitAnalyStack()
        {
            stack.Push(Term.end, new DynamicProperty());
            stack.Push(Compiler.parser.GetStartNTerm(), new DynamicProperty());
        }

        private void RestoreLastInput(LexUnit input)
        {
            lastInput = input;
        }

        private bool GetLexInput(out LexUnit input)
        {
            return Compiler.lex.GetUnit(out input);
        }

        private bool IsTopInputMatch(Symbols top, LexUnit input) 
        {
            if(top.IsTerm())
            {
                return top.GetName() == input.name;
            }
            return false;
        }

        private int IndexOfInputTerm(LexUnit input)
        {
            return termsName.IndexOf(input.name);
        }

        private PredictTableItem FindPredictItem(NTerm nTerm, LexUnit input)
        {
            int x = nTerms.IndexOf(nTerm);
            int y = IndexOfInputTerm(input);
            if (x < 0 || y < 0) { return new PredictTableItem(); }
            return table[x, y];
        }

        private bool GramAnalyTermHandler(LexUnit input, Symbols top, ref bool IsNext, ref bool IsEnd)
        {
            // Analysis Success.
            if (top == Term.end && IsTopInputMatch(top, input)) 
            {
                LogUtility.NewActionRecord(top, input, "分析完成");
                IsNext = false;
                IsEnd = true;
                return true; 
            }
            // Terminal match, pop and move to next input
            else if (IsTopInputMatch(top, input))
            {
                LogUtility.NewActionRecord(top, input, "匹配弹出，读入新符");
                stack.Pop();
                IsNext = true;
                return true;
            }
            else
            {
                LogUtility.NewActionRecord(top, input, "无法匹配，跳过输入");
                Compiler.GetInstance().ReportFrontInfo(this,
                    new CompilerReportArgs(LogItem.MsgType.WARN,
                    $"非预期的语法，跳过输入符号：{input.cont} ", input.line));
                IsNext = true;
                return false;
            }
            
        }

        private bool GramAnalyNTermHandler(LexUnit input, Symbols top, DynamicProperty prop ,ref bool IsNext)
        {
            PredictTableItem item = FindPredictItem((NTerm)top, input);
            // Encounter Sync Symbol , handle error before
            if (item.form == PredictTableItem.Form.SYNCH)
            {
                LogUtility.NewActionRecord(top, input, "同步符号，弹出栈顶");
                Compiler.GetInstance().ReportFrontInfo(this,
                    new CompilerReportArgs(LogItem.MsgType.WARN,
                    $"遭遇同步符号，{input.cont} ", input.line));
                stack.Pop();
                IsNext = false;
                return false;
            }
            // Unexpected input terminal , jump over and continue
            else if (item.form == PredictTableItem.Form.BLANK)
            {
                LogUtility.NewActionRecord(top, input, "无法匹配，跳过输入");
                Compiler.GetInstance().ReportFrontInfo(this,
                    new CompilerReportArgs(LogItem.MsgType.WARN,
                    $"非预期的语法，跳过输入符号：{input.cont} ", input.line));
                IsNext = true;
                return false;
            }
            else
            {
                // TODO 栈顶为非终结符且预测表项有值 进行推导与语义动作入栈控制
                IsNext = false;
                LogUtility.NewActionRecord(top, input, item.prod.ToString());
                Compiler.midGen.DeriveHandler(item, input);
                return true;
            }
        }

        private bool GramAnalyActionHandler(LexUnit input, Symbols top, ref bool IsNext)
        {
            LogUtility.NewActionRecord(top, input, "执行语义动作");
            GramAction act = (GramAction)top;
            bool IsActionCorrect = act.Activate();

            stack.Pop();
            IsNext = false;
            return IsActionCorrect;
        }

        internal bool StartGramAnaly()
        {
            InitAnalyStack();
            bool IsNext = true, IsEnd = false, IsCorrect = true;
            LexUnit input = new();
            while (!IsEnd)
            {
                stack.Top(out Symbols top, out dynamic prop);
                if(IsNext) 
                { 
                    RestoreLastInput(input);
                    GetLexInput(out input); 
                }

                if(top.IsTerm()) { IsCorrect &= GramAnalyTermHandler(input, top, ref IsNext, ref IsEnd); } 
                else if(top.IsNTerm()) { IsCorrect &= GramAnalyNTermHandler(input, top, prop, ref IsNext); }
                else if(top.IsAction()) { IsCorrect &= GramAnalyActionHandler(input, top, ref IsNext); } 
                else
                {
                    Compiler.GetInstance().ReportBackInfo(this, 
                        new CompilerReportArgs(LogItem.MsgType.ERROR, "分析栈栈顶异常"));
                    throw new Exception();
                }
            }
            return IsCorrect;
        }
    }
}
