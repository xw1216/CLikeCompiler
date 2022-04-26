using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Symbol
{
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
}
