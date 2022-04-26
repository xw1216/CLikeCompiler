using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Symbol
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
}
