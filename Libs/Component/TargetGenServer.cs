using System;
using System.Collections.Generic;
using System.Text;
using CLikeCompiler.Libs.Enum;
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
    internal class TargetGenServer
    {
        private readonly List<Target> targetCodeList = new();
        private readonly List<string> targetDataList = new();
        private FuncRecord funcNow = null;
        private List<FuncRecord> funcList;  
        private List<CallRecord> callList;

        // register at compiler from mid code generator
        private RecordTable recordTable;
        private RegFiles regFiles;
        private QuadTable quadTable;

        private const int DWord = 8;

        #region Init

        internal TargetGenServer() {}

        internal void InitExternalComponents(RegFiles regs, QuadTable quadTableIn, RecordTable recordTableIn)
        {
            this.regFiles = regs;
            this.quadTable = quadTableIn;
            this.recordTable = recordTableIn;
            funcList = recordTableIn.GetFuncList();
            callList = recordTableIn.GetCallList();
        }

        private void SendBackMessage(string msg, LogMsgItem.Type type)
        {
            LogReportArgs args = new(type, msg);
            Compiler.Instance().ReportBackInfo(this, args);
        }

        #endregion

        #region Main

        internal List<string> StartCodeGen()
        {
            FuncPreProcess();
            CallRecordPrePrecess();
            GenDataCode();
            GenTextCode();
            MergeTargetCode();
            return targetDataList;
        }

        internal void ResetTargetGen()
        {
            targetCodeList.Clear();
            targetDataList.Clear();
            funcNow = null;
        }

        private void MergeTargetCode()
        {
            foreach (Target target in targetCodeList)
            {
                targetDataList.Add(target.ToString());
            }
        }

        #endregion

        #region Pre Process

        private void FuncPreProcess()
        {
            for (int i = 0; i < funcList.Count; i++)
            {
                FuncRecord func = funcList[i];
                func.CalcuStackLayout();
            }
        }

        private void CallRecordPrePrecess()
        {
            for (int i = 0; i < callList.Count; i++)
            {
                CallRecord call = callList[i];
                call.CalcuCallerSaveRegs();
            }
        }

        #endregion

        #region Data Segment

        private void GenDataCode()
        {
            targetDataList.Add(".data");
            DataGlobalVar();
            DataConsVar();
        }

        private void DataGlobalVar()
        {
            ScopeTable global = recordTable.GetGlobalTable();
            for (int i = 0; i < global.Count; i++)
            {
                targetDataList.Add(global[i].Name + ":");
                targetDataList.Add("\t" + ".space" + global[i].Length);
            }
        }

        private void DataConsVar()
        {
            ScopeTable consTable = recordTable.GetConsTable();
            for (int i = 0; i < consTable.Count; i++)
            {
                if (consTable[i] is not ConsVarRecord rec)
                {
                    throw new Exception("无法识别的常量");
                }
                string width = GetWidthStr(rec.Type);
                
                targetDataList.Add(rec.Name + ":");
                targetDataList.Add("\t" + width + rec.Val);
            }
        }

        private static string GetWidthStr(VarType varType)
        {
            switch (varType)
            {
                case VarType.BOOL:
                case VarType.CHAR:
                    return ".byte";
                case VarType.INT:
                    return ".word";
                case VarType.LONG:
                    return ".dword";
                default:
                    throw new ArgumentOutOfRangeException(nameof(varType), varType, null);
            }
        }

        #endregion

        #region Text Segment

        private void AddTarget(Target target)
        {
            targetCodeList.Add(target);
        }

        private void GenTextCode()
        {
            List<Quad> quadList = quadTable.GetQuadList();
            for (int i = 0; i < quadList.Count; i++)
            {
                Quad quad = quadList[i];
                string op = quad.Name;
                if (!(Quad.IsLegalOp(op)))
                {
                    SendBackMessage("无法识别的中间代码式", LogMsgItem.Type.ERROR);
                    throw new Exception();
                }
                // 如果有标识 先插入跳转标识
                InsertLabelCode(quad);

                

                // 跳转操作 出现 Reg Label 记录
                if (Quad.IsJumpOp(op))
                {
                    Target target = new()  {  Op = quad.Name, };
                    JumpQuadHandler(quad, target);
                }

                // 调用操作 出现 Func Call 记录
                else if (Quad.IsCallOp(op)) { CallQuadHandler(quad); }

                // 内存操作 出现 Type Var Reg Array ConsVar TempVar记录
                else if (Quad.IsMemOp(op)) { MemQuadHandler(quad); }

                // 复制操作 出现 Reg Imm 记录
                else if (Quad.IsCopyOp(op))
                {
                    Target target = new()  {  Op = quad.Name, };
                    CopyQuadHandler(quad, target);
                }

                // 普通操作 出现 Reg Imm 记录
                else
                {
                    Target target = new()  {  Op = quad.Name, };
                    StdQuadHandler(quad, target);
                }
            }
        }

        #region Utils

        private void InsertLabelCode(Quad quad)
        {
            if (quad.Label != null)
            {
                Target label = new()
                {
                    IsLabel = true,
                    Op = quad.Label.Name
                };
                targetCodeList.Add(label);
            }
        }

        private string GetCheckRegOrImm(IRecord rec)
        {
            if (rec is ImmRecord imm)
            {
                return imm.Value;
            }

            if (rec is Regs)
            {
                return rec.Name;
            }

            SendBackMessage("非寄存器或立即数类型记录", LogMsgItem.Type.ERROR);
            throw new Exception();
        }

        
        private static string StackPointerStr(int offset, bool isFp)
        {
            return offset + "(" + (isFp ? "fp" : "sp") + ")";
        }

        private string LoadStoreTypeStr(VarRecord var, bool isStore)
        {
            StringBuilder builder = new();
            builder.Append(isStore ? 's' : 'l');
            switch (var.Width)
            {
                case 1: builder.Append('b');
                    break;
                case 2: builder.Append('h');
                    break;
                case 4: builder.Append('w'); 
                    break;
                case 8: builder.Append('d');
                    break;
                default:
                    SendBackMessage("无法加载的变量长度", LogMsgItem.Type.ERROR);
                    throw new Exception();
            }
            return builder.ToString();
        }

        private string LoadStoreTypeStr(VarType varType, bool isStore)
        {
            StringBuilder builder = new();
            builder.Append(isStore ? 's' : 'l');
            switch (varType)
            {
                case VarType.BOOL:
                case VarType.CHAR:
                    builder.Append('b');
                    break;
                case VarType.INT:
                    builder.Append('w'); 
                    break;
                case VarType.LONG:
                    builder.Append('d');
                    break;
                default:
                    SendBackMessage("无法加载的变量长度", LogMsgItem.Type.ERROR);
                    throw new Exception();
            }
            return builder.ToString();
        }
        
        #endregion

        #region Jump Quad

        private void JumpQuadHandler(Quad quad, Target target)
        {
            if (quad.Lhs != null)
            {
                target.Args.Add(GetCheckRegOrImm(quad.Lhs));
            }

            if (quad.Rhs != null)
            {
                target.Args.Add(GetCheckRegOrImm(quad.Rhs));
            }

            if (quad.Dst is not LabelRecord label)
            {
                SendBackMessage("跳转式没有目标地址", LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            target.Args.Add(label.Name);
            AddTarget(target);
        }

        #endregion

        #region Copy Quad Gens

        private void CopyQuadHandler(Quad quad, Target target)
        {
            if (quad.Dst == null || quad.Lhs == null || quad.Rhs != null)
            {
                SendBackMessage("复制语句参数错误", LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            target.Args[0] = "mv";

            if (quad.Dst == quad.Lhs)
            {
                quadTable.Remove(quad);
            }

            target.Args.Add(GetCheckRegOrImm(quad.Dst));
            target.Args.Add(GetCheckRegOrImm(quad.Lhs));
            AddTarget(target);
        }

        #endregion

        #region Std Quad Gens

        private void StdQuadHandler(Quad quad, Target target)
        {
            if (quad.Dst != null)
            {
                target.Args.Add(GetCheckRegOrImm(quad.Dst));
            }

            if (quad.Lhs != null)
            {
                target.Args.Add(GetCheckRegOrImm(quad.Lhs));
            }

            if (quad.Rhs != null)
            {
                target.Args.Add(GetCheckRegOrImm(quad.Rhs));
            }
            AddTarget(target);
        }

        #endregion

        #region Mem Quad Gens

        private void MemQuadHandler(Quad quad)
        {
            string op = quad.Name;

            switch (op)
            {
                case "LoadAddr":
                    LoadAddrHandler(quad);
                    break;
                case "Load":
                    LoadStoreIndirectHandler(quad, false);
                    break;
                case "Store":
                    LoadStoreIndirectHandler(quad, true);
                    break;
                case "ArrayOffset":
                    ArrayOffsetHandler(quad);
                    break;
                case "ld":
                    LoadStoreHandler(quad, false);
                    break;
                case "st":
                    LoadStoreHandler(quad, true);
                    break;
                default:
                    ThrowMemError();
                    throw new Exception();
            }
        }

        private void ThrowMemError()
        {
            SendBackMessage("无法识别的内存操作数", LogMsgItem.Type.ERROR);
        }

        #region Indirect Access

        private void LoadAddrHandler(Quad quad)
        {
            if (quad.Lhs == null || quad.Rhs != null || quad.Dst == null)
            {
                ThrowMemError();
                throw new Exception();
            }

            Regs reg = (Regs)quad.Dst;

            AddTarget(new Target("lui", reg.Name, "%hi(" + quad.Lhs.Name + ")"));
            AddTarget(new Target("addi", reg.Name, "%lo(" + quad.Lhs.Name + ")"));
        }

        private void LoadStoreIndirectHandler(Quad quad, bool isStore)
        {
            if (quad.Lhs == null || quad.Rhs == null || quad.Dst == null)
            {
                ThrowMemError();
                throw new Exception();
            }

            Regs refReg, reg;

            if (isStore)
            {
                refReg = (Regs)quad.Dst;
                reg = (Regs)quad.Lhs;
            }
            else
            {
                refReg = (Regs)quad.Lhs;
                reg = (Regs)quad.Dst;
            }

            TypeRecord type = (TypeRecord)quad.Rhs;

            string op = LoadStoreTypeStr(type.VarType, isStore);
            AddTarget(new Target(op, reg.Name, "(" + refReg + ")"));
        }

        private void ArrayOffsetHandler(Quad quad)
        {
            if (quad.Lhs == null || quad.Rhs != null || quad.Dst == null)
            {
                ThrowMemError();
                throw new Exception();
            }

            ArrayRecord arrayRecord = (ArrayRecord)quad.Lhs;
            Regs reg = (Regs)quad.Dst;

            AddTarget(new Target("addi", reg.Name, reg.Name, arrayRecord.Offset.ToString()));

        }

        #endregion

        #region DirectAccess

        private void LoadStoreHandler(Quad quad, bool isStore)
        {
            if (quad.Lhs == null || quad.Rhs != null || quad.Dst == null)
            {
                ThrowMemError();
                throw new Exception();
            }

            Regs reg;
            VarRecord var;

            if (isStore)
            {
                reg = (Regs)quad.Dst;
                var = (VarRecord)quad.Lhs;
            }
            else
            {
                reg = (Regs)quad.Lhs;
                var = (VarRecord)quad.Dst;
            }

            string op = LoadStoreTypeStr(var, isStore);
            AddTarget(new Target(op, reg.Name, StackPointerStr(var.Offset, true)));
        }

        #endregion
        
        #endregion

        #region Call Quad Gens

        private void CallQuadHandler(Quad quad)
        {
            if (quad.Lhs != null || quad.Rhs != null || quad.Dst == null)
            {
                SendBackMessage("格式不正确的调用中间代码", LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            switch (quad.Name)
            {
                case "CalleeEntry": 
                    CalleeEntryHandler((FuncRecord)quad.Dst);
                    break;
                case "CalleeSave": 
                    CalleeSaveRestoreHandler((FuncRecord)quad.Dst, false);
                    break;
                case "CalleeRestore": 
                    CalleeSaveRestoreHandler((FuncRecord)quad.Dst, true);
                    break;
                case "CalleeExit": 
                    CalleeExitHandler((FuncRecord)quad.Dst);
                    break;
                case "CallerEntry": 
                    CallerEntryExitHandler((CallRecord)quad.Dst, false);
                    break;
                case "CallerSave": 
                    CallerSaveRestoreHandler((CallRecord)quad.Dst, false);
                    break;
                case "CallerArgs": 
                    CallerArgsHandler((CallRecord)quad.Dst);
                    break;
                case "CallerRestore": 
                    CallerSaveRestoreHandler((CallRecord)quad.Dst, true);
                    break;
                case "CallerExit": 
                    CallerEntryExitHandler((CallRecord)quad.Dst, true);
                    break;
                default:
                    SendBackMessage("无法识别的调用中间代码", LogMsgItem.Type.ERROR);
                    throw new Exception();
            }
        }

        #region Callee

        private void CalleeEntryHandler(FuncRecord func)
        {
            funcNow = func;
            int frameSize = func.FrameLength;
            AddTarget(new Target("subi", "sp", "sp", frameSize.ToString()));
            AddTarget(new Target("sd", "ra", StackPointerStr(frameSize - DWord, false)));
            AddTarget(new Target("sd", "fp", StackPointerStr(frameSize - 2 * DWord, false)));
            AddTarget(new Target("addi", "fp", "sp", frameSize.ToString()));
        }

        private void CalleeSaveRestoreHandler(FuncRecord func, bool isRestore)
        {
            int baseOffset = 3 * DWord;
            List<Regs> regs = func.SaveRegList;
            string op = isRestore ? "ld" : "sd";
            foreach (Regs reg in regs)
            {
                AddTarget(new Target( op, reg.Name, 
                    StackPointerStr(func.FrameLength - baseOffset, false)));
                baseOffset -= DWord;
            }
        }

        private void CalleeExitHandler(FuncRecord func)
        {
            int frameSize = func.FrameLength;
            funcNow = null;
            AddTarget(new Target("ld", "ra", StackPointerStr(frameSize - DWord, false)));
            AddTarget(new Target("ld", "fp", StackPointerStr(frameSize - 2 * DWord, false)));
            AddTarget(new Target("addi", "sp", "sp", frameSize.ToString()));
            AddTarget(new Target("jr", "ra"));
        }
        
        #endregion

        #region Caller

        private void CallerEntryExitHandler(CallRecord call, bool isExit)
        {
            string op = isExit ? "addi" : "subi";
            AddTarget(new Target(op, "sp", "sp", call.CallLength.ToString()));
        }

        private void CallerSaveRestoreHandler(CallRecord call, bool isRestore)
        {
            int baseOffset = call.CallLength - DWord;
            List<Regs> regs = call.SaveRegList;
            string op = isRestore ? "ld" : "sd";

            foreach (Regs reg in regs)
            {
                AddTarget(new Target(op, reg.Name, StackPointerStr(baseOffset, false)));
                baseOffset -= DWord;
            }
        }

        private void CallerArgsHandler(CallRecord call)
        {
            List<VarRecord> args = call.ArgsList;
            for (int i = 0; i < args.Count; i++)
            {
                string loadOp = LoadStoreTypeStr(args[i], false);
                if (i < 8)
                {
                    AddTarget(new Target(loadOp, regFiles.FindFuncArgRegs(i).Name, 
                        StackPointerStr(args[i].Offset, true)));
                }
                else
                {
                    string storeOp = LoadStoreTypeStr(args[i], true);
                    AddTarget(new Target(loadOp, "gp", StackPointerStr(args[i].Offset, true)));
                    AddTarget(new Target(storeOp, "gp", StackPointerStr(call.CallLength - args[i].Offset, true)));
                }
            }
        }

        #endregion

        #endregion
        

        #endregion


    }
}
