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

        internal Form GetForm() { return form; }
        protected void SetForm(Form form) { this.form = form; }
        internal string GetName() { return name; }
        internal void SetName(string name) { this.name = name; }

        internal void IncRef() { refCnt++; }
        internal void DecRef() { refCnt--; }
        internal int GetRefCnt() { return refCnt; }
        
        internal bool BeRef() { return refCnt > 0; }

        internal bool IsTerm() { return form == Form.TERM; }
        internal bool IsNTerm() { return form == Form.NONTERM; }
        internal bool IsAction() { return form == Form.ACTION; }
        internal bool IsBlank() { return form == Form.BLANK; }

    }

    internal class Term : Symbols
    {
        internal Term()
        {
            this.SetForm(Form.TERM);
        }

        internal static Term blank = new Term();
        internal static Term end = new Term();

        internal static void Init()
        {
            blank.SetName("blank");
            end.SetName("end");
            blank.SetForm(Form.BLANK);
        }

        internal bool CanTermRecog(ref string str)
        {
            return Compiler.lex.IsKeyRecog(str);
        }
    }

    internal class NTerm : Symbols
    {
        internal NTerm()
        {
            this.SetForm(Form.NONTERM);
        }

        internal int prodIndex { get; set; } = -1;

        internal List<Term> first = new();
        internal List<Term> follow = new();
        internal List<Term> synch = new();

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

    }

    internal class GramAction : Symbols
    {
        internal GramAction()
        {
            this.SetForm(Form.ACTION);
        }

        internal GramAction(string name)
        {
            this.SetName(name);
            this.SetForm(Form.ACTION);
        }

        internal bool Activate()
        {
            return detected.Invoke();
        }

        internal void AddHandler(ActionHandler action)
        {
            detected += action; 
        }

        internal delegate bool ActionHandler();
        private event ActionHandler detected;
    }
}
