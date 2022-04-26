using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Quad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Runtime
{
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

        internal void GenQuad(string name, IRecord lhs, IRecord rhs, IRecord dst)
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
            if (index < 0 || index >= quadList.Count) { return null; }
            return quadList[index];
        }

        internal void Clear()
        {
            quadList.Clear();
        }

    }
}
