using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Symbol
{
    internal abstract class Symbols
    {
        internal enum Type
        {
            BLANK,
            TERM,
            NONTERM,
            ACTION
        }
        private Type type;
        private string name;
        private int refCnt;

        internal Symbols()
        {
            type = Type.BLANK;
            name = "";
            refCnt = 0;
        }

        public override string ToString()
        {
            return type + " " + name;
        }

        internal Type GetForm() { return type; }
        protected void SetForm(Type form) { this.type = form; }
        internal string GetName() { return name; }
        internal void SetName(string name) { this.name = name; }

        internal void IncRef() { refCnt++; }
        internal void DecRef() { refCnt--; }
        internal int GetRefCnt() { return refCnt; }

        internal bool BeRef() { return refCnt > 0; }

        internal bool IsTerm() { return type == Type.TERM; }
        internal bool IsNTerm() { return type == Type.NONTERM; }
        internal bool IsAction() { return type == Type.ACTION; }
        internal bool IsBlank() { return type == Type.BLANK; }

    }
}
