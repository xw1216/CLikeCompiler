using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class Quad
    {
        internal static readonly List<string> quadOp;

        internal string Name { get; set; }
        internal Record Lhs { get; set; }
        internal Record Rhs { get; set; }
        internal Record Dst { get; set; }
    }

    internal class QuadTable
    {
        private readonly List<Quad> quadList = new();

        internal int Count
        {
            get { return quadList.Count; }
        }

        internal int NextQuadAddr()
        {
            return quadList.Count;
        }

        internal void GenQuad(string name, Record lhs, Record rhs, Record dst)
        {
            Quad quad = new();
            quad.Name = name;
            quad.Lhs = lhs;
            quad.Rhs = rhs;
            quad.Dst = dst;
            quadList.Add(quad);
        }

        internal Quad ElemAt(int index)
        {
            if(index < 0 || index >= quadList.Count) { return null; }
            return quadList[index];
        }

        internal void Clear()
        {
            quadList.Clear();
        }

    }
}
