using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal abstract class Symbols
    {
        internal enum Form
        {
            BLANK,
            TERM,
            NONTERM,
            ACTION
        }
        private Form form;
        private string name;
        private int refCnt;

        internal Symbols()
        {
            form = Form.BLANK;
            name = "";
            refCnt = 0;
        }

        internal abstract Form GetForm();
        protected void SetForm(Form form) { this.form = form; }
        internal string GetName() { return name; }
        internal void SetName(string name) { this.name = name; }

        internal void IncRef() { refCnt++; }
        internal void DecRef() { refCnt--; }
        internal int GetRefCnt() { return refCnt; }
        
        internal bool BeRef() { return refCnt > 0; }

    }

    internal class Term : Symbols
    {
        internal static Term blank = new Term();
        internal static Term end = new Term();

        internal static void Init()
        {
            blank.SetName("blank");
            end.SetName("end");
            end.SetForm(Form.TERM);
        }

        internal override Form GetForm()
        {
            return Form.TERM;
        }

        internal bool IsTerm(ref string str)
        {
            return Compiler.lex.IsKeyRecog(ref str);
        }
    }

    internal class NTerm : Symbols
    {
        internal int prodIndex { get; set; }

        List<Term> first = new();
        List<Term> follow = new();
        List<Term> synch = new();

        internal bool IsNullable()
        {
            return first.Contains(Term.blank);
        }

        internal bool IsFirstFollowCross()
        {
            return first.Any(IsInFollow);
        }

        internal bool IsInFollow(Term term)
        {
            return follow.Contains(term);
        }

        internal bool IsInFirst(Term term)
        {
            return first.Contains(term);
        }

        internal override Form GetForm()
        {
            return Form.NONTERM;
        }

    }

    internal class GramAction : Symbols
    {
        internal override Form GetForm()
        {
            return Form.ACTION;
        }

        internal void Activate()
        {
            detected.Invoke();
        }

        internal void AddHandler(ActionHandler action)
        {
            detected += action; 
        }

        internal delegate void ActionHandler();
        private event ActionHandler detected;
    }
}
