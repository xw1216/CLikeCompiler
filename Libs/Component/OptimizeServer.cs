using System.Collections.Generic;
using System.Linq;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Quads;
using CLikeCompiler.Libs.Unit.Reg;
using CLikeCompiler.Libs.Unit.Target;
using CLikeCompiler.Libs.Util.LogItem;
using Microsoft.UI.Xaml.Automation;

namespace CLikeCompiler.Libs.Component
{
    internal class OptimizeServer
    {
        // operative vars for function now 
        private readonly List<BasicBlock> basicBlockList;
        private FuncRecord func;

        // todo register at compiler
        private QuadTable quadTable;
        private RegFiles regFiles;
        private List<FuncRecord> funcList;

        private readonly List<RegDescriptor> regDescriptorList;
        
        internal OptimizeServer()
        {
            this.basicBlockList = new List<BasicBlock>();
            this.regDescriptorList = new List<RegDescriptor>();
        }

        #region Main

        // 主调用入口
        internal void StartOptimize()
        {
            InitRegDescriptor();

            for (int i = 0; i < funcList.Count; i++)
            {
                DetermineFunc(funcList[i]);
            }
        }

        private void DetermineFunc(FuncRecord funcRecord)
        {
            basicBlockList.Clear();
            ClearRegDescriptorVars();
            func = funcRecord;
            List<Quad> funcQuadList = GetQuadsBetween(func.QuadStart, func.QuadEnd, true);
            DivideBasicBlocks(funcQuadList, out Dictionary<Quad, Quad> jumpDict);
            TopoDataFlow(jumpDict);

            CalcuBlockInoutActive();
            CalcuActiveInfo();
            CalcuRegisterDispatcher();
        }

        #endregion

        #region Init & Logs

        private void InitRegDescriptor()
        {
            if (regDescriptorList.Count > 0)
            {
                return;
            }

            List<Regs> regIndex = new();
            regIndex.AddRange(regFiles.CalleeSaveList);
            regIndex.AddRange(regFiles.CallerSaveList);

            foreach (Regs reg in regIndex)
            {
                RegDescriptor descriptor = new (reg);
                regDescriptorList.Add(descriptor);
            }
        }

        internal void SetQuadTable(QuadTable table)
        {
            quadTable = table;
        }

        internal void SetRegFiles(RegFiles regs)
        {
            regFiles = regs;
        }

        internal void SetFuncList(List<FuncRecord> funcs)
        {
            funcList = funcs;
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
                if(quadTable.ElemAt(i) == start) {break;}
            }

            for (; i < quadList.Count; i++)
            {
                // not include the last
                if(includeLast && quadTable.ElemAt(i) == end) {break;}
                quadList.Add(quadTable.ElemAt(i));
                // include the last
                if(quadTable.ElemAt(i) == end) {break;}
            }
            return quadList;
        }

        // 将四元式集合按照原中间产生式表的顺序排列
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

        // 找到基本块入口式
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
                    // 构建跳转关系 用于构建流图
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

        // 划分基本块
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

        // 构建流图
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

        // 迭代法计算每个基本块的入口、出口活跃变量
        private void CalcuBlockInoutActive()
        {
            bool isAnyInChange = true;
            while (isAnyInChange)
            {
                isAnyInChange = false;
                // 基本块数一定大于等于 3 
                for (int i = basicBlockList.Count - 2; i >= 0; i--)
                {
                    
                    BasicBlock block = basicBlockList[i];
                    // Out[B] = In[next_1] U In[next_2] U ...
                    List<VarRecord> outList = new();
                    for (int j = 0; j < block.NextBlocks.Count; j++)
                    {
                        outList = outList.Union(block.inActiveList).ToList();
                    }
                    block.outActiveList = outList;

                    // In[B] = use U (Out[B] - def)
                    List<VarRecord> inList = block.useVarList.Union(block.outActiveList.Except(block.defVarList)).ToList();
                    if (IsVarListSame(block.inActiveList, inList)) continue;
                    isAnyInChange = true;
                    block.inActiveList = inList;
                }
            }
        }

        private bool IsVarListSame(List<VarRecord> lhs, List<VarRecord> rhs)
        {
            if (lhs.Count != rhs.Count)
            {
                return false;
            }

            return lhs.Except(rhs).ToList().Count == 0;
        }

        // 从后向前计算每个基本块内每个四元式的活跃信息
        private void CalcuActiveInfo()
        {
            for (int i = basicBlockList.Count - 1; i >= 0; i--)
            {
                //  获得出口活跃信息并初始化
                BasicBlock block = basicBlockList[i];
                block.InitOutActiveVars();

                for (int j = block.QuadList.Count - 1; j >= 0; j--)
                {
                    QuadDescriptor quad = block.QuadList[j];

                    VarDescriptor dstDescriptor = GetQuadVarDescriptor(block, quad.ToQuad.Dst);
                    if (dstDescriptor != null)
                    {
                        quad.VarNum++;
                        quad.DstDescriptor = dstDescriptor.Active;
                        dstDescriptor.Active = new ActiveDescriptor(dstDescriptor);
                    }

                    VarDescriptor lhsDescriptor = GetQuadVarDescriptor(block, quad.ToQuad.Lhs);
                    
                    if (lhsDescriptor != null)
                    {
                        quad.VarNum++;
                        quad.LhsDescriptor = lhsDescriptor.Active;
                        lhsDescriptor.Active = new ActiveDescriptor(lhsDescriptor)
                        {
                            IsActive = true,
                            NextUseQuad = quad,
                            Var = lhsDescriptor
                        };
                    }

                    VarDescriptor rhsDescriptor = GetQuadVarDescriptor(block, quad.ToQuad.Rhs);
                    if (rhsDescriptor != null)
                    {
                        quad.VarNum++;
                        quad.RhsDescriptor = rhsDescriptor.Active;
                        rhsDescriptor.Active = new ActiveDescriptor(rhsDescriptor)
                        {
                            IsActive = true,
                            NextUseQuad = quad,
                            Var = rhsDescriptor
                        };
                    }
                }
            }
        }

        private VarDescriptor GetQuadVarDescriptor(BasicBlock block,IRecord rec)
        {
            if (rec is not VarRecord varRecord)
            {
                return null;
            }

            if (varRecord is ConsVarRecord)
            {
                return null;
            }

            return block.FindVarDescriptor(varRecord);
        }

        #endregion

        #region Register Dispatcher

        #region Pre Process

        private void InitEntryBlock()
        {
            BasicBlock entryBlock = basicBlockList[0];
            List<VarRecord> useArgs = entryBlock.outActiveList.Intersect(func.ArgsList).ToList();
            // 确定 CalleeEntry 语句
            int targetIndex = quadTable.IndexOf(func.QuadStart);
            if (targetIndex == -1)
            {
                SendBackMessage("找不到函数入口语句", LogMsgItem.Type.ERROR);
                throw new ElementNotAvailableException();
            }
            // 确定 CalleeSave 后一句位置
            targetIndex += 2;

            // 在 Entry 基本块内将使用到的函数参数送入栈中 确保变量位置正确
            for (int i = func.ArgsList.Count - 1; i >= 0; i--)
            {
                if (i >= 8 || !useArgs.Contains(func.ArgsList[i])) continue;
                Quad quad = new ()
                {
                    Name = "St",
                    Lhs = regFiles.FindRegs("a" + i),
                    Rhs = null,
                    Dst = func.ArgsList[i]
                };
                quadTable.Insert(targetIndex, quad);
            }
        }

        private void ClearRegDescriptorVars()
        {
            for (int i = 0; i < regDescriptorList.Count; i++)
            {
                regDescriptorList[i].Vars.Clear();
            }
        }

        #endregion

        #region Main Process

        private void CalcuRegisterDispatcher()
        {
            InitEntryBlock();

            for (int i = 1; i < basicBlockList.Count - 1; i++)
            {
                ClearRegDescriptorVars();
                DispatcherBlockRegister(basicBlockList[i]);
                // todo 需要确定 ~Tmp 是否跨基本块使用从而确定优化与否
                PostDispatcherStore(basicBlockList[i]);
            }

        }

        private void SaveCalleeUsedReg(RegDescriptor regDescriptor)
        {
            Regs reg = regDescriptor.Reg;
            func.AddUsedRegs(reg);
        }

        private void DispatcherBlockRegister(BasicBlock block)
        {
            List<QuadDescriptor> quadsList = block.QuadList;
            for (int i = 0; i < quadsList.Count; i++)
            {
                QuadDescriptor quad = quadsList[i];
                string op = quad.ToQuad.Name;
                
                if(quad.VarNum < 1) {continue;}

                // 对于语句 op dst, lhs, rhs , 获取 R_dst, R_lhs, R_rhs
                GetReg(quad, out RegDescriptor lhsRegDescriptor,
                    out RegDescriptor rhsRegDescriptor, out RegDescriptor dstRegDescriptor);

                // 如果 lhs 不在对应的 R 中，更改描述符，生成 Ld 语句并进行相应关联
                if (lhsRegDescriptor != null)
                {
                    VarLoadHandler(quad, quad.LhsDescriptor.Var, lhsRegDescriptor);
                    SaveCalleeUsedReg(lhsRegDescriptor);
                    quad.ToQuad.Lhs = lhsRegDescriptor.Reg;
                }

                // 对于 mv 与 itr 的复制语句 若有二元则一定处于同寄存器中
                if (Quad.IsCopyOp(op))
                {
                    if (dstRegDescriptor != null)
                    {
                        // 此时不强制 var 独占 reg
                        dstRegDescriptor.Vars.Add(quad.DstDescriptor.Var);
                        AddrDescriptor dstAddrDescriptor = quad.DstDescriptor.Var.Addr;
                        dstAddrDescriptor.InReg = true;
                        dstAddrDescriptor.RegAt = dstRegDescriptor;
                        dstAddrDescriptor.InMem = false;
                        SaveCalleeUsedReg(dstRegDescriptor);
                    }
                }
                else
                {
                    // 继续判断 rhs 的位置并更改描述符，生成 Ld
                    if (rhsRegDescriptor != null)
                    {
                        VarLoadHandler(quad, quad.RhsDescriptor.Var, rhsRegDescriptor);
                        SaveCalleeUsedReg(rhsRegDescriptor);
                        quad.ToQuad.Rhs = rhsRegDescriptor.Reg;
                    }

                    // dst 目前暂存在 reg 中，需要更改描述符，并抹除 MEM 记录
                    if (dstRegDescriptor != null)
                    {
                        quad.ToQuad.Dst = dstRegDescriptor.Reg;
                        RegMonopolizeByVar(dstRegDescriptor, quad.DstDescriptor.Var, true);
                        SaveCalleeUsedReg(dstRegDescriptor);
                    }
                }
            }
        }

        #endregion

        #region Post Proces

        private void PostDispatcherStore(BasicBlock block)
        {
            // 确定 CalleeRestore 句的位置
            int targetIndex = quadTable.IndexOf(func.QuadEnd);
            if (targetIndex == -1)
            {
                SendBackMessage("找不到函数出口语句", LogMsgItem.Type.ERROR);
                throw new ElementNotAvailableException();
            }

            targetIndex--;

            for (int i = 0; i < block.outActiveList.Count; i++)
            {
                VarDescriptor var = block.FindVarDescriptor(block.outActiveList[i]);
                if (var != null && var.Addr.InMem == false )
                {
                    VarStoreHandler(var);
                    Quad targetQuad = new()
                    {
                        Name = "st",
                        Lhs = var.Var,
                        Rhs = null,
                        Dst = var.Addr.RegAt.Reg
                    };
                    quadTable.Insert(targetIndex, targetQuad);
                }
            }
        }

        private void VarStoreHandler(VarDescriptor var)
        {
            var.Addr.InMem = true;
        }

        
        #endregion

        #region Load

        private void VarLoadHandler(QuadDescriptor quad, VarDescriptor var, RegDescriptor reg)
        {
            // 已经在寄存器中 无需移动
            if (var.Addr.InReg && var.Addr.RegAt == reg) { return; }

            // 未在寄存器中 首先管理描述符 并使得 reg 被 var 独占
            RegMonopolizeByVar(reg, var);

            // 生成 Load 指令并插入
            Quad targetQuad = new()
            {
                Name = "ld",
                Lhs = var.Var,
                Rhs = null,
                Dst = reg.Reg
            };
            int targetIndex = quadTable.IndexOf(quad.ToQuad);
            if (targetIndex != -1)
            {
                quadTable.Insert(targetIndex, targetQuad);
            }
        }

        private void RegMonopolizeByVar(RegDescriptor reg, VarDescriptor var, bool isDst = false)
        {
            RemoveOtherVarInReg(reg);
            reg.Vars.Add(var);
            var.Addr.InReg = true;
            var.Addr.RegAt = reg;
            // 当为 Dst 时 还需要地址描述符不含 Mem 位置
            if (isDst)
            {
                var.Addr.InMem = false;
            }
        }

        private void RemoveOtherVarInReg(RegDescriptor reg)
        {
            for (int i = 0; i < reg.Vars.Count; i++)
            {
                VarDescriptor var = reg.Vars[i];
                var.Addr.InReg = false;
                var.Addr.RegAt = null;
            }
            reg.Vars.Clear();
        }

        #endregion

        #region Get Reg

        // 完成 Get Reg 函数
        private int GetReg(
            QuadDescriptor quad, out RegDescriptor lhsDescriptor, 
            out RegDescriptor rhsDescriptor, out RegDescriptor dstDescriptor)  
        {
            if (quad.LhsDescriptor != null)
            {
                VarDescriptor lhs = quad.LhsDescriptor.Var;
                if (lhs.Addr.InReg)
                {
                    return lhs.Addr.RegAt;
                }

                RegDescriptor emptyReg = SelectEmptyRegDescriptor();
                if (emptyReg != null)
                {
                    return emptyReg;
                }



            }
        }

        private RegDescriptor SelectEmptyRegDescriptor()
        {
            for (int i = 0; i < regDescriptorList.Count; i++)
            {
                if (regDescriptorList[i].Vars.Count == 0)
                {
                    return regDescriptorList[i];
                }
            }
            return null;
        }

        #endregion




        #endregion


    }
}
