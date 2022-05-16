using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Analy;
using CLikeCompiler.Libs.Unit.Symbol;
using CLikeCompiler.Libs.Util.LogItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Quads;
using CLikeCompiler.Libs.Unit.Reg;
// ReSharper disable UnusedMember.Global

// ReSharper disable UnusedMember.Local

namespace CLikeCompiler.Libs.Component
{
    public class MidGenServer
    {
        private readonly AnalyStack stack;

        private int predictRow;
        private int predictCol;
        private LexUnit inputNow;
        private LexUnit inputLast;

        private List<Symbols> engageQueue;
        private QuadTable quadTable;
        private RecordTable recordTable;

        internal MidGenServer(GramServer gram)
        {
            stack = gram.GetStack();
        }

        internal void ResetMidGenServer()
        {
            predictCol = 0;
            predictCol = 0;
            inputLast = null;
            inputNow = null;
            engageQueue?.Clear();
            quadTable?.Clear();
            recordTable?.ResetRecordTable();
        }

        internal void SetTable(QuadTable quadTableArg, RecordTable recordTableArg)
        {
            this.quadTable = quadTableArg;
            this.recordTable = recordTableArg;
        }

        private void SendFrontMessage(string msg, LogMsgItem.Type type, int linePos)
        {
            LogReportArgs args = new(type, msg, linePos);
            Compiler.Instance().ReportFrontInfo(this, args);
        }

        private void SendBackMessage(string msg, LogMsgItem.Type type)
        {
            LogReportArgs args = new(type, msg, inputNow.line);
            Compiler.Instance().ReportBackInfo(this, args);
        }

        #region Stack Control

        private void StackTopProp(out dynamic prop)
        {
            stack.Top(out _, out prop);
        }

        private void StackPop()
        {
            stack.Pop();
        }

        private void AutoPush(int cnt)
        {
            if(cnt > engageQueue.Count) { return; }
            for(; cnt > 0; cnt--)
            {
                stack.Push(engageQueue[0]);
                engageQueue.RemoveAt(0);
            }
        }

        private void AutoPush(DynamicProperty prop)
        {
            if(engageQueue.Count < 1) { return; }
            stack.Push(engageQueue[0], prop);
            engageQueue.RemoveAt(0);
        }

        #endregion

        #region Action Dispatcher

        private GramAction CreateBindActions(string name)
        {
            string methodName = name.Replace(" ", "Action");
            Type server =  this.GetType();
            MethodInfo method = server.GetMethod(methodName);
            if(method == null)
            {
                SendBackMessage("未找到成员函数：" + methodName, LogMsgItem.Type.ERROR);
                throw new Exception();
            }

            GramAction action = new(name);
            Delegate del =  Delegate.CreateDelegate(typeof(GramAction.ActionHandler), this, method);
            action.AddHandler(del as GramAction.ActionHandler);
            return action;
        }

        private static void BackPatch(List<Quad> quadJumpList, Quad targetQuad)
        {
            foreach (Quad quad in quadJumpList)
            {
                quad.Dst = targetQuad.Label;
            }
        }

        private void ItrDataType(ref VarRecord lhsEntry, ref VarRecord rhsEntry)
        {
            if (lhsEntry.GetRecordType() != RecordType.VAR || rhsEntry.GetRecordType() != RecordType.VAR)
            {
                throw new ArgumentException("非单变量无法转换类型");
            }

            if (lhsEntry.Type == rhsEntry.Type) return;
            if (lhsEntry.Type == VarType.VOID || rhsEntry.Type == VarType.VOID)
            {
                throw new ArgumentException("void 无法转换类型");
            }
            VarTempRecord temp;
            if (lhsEntry.Width < rhsEntry.Width)
            {
                temp = recordTable.CreateTempVarRecord(rhsEntry.Type);
                quadTable.GenQuad("itr", lhsEntry, null, temp);
                lhsEntry = temp;
            }
            else
            {
                temp = recordTable.CreateTempVarRecord(lhsEntry.Type);
                quadTable.GenQuad("itr", rhsEntry, null, temp);
                rhsEntry = temp;
            }
        }

        private void ItrDataType(ref VarRecord record, VarType type)
        {
            VarType recType = record.Type;
            if (record.GetRecordType() != RecordType.VAR)
            {
                throw new ArgumentException("非单变量无法转换类型");
            }

            if (recType == type) return;
            if (recType == VarType.VOID || type == VarType.VOID)
            {
                throw new ArgumentException("void 无法转换类型");
            }
            VarTempRecord temp = recordTable.CreateTempVarRecord(type);
            quadTable.GenQuad("itr", record, null, temp);
            record = temp;
        }

        private VarRecord FilterFindVarRecord(string name)
        {
            IDataRecord entry = recordTable.FindRecord(name);
            if (entry == null)
            {
                throw new ArgumentException("不存在该标识符：" + name);
            }

            if (entry.GetRecordType() != RecordType.VAR)
            {
                throw new ArgumentException("不支持引用非单变量：" + name);
            }

            return (VarRecord)entry;
        }

        private ArrayRecord FilterFindArrayRecord(string name)
        {
            IDataRecord entry = recordTable.FindRecord(name);
            if (entry == null)
            {
                throw new ArgumentException("不存在该标识符：" + name);
            }

            if (entry.GetRecordType() != RecordType.ARRAY)
            {
                throw new ArgumentException("不支持单变量的数组引用：" + name);
            }

            return (ArrayRecord)entry;
        }

        private void DefaultDeriveNoProp()
        {
            StackPop();
            while(engageQueue.Count > 0)
            {
                stack.Push(engageQueue[0], new DynamicProperty());
                engageQueue.RemoveAt(0);
            }
        }

        internal void DeriveHandler(PredictTableItem item, LexUnit unit)
        {
            predictRow = item.pos[0];
            predictCol = item.pos[1];
            inputNow = unit;
            inputLast = Compiler.gram.GetLastInput();
            engageQueue = new List<Symbols>(item.prod.GetRhs().First());
            engageQueue.Reverse();
            DeriveDispatcher();
        }

        private void DeriveDispatcher()
        {
            switch(predictRow)
            {
                case 0:  ProgramPushCtrl(); break;
                case 1:  DeclareClusterPushCtrl();  break;
                case 2:  DeclareClusterLoopPushCtrl();  break;
                case 3:  DeclarePushCtrl(); break;
                case 4:  TypeDeclarePushCtrl(); break;
                case 5:  VarDeclarePushCtrl(); break;
                case 6:  FuncDeclarePushCtrl(); break;
                case 7:  ArrayDeclarePushCtrl(); break;
                case 8:  ArrayDeclareLoopPushCtrl(); break;
                case 9:  FormParamPushCtrl(); break;
                case 10: ParamListPushCtrl(); break;
                case 11: ParamListLoopPushCtrl(); break;
                case 12: ParamPushCtrl(); break;
                case 13: ArgsPushCtrl(); break;
                case 14: ArgsListPushCtrl(); break;
                case 15: ArgListLoopPushCtrl(); break;
                case 16: StateBlockPushCtrl(); break;
                case 17: InnerDeclarePushCtrl(); break;
                case 18: InnerVarDeclarePushCtrl(); break;
                case 19: StateClusterPushCtrl(); break;
                case 20: StatePushCtrl(); break;
                case 21: AssignPushCtrl(); break;
                case 22: ReturnPushCtrl(); break;
                case 23: ReturnAlterPushCtrl(); break;
                case 24: WhilePushCtrl(); break;
                case 25: IfPushCtrl(); break;
                case 26: IfAlterPushCtrl(); break;
                case 27: ExprPushCtrl(); break;
                case 28: ExprLoopPushCtrl(); break;
                case 29: AddExprPushCtrl(); break;
                case 30: AddExprLoopPushCtrl(); break;
                case 31: ItemPushCtrl(); break;
                case 32: ItemLoopPushCtrl(); break;
                case 33: FactorPushCtrl(); break;
                case 34: CallTypePushCtrl(); break;
                case 35: CallPushCtrl(); break;
                case 36: IdPushCtrl(); break;
                case 37: VarTypePushCtrl(); break;
                case 38: NumPushCtrl(); break;
                case 39: RelopPushCtrl(); break;
                case 40: ArraySPushCtrl(); break;
                case 41: DeclareTPushCtrl(); break;
                case 42: AssignTPushCtrl(); break;
                case 43: FactorTPushCtrl(); break;
                default: DefaultDeriveNoProp();  break;
            }
        }

        #endregion

        #region Push Control & Gram Action Handler


        #region No.0 Program
        
        // 0 Program -> DeclareCluster
        private void ProgramPushCtrl()
        {
            StackPop();
            AutoPush(engageQueue.Count);
        }

        #endregion

        #region No.1 DeclareCluster

        // 1. DeclareCluster -> S1 Declare DeclareClusterLoop
        private void DeclareClusterPushCtrl()
        {
            StackPop();
            AutoPush(engageQueue.Count);
        }
        #endregion

        #region No.2 DeclareClusterLoop

        // 2. DeclareClusterLoop -> Declare DeclareClusterLoop | blank

        private void DeclareClusterLoopPushCtrl()
        {
            StackPop();
            AutoPush(engageQueue.Count);
        }
        #endregion

        #region No.3 Declare

        // 3. Declare -> void Id S1 FuncDeclare | VarType S2 Id S3 Declare ~
        private void DeclarePushCtrl()
        {
            StackPop();
            if(predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("Declare S1");
                AutoPush(1);
                stack.Push(actionS1);
                AutoPush(2);
            } else
            {
                GramAction actionS2 = CreateBindActions("Declare S2");
                GramAction actionS3 = CreateBindActions("Declare S3");
                AutoPush(1);
                stack.Push(actionS3);
                AutoPush(1);
                stack.Push(actionS2);
                AutoPush(1);
            }
        }

        public bool DeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic funcDeclareProp);
            funcDeclareProp.returnType = VarType.VOID;
            funcDeclareProp.funcName = s1Prop.name;
            return true;
        }

        public bool DeclareActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(3, out dynamic declareProp);
            declareProp.type = s2Prop.varType;
            return true;
        }

        public bool DeclareActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(1, out dynamic declareProp);
            declareProp.name = s3Prop.name;
            return true;
        }
        #endregion

        #region No.4 TypeDeclare

        // 4. TypeDeclare -> VarDeclare | ArrayDeclare

        private void TypeDeclarePushCtrl()
        {
            StackTopProp(out dynamic typeDeclareProp);
            dynamic backProp = new DynamicProperty();
            backProp.name = typeDeclareProp.name;
            backProp.type = typeDeclareProp.type;

            StackPop();
            AutoPush(backProp);
        }
        #endregion

        #region No.5 VarDeclare

        // 5. VarDeclare -> smc
        private void VarDeclarePushCtrl()
        {
            StackTopProp(out dynamic varDeclareProp);
            recordTable.CreateLocalVarRecord(varDeclareProp.name, varDeclareProp.type);

            StackPop();
            AutoPush(1);
        }
        #endregion

        #region No.6 FuncDeclare

        // 6. FuncDeclare -> lpar FormParam S1 rpar S2 StateBlock S3
        private void FuncDeclarePushCtrl()
        {
            StackTopProp(out dynamic funcDeclareProp);
            StackPop();

            dynamic stateBlockProp = new DynamicProperty();
            dynamic s2Prop = new DynamicProperty();
            stateBlockProp.returnType = funcDeclareProp.returnType;
            s2Prop.returnType = funcDeclareProp.returnType;
            s2Prop.funcName = funcDeclareProp.funcName;

            GramAction actionS1 = CreateBindActions("FuncDeclare S1");
            GramAction actionS2 = CreateBindActions("FuncDeclare S2");
            GramAction actionS3 = CreateBindActions("FuncDeclare S3");
            stack.Push(actionS3);
            AutoPush(stateBlockProp);
            stack.Push(actionS2, s2Prop);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(2);
        }

        public bool FuncDeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic s2Prop);
            s2Prop.paramDict = s1Prop.paramDict;
            return true;
        }

        public bool FuncDeclareActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(2, out dynamic s3Prop);
            Dictionary<string, VarType> paramDict = s2Prop.paramDict;
            FuncRecord func = recordTable.CreateFuncRecord(
                s2Prop.returnType, s2Prop.funcName, 
                paramDict, quadTable.NextQuadRef());
            if (func == null)
            {
                SendBackMessage("重复的函数签名定义", LogMsgItem.Type.ERROR);
                return false;
            }
            // 重要！寄存器分配延迟到目标代码阶段 完成所有变量的偏移计算后才能生成正确
            /*
             * 进入函数后先执行预定的动作，构造栈帧空间
             * subi sp, sp, frameSize
             * sd   ra, frameSize-8(sp)
             * sd   fp, frameSize-16(sp)
             * addi fp,  sp, frameSize
             */
            quadTable.GenQuad("CalleeEntry", null, null, func);
            /*
             * 然后保存 calleeSaved 且本函数后续用到的 regs
             * e.g. : sd s1, frameSize-24(sp) ... 
             */
            quadTable.GenQuad("CalleeSave", null, null, func);

            s3Prop.func = func;
            return true;
        }

        public bool FuncDeclareActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            FuncRecord func = s3Prop.func;
            // 需要确保返回值已经正确放置到 a0, a1 处
            // 函数作用域已经在 StateBlock 中退出到全局作用域
            /*
             * 先恢复 callee 保存的所有寄存器
             * e.g. : ld s1, frameSize-24(sp) ... 
             */
            quadTable.GenQuad("CalleeRestore", null, null, func);
            /*
             * 然后销毁栈帧返回原执行位置
             * ld   ra, frameSize-8(sp)
             * ld   fp, frameSize-16(sp)
             * subi sp, sp, frameSize
             */
            quadTable.GenQuad("CalleeExit", null, null, func);

            Regs ra = Compiler.regFiles.FindRegs("ra");
            Quad last = quadTable.GenQuad("jr", null, null, ra);

            func.QuadEnd = last;
            return true;
        }
        #endregion

        #region No.7 ArrayDeclare

        // 7. ArrayDeclare -> lsbrc Num S1 rsbrc ArrayDeclareLoop S2 smc
        private void ArrayDeclarePushCtrl()
        {
            StackTopProp(out dynamic arrayDeclareProp);
            StackPop();

            GramAction actionS1 = CreateBindActions("ArrayDeclare S1");
            GramAction actionS2 = CreateBindActions("ArrayDeclare S2");

            dynamic s1Prop = new DynamicProperty();
            s1Prop.dimList = new List<int>();

            dynamic s2Prop = new DynamicProperty();
            s2Prop.name = arrayDeclareProp.name;
            s2Prop.type = arrayDeclareProp.type;

            AutoPush(1);
            stack.Push(actionS2, s2Prop);
            AutoPush(2);
            stack.Push(actionS1, s1Prop);
            AutoPush(2);
        }

        public bool ArrayDeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic loopProp);
            s1Prop.dim = 1;
            dynamic num = ((ConsVarRecord)s1Prop.entry).Val;
            s1Prop.dimList.Add(num);
            loopProp.dim = s1Prop.dim;
            loopProp.dimList = s1Prop.dimList;
            return true;
        }

        public bool ArrayDeclareActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            ArrayRecord array = 
                recordTable.CreateLocalArrayRecord(
                    s2Prop.name, s2Prop.type, s2Prop.dimList);
            if (array != null) return true;
            SendBackMessage("重复的数组定义", LogMsgItem.Type.ERROR);
            return false;
        }

        #endregion

        #region No.8 ArrayDeclareLoop

        // 8. ArrayDeclareLoop -> lsbrc Num S1 rsbrc ArrayDeclareLoop S2 | blank S3
        private void ArrayDeclareLoopPushCtrl()
        {
            StackTopProp(out dynamic arrayDeclareLoopProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.dim = arrayDeclareLoopProp.dim;
            hierarchyProp.dimList = arrayDeclareLoopProp.dimList;

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("ArrayDeclareLoop S1");
                GramAction actionS2 = CreateBindActions("ArrayDeclareLoop S2");
                stack.Push(actionS2);
                AutoPush(2);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(2);
            }
            else
            {
                GramAction actionS3 = CreateBindActions("ArrayDeclareLoop S2");
                stack.Push(actionS3, hierarchyProp);
            }
        }

        public bool ArrayDeclareLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic arrayDeclareLoopProp);
            s1Prop.dim++;
            dynamic num = ((ConsVarRecord)s1Prop.entry).Val;
            s1Prop.dimList.Add(num);
            arrayDeclareLoopProp.dim = s1Prop.dim;
            arrayDeclareLoopProp.dimList = s1Prop.dimList;
            return true;
        }

        public bool ArrayDeclareLoopActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.dim = s2Prop.dim;
            backProp.dimList = s2Prop.dimList;
            return true;
        }

        #endregion

        #region No.9 FormParamPushCtrl

        // 9. FormParam -> ParamList S1 | void S2
        private void FormParamPushCtrl()
        {
            StackPop();
            GramAction actionS1 = CreateBindActions("FormParam S1");
            GramAction actionS2 = CreateBindActions("FormParam S2");

            stack.Push(predictCol == 0 ? actionS1 : actionS2);
            AutoPush(1);
        }

        public bool FormParamActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.paramDict = s1Prop.paramDict;
            return true;
        }

        public bool FormParamActionS2()
        {
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.paramDict = new Dictionary<string, VarType>();
            return true;
        }

        #endregion

        #region No.10 ParamList

        // 10. ParamList -> Param S1 ParamListLoop S2
        private void ParamListPushCtrl()
        {
            StackPop();
            GramAction actionS1 = CreateBindActions("ParamList S1");
            GramAction actionS2 = CreateBindActions("ParamList S2");

            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool ParamListActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic paramListLoopProp);
            s1Prop.paramDict = new Dictionary<string, VarType>();
            s1Prop.paramDict.Add(s1Prop.paramName, s1Prop.paramType);
            paramListLoopProp.paramDict = s1Prop.paramDict;
            return true;
        }

        public bool ParamListActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.paramDict = s2Prop.paramDict;
            return true;
        }

        #endregion

        #region No.11 ParamListLoop

        // 11. ParamListLoop -> cma Param S1 ParamListLoop S2 | blank S3
        private void ParamListLoopPushCtrl()
        {
            StackTopProp(out dynamic paramListLoopProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.paramDict = paramListLoopProp.paramDict;
            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("ParamListLoop S1");
                GramAction actionS2 = CreateBindActions("ParamListLoop S2");
                stack.Push(actionS2);
                AutoPush(1);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(2);
            }
            else
            {
                GramAction actionS3 = CreateBindActions("ParamListLoop S2");
                stack.Push(actionS3, hierarchyProp);
            }
        }

        public bool ParamListLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic paramListLoopProp);
            Dictionary<string, VarType> dict = s1Prop.paramDict;
            if (dict.ContainsKey(s1Prop.paramName))
            {
                throw new ArgumentException("重复的参数名");
            }
            dict.Add(s1Prop.paramName, s1Prop.paramType);
            paramListLoopProp.paramDict = dict;
            return true;
        }

        public bool ParamListLoopActionS2()
        {
            StackTopProp(out dynamic nowProp);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.paramDict = nowProp.paramDict;
            return true;
        }

        #endregion

        #region No.12 Param

        // 12. Param -> VarType S1 Id S2
        private void ParamPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("Param S1");
            GramAction actionS2 = CreateBindActions("Param S2");

            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool ParamActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(3, out dynamic backProp);
            backProp.paramType = s1Prop.varType;
            return true;
        }

        public bool ParamActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.paramName = s2Prop.name;
            return true;
        }
        #endregion

        #region No.13 Args

        // 13. Args -> ArgsList S1 | blank S2
        private void ArgsPushCtrl()
        {
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.argsList = new List<VarRecord>();

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("Args S1");
                stack.Push(actionS1);
                AutoPush(1);
            }
            else
            {
                GramAction actionS2 = CreateBindActions("Args S1");
                stack.Push(actionS2, hierarchyProp);
            }
        }

        public bool ArgsActionS1()
        {
            StackTopProp(out dynamic nowProp);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.argsList = nowProp.argsList;
            return true;
        }
        #endregion

        #region No.14 ArgsList

        // 14. ArgsList -> Expr S1 ArgListLoop S2
        private void ArgsListPushCtrl()
        {
            StackPop();
            GramAction actionS1 = CreateBindActions("ArgsList S1");
            GramAction actionS2 = CreateBindActions("ArgsList S2");

            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool ArgsListActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic argsListLoopProp);
            List<VarRecord> list = new() { s1Prop.entry };
            argsListLoopProp.argsList = list;
            return true;
        }

        public bool ArgsListActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.argsList = s2Prop.argsList;
            return true;
        }
        #endregion

        #region No.15 ArgListLoop

        // 15. ArgListLoop -> cma Expr S1 ArgListLoop S2 | blank S3
        private void ArgListLoopPushCtrl()
        {
            StackTopProp(out dynamic argListProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.argsList =argListProp.argsList;
            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("ArgListLoop S1");
                GramAction actionS2 = CreateBindActions("ArgListLoop S2");
                stack.Push(actionS2);
                AutoPush(1);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(2);
            }
            else
            {
                GramAction actionS3 = CreateBindActions("ArgListLoop S2");
                stack.Push(actionS3, hierarchyProp);
            }
        }

        public bool ArgListLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic argsListLoopProp);
            VarRecord data = s1Prop.entry;
            List<VarRecord> list = s1Prop.argsList;
            list.Add(data);
            argsListLoopProp.argsList = list;
            return true;
        }

        public bool ArgListLoopActionS2()
        {
            StackTopProp(out dynamic nowProp);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.argsList = nowProp.argsList;
            return true;
        }

        #endregion

        #region No.16 StateBlock

        // 16. StateBlock -> S1 lbrc InnerDeclare StateCluster rbrc S2
        private void StateBlockPushCtrl()
        {
            StackTopProp(out dynamic stateBlockProp);
            StackPop();

            GramAction actionS1 = CreateBindActions("StateBlock S1");
            GramAction actionS2 = CreateBindActions("StateBlock S2");
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.returnType = stateBlockProp.returnType;

            stack.Push(actionS2);
            AutoPush(1);
            AutoPush(hierarchyProp);
            AutoPush(2);
            stack.Push(actionS1);
        }

        public bool StateBlockActionS1()
        {
            recordTable.EnterScope();
            return true;
        }

        public bool StateBlockActionS2()
        {
            recordTable.LeaveScope();
            return true;
        }

        #endregion

        #region No.17 InnerDeclare

        // 17. InnerDeclare -> blank | InnerVarDeclare InnerDeclare
        private void InnerDeclarePushCtrl()
        {
            StackPop();

            if (predictCol == 1)
            {
                AutoPush(2);
            }
        }
        #endregion

        #region No.18 InnerVarDeclare

        // 18. InnerVarDeclare -> VarType S1 Id S2 TypeDeclare
        private void InnerVarDeclarePushCtrl()
        {
            StackPop();
            GramAction actionS1 = CreateBindActions("InnerVarDeclare S1");
            GramAction actionS2 = CreateBindActions("InnerVarDeclare S2");

            AutoPush(1);
            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool InnerVarDeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(3, out dynamic typeDeclareProp);
            typeDeclareProp.type = s1Prop.varType;
            return true;
        }

        public bool InnerVarDeclareActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic typeDeclareProp);
            typeDeclareProp.name = s2Prop.name;
            return true;
        }

        #endregion

        #region No.19 StateCluster

        // 19. StateCluster -> blank | State StateCluster
        private void StateClusterPushCtrl()
        {
            StackTopProp(out dynamic stateClusterProp);
            StackPop();

            if (predictCol == 0) { return; }

            dynamic stateProp = new DynamicProperty();
            stateProp.returnType = stateClusterProp.returnType;
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.returnType = stateClusterProp.returnType;

            // 注意可能存在的引用失效
            AutoPush(hierarchyProp);
            AutoPush(stateProp);
        }
        #endregion

        #region No.20 State

        // 20. State -> If | While | Return | Assign
        private void StatePushCtrl()
        {
            StackTopProp(out dynamic stateProp);
            StackPop();
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.returnType = stateProp.returnType;
            if (predictCol == 3)
            {
                AutoPush(1);
            }
            else
            {
                AutoPush(hierarchyProp);
            }
        }
        #endregion

        #region No.21 Assign

        // 21. Assign -> id S1 Assign~
        private void AssignPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("Assign S1");

            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool AssignActionS1()
        {
            stack.RelativeFetch(1, out dynamic assignProp);
            if (inputNow.type != LexUnit.Type.ID)
            {
                throw new ArgumentException("变量标识符无法识别");
            }
            assignProp.name =inputNow.cont;
            return true;
        }
        #endregion

        #region No.22 Return

        // 22. Return -> return ReturnAlter S1 smc
        private void ReturnPushCtrl()
        {
            StackTopProp(out dynamic returnProp);
            StackPop();

            GramAction actionS1 = CreateBindActions("Return S1");

            dynamic s1Prop = new DynamicProperty();
            s1Prop.returnType = returnProp.returnType;

            AutoPush(1);
            stack.Push(actionS1, s1Prop);
            AutoPush(2);
        }

        public bool ReturnActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            if (s1Prop.returnType != s1Prop.type)
            {
                throw new ArgumentOutOfRangeException($"不符合定义的返回值类型");
            }
            if (s1Prop.type == VarType.VOID) { return true; }
            VarRecord data = s1Prop.entry;
            if (data.Type == VarType.VOID) { return true; }

            quadTable.GenQuad("mv", data, null, Compiler.regFiles.FindRegs("a0"));
            return true;
        }

        #endregion

        #region No.23 ReturnAlter

        // 23. ReturnAlter -> Expr S1 | blank S2
        private void ReturnAlterPushCtrl()
        {
            StackPop();

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("ReturnAlter S1");
                stack.Push(actionS1);
                AutoPush(1);
            }
            else
            {
                GramAction actionS2 = CreateBindActions("ReturnAlter S2");
                stack.Push(actionS2);
            }
        }

        public bool ReturnAlterActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            VarRecord data = s1Prop.entry;
            backProp.entry = data;
            backProp.type = data.Type;
            return true;
        }

        public bool ReturnAlterActionS2()
        {
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.type = VarType.VOID;
            return true;
        }
        #endregion

        #region No.24 While

        // 24. While -> while lpar S1 Expr S2 rpar StateBlock S3
        private void WhilePushCtrl()
        {
            StackTopProp(out dynamic whileProp);
            StackPop();

            dynamic stateBlockProp = new DynamicProperty();
            stateBlockProp.returnType = whileProp.returnType;

            GramAction actionS1 = CreateBindActions("While S1");
            GramAction actionS2 = CreateBindActions("While S2");
            GramAction actionS3 = CreateBindActions("While S3");

            stack.Push(actionS3);
            AutoPush(stateBlockProp);
            AutoPush(1);
            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(2);
        }

        public bool WhileActionS1()
        {
            stack.RelativeFetch(5, out dynamic s3Prop);

            Quad quad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(quad);
            s3Prop.loopQuad = quad;
            return true;
        }

        public bool WhileActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(3, out dynamic s3Prop);

            List<Quad> trueList = new() { quadTable.NextQuadRef() };
            VarRecord data = s2Prop.entry;
            quadTable.GenQuad("bnez", data, null, null);

            List<Quad> falseList = new() { quadTable.NextQuadRef() };
            quadTable.GenQuad("j", null, null, null);

            s3Prop.trueList = trueList;
            s3Prop.falseList = falseList;

            Quad quad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(quad);
            s3Prop.trueQuad = quad;
            return true;
        }

        public bool WhileActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            Quad loopQuad = s3Prop.loopQuad;
            quadTable.GenQuad("j", null, null, loopQuad.Label);

            Quad falseQuad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(falseQuad);

            BackPatch(s3Prop.trueList, s3Prop.trueQuad);
            BackPatch(s3Prop.falseList, falseQuad);
            return true;
        }  
        #endregion

        #region No.25 If

        // 25. If -> if lpar Expr S1 rpar StateBlock IfAlter 
        private void IfPushCtrl()
        {
            StackTopProp(out dynamic ifProp);
            StackPop();

            dynamic stateBlockProp = new DynamicProperty();
            dynamic ifAlterProp = new DynamicProperty();
            stateBlockProp.returnType = ifProp.returnType;
            ifAlterProp.returnType = ifProp.returnType;

            GramAction actionS1 = CreateBindActions("If S1");

            AutoPush(ifAlterProp);
            AutoPush(stateBlockProp);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(3);
        }

        public bool IfActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(3, out dynamic ifAlterProp);

            VarRecord data = s1Prop.entry;
            List<Quad> trueList = new() { quadTable.NextQuadRef() };
            quadTable.GenQuad("bnez", data, null, null);

            List<Quad> falseList = new() { quadTable.NextQuadRef() };
            quadTable.GenQuad("j", null, null, null);

            Quad trueQuad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(trueQuad);

            BackPatch(trueList, trueQuad);

            ifAlterProp.falseList = falseList;
            return true;
        }

        #endregion

        #region No.26 IfAlter

        // 26. IfAlter -> else S1 StateBlock S2 | blank S3
        private void IfAlterPushCtrl()
        {
            StackTopProp(out dynamic ifAlterProp);
            StackPop();
            
            dynamic stateBlockProp = new DynamicProperty();
            stateBlockProp.returnType = ifAlterProp.returnType;
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.falseList = ifAlterProp.falseList;

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("IfAlter S1");
                GramAction actionS2 = CreateBindActions("IfAlter S2");
                stack.Push(actionS2);
                AutoPush(stateBlockProp);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(1);
            }
            else
            {
                GramAction actionS3 = CreateBindActions("IfAlter S3");
                stack.Push(actionS3, hierarchyProp);
            }
        }

        public bool IfAlterActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic s2Prop);

            List<Quad> ifNextList = new() { quadTable.NextQuadRef() };
            s2Prop.ifNextList = ifNextList;

            quadTable.GenQuad("j", null, null, null);
            Quad falseQuad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(falseQuad);

            List<Quad> falseList = s1Prop.falseList;
            BackPatch(falseList, falseQuad);
            return true;
        }

        public bool IfAlterActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            Quad endQuad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(endQuad);
            List<Quad> ifNextList = s2Prop.ifNextList;
            BackPatch(ifNextList, endQuad);
            return true;
        }

        public bool IfAlterActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            Quad falseQuad = quadTable.NextQuadRef();
            List<Quad> falseList = s3Prop.falseList;
            BackPatch(falseList, falseQuad);
            return true;
        }

        #endregion

        #region No.27 Expr 

        // 27. Expr -> AddExpr S1 ExprLoop S2
        private void ExprPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("Expr S1");
            GramAction actionS2 = CreateBindActions("Expr S2");

            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool ExprActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic exprLoopProp);

            exprLoopProp.lhsEntry = s1Prop.entry;
            return true;
        }

        public bool ExprActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);

            backProp.entry = s2Prop.entry;
            return true;
        }
        #endregion

        #region No.28 ExprLoop

        // 28. ExprLoop -> Relop S1 AddExpr S2 ExprLoop S3 | blank S4
        private void ExprLoopPushCtrl()
        {
            StackTopProp(out dynamic exprLoopProp);
            StackPop();
            
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.lhsEntry = exprLoopProp.lhsEntry;

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("ExprLoop S1");
                GramAction actionS2 = CreateBindActions("ExprLoop S2");
                GramAction actionS3 = CreateBindActions("ExprLoop S3");
                stack.Push(actionS3);
                AutoPush(1);
                stack.Push(actionS2, hierarchyProp);
                AutoPush(1);
                stack.Push(actionS1);
                AutoPush(1);
            }
            else
            {
                GramAction actionS4 = CreateBindActions("ExprLoop S4");
                stack.Push(actionS4, hierarchyProp);
            }
        }

        public bool ExprLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic s2Prop);
            s2Prop.op = s1Prop.op;
            return true;
        }

        public bool ExprLoopActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic exprLoopProp);
            string op = s2Prop.op;
            VarRecord lhsEntry = s2Prop.lhsEntry;
            VarRecord rhsEntry = s2Prop.entry;

            ImmRecord immTrue = new("true", 1);
            ImmRecord immFalse = new("false", 0);
             
            ItrDataType(ref lhsEntry, ref  rhsEntry);
            VarRecord resultEntry = recordTable.CreateTempVarRecord(VarType.BOOL);
            Regs zero = Compiler.regFiles.FindRegs("zero");
            Quad opQuad = GenRelopQuad(op, lhsEntry, rhsEntry, out bool jumpTrue);
            quadTable.GenQuad("addi", zero,jumpTrue ? immFalse : immTrue,  resultEntry);
            Quad jumpEndQuad = quadTable.GenQuad("j", null, null, null);
            Quad trueQuad = quadTable.GenQuad("addi", zero, jumpTrue ? immTrue : immFalse, resultEntry);

            recordTable.CreateTmpLabelRecord(trueQuad);
            Quad endQuad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(endQuad);

            opQuad.Dst = trueQuad.Label;
            jumpEndQuad.Dst = endQuad.Label;

            exprLoopProp.lhsEntry = resultEntry;
            return true;
        }

        public bool ExprLoopActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s3Prop.entry;
            return true;
        }

        public bool ExprLoopActionS4()
        {
            StackTopProp(out dynamic s4Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s4Prop.lhsEntry;
            return true;
        }



        private Quad GenRelopQuad(string op, IRecord lhs, IRecord rhs, out  bool jumpTrue)
        {
            switch (op)
            {
                case "eq":
                    jumpTrue = true;
                    return quadTable.GenQuad("beq", lhs, rhs, null);
                case "neq":
                    jumpTrue = true;
                    return quadTable.GenQuad("bne", lhs, rhs, null);
                case "leq":
                    jumpTrue = false;
                    return quadTable.GenQuad("blt", rhs, lhs, null);
                case "geq": 
                    jumpTrue = true;
                    return quadTable.GenQuad("bge", lhs, rhs, null);
                case "gre": 
                    jumpTrue = false;
                    return quadTable.GenQuad("bge", rhs, lhs, null);
                case "les": 
                    jumpTrue = true;
                    return quadTable.GenQuad("blt", lhs, rhs, null);
                default:
                    throw new ArgumentException("无法识别关系操作符");
            }
        }
        #endregion

        #region No.29 AddExpr

        // 29. AddExpr -> Item S1 AddExprLoop S2
        private void AddExprPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("AddExpr S1");
            GramAction actionS2 = CreateBindActions("AddExpr S2");

            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool AddExprActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic addExprLoopProp);
            addExprLoopProp.lhsEntry = s1Prop.entry;
            return true;
        }


        public bool AddExprActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s2Prop.entry;
            return true;
        }
        #endregion

        #region No.30 AddExprLoop

        // 30. AddExprLoop -> plus Item S1 AddExprLoop S2 | sub Item S1 AddExprLoop S2 | blank S3
        private void AddExprLoopPushCtrl()
        {
            StackTopProp(out dynamic addExprLoopProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.lhsEntry = addExprLoopProp.lhsEntry;

            switch (predictCol)
            {
                case > 1:
                {
                    GramAction actionS5 = CreateBindActions("AddExprLoop S3");
                    stack.Push(actionS5, hierarchyProp);
                    return;
                }
                case 0:
                    hierarchyProp.op = "plus";
                    break;
                case 1:
                    hierarchyProp.op = "sub";
                    break;
            }

            GramAction actionS1 = CreateBindActions("AddExprLoop S1");
            GramAction actionS2 = CreateBindActions("AddExprLoop S2");
            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1, hierarchyProp);
            AutoPush(2);
        }

        public bool AddExprLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic addExprLoopProp);
            VarRecord lhsEntry = s1Prop.lhsEntry;
            VarRecord rhsEntry = s1Prop.entry;
            string op = s1Prop.op;

            ItrDataType(ref lhsEntry, ref rhsEntry);

            VarRecord resultEntry = recordTable.CreateTempVarRecord(lhsEntry.Type);
            if (op == "plus")
            {
                op = "add";
            }

            quadTable.GenQuad(op, lhsEntry, rhsEntry, resultEntry);
            addExprLoopProp.lhsEntry = resultEntry;
            return true;
        }

        public bool AddExprLoopActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s2Prop.entry;
            return true;
        }

        public bool AddExprLoopActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s3Prop.lhsEntry;
            return true;
        }
        #endregion

        #region No.31 Item

        // 31. Item -> Factor S1 ItemLoop S2
        private void ItemPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("Item S1");
            GramAction actionS2 = CreateBindActions("Item S2");

            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool ItemActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic itemLoopProp);
            itemLoopProp.lhsEntry = s1Prop.entry;
            return true;
        }

        public bool ItemActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s2Prop.entry;
            return true;
        }
        #endregion

        #region No.32 ItemLoop

        // 32. ItemLoop -> mul Factor S1 ItemLoop S2 | div Factor S1 ItemLoop S2 | blank S3
        private void ItemLoopPushCtrl()
        {
            StackTopProp(out dynamic itemLoopProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.lhsEntry = itemLoopProp.lhsEntry;

            switch (predictCol)
            {
                case > 1:
                {
                    GramAction actionS5 = CreateBindActions("ItemLoop S3");
                    stack.Push(actionS5, hierarchyProp);
                    return;
                }
                case 0:
                    hierarchyProp.op = "mul";
                    break;
                case 1:
                    hierarchyProp.op = "div";
                    break;
            }

            GramAction actionS1 = CreateBindActions("ItemLoop S1");
            GramAction actionS2 = CreateBindActions("ItemLoop S2");
            stack.Push(actionS2);
            AutoPush(1);
            stack.Push(actionS1, hierarchyProp);
            AutoPush(2);
        }

        public bool ItemLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic itemLoopProp);
            VarRecord lhsEntry = s1Prop.lhsEntry;
            VarRecord rhsEntry = s1Prop.entry;
            string op = s1Prop.op;

            ItrDataType(ref lhsEntry, ref rhsEntry);
            VarRecord resultEntry = recordTable.CreateTempVarRecord(lhsEntry.Type);

            quadTable.GenQuad(op, lhsEntry, rhsEntry, resultEntry);
            itemLoopProp.lhsEntry = resultEntry;
            return true;
        }

        public bool ItemLoopActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s2Prop.entry;
            return true;
        }

        public bool ItemLoopActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s3Prop.lhsEntry;
            return true;
        }
        #endregion

        #region No.33 Factor

        // 33. Factor -> Num S1 | str | ch | true | false S2 | lpar Expr S3 rpar | id S4 Factor~ S5
        private void FactorPushCtrl()
        {
            StackPop();

            switch (predictCol)
            {
                case 0:
                {
                    GramAction actionS1 = CreateBindActions("Factor S1");
                    stack.Push(actionS1);
                    AutoPush(1);
                    break;
                }
                case 5:
                {
                    GramAction actionS3 = CreateBindActions("Factor S3");
                    AutoPush(1);
                    stack.Push(actionS3);
                    AutoPush(2);
                    break;
                }
                case 6:
                {
                    GramAction actionS4 = CreateBindActions("Factor S4");
                    GramAction actionS5 = CreateBindActions("Factor S5");
                    stack.Push(actionS5);
                    AutoPush(1);
                    stack.Push(actionS4);
                    AutoPush(1);
                    break;
                }
                default:
                {
                    GramAction actionS4 = CreateBindActions("Factor S2");
                    stack.Push(actionS4);
                    AutoPush(1);
                    break;
                }
            }
        }

        public bool FactorActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s1Prop.entry;
            return true;
        }

        public bool FactorActionS2()
        {
            stack.RelativeFetch(1, out dynamic backProp);
            VarType type = RecognizeInputType();
            VarRecord constant;
            if (type == VarType.CHAR && inputNow.cont.Length > 1)
            {
                // constant = recordTable.CreateConsArrayRecord(VarType.CHAR, inputNow.cont);
                throw new ArgumentException("不支持数组直接赋值");
            }
            else
            {
                constant = recordTable.CreateConsRecord(type, inputNow.cont);
            }
            backProp.entry = constant;
            return true;
        }

        private VarType RecognizeInputType()
        {
            switch (inputNow.type)
            {
                case LexUnit.Type.CH:
                case LexUnit.Type.STR:
                    return VarType.CHAR;
                case LexUnit.Type.KEYWORD:
                    if (inputNow.name is "true" or "false")
                    {
                        return VarType.BOOL;
                    }
                    throw new ArgumentException("无法识别的因子");
                default:
                    throw new ArgumentException("无法识别的因子");
            }
        }

        public bool FactorActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(2, out dynamic backProp);
            backProp.entry = s3Prop.entry;
            return true;
        }

        public bool FactorActionS4()
        {
            stack.RelativeFetch(1, out dynamic factorProp);
            if (inputNow.type != LexUnit.Type.ID)
            {
                throw new ArgumentException("变量标识符无法识别");
            }
            factorProp.name = inputNow.cont;
            return true;
        }

        public bool FactorActionS5()
        {
            StackTopProp(out dynamic s4Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s4Prop.entry;
            return true;
        }
        #endregion

        #region No.34 CallType

        // 34. CallType -> Call S1 | blank S2
        private void CallTypePushCtrl()
        {
            StackTopProp(out dynamic callTypeProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.name = callTypeProp.name;

            GramAction actionS1 = CreateBindActions("CallType S1");
            GramAction actionS2 = CreateBindActions("CallType S2");

            if (predictCol == 0)
            {
                stack.Push(actionS1);
                AutoPush(hierarchyProp);
            }
            else
            {
                stack.Push(actionS2, hierarchyProp);
            }
        }

        // 函数调用
        public bool CallTypeActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s1Prop.entry;
            return true;
        }

        // 变量引用
        public bool CallTypeActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            VarRecord entry = FilterFindVarRecord(s2Prop.name);
            backProp.entry = entry;
            return true;
        }


        #endregion

        #region No.35 Call

        // 35. Call -> lpar Args S1 rpar
        private void CallPushCtrl()
        {
            StackTopProp(out dynamic callProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.name = callProp.name;

            GramAction actionS1 = CreateBindActions("Call S1");

            AutoPush(1);
            stack.Push(actionS1, hierarchyProp);
            AutoPush(2);
        }

        public bool CallActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic backProp);
            List<VarRecord> argsList = s1Prop.argsList;
            string funcName = s1Prop.name;

            FuncRecord func = recordTable.FindFuncRecord(funcName, argsList);
            if (func == null)
            {
                throw new ArgumentException("未定义的函数引用");
            }

            CallRecord callRecord = recordTable.CreateCallRecord(recordTable.GetFuncRecord(), func);
            callRecord.ArgsList = argsList;
            callRecord.Name = callRecord.ToString();

            Regs ra = Compiler.regFiles.FindRegs("ra");

            /* 调用入口
             * subi sp, sp, callSize
             */
            quadTable.GenQuad("CallerEntry", null, null, callRecord);
            /* 保存 Caller 寄存器
             * e.g. sd a2,  callSize-8(sp) ...
             */
            quadTable.GenQuad("CallerSave", null, null, callRecord);
            /* 移动实参到指定位置
             * 八个以内 e.g. l { b | h | w | d }  a0, arg0.offset(fp) ...
             * 超出 e.g.
             * l { b | h | w | d } gp, arg8.offset(fp) 
             * s { b | h | w | d } gp, callSize - param8.offset(sp)
             */
            // todo 需要将 Args 展开显式传入 以便计算活跃信息
            quadTable.GenQuad("CallerArgs", null, null, callRecord);
            /* 实际调用
             * e.g. jal ra, offset
             */
            quadTable.GenQuad("jal",ra , null, callRecord.Callee.Label);

            /* 恢复 Caller 寄存器
             * e.g. ld a2, callSize-8(sp)
             */
            quadTable.GenQuad("CallerRestore", null, null, callRecord);
            /* 释放调用入口
             * addi sp, sp, callSize
             */
            quadTable.GenQuad("CallerExit", null, null, callRecord);

            Regs reg = Compiler.regFiles.FindRegs("a0");

            VarTempRecord record = recordTable.CreateTempVarRecord(func.ReturnType);
            record.Pos = RecordPos.REG;
            record.Reg = Compiler.regFiles.FindRegs("a0");

            quadTable.GenQuad("mv", reg, null, record);

            backProp.entry = record;
            return true;
        }
        #endregion

        #region No.36 Id

        // 36. Id -> id S1
        private void IdPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("Id S1");

            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool IdActionS1()
        {
            stack.RelativeFetch(1, out dynamic backProp);
            if (inputNow.type != LexUnit.Type.ID)
            {
                throw new ArgumentException("无法识别的标识符");
            }
            backProp.name = inputNow.cont;
            return true;
        }
        #endregion

        #region No.37 VarType

        // 37. VarType -> int | long | bool | char S1
        private void VarTypePushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("VarType S1");

            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool VarTypeActionS1()
        {
            stack.RelativeFetch(1, out dynamic backProp);
            VarType varType = ParseInputType();
            backProp.varType = varType;
            return true;
        }

        private VarType ParseInputType()
        {
            LexUnit.Type lexType = inputNow.type;
            switch (lexType) {
                case LexUnit.Type.KEYWORD:
                    VarType varType = inputNow.cont switch
                    {
                        "int" => VarType.INT,
                        "long" => VarType.LONG,
                        "bool" => VarType.BOOL,
                        "char" => VarType.CHAR,
                        _ => throw new ArgumentOutOfRangeException($"不支持的变量类型")
                    };

                    return varType;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region No.38 Num

        // 38. Num -> integer S1
        private void NumPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("Num S1");

            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool NumActionS1()
        {
            stack.RelativeFetch(1, out dynamic backProp);
            LexUnit.Type lexType = inputNow.type;
            if (lexType != LexUnit.Type.INT)
            {
                throw new ArgumentOutOfRangeException($"不支持的数字常量类型");
            }

            long longNum = long.Parse(inputNow.cont);
            VarRecord varRecord;
            if (longNum is <= int.MaxValue and >= int.MinValue)
            {
                varRecord = recordTable.CreateConsRecord(VarType.INT, inputNow.cont);
            }
            else
            {
                varRecord = recordTable.CreateConsRecord(VarType.LONG, inputNow.cont);
            }

            backProp.entry = varRecord;
            return true;
        }

        #endregion

        #region No.39 Relop

        // 39. Relop -> eq | neq | leq | geq | gre | les S1
        private void RelopPushCtrl()
        {
            StackPop();

            GramAction actionS1 = CreateBindActions("Relop S1");

            stack.Push(actionS1);
            AutoPush(1);
        }

        public bool RelopActionS1()
        {
            stack.RelativeFetch(1, out dynamic backProp);
            LexUnit.Type lexType = inputNow.type;
            if (lexType != LexUnit.Type.OP)
            {
                throw new ArgumentOutOfRangeException($"不支持的操作符类型");
            }

            backProp.op = inputNow.name;
            return true;
        }
        #endregion

        #region No.40 Array'

        // 40. Array' -> blank S1 | lsbrc Expr S2 rsbrc Array'  S3
        private void ArraySPushCtrl()
        {
            StackTopProp(out dynamic arraySProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.name = arraySProp.name;
            hierarchyProp.indexList = arraySProp.indexList;
            hierarchyProp.isRef = arraySProp.isRef;

            dynamic refProp = new DynamicProperty();
            refProp.isRef = arraySProp.isRef;

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("ArrayS S1");
                stack.Push(actionS1, hierarchyProp);
            } else if (predictCol == 1)
            {
                GramAction actionS2 = CreateBindActions("ArrayS S2");
                GramAction actionS3 = CreateBindActions("ArrayS S3");
                stack.Push(actionS3, refProp);
                AutoPush(2);
                stack.Push(actionS2, hierarchyProp);
                AutoPush(2);
            }
        }

        public bool ArraySActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            bool isRef = s1Prop.isRef;

            string name = s1Prop.name;
            List<VarRecord> indexList = s1Prop.indexList;
            ArrayRecord arrayRecord = FilterFindArrayRecord(name);
            VarRecord dstRecord = CalcuArrayIndex(indexList, arrayRecord);
            ArrayRefRecord arrayRef = new (arrayRecord, dstRecord);
            if (isRef)
            {
                dstRecord = GetArrayElem(arrayRef);
                backProp.entry = dstRecord;
            }
            else
            {
                backProp.arrayRef = arrayRef;
            }
            return true;
        }

        private VarRecord CalcuArrayIndex(IReadOnlyList<VarRecord> indexList, ArrayRecord arrayRecord)
        {
            if (indexList.Count != arrayRecord.Dim)
            {
                throw new ArgumentException("不支持数组指针");
            }

            VarTempRecord dstVar = recordTable.CreateTempVarRecord(VarType.LONG);
            VarTempRecord indexVar = recordTable.CreateTempVarRecord(VarType.LONG);
            
            Regs zero = Compiler.regFiles.FindRegs("zero");

            if (arrayRecord.Dim == 1)
            {
                ImmRecord index = new ("index", 0);
                quadTable.GenQuad("addi", indexList[0], index, dstVar);
            }
            else
            {
                // todo 修正 dstVar 无意义加法
                quadTable.GenQuad("add", zero, dstVar, dstVar);
                for (int i = 0; i < indexList.Count - 1; i++)
                {
                    // 需要支持大下标数组 那么需要将立即数拆分为 20bit 与 12 bit 的 16 进制数
                    // lui indexVar, 0x12345
                    // addi indexVar, 0x123
                    
                    LargeIndexLoadQuadGen(arrayRecord.GetDimLen(i + 1),  indexVar);
                    quadTable.GenQuad("mul", i == 0 ? indexList[0] : dstVar, indexVar, dstVar);
                    quadTable.GenQuad("add", dstVar, indexList[i + 1], dstVar);
                }
            }
            ImmRecord width = new ("width", arrayRecord.Width);
            quadTable.GenQuad("addi", zero, width, indexVar);
            quadTable.GenQuad("mul", dstVar, indexVar, dstVar);
            UpdateArrayPtrOffset(arrayRecord, dstVar);
            return dstVar;
        }

        private void LargeIndexLoadQuadGen(int index, VarRecord indexVar )
        {
            Regs zero = Compiler.regFiles.FindRegs("zero");
            if (index is < 2048 and >= -2048)
            {
                ImmRecord immRecord = new ("index", index);
                quadTable.GenQuad("addi", zero, immRecord, indexVar);
            }
            else
            {
                int hi = (index >> 12) & 0x000F_FFFF;
                int lo = index & 0x0000_0FFF;
                ImmRecord hiRecord = new("indexHi", "0x" + hi.ToString("X"));
                ImmRecord loRecord = new("indexLo", "0x" + lo.ToString("X"));
                quadTable.GenQuad("lui", hiRecord, null, indexVar);
                quadTable.GenQuad("addi", indexVar, loRecord, indexVar);
            }
        }

        private void UpdateArrayPtrOffset(ArrayRecord arrayRecord,  VarRecord dstRecord)
        {
            // 栈内数组
            /* dstRec 修正元素偏移量
             * add dstRec, dstRec, fp
             * add dstRec, dstRec, arrayRec.offset
             */
            if(!(recordTable.GetGlobalTable().ContainsRecord(arrayRecord)))
            {
                quadTable.GenQuad("add", dstRecord, Compiler.regFiles.FindRegs("fp"), dstRecord);
                quadTable.GenQuad("ArrayOffset", arrayRecord, null , dstRecord);
            }
            // 全局数组
            else
            {
                /*
                 * lui tp, %hi(arr)
                 * addi tp, %lo(arr)
                 * add dstRec, tp, dstRec
                 */
                Regs addrReg = Compiler.regFiles.FindRegs("tp");
                quadTable.GenQuad("LoadAddr", arrayRecord, null, addrReg);
                quadTable.GenQuad("add", dstRecord, addrReg, dstRecord);
            }
        }

        private VarRecord GetArrayElem(ArrayRefRecord arrayRef)
        {
            /* 
             * mv srcVar, (dstRec)
             */
            VarTempRecord srcVar = recordTable.CreateTempVarRecord(arrayRef.RefArray.Type);
            // 需要传递类型信息 
            TypeRecord typeRecord = new(arrayRef.RefArray.Type);
            quadTable.GenQuad("Load", arrayRef.RefIndex, typeRecord , srcVar);
            return srcVar;
        }

        public bool ArraySActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(2, out dynamic arraySProp);

            arraySProp.name = s2Prop.name;
            List<VarRecord> indexList = s2Prop.indexList;
            VarRecord entry = s2Prop.entry;
            indexList.Add(entry);
            arraySProp.indexList = indexList;
            arraySProp.isRef = s2Prop.isRef;
            return true;
        }

        public bool ArraySActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            bool isRef = s3Prop.isRef;
            if (isRef)
            {
                backProp.entry = s3Prop.entry;
            }
            else
            {
                backProp.arrayRef = s3Prop.arrayRef;
            }
            return true;
        }
        #endregion

        #region No.41 Declare~

        // 41. Declare ~ -> TypeDeclare | FuncDeclare
        private void DeclareTPushCtrl()
        {
            StackTopProp(out dynamic declareProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            if (predictCol == 0)
            {
                hierarchyProp.name = declareProp.name;
                hierarchyProp.type = declareProp.type;
            } else if (predictCol == 1)
            {
                hierarchyProp.funcName = declareProp.name;
                hierarchyProp.returnType = declareProp.type;
            }
            AutoPush(hierarchyProp);
        }
        #endregion

        #region No.42 Assign~

        // 42. Assign~ -> assign Expr S1 smc | lsbrc Expr S2 rsbrc Array' S3 assign Expr S4 smc 
        private void AssignTPushCtrl()
        {
            StackTopProp(out dynamic assignTProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.name = assignTProp.name;

            dynamic arraySProp = new DynamicProperty();
            arraySProp.isRef = false;

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("AssignT S1");
                AutoPush(1);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(2);
            }
            else if(predictCol == 1)
            {
                GramAction actionS2 = CreateBindActions("AssignT S2");
                GramAction actionS3 = CreateBindActions("AssignT S3");
                GramAction actionS4 = CreateBindActions("AssignT S4");
                AutoPush(1);
                stack.Push(actionS4);
                AutoPush(2);
                stack.Push(actionS3);
                AutoPush(arraySProp);
                AutoPush(1);
                stack.Push(actionS2, hierarchyProp);
                AutoPush(2);
            }
        }

        public bool AssignTActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            string name = s1Prop.name;
            VarRecord lhsRecord = FilterFindVarRecord(name);
            VarRecord rhsRecord = s1Prop.entry;
            ItrDataType(ref lhsRecord, ref rhsRecord);
            quadTable.GenQuad("mv", rhsRecord, null, lhsRecord);
            return true;
        }

        public bool AssignTActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(2, out dynamic arraySProp);
            VarRecord var = s2Prop.entry;
            List<VarRecord> indexList = new() { var };
            arraySProp.name = s2Prop.name;
            arraySProp.indexList = indexList;
            return true;
        }

        public bool AssignTActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(3, out dynamic s4Prop);
            s4Prop.lhsEntry = s3Prop.arrayRef;
            return true;
        }

        public bool AssignTActionS4()
        {
            StackTopProp(out dynamic s4Prop);
            ArrayRefRecord arrayRecord = s4Prop.lhsEntry;
            VarRecord rhsRecord = s4Prop.entry;
            ItrDataType(ref rhsRecord, arrayRecord.RefArray.Type);
            /* 
             * 随后使用
             * mv (arrayRecord.refIndex), rhsRecord 
             */
            TypeRecord typeRecord = new(arrayRecord.RefArray.Type);
            quadTable.GenQuad("Store", rhsRecord, typeRecord, arrayRecord.RefIndex);
            return true;
        }
        #endregion

        #region No.43 Factor~

        // 43. Factor ~ -> CallType S1 | lsbrc Expr S2 rsbrc Array' S3
        private void FactorTPushCtrl()
        {
            StackTopProp(out dynamic factorTProp);
            StackPop();

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.name = factorTProp.name;

            if (predictCol == 0)
            {
                GramAction actionS1 = CreateBindActions("FactorT S1");
                stack.Push(actionS1);
                AutoPush(hierarchyProp);
            } else if (predictCol == 1)
            {
                GramAction actionS2 = CreateBindActions("FactorT S2");
                GramAction actionS3 = CreateBindActions("FactorT S3");
                stack.Push(actionS3);
                AutoPush(hierarchyProp);
                AutoPush(1);
                stack.Push(actionS2);
                AutoPush(2);
            }
        }

        public bool FactorTActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s1Prop.entry;
            return true;
        }

        public bool FactorTActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(2, out dynamic arraySProp);
            VarRecord var = s2Prop.entry;
            List<VarRecord> indexList = new() { var };
            arraySProp.indexList = indexList;
            arraySProp.isRef = true;
            return true;
        }

        public bool FactorTActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.entry = s3Prop.entry;
            return true;
        }
        #endregion
        
        #endregion
    }
}
