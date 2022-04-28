using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Component;

namespace CLikeCompiler.Libs.Unit.Symbol
{
    internal class Term : Symbols
    {
        internal Term()
        {
            this.SetForm(Type.TERM);
        }

        internal static readonly Term Blank = new();
        internal static readonly Term End = new();

        internal static void Init()
        {
            Blank.SetName("blank");
            End.SetName("end");
            Blank.SetForm(Type.BLANK);
        }

        internal static bool CanTermRecognize(ref string str)
        {
            return LexServer.IsKeyRecognize(str);
        }
    }
}
