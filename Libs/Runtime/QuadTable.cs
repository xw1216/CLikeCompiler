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
        internal int Count => quadList.Count;

        private Quad TempQuad { get; set; } = null;

        internal int NextQuadAddr()
        {
            return quadList.Count;
        }

        internal Quad NextQuadRef()
        {
            if (TempQuad is { Name: "" })
            {
                return TempQuad;
            }

            TempQuad = new Quad();
            return TempQuad;
        }

        internal Quad GenQuad(string name, IRecord lhs, IRecord rhs, IRecord dst)
        {
            if (TempQuad != null)
            {
                TempQuad.Name = name;
                TempQuad.Lhs = lhs;
                TempQuad.Rhs = rhs;
                TempQuad.Dst = dst;
                Quad quad = TempQuad;
                quadList.Add(TempQuad);
                TempQuad = null;
                return quad;
            }
            else
            {
                Quad quad = new()
                {
                    Name = name,
                    Lhs = lhs,
                    Rhs = rhs,
                    Dst = dst
                };
                quadList.Add(quad);
                return quad;
            }
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
