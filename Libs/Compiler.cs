using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    public class Compiler
    {
        private static Compiler compiler = new();

        public Compiler()
        {
            
        }

        static ref Compiler GetInstance()
        {
            return ref compiler;
        }
    }
}
