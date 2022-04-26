using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Runtime
{
    internal class MacroTable
    {
        private static readonly List<string> globalMacros = new();
        private readonly Dictionary<string, string> replaceMacros = new();

        public void AddDefineValue(string key, string value)
        {
            replaceMacros.Add(key, value);
            if (!globalMacros.Contains(key))
            {
                globalMacros.Add(key);
            }
        }

        public Dictionary<string, string> GetLocalMacros()
        {
            return replaceMacros;
        }

        public void ClearLocalMacros()
        {
            replaceMacros.Clear();
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
