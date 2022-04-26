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
            this.SetForm(Type.TERM);
        }

        internal static Term blank = new();
        internal static Term end = new();

        internal static void Init()
        {
            blank.SetName("blank");
            end.SetName("end");
            blank.SetForm(Type.BLANK);
        }

        internal static bool CanTermRecog(ref string str)
        {
            return Compiler.lex.IsKeyRecog(str);
        }
    }
}
