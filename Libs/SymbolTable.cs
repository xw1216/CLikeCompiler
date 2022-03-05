using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class Symbols
    {
    }

    internal class MacroTable
    {
        private List<string> globalMacros = new();
        private Dictionary<string, string> replaceMacros = new();

        public void AddDefineValue(string key, string value)
        {
            replaceMacros[key] = value;
            if(!globalMacros.Contains(key))
            {
                globalMacros.Add(key);
            }
        }

        public ref Dictionary<string, string> GetLocalMacros()
        {
            return ref replaceMacros;
        }

        public void AddDefineInclude(string key)
        {
            globalMacros.Add(key);
        }

        public bool IsMacroExist(string key)
        {
            return globalMacros.Contains(key);
        }

        public void ResetMacroTable()
        {
            globalMacros.Clear();
            replaceMacros.Clear();
        }
    }
}
