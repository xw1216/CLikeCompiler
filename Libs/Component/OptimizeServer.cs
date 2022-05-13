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
            Reset();            

            func = funcRecord;
            List<Quad> funcQuadList = GetQuadsBetween(func.QuadStart, func.QuadEnd, true);
            DivideBasicBlocks(funcQuadList, out Dictionary<Quad, Quad> jumpDict);
            TopoDataFlow(jumpDict);

            CalcuBlockInoutActive();
            CalcuActiveInfo();
            CalcuRegisterDispatcher();

            CleanTempVar();
        }

        private void Reset()
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
                RecordBlockUnusedTempVar(basicBlockList[i]);
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
                    Rhs = new TypeRecord(var.Var.Type),
                    Dst = addrReg
                };
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
            quadTable.Insert(targetIndex, loadAddrQuad);
        }

        private void VarLoadHandler(QuadDescriptor quad, VarDescriptor var, RegDescriptor reg)
        {
            // 管理描述符 并使得 reg 被 var 独占
            RegMonopolizeByVar(reg, var);
            int targetIndex = quadTable.IndexOf(quad.ToQuad);
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
                quadTable.Insert(targetIndex, targetQuad);
            }
        }

        private void RegMonopolizeByVar(RegDescriptor reg, VarDescriptor var, bool isDst = false)
        {
            // 清除该寄存器内所有其他变量关系
            RemoveOtherVarInReg(reg);
            reg.Vars.Add(var);
            var.Addr.InReg = true;
            var.Addr.RegAt = reg;
            // 当为 Dst 时 还需要地址描述符不含 Mem 位置
            if (isDst)
            {
                var.Addr.InMem = false;
            }

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

        // 完成 Get Reg 函数
        private void GetReg(
            QuadDescriptor quad, out RegDescriptor lhsDescriptor, 
            out RegDescriptor rhsDescriptor, out RegDescriptor dstDescriptor)
        {
            VarDescriptor lhsVar = GetVarFromActive(quad.LhsDescriptor);
            VarDescriptor rhsVar = GetVarFromActive(quad.RhsDescriptor);
            VarDescriptor dstVar = GetVarFromActive(quad.DstDescriptor);

            if (dstVar is { IsCon: true })
            {
                SendBackMessage("不能向常量中写入", LogMsgItem.Type.ERROR);
                throw new ArgumentException();
            }

            // 复制语句 dst = lhs 中确保二者寄存器一致
            if (Quad.IsCopyOp(quad.ToQuad.Name))
            {
                lhsDescriptor = GetRegForSingleVar(quad, lhsVar, rhsVar, dstVar);
                rhsDescriptor = null;
                dstDescriptor = lhsDescriptor;
            }

            // 分别选择 lhs & rhs 的寄存器
            lhsDescriptor = GetRegForSingleVar(quad, lhsVar, rhsVar, dstVar);
            rhsDescriptor = GetRegForSingleVar(quad, rhsVar, lhsVar, dstVar);
            dstDescriptor = GetRegForSingleVar(quad, dstVar, lhsVar, rhsVar, 
                true, lhsDescriptor, rhsDescriptor);
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

        private RegDescriptor GetRegForDstVarShare(VarDescriptor lhsVar, VarDescriptor rhsVar, 
            RegDescriptor lhsReg , RegDescriptor rhsReg ) 
        {
            // lhs 或者 rhs 任意一方在本指令后不再使用 且 R_lhs 或 R_rhs 仅保存了这个变量
            if (lhsVar != null && lhsReg != null && lhsVar.Active.IsActive == false)
            {
                return lhsReg;
            }

            if (rhsVar != null && rhsReg != null && rhsVar.Active.IsActive == false)
            {
                return rhsReg;
            }

            return null;
        }

        // 注意为 dst 分配寄存器时 与 lhs & rhs 有较大的不同
        private RegDescriptor GetRegForSingleVar(QuadDescriptor quad, 
            VarDescriptor srcVar, VarDescriptor otherVar, VarDescriptor dstVar, 
            bool isDst = false, RegDescriptor lhsReg = null, RegDescriptor rhsReg = null)
        {
            if (srcVar == null) { return null; }

            // 快速选择 根据简易规则 寻找空或已存在寄存器
            RegDescriptor fastReg = GetRegForSingleVarFast(srcVar, isDst);
            if (fastReg != null) { return fastReg; }

            // 选择 R_dst 时特殊步骤：共用 R_lhs 或 R_rhs
            if (isDst)
            {
                RegDescriptor shareReg = GetRegForDstVarShare(otherVar, dstVar, lhsReg, rhsReg);
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
                    
                    // 如果 nowVar 后续不再使用 那么可占用
                    if (nowVar.Active.IsActive == false) { continue;}

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

        private void RecordBlockUnusedTempVar(BasicBlock block)
        {
            for (int i = 0; i < block.VarList.Count; i++)
            {
                VarDescriptor var = block.VarList[i];

                if (!var.IsTemp || var.IsNeedSpace) {continue;}
                if(tempCleanList.Contains(var.Var)) {continue;}
                tempCleanList.Add(var.Var);
            } 
        }

        private void CleanTempVar()
        {
            // 不存在跨函数临时变量 故可以在函数范围内操作
            func.CleanTempVar(tempCleanList);
        }

        #endregion
        
        #endregion
    }
}
