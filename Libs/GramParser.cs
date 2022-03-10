using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class GramParser
    {
        private readonly string gram = "Grammar";
        private string gramSrc;

        private List<Prod> prods = new List<Prod>();
        private List<Term> terms = new List<Term>();
        private List<NTerm> nTerms = new List<NTerm>();

        private NTerm startNTerm;

        internal GramParser()
        {
            ResetGramParser();
        }

        internal void StartGramParse()
        {
            GetGramSrc();
            RuleParse();
        }

        internal void ResetGramParser()
        {
            gramSrc = "";
            terms.Clear();
            nTerms.Clear();
            prods.Clear();
            startNTerm = null;
        }

        internal void AddNewNTerm(NTerm nTerm) 
        { 
            nTerms.Add(nTerm);
        }

        internal NTerm GetStartNTerm()
        {
            return startNTerm;
        }

        internal void GetSymbolRefs(ref List<Prod> prods, ref List<Term> terms, ref List<NTerm> nTerms)
        {
            prods = this.prods;
            terms = this.terms;
            nTerms = this.nTerms;
        }

        private void GetGramSrc()
        {
            gramSrc = Compiler.GetInstance().GetStrFromResw(gram);
        }


        private void RuleParse()
        {
            terms.Add(Term.end);
            Term.end.IncRef();
            List<string> rules = new (gramSrc.Replace("\r","").Split('\n', 
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            foreach (string rule in rules)
            {
                List<string> side = new(rule.Split('@', StringSplitOptions.TrimEntries));
                GenProd(ref side);
            }
        }

        private void GenProd(ref List<string> side)
        {
            if(side.Count != 2)
            {
                Compiler.GetInstance().ReportBackInfo(this, 
                    new CompilerReportArgs(LogItem.MsgType.ERROR, "文法格式错误" + side.ToString()));
            }
            Prod prod = new Prod();
            Lhshandler(ref prod, side.First());
            RhsHandler(ref prod, side.Last());
            prods.Add(prod);
        }

        private void Lhshandler(ref Prod prod, string str)
        {
            NTerm lhs = CreateGetNTerm(str);
            lhs.prodIndex = prods.Count;
            prod.SetLhs(lhs);
        }

        private void RhsHandler(ref Prod prod, string str)
        {
            StringBuilder builder = new StringBuilder();
            
            for(int pos = 0; pos < str.Length; pos++)
            {
                if(str[pos] == '|')
                {
                    builder.Clear();
                    prod.NewSubProd();
                } else if(str[pos] == ' ')
                {
                    builder.Clear();
                    continue;
                } 
                else if(str[pos]== '$')
                {
                    pos  = SubNameCut(ref builder, ref str, pos + 1);
                    if(!(Compiler.lex.IsKeyRecog(builder.ToString())))
                    {
                        Compiler.GetInstance().ReportBackInfo(this,
                        new CompilerReportArgs(LogItem.MsgType.ERROR, "无法识别的文法终结符符号"));
                    }
                    Term term = CreateGetTerm(builder.ToString());
                    if(term != null)
                    {
                        prod.AddSubProdUnit(term);
                    }
                    builder.Clear();
                } 
                else if(IsLetter(str[pos]))
                {
                    pos = SubNameCut(ref builder, ref str, pos);
                    NTerm nTerm = CreateGetNTerm(builder.ToString());
                    prod.AddSubProdUnit(nTerm);
                    builder.Clear();
                }
                else
                {
                    Compiler.GetInstance().ReportBackInfo(this,
                    new CompilerReportArgs(LogItem.MsgType.ERROR, "无法识别的文法符号"));
                }
            }
        }

        private bool IsLetter(char c)
        {
            if((c >= 'a' && c <= 'z' ) || (c >= 'A' && c <= 'Z')) { return true; }
            else { return false; }
        }

        private int SubNameCut(ref StringBuilder builder, ref string str, int pos)
        {
            for(; pos < str.Length; pos++)
            {
                if (str[pos] == ' ') { break; }
                builder.Append(str[pos]);
            }
            return pos - 1;
        }

        private Term CreateGetTerm(string name)
        {
            if (name == "blank") { return null; }
            foreach (Term term in terms)
            {
                if(term.GetName() == name) { 
                    term.IncRef();
                    return term; 
                }
            }

            Term newTerm = new();
            newTerm.SetName(name);
            newTerm.IncRef();
            terms.Add(newTerm);
            return newTerm;
        }

        private NTerm CreateGetNTerm(string name)
        {
            foreach(NTerm nTerm in nTerms)
            {
                if(nTerm.GetName() == name) 
                { 
                    nTerm.IncRef();
                    return nTerm; 
                }
            }

            NTerm newNTerm = new();
            if(startNTerm == null)
            {
                startNTerm = newNTerm;
            }
            newNTerm.SetName(name);
            newNTerm.IncRef();
            nTerms.Add(newNTerm);
            return newNTerm;
        }
    }
}
