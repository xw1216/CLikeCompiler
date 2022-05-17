using System;
using System.Collections.Generic;
using System.Linq;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Descriptor;
using CLikeCompiler.Libs.Unit.Quads;
using CLikeCompiler.Libs.Unit.Reg;
using CLikeCompiler.Libs.Util.LogItem;
using Microsoft.UI.Xaml.Automation;

namespace CLikeCompiler.Libs.Component
{
    internal class OptimizeServer
    {
        // operative vars for function now 
        private readonly List<RegDescriptor> regDescriptorList;
        private readonly List<BasicBlock> basicBlockList;
        private readonly List<VarRecord> tempCleanList;
        private FuncRecord func;

        // register at compiler from mid code generator
        private RegFiles regFiles;
        private QuadTable quadTable;
        private List<FuncRecord> funcList;

        
        internal OptimizeServer()
        {
            this.basicBlockList = new List<BasicBlock>();
            this.regDescriptorList = new List<RegDescriptor>();
            this.tempCleanList = new List<VarRecord>();
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
            ResetOptimize();            

            func = funcRecord;
            List<Quad> funcQuadList = GetQuadsBetween(func.QuadStart, func.QuadEnd, true);
            DivideBasicBlocks(funcQuadList);
            TopoDataFlow();

            CalcuBlockInoutActive();
            CalcuActiveInfo();
            CalcuRegisterDispatcher();

            CleanTempVar();
        }

        internal void ResetOptimize()
        {
            ClearRegDescriptorVars();
            basicBlockList.Clear();
            func = null;
            tempCleanList.Clear();
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

        internal void InitExternalComponents(RegFiles regs, QuadTable table, List<FuncRecord> funcs)
        {
            regFiles = regs;
            quadTable = table;
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
            int startIndex = quadTable.IndexOf(start);
            int endIndex = quadTable.IndexOf(end);

            if (startIndex == -1 || endIndex == -1)
            {
                SendBackMessage("找不到对应四元式", LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                quadList.Add(quadTable.ElemAt(i));
            }

            if (includeLast)
            {
                quadList.Add(quadTable.ElemAt(endIndex));
            }
            return quadList;
        }

        // 将四元式集合按照原中间产生式表的顺序排列
        private void SortQuadList(List<Quad> refList, List<Quad> entryList)
        {
            for (int i = 0; i < entryList.Count; i++)
            {
                bool isSwapped = false;
                for (int j = 0; j < entryList.Count - 1 - i; j++)
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
        private List<Quad> GetEntryList(List<Quad> quadList)
        {
            List<Quad> entryList = new();
            // jumpDict = new Dictionary<Quad, Quad>();

            bool isNextEntry = true;
            for (int i = 0; i < quadList.Count; i++)
            {
                Quad quad = quadList[i];
                if (isNextEntry)
                {
                    if (!(entryList.Contains(quad)))
                    {
                        entryList.Add(quad);
                    }
                    isNextEntry = false;
                }

                if (!IsJump(quad)) continue;
                if (quad.Name.Equals("jal") || quad.Name.Equals("jr"))
                {
                    isNextEntry = true;
                    continue;
                }

                Quad target = ((LabelRecord)quad.Dst).ToQuad;
                if (!(entryList.Contains(target)))
                {
                    // 构建跳转关系 用于构建流图
                    // jumpDict.Add(quad, target);
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
        private void DivideBasicBlocks(List<Quad> quadList)
        {
            basicBlockList.Clear();
            List<Quad> entryList = GetEntryList(quadList);
            // 用于构建流图的跳转关系
            // jumpDict = jumpMap;

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
        private void TopoDataFlow()
        {
            // 直接后继块
            for (var i = 0; i < basicBlockList.Count - 1; i++)
            {
                BasicBlock block = basicBlockList[i];
                if (!(block.IsLastUnconditionalJump()))
                {
                    block.NextBlocks.Add(basicBlockList[i+1]);
                }
            }

            // 间接后继块
            for (int i = 0; i < basicBlockList.Count; i++)
            {
                BasicBlock srcBlock = basicBlockList[i];
                for (int j = 0; j < srcBlock.QuadList.Count; j++)
                {
                    QuadDescriptor quad = srcBlock.QuadList[j];
                    string op = quad.ToQuad.Name;
                    if (!Quad.IsJumpOp(op) || op == "jal" || op == "jr") continue;
                    LabelRecord label = (LabelRecord)quad.ToQuad.Dst;
                    BasicBlock dstBlock = FindJumpTargetBlock(label.ToQuad);
                    srcBlock.NextBlocks.Add(dstBlock);
                }
            }
        }

        private BasicBlock FindJumpTargetBlock(Quad targetQuad)
        {
            for (int i = 0; i < basicBlockList.Count; i++)
            {
                if (!basicBlockList[i].IsStartWith(targetQuad)) continue;
                return basicBlockList[i];
            }

            SendBackMessage("找不到跳转目标", LogMsgItem.Type.ERROR);
            throw new Exception();
        }

        private void ExchangeJumpLabel(Quad origin, Quad destination)
        {
            if (origin.Label == null) {return;}

            origin.Label.ToQuad = destination;
            destination.Label = origin.Label;
            origin.Label = null;
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
                    List<VarRecord> outList = new();

                    // Out[B] = In[next_1] U In[next_2] U ...
                    for (int j = 0; j < block.NextBlocks.Count; j++)
                    {
                        outList = outList.Union(block.NextBlocks[j].inActiveList).ToList();
                    }
                    block.outActiveList = outList;

                    // In[B] = use U (Out[B] - def)
                    List<VarRecord> inList = 
                        block.useVarList.Union(
                            block.outActiveList.Except(
                                block.defVarList)).ToList();

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
                    Name = "st",
                    Lhs = regFiles.FindRegs("a" + i),
                    Rhs = null,
                    Dst = func.ArgsList[i]
                };
                ExchangeJumpLabel(quadTable.ElemAt(targetIndex), quad);
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
                PostDispatcherStore(basicBlockList[i]);
            }
            RecordBlockUnusedTempVar();
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
                
                if(quad.VarNum < 1) {continue;}

                // 刷新当前四元式中变量在寄存器中的活跃情况
                UpdateVarActive(quad);

                // 对于语句 op dst, lhs, rhs , 获取 R_dst, R_lhs, R_rhs
                GetReg(block, quad);
            }
        }

        private static void UpdateVarActive(QuadDescriptor quad)
        {
            if (quad.LhsDescriptor != null)
            {
                quad.LhsDescriptor.Var.Active = quad.LhsDescriptor;
            }

            if (quad.RhsDescriptor != null)
            {
                quad.RhsDescriptor.Var.Active = quad.RhsDescriptor;
            }

            if (quad.DstDescriptor != null)
            {
                quad.DstDescriptor.Var.Active = quad.DstDescriptor;
            }
        }

        #endregion

        #region Post Proces

        private int ClosestCallerEntryIndex(BasicBlock block)
        {
            QuadDescriptor entryQuad = null;
            for (int i = block.QuadList.Count - 1; i >= 0; i--)
            {
                if (block.QuadList[i].ToQuad.Name != "CallerEntry") continue;
                entryQuad = block.QuadList[i];
                break;
            }

            if (entryQuad == null)
            {
                SendBackMessage("没有找到函数调用入口", LogMsgItem.Type.ERROR);
                throw new Exception();
            }
            int targetIndex = quadTable.IndexOf(entryQuad.ToQuad);
            return targetIndex;
        }

        private void PostDispatcherStore(BasicBlock block)
        {
            int targetIndex;
            // 调用时 CallerEntry 之前就要保存
            if (block.QuadList.Last().ToQuad.Name == "jal")
            {
                targetIndex = ClosestCallerEntryIndex(block);
            }
            // 定义时 CalleeRestore 之前就要保存
            else if (block.QuadList.Last().ToQuad.Name == "jr")
            {
                targetIndex = quadTable.IndexOf(block.QuadList.Last().ToQuad);
                targetIndex -= 2;
            }
            else
            {
                targetIndex = quadTable.IndexOf(block.QuadList.Last().ToQuad);
            }

            if (targetIndex == -1)
            {
                SendBackMessage("找不到基本块出口语句", LogMsgItem.Type.ERROR);
                throw new ElementNotAvailableException();
            }

            for (int i = 0; i < block.outActiveList.Count; i++)
            {
                VarDescriptor var = block.FindVarDescriptor(block.outActiveList[i]);
                if (var != null && var.Addr.InMem == false)
                {
                    VarStoreHandler(var, targetIndex);
                }
            }
        }

        private void VarStoreHandler(VarDescriptor var, int targetIndex)
        {
            var.Addr.InMem = true;
            if (var.IsTemp)
            {
                var.IsNeedSpace = true;
            }

            if (var.IsGlobal)
            {
                // 全局变量 先加载地址 然后存入 逆序插入表现为正序
                Regs addrReg = regFiles.FindRegs("tp");
                Quad storeQuad = new()
                {
                    Name = "Store",
                    Lhs = var.Addr.RegAt.Reg,
                    Rhs = addrReg,
                    Dst = new TypeRecord(var.Var.Type)
                };
                ExchangeJumpLabel(quadTable.ElemAt(targetIndex), storeQuad);
                quadTable.Insert(targetIndex, storeQuad);

                InsertLoadAddr(var, targetIndex);
            }
            else
            {
                Quad targetQuad = new()
                {
                    Name = "st",
                    Lhs = var.Addr.RegAt.Reg,
                    Rhs = null,
                    Dst = var.Var,
                };
                ExchangeJumpLabel(quadTable.ElemAt(targetIndex), targetQuad);
                quadTable.Insert(targetIndex, targetQuad);
            }
        }

        #endregion

        #region Load

        private void InsertLoadAddr(VarDescriptor var, int targetIndex)
        {
            Regs addrReg = regFiles.FindRegs("tp");
            Quad loadAddrQuad = new()
            {
                Name = "LoadAddr",
                Lhs = var.Var,
                Rhs = null,
                Dst = addrReg
            };
            ExchangeJumpLabel(quadTable.ElemAt(targetIndex), loadAddrQuad);
            quadTable.Insert(targetIndex, loadAddrQuad);
        }

        private void VarLoadHandler(BasicBlock block, QuadDescriptor quad, VarDescriptor var, RegDescriptor reg)
        {
            // 管理描述符 并使得 reg 被 var 独占
            RegMonopolizeByVar(reg, var);

            var targetIndex = quad.ToQuad.Name == "CallerArg" ? 
                ClosestCallerEntryIndex(block) : quadTable.IndexOf(quad.ToQuad);
            
            // 插入对应的 Load 指令
            if (targetIndex == -1)
            {
                SendBackMessage("表中找不到目标四元式", LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            if (var.IsGlobal)
            {
                // 全局变量 先加载 data 域地址 然后间接寻址 Load
                // 列表项后退 逆序插入 实际正序
                Regs addrReg = regFiles.FindRegs("tp");
                Quad loadQuad = new()
                {
                    Name = "Load",
                    Lhs = addrReg,
                    Rhs = new TypeRecord(var.Var.Type),
                    Dst = reg.Reg
                };
                ExchangeJumpLabel(quadTable.ElemAt(targetIndex), loadQuad);
                quadTable.Insert(targetIndex, loadQuad);
                
                InsertLoadAddr(var, targetIndex);
            }
            else
            {
                // 普通变量 按栈内偏移量 生成 Load 指令并插入
                Quad targetQuad = new()
                {
                    Name = "ld",
                    Lhs = var.Var,
                    Rhs = null,
                    Dst = reg.Reg
                };
                ExchangeJumpLabel(quadTable.ElemAt(targetIndex), targetQuad);
                quadTable.Insert(targetIndex, targetQuad);
            }
        }

        private void RegMonopolizeByVar(RegDescriptor reg, VarDescriptor var, bool isDst = false)
        {
            // 清除该寄存器内所有其他变量关系
            RemoveOtherVarInReg(reg);

            // 设置寄存器位置
            reg.Vars.Add(var);
            var.Addr.RegAt = reg;
            var.Addr.InReg = true;

            // 当为 Dst 时 还需要地址描述符不含 Mem 位置
            if (isDst)
            {
                var.Addr.InMem = false;
            }

            // 如果是常量 那么一直在 mem 中
            if (var.IsCon)
            {
                var.Addr.InMem = true;
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

        private RegDescriptor GetRegLhs(BasicBlock block, QuadDescriptor quad, VarDescriptor lhsVar, VarDescriptor rhsVar, VarDescriptor dstVar) 
        {
            RegDescriptor lhsReg = GetRegForSingleVar(block, quad, lhsVar, rhsVar, dstVar);

            if(lhsReg == null) return null;
            quad.ToQuad.Lhs = lhsReg.Reg;

            // lhs 已经在 R 中 直接返回
            if (lhsReg == lhsVar.Addr.RegAt) return lhsReg;

            // 如果 lhs 不在对应的 R 中，更改描述符，生成 Ld 语句并进行相应关联
            SaveCalleeUsedReg(lhsReg);
            VarLoadHandler(block, quad, quad.LhsDescriptor.Var, lhsReg);
            
            return lhsReg;
        }

        private RegDescriptor GetRegRhs(BasicBlock block, QuadDescriptor quad, VarDescriptor lhsVar, VarDescriptor rhsVar, VarDescriptor dstVar)
        {
            // 选择 rhs 的寄存器
            RegDescriptor rhsReg = GetRegForSingleVar(block, quad, rhsVar, lhsVar, dstVar);

            if(rhsReg == null) return null;
            quad.ToQuad.Rhs = rhsReg.Reg;

            // rhs 已经在 R 中 直接返回
            if(rhsReg == rhsVar.Addr.RegAt) return rhsReg;
            // 继续判断 rhs 的位置并更改描述符，生成 Ld
            SaveCalleeUsedReg(rhsReg);
            VarLoadHandler(block, quad, quad.RhsDescriptor.Var, rhsReg);
            return rhsReg;
        }

        private void GetRegCopyOpHandler(BasicBlock block, QuadDescriptor quad, VarDescriptor lhsVar, VarDescriptor dstVar, RegDescriptor lhsReg)
        {
            if (lhsVar == null && dstVar == null)
            {
                SendBackMessage("无法识别的复制语句", LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            // dst 为空 无需处理
            if (dstVar == null) { return; }

            RegDescriptor dstReg;
            // 仅有 dst 需要寄存器
            if (lhsVar == null)
            {
                dstReg = GetRegForSingleVar(block, quad, dstVar, null, null, true);
                if(dstReg == null) return;
                quad.ToQuad.Dst = dstReg.Reg;
                SaveCalleeUsedReg(dstReg);
                RegMonopolizeByVar(dstReg, dstVar, true);
            }
            // lhs 不空 且 dst 不空 则让 R_dst = R_lhs 此时不强制 var 独占 reg
            else
            {
                dstReg = lhsReg;

                if (dstVar.Addr.InReg)
                {
                    dstVar.Addr.RegAt.Vars.Remove(dstVar);
                }
                // 更改四元式
                quad.ToQuad.Dst = dstReg.Reg;
                // 更新使用寄存器
                SaveCalleeUsedReg(dstReg);
                // 更新寄存器信息
                dstReg.Vars.Add(quad.DstDescriptor.Var);
                // 更新变量信息
                AddrDescriptor dstAddr = quad.DstDescriptor.Var.Addr;
                dstAddr.InReg = true;
                dstAddr.RegAt = dstReg;
                dstAddr.InMem = false;
            }
        }

        // 完成 Get Reg 函数
        private void GetReg(BasicBlock block, QuadDescriptor quad)
        {
            VarDescriptor lhsVar = GetVarFromActive(quad.LhsDescriptor);
            VarDescriptor rhsVar = GetVarFromActive(quad.RhsDescriptor);
            VarDescriptor dstVar = GetVarFromActive(quad.DstDescriptor);

            if (dstVar is { IsCon: true })
            {
                SendBackMessage("不能向常量中写入", LogMsgItem.Type.ERROR);
                throw new ArgumentException();
            }

            // 选择 lhs 的寄存器
            RegDescriptor lhsReg = GetRegLhs(block, quad, lhsVar, rhsVar, dstVar);

            // 如果是复制语句 针对 dst 处理
            if (Quad.IsCopyOp(quad.ToQuad.Name))
            {
                // 对于 mv 与 itr 的复制语句 若有二元则一定处于同寄存器中
                GetRegCopyOpHandler(block, quad, lhsVar, dstVar, lhsReg);
                return;
            }

            // 选择 rhs 的寄存器
            RegDescriptor rhsReg = GetRegRhs(block, quad, lhsVar, rhsVar, dstVar);
            
            // 选择 dst 寄存器
            RegDescriptor dstReg = GetRegForSingleVar(
                block, quad, 
                dstVar, lhsVar, rhsVar, 
                true, lhsReg, rhsReg);

            if (dstReg == null) return;

            // dst 目前暂存在 reg 中，需要更改描述符，并抹除 MEM 记录
            quad.ToQuad.Dst = dstReg.Reg;
            SaveCalleeUsedReg(dstReg);
            RegMonopolizeByVar(dstReg, quad.DstDescriptor.Var, true);
        }

        private static VarDescriptor GetVarFromActive(ActiveDescriptor active)
        {
            return active?.Var;
        }

        private RegDescriptor GetRegForSingleVarFast(VarDescriptor srcVar, bool isDst)
        {
            if (isDst)
            {
                // 对于 dst 选择只存放了 dst 的寄存器
                if (srcVar.Addr.InReg && srcVar.Addr.RegAt.Vars.Count == 1)
                {
                    return srcVar.Addr.RegAt;
                }
            }
            else
            {
                // 非 dst 变量本来就在寄存器中 选择该寄存器
                if (srcVar.Addr.InReg) { return srcVar.Addr.RegAt; }
            }
            
            // 有空闲寄存器
            RegDescriptor emptyReg = SelectEmptyRegDescriptor();
            return emptyReg;
        }

        private static RegDescriptor GetRegForDstVarShare(QuadDescriptor quad,  VarDescriptor lhsVar, VarDescriptor rhsVar, 
            RegDescriptor lhsReg , RegDescriptor rhsReg ) 
        {
            // lhs 或者 rhs 任意一方在本指令后不再使用 且 R_lhs 或 R_rhs 仅保存了这个变量
            if (lhsVar != null && lhsReg != null && quad.LhsDescriptor?.IsActive == false && lhsReg.Vars.Count == 1)
            {
                return lhsReg;
            }

            if (rhsVar != null && rhsReg != null && quad.RhsDescriptor?.IsActive == false && rhsReg.Vars.Count == 1)
            {
                return rhsReg;
            }

            return null;
        }

        // 注意为 dst 分配寄存器时 与 lhs & rhs 有较大的不同
        private RegDescriptor GetRegForSingleVar(BasicBlock block, QuadDescriptor quad, VarDescriptor srcVar, VarDescriptor otherVar, VarDescriptor dstVar, 
            bool isDst = false, RegDescriptor lhsReg = null, RegDescriptor rhsReg = null)
        {
            if (srcVar == null) { return null; }

            // 快速选择 根据简易规则 寻找空或已存在寄存器
            RegDescriptor fastReg = GetRegForSingleVarFast(srcVar, isDst);
            if (fastReg != null) { return fastReg; }

            // 选择 R_dst 时特殊步骤：共用 R_lhs 或 R_rhs
            if (isDst)
            {
                RegDescriptor shareReg = GetRegForDstVarShare(quad, otherVar, dstVar, lhsReg, rhsReg);
                if(shareReg != null) { return shareReg; }
            }

            // 检测其他寄存器进行抢占
            int minCost = int.MaxValue;
            RegDescriptor bestReg = null;
            List<VarDescriptor> storeList = null; 
            for (int i = 0; i < regDescriptorList.Count; i++)
            {
                int cost = 0;
                List<VarDescriptor> nowStoreList = new();
                RegDescriptor regNow = regDescriptorList[i];
                for (int j = 0; j < regNow.Vars.Count; j++)
                {
                    VarDescriptor nowVar = regNow.Vars[j];
                    // nowVar 在内存有副本 则可占用
                    if (nowVar.Addr.InMem) { continue;}

                    //遍历到就是前序的 dst 且不为当前的 lhs 与 rhs 那么可占用
                    if (!isDst)
                    {
                        if((nowVar == dstVar && nowVar != srcVar && nowVar != otherVar)) {continue;}
                    }
                    else
                    {
                        // 为 R_dst 选择时 传入参数位置不同
                        if(nowVar == srcVar && nowVar != otherVar && nowVar != dstVar) {continue;}
                    }
                    
                    // 如果 nowVar 后续不再使用（且不在基本块出口活跃） 那么可占用
                    if (nowVar.Active.IsActive == false && !(block.outActiveList.Contains(nowVar.Var))) { continue;}

                    // 否则该变量需要生成 st 语句 代价增加
                    cost++;
                    nowStoreList.Add(nowVar);
                }

                if (cost >= minCost) continue;
                minCost = cost;
                bestReg = regNow;
                storeList = nowStoreList;
            }

            if (bestReg == null)
            {
                SendBackMessage("无法计算出分配或抢占的寄存器", LogMsgItem.Type.ERROR);
                throw new NullReferenceException();
            }

            SaveVarForGetReg(quad, storeList);
            return bestReg;
        }


        private void SaveVarForGetReg(QuadDescriptor quad, List<VarDescriptor> storeList)
        {
            if(storeList == null || storeList.Count == 0) return;
            int targetIndex = quadTable.IndexOf(quad.ToQuad);

            for (int i = 0; i < storeList.Count; i++)
            {
                VarDescriptor var = storeList[i];
                if(var.Addr.InMem) {continue;}
                VarStoreHandler(var, targetIndex);
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

        #region Temp Var Clean

        private void RecordBlockUnusedTempVar()
        {
            Dictionary<VarRecord, bool> cleanDict = new();
            for (int i = 0; i < basicBlockList.Count; i++)
            {
                BasicBlock block = basicBlockList[i];
                for (int j = 0; j < block.VarList.Count; j++)
                {
                    VarRecord var = block.VarList[j].Var;
                    if(!(var.IsTemp())) {continue;}
                    bool needSpace = block.VarList[j].IsNeedSpace;
                    
                    if (cleanDict.ContainsKey(var))
                    {
                        if (needSpace)
                        {
                            cleanDict[var] = true;
                        }
                    }
                    else
                    {
                        cleanDict.Add(var, needSpace);
                    }
                }
            }

            foreach (KeyValuePair<VarRecord, bool> pair in cleanDict)
            {
                if (!(pair.Value))
                {
                    tempCleanList.Add(pair.Key);
                }
            }
        }

        private void CleanTempVar()
        {
            // 不存在跨函数临时变量 故可以在函数范围内操作
            func.CleanTempVar(tempCleanList);
            tempCleanList.Clear();
        }

        #endregion
        
        #endregion
    }
}
