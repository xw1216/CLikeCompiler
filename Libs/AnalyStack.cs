using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class DynamicProperty : DynamicObject
    {
        private readonly Dictionary<string, object> prop;

        public DynamicProperty() { }

        public DynamicProperty(Dictionary<string, object> prop)
        {
            this.prop = prop;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return prop.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (prop.ContainsKey(binder.Name))
            {
                result = prop[binder.Name];
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            prop[binder.Name] = value;
            return true;
        }
    }

    internal class AnalyStack
    {
        List<Symbols> symbols = new();
        List<DynamicProperty> properties = new();

        internal void ResetStack()
        {
            symbols.Clear();
            properties.Clear();
        }

        internal int length { get { return symbols.Count; } }

        internal void Push(Symbols sym, DynamicProperty prop)
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

        internal bool Pop(out Symbols sym, out DynamicProperty prop)
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

        internal bool Top(out Symbols sym, out DynamicProperty prop)
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

        internal bool RelativeFetch(int dis, out DynamicProperty prop)
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
