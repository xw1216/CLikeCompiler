using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Target
{
    internal class Target
    {
        internal string Op { get; set; }
        internal List<string> Args { get; }
        internal bool IsLabel { get; set; }

        internal Target()
        {
            Args = new List<string>();
            IsLabel = false;
        }

        internal Target(string op, params string[] args)
        {
            Op = op;
            Args = new List<string>(args);
            IsLabel = false;
        }

        public override string ToString()
        {
            StringBuilder builder = new ();
            builder.Append(Op);

            if (Args.Count > 0)
            {
                builder.Append('\t');
            }

            for (int i = 0; i < Args.Count; i++)
            {
                builder.Append(Args[i]);
                if (i < Args.Count - 1)
                {
                    builder.Append(",\t");
                }
            }
            return builder.ToString();
        }
    }
}
