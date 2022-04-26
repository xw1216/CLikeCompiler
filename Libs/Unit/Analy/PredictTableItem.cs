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
        internal Form form = Form.BLANK;
        internal int[] pos = new int[2] { 0, 0 };



        internal enum Form
        {
            BLANK,
            FILL,
            SYNCH
        }

        internal bool IsBlank()
        {
            return form == Form.BLANK;
        }
    }
}
