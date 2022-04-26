using CLikeCompiler.Libs.Unit.Analy;
using CLikeCompiler.Libs.Unit.Symbol;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class AnalyStack
    {
        private readonly List<Symbols> symbols = new();
        private readonly List<dynamic> properties = new();

        public void ResetStack()
        {
            symbols.Clear();
            properties.Clear();
        }

        internal int length { get { return symbols.Count; } }

        internal void Push(Symbols sym)
        {
            symbols.Add(sym);
            properties.Add(new DynamicProperty());
        }

        internal void Push(Symbols sym, dynamic prop)
        {
            symbols.Add(sym);
            properties.Add(prop);
        }

        internal void Pop()
        {
            if(properties.Count > 0)
            {
                symbols.RemoveAt(symbols.Count - 1);
                properties.RemoveAt(properties.Count - 1);
            }
        }

        internal bool Pop(out Symbols sym, out dynamic prop)
        {
            if (properties.Count <= 0)
            {
                sym = null;
                prop = null;
                return false;
            }
            else
            {
                sym = symbols.Last();
                symbols.RemoveAt(symbols.Count - 1);
                prop = properties.Last();
                properties.RemoveAt(properties.Count - 1);
                return true;
            }
        }

        internal bool Top(out Symbols sym, out dynamic prop)
        {
            if (properties.Count <= 0)
            {
                sym = null;
                prop = null;
                return false;
            }
            else
            {
                sym = symbols.Last();
                prop = properties.Last();
                return true;
            }
        }

        internal bool RelativeFetch(int dis, out dynamic prop)
        {
            if (properties.Count > dis)
            {
                prop = properties[properties.Count - 1 - dis];
                return true;
            }
            else
            {
                prop = null;
                return false;
            }
        }

    }
}
