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
            return Detected != null && Detected.Invoke();
        }

        internal void AddHandler(ActionHandler action)
        {
            Detected += action;
        }

        public delegate bool ActionHandler();
        public event ActionHandler Detected;
    }
}
