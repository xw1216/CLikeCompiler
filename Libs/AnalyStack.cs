using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class DynamicProperty : DynamicObject
    {
        private readonly Dictionary<string, object> prop = new();

        public DynamicProperty() { }

        public DynamicProperty(DynamicProperty lhs)
        {
            List<string> names = lhs.GetDynamicMemberNames().ToList();
            foreach (string name in names)
            {
                if(prop.ContainsKey(name)) { continue; }
                prop.Add(name, lhs.GetMember(name));
            }
        }

        public DynamicProperty(Dictionary<string, object> prop)
        {
            this.prop = prop;
        }

        public static DynamicProperty CreateByDynamic(dynamic lhs)
        {
            if (lhs.GetType() != typeof(DynamicProperty)) { return null; }
            return new DynamicProperty((DynamicProperty)lhs);
        }

        public object GetMember(string name)
        {
            if (prop.ContainsKey(name)) { return null; }
            return prop[name];
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
        private List<Symbols> symbols = new();
        private List<dynamic> properties = new();

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
