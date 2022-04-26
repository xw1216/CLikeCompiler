using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Symbol
{
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
}
