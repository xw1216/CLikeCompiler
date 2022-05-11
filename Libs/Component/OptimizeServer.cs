using System.Collections.Generic;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Quads;
using CLikeCompiler.Libs.Unit.Reg;
using CLikeCompiler.Libs.Unit.Target;
using CLikeCompiler.Libs.Util.LogItem;

namespace CLikeCompiler.Libs.Component
{
    internal class OptimizeServer
    {
        private readonly List<BasicBlock> basicBlockList;

        // todo register at compiler
        private List<Quad> quadTable;
        private RegFiles regFiles;


        internal OptimizeServer()
        {
            this.basicBlockList = new List<BasicBlock>();
        }

        // 主调用入口
        internal void DetermineFunc(FuncRecord func)
        {
            basicBlockList.Clear();
            List<Quad> funcQuadList = GetQuadsBetween(func.QuadStart, func.QuadEnd, true);
            DivideBasicBlocks(funcQuadList, out Dictionary<Quad, Quad> jumpDict);
            TopoDataFlow(jumpDict);
            CalcuActiveInfo();
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

        #region Basic Block
        private static bool IsJump(Quad quad)
        {
            string op = quad.Name;
            return Quad.IsJumpOp(op);
        }

        private List<Quad> GetQuadsBetween(Quad start, Quad end, bool includeLast)
        {
            List<Quad> quadList = new();
            int i;
            for (i = 0; i < quadTable.Count; i++)
            {
                if(quadTable[i] == start) {break;}
            }

            for (; i < quadList.Count; i++)
            {
                // not include the last
                if(includeLast && quadTable[i] == end) {break;}
                quadList.Add(quadTable[i]);
                // include the last
                if(quadTable[i] == end) {break;}
            }
            return quadList;
        }

        private void SortQuadList(List<Quad> refList, List<Quad> entryList)
        {
            for (int i = 0; i < entryList.Count; i++)
            {
                bool isSwapped = false;
                for (int j = 0; i < entryList.Count - 1 - i; j++)
                {
                    if (refList.IndexOf(entryList[j]) <= refList.IndexOf(entryList[j + 1])) continue;

                    (entryList[j], entryList[j + 1]) = (entryList[j + 1], entryList[j]);
                    if (!isSwapped)
                    {
                        isSwapped = true;
                    }
                }

                if (!isSwapped)
                {
                    return;
                }
            }
        }

        private List<Quad> GetEntryList(List<Quad> quadList, out Dictionary<Quad, Quad> jumpDict)
        {
            List<Quad> entryList = new() { quadList[0] };
            jumpDict = new Dictionary<Quad, Quad>();

            bool isNextEntry = false;
            for (int i = 0; i < quadList.Count; i++)
            {
                if (isNextEntry)
                {
                    if (!(entryList.Contains(quadList[i])))
                    {
                        entryList.Add(quadList[i]);
                    }
                }

                if (!IsJump(quadList[i])) continue;
                if (quadList[i].Name.Equals("jal") || quadList[i].Name.Equals("jr"))
                {
                    isNextEntry = true;
                    continue;
                }

                Quad target = ((LabelRecord)quadList[i].Dst).ToQuad;
                if (!(entryList.Contains(target)))
                {
                    jumpDict.Add(quadList[i], target);
                    entryList.Add(target);
                }
                isNextEntry = true;
            }

            SortQuadList(quadList, entryList);

            return entryList;
        }

        private static List<QuadDescriptor> CreateQuadDescriptorList(List<Quad> quadList)
        {
            List<QuadDescriptor> descriptorList = new();
            foreach (Quad quad in quadList)
            {
                QuadDescriptor descriptor = new (quad);
                descriptorList.Add(descriptor);
            }
            return descriptorList;
        }

        private void DivideBasicBlocks(List<Quad> quadList, out Dictionary<Quad, Quad> jumpDict)
        {
            basicBlockList.Clear();
            List<Quad> entryList = GetEntryList(quadList, out Dictionary<Quad, Quad> jumpMap);
            // 用于构建流图的跳转关系
            jumpDict = jumpMap;

            // ENTRY 起始空基本块
            basicBlockList.Add(new BasicBlock());

            for (int i = 0; i < entryList.Count; i++)
            {
                List<Quad> blockQuads = (i == entryList.Count - 1) 
                    ? GetQuadsBetween(entryList[^1], quadList[^1], true) 
                    : GetQuadsBetween(entryList[i], entryList[i + 1], false);

                List<QuadDescriptor> quadDescriptors = CreateQuadDescriptorList(blockQuads);
                basicBlockList.Add(new BasicBlock(quadDescriptors));
            }

            // EXIT 结束空基本块
            basicBlockList.Add(new BasicBlock());

            
        }

        #endregion

        #region Data flow

        private void TopoDataFlow(Dictionary<Quad, Quad> jumpMap)
        {
            // 直接后继块
            for (var i = 0; i < basicBlockList.Count - 1; i++)
            {
                BasicBlock block = basicBlockList[i];
                if (!(block.IsLastJump()))
                {
                    block.NextBlocks.Add(basicBlockList[i+1]);
                }
            }

            // 间接后继块
            foreach (KeyValuePair<Quad, Quad> pair in jumpMap)
            {
                BasicBlock srcBlock = null, dstBlock = null;
                for (int i = 0; i < basicBlockList.Count; i++)
                {
                    if (!basicBlockList[i].IsQuadIn(pair.Key)) continue;
                    srcBlock = basicBlockList[i];
                    break;
                }
                
                for (int i = 0; i < basicBlockList.Count; i++)
                {
                    if (!basicBlockList[i].IsStartWith(pair.Value)) continue;
                    dstBlock = basicBlockList[i];
                    break;
                }

                if (srcBlock != null && dstBlock != null)
                {
                    srcBlock.NextBlocks.Add(dstBlock);
                }
            }

        }

        #endregion

        #region Active Info

        private void CalcuBlockInoutActive()
        {
            bool isAnyInChange = true;
            while (isAnyInChange)
            {
                // 基本块数一定大于等于 3 
                for (int i = basicBlockList.Count - 2; i >= 0; i--)
                {

                }
            }

        }

        private void CalcuActiveInfo()
        {
            for (int i = basicBlockList.Count - 1; i >= 0; i--)
            {
                // List<QuadDescriptor> quadList = basicBlockList[i];
                // List<VarDescriptor> varList = blockVarList[i];

                // todo 获得出口活跃信息并初始化

                if (i != basicBlockList.Count - 1)
                {

                }


            }
        }

        #endregion


    }
}
