using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Unit.Prods;

namespace CLikeCompiler.Libs.Unit.Analy
{
    internal class PredictTableItem
    {
        internal Prod prod = null;
        internal Status status = Status.BLANK;
        internal readonly int[] pos = new int[2] { 0, 0 };

        internal enum Status
        {
            BLANK,
            FILL,
            SYNCH
        }

        internal bool IsBlank()
        {
            return status == Status.BLANK;
        }
    }
}
