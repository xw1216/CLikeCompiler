using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Runtime
{
    public sealed class QuadTable
    {
        private readonly List<Quad> quadList = new();
        public int Count => quadList.Count;

        public delegate void NewQuadHandler(Quad quad);
        public event NewQuadHandler NewQuadEvent;

        public delegate void ClearQuadHandler();
        public event ClearQuadHandler ClearQuadEvent;

        private Quad TempQuad { get; set; } = null;

        public QuadTable()
        {
            // QuadTest();
        }

        private void QuadTest()
        {
            Quad quad = new()
            {
                Name = "test",
                Lhs = new VarTempRecord(VarType.CHAR),
                Rhs = new VarRecord("testVar", VarType.INT),
                Dst = new ConsVarRecord(VarType.LONG),
            };

            for (int i = 0; i < 20; i++)
            {
                quadList.Add(quad);
            }
        }

        internal List<Quad> GetQuadBetween(int start, int end)
        {
            List<Quad> quads = new();
            if (start >= quadList.Count)
            {
                return quads;
            }

            if (end > quadList.Count)
            {
                for (int i = start; i < quadList.Count; i++)
                {
                    quads.Add(quadList[i]);
                }
            }
            else
            {
                for (int i = start; i < end; i++)
                {
                    quads.Add(quadList[i]);
                }
            }
            return quads;
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
                OnNewQuadEvent(quad);
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
                OnNewQuadEvent(quad);
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
            OnClearQuadEvent();
        }

        private void OnNewQuadEvent(Quad quad)
        {
            NewQuadEvent?.Invoke(quad);
        }

        private void OnClearQuadEvent()
        {
            ClearQuadEvent?.Invoke();
        }
    }
}
