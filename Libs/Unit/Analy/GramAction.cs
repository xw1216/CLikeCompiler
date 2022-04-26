using CLikeCompiler.Libs.Unit.Symbol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Analy
{
    internal class GramAction : Symbols
    {
        internal GramAction()
        {
            SetForm(Type.ACTION);
        }

        internal GramAction(string name)
        {
            SetName(name);
            SetForm(Type.ACTION);
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
