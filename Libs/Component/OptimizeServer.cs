using System.Collections.Generic;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Unit.Quad;
using CLikeCompiler.Libs.Unit.Reg;
using CLikeCompiler.Libs.Util.LogItem;

namespace CLikeCompiler.Libs.Component
{
    internal class OptimizeServer
    {
        private List<List<Quad>> basicBlockList;


        private List<Quad> quadTable;
        private RegFiles regFiles;


        internal OptimizeServer()
        {
            this.basicBlockList = new List<List<Quad>>();
        }

        #region Init & Logs

        internal void SetQuadTable(List<Quad> table)
        {
            quadTable = table;
        }

        internal void SetRegFiles(RegFiles regs)
        {
            regFiles = regs;
        }

        private void SendBackMessage(string msg, LogMsgItem.Type type)
        {
            LogReportArgs args = new(type, msg);
            Compiler.Instance().ReportBackInfo(this, args);
        }

        #endregion

        // 主调用入口
        internal void DetermineFunc(FuncRecord func)
        {
            List<Quad> quadList = GetQuadsBetween(func);

            basicBlockList.Clear();
            DivideBasicBlocks(quadList);
        }

        private List<Quad> GetQuadsBetween(FuncRecord func)
        {
            Quad start = func.QuadStart;
            Quad end = func.QuadEnd;

            List<Quad> quadList = new();
            int i;
            for (i = 0; i < quadTable.Count; i++)
            {
                if(quadTable[i] == start) {break;}
            }

            for (; i < quadList.Count; i++)
            {
                quadList.Add(quadTable[i]);
                if(quadTable[i] == end) {break;}
            }
            return quadList;
        }

        private void DivideBasicBlocks(List<Quad> quadList)
        {

        }




    }
}
