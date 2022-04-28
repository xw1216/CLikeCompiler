using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Analy
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
                if (prop.ContainsKey(name)) { continue; }
                prop.Add(name, lhs.GetMember(name));
            }
        }

        public DynamicProperty(Dictionary<string, object> prop)
        {
            this.prop = prop;
        }

        public static DynamicProperty CreateByDynamic(dynamic lhs)
        {
            return lhs.GetType() != typeof(DynamicProperty) ? null : new DynamicProperty((DynamicProperty)lhs);
        }

        private object GetMember(string name)
        {
            return prop.ContainsKey(name) ? null : prop[name];
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
}
