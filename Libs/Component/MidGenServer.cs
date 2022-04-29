﻿using CLikeCompiler.Libs.Enum;
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
using CLikeCompiler.Libs.Unit.Quad;
using CLikeCompiler.Libs.Unit.Reg;

// ReSharper disable UnusedMember.Local

namespace CLikeCompiler.Libs.Component
{
    internal class MidGenServer
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

        internal void StackTopProp(out dynamic prop)
        {
            stack.Top(out _, out prop);
        }

        private void StackPop()
        {
            stack.Pop();
        }

        private void AutoPush(int cnt)
        {
            if(cnt >= engageQueue.Count) { return; }
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
            Delegate del =  Delegate.CreateDelegate(typeof(GramAction.ActionHandler), method);
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

        private bool DeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic funcDeclareProp);
            funcDeclareProp.returnType = VarType.VOID;
            funcDeclareProp.funcName = s1Prop.name;
            return true;
        }

        private bool DeclareActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(3, out dynamic declareProp);
            declareProp.type = s2Prop.varType;
            return true;
        }

        private bool DeclareActionS3()
        {
            StackTopProp(out dynamic s3Prop);
            stack.RelativeFetch(1, out dynamic declareProp);
            declareProp.type = s3Prop.varType;
            return true;
        }
        #endregion

        #region No.4 TypeDeclare

        // 4. TypeDeclare -> VarDeclare | ArrayDeclare

        private void TypeDeclarePushCtrl()
        {
            StackTopProp(out dynamic typeDeclareProp);
            dynamic backProp = DynamicProperty.CreateByDynamic(typeDeclareProp);

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
        // TODO 记录函数四元式开始与结束位置
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

        private bool FuncDeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic s2Prop);
            s2Prop.paramDict = s1Prop.paramDict;
            return true;
        }

        private bool FuncDeclareActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(2, out dynamic s3Prop);
            FuncRecord func = recordTable.CreateFuncRecord(
                s2Prop.returnType, s2Prop.funcName, 
                s2Prop.paramDict, quadTable.NextQuadRef());
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

        private bool FuncDeclareActionS3()
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
             * jr   ra
             */
            Quad last = quadTable.GenQuad("CalleeExit", null, null, func);
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
            s1Prop.dim = 1;
            s1Prop.dimList = new List<int>();
            s1Prop.dimList.Add(((ConsVarRecord)s1Prop.entry).Val);
            
            dynamic s2Prop = new DynamicProperty();
            s2Prop.name = arrayDeclareProp.name;
            s2Prop.type = arrayDeclareProp.type;

            AutoPush(1);
            stack.Push(actionS2, s2Prop);
            AutoPush(2);
            stack.Push(actionS1, s1Prop);
            AutoPush(2);
        }

        private bool ArrayDeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic loopProp);

            loopProp.dim = s1Prop.dim;
            loopProp.dimList = s1Prop.dimList;
            return true;
        }

        private bool ArrayDeclareActionS2()
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

            GramAction actionS1 = CreateBindActions("ArrayDeclareLoop S1");
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.dim = arrayDeclareLoopProp.dim;
            hierarchyProp.dimList = arrayDeclareLoopProp.dimList;
            GramAction actionS2 = CreateBindActions("ArrayDeclareLoop S2");
            GramAction actionS3 = CreateBindActions("ArrayDeclareLoop S2");

            if (predictCol == 0)
            {
                stack.Push(actionS2);
                AutoPush(2);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(2);
            }
            else
            {
                stack.Push(actionS3, hierarchyProp);
            }
        }

        private bool ArrayDeclareLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(2, out dynamic arrayDeclareLoopProp);
            s1Prop.dim++;
            s1Prop.dimList.Add(((ConsVarRecord)s1Prop.entry).Val);
            arrayDeclareLoopProp.dim = s1Prop.dim;
            arrayDeclareLoopProp.dimList = s1Prop.dimList;
            return true;
        }

        private bool ArrayDeclareLoopActionS2()
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

        private bool FormParamActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.paramDict = s1Prop.paramDict;
            return true;
        }

        private bool FormParamActionS2()
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

        private bool ParamListActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic paramListLoopProp);
            s1Prop.paramDict = new Dictionary<string, VarType>();
            s1Prop.paramDict.Add(s1Prop.paramName, s1Prop.paramType);
            paramListLoopProp.paramDict = s1Prop.paramDict;
            return true;
        }

        private bool ParamListActionS2()
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
            
            GramAction actionS1 = CreateBindActions("ParamListLoop S1");
            GramAction actionS2 = CreateBindActions("ParamListLoop S2");
            GramAction actionS3 = CreateBindActions("ParamListLoop S2");

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.paramDict = paramListLoopProp.paramDict;
            if (predictCol == 0)
            {
                stack.Push(actionS2);
                AutoPush(1);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(2);
            }
            else
            {
                stack.Push(actionS3, hierarchyProp);
            }
        }

        private bool ParamListLoopActionS1()
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

        private bool ParamListLoopActionS2()
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

        private bool ParamActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(3, out dynamic backProp);
            backProp.paramType = s1Prop.varType;
            return true;
        }

        private bool ParamActionS2()
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

            GramAction actionS1 = CreateBindActions("Args S1");
            GramAction actionS2 = CreateBindActions("Args S1");
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.argsList = new List<IDataRecord>();

            if (predictCol == 0)
            {
                stack.Push(actionS1);
                AutoPush(1);
            }
            else
            {
                stack.Push(actionS2, hierarchyProp);
            }
        }

        private bool ArgsActionS1()
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

        private bool ArgsListActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic argsListLoopProp);
            List<IDataRecord> list = new() { s1Prop.entry };
            argsListLoopProp.argsList = list;
            return true;
        }

        private bool ArgsListActionS2()
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
            GramAction actionS1 = CreateBindActions("ArgListLoop S1");
            GramAction actionS2 = CreateBindActions("ArgListLoop S2");
            GramAction actionS3 = CreateBindActions("ArgListLoop S2");

            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.argsList =argListProp.argsList;
            if (predictCol == 0)
            {
                stack.Push(actionS2);
                AutoPush(1);
                stack.Push(actionS1, hierarchyProp);
                AutoPush(2);
            }
            else
            {
                stack.Push(actionS3, hierarchyProp);
            }
        }

        private bool ArgListLoopActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic argsListLoopProp);
            IDataRecord data = s1Prop.entry;
            List<IDataRecord> list = s1Prop.argsList;
            list.Add(data);
            argsListLoopProp.argsList = list;
            return true;
        }

        private bool ArgListLoopActionS2()
        {
            StackTopProp(out dynamic nowProp);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.argsList = nowProp.argsList;
            return true;
        }

        #endregion

        #region No.16 StateBlock

        // 16. StateBlock -> lbrc InnerDeclare StateCluster rbrc
        private void StateBlockPushCtrl()
        {
            StackTopProp(out dynamic stateBlockProp);
            StackPop();

            GramAction actionS1 = CreateBindActions("StateBlock S1");
            dynamic hierarchyProp = new DynamicProperty();
            hierarchyProp.returnType = stateBlockProp.returnType;

            AutoPush(1);
            AutoPush(hierarchyProp);
            AutoPush(2);
            stack.Push(actionS1);
        }

        private bool StateBlockActionS1()
        {
            recordTable.EnterScope();
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

        // 18. InnerVarDeclare -> VarType Id TypeDeclare
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

        private bool InnerVarDeclareActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(3, out dynamic typeDeclareProp);
            typeDeclareProp.type = s1Prop.varType;
            return true;
        }

        private bool InnerVarDeclareActionS2()
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
            dynamic hierarchyProp = new DynamicProperty(stateProp);

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

        private bool AssignActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic assignProp);
            assignProp.name = s1Prop.name;
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

        private bool ReturnActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            if (s1Prop.type == VarType.VOID) { return true; }
            IDataRecord data = s1Prop.entry;
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

            GramAction actionS1 = CreateBindActions("ReturnAlter S1");
            GramAction actionS2 = CreateBindActions("ReturnAlter S2");

            if (predictCol == 0)
            {
                stack.Push(actionS1);
                AutoPush(1);
            }
            else
            {
                stack.Push(actionS2);
            }
        }

        private bool ReturnAlterActionS1()
        {
            StackTopProp(out dynamic s1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            IDataRecord data = s1Prop.entry;
            backProp.entry = data;
            backProp.type = data.Type;
            return true;
        }

        private bool ReturnAlterActionS2()
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

        private bool WhileActionS1()
        {
            stack.RelativeFetch(5, out dynamic s3Prop);

            Quad quad = quadTable.NextQuadRef();
            recordTable.CreateTmpLabelRecord(quad);
            s3Prop.loopQuad = quad;
            return true;
        }

        private bool WhileActionS2()
        {
            StackTopProp(out dynamic s2Prop);
            stack.RelativeFetch(3, out dynamic s3Prop);

            List<Quad> trueList = new() { quadTable.NextQuadRef() };
            IDataRecord data = s2Prop.entry;
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

        private bool WhileActionS3()
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

        // 25. If -> if lpar Expr S1 rpar StateBlock S2 IfAlter S3
        private void IfPushCtrl()
        {

        }
        #endregion

        #region No.26 IfAlter

        // 26. IfAlter -> else S1 StateBlock S2 | blank S3
        private void IfAlterPushCtrl()
        {

        }
        #endregion

        #region No.27 Expr 

        // 27. Expr -> AddExpr S1 ExprLoop S2
        private void ExprPushCtrl()
        {

        }
        #endregion

        #region No.28 ExprLoop

        // 28. ExprLoop -> Relop S1 AddExpr S2 ExprLoop S3 | blank S4
        private void ExprLoopPushCtrl()
        {

        }
        #endregion

        #region No.29 AddExpr

        // 29. AddExpr -> Item S1 AddExprLoop S2
        private void AddExprPushCtrl()
        {

        }
        #endregion

        #region No.30 AddExprLoop

        // 30. AddExprLoop -> plus Item S1 AddExprLoop S2 | sub Item S3 AddExprLoop S4 | blank S5
        private void AddExprLoopPushCtrl()
        {

        }
        #endregion

        #region No.31 Item

        // 31. Item -> Factor S1 ItemLoop S2
        private void ItemPushCtrl()
        {

        }
        #endregion

        #region No.32 ItemLoop

        // 32. ItemLoop -> mul Factor S1 ItemLoop S2 | div Factor S3 ItemLoop S4 | blank S5
        private void ItemLoopPushCtrl()
        {

        }
        #endregion

        #region No.33 Factor

        // 33. Factor -> Num S1 | str | ch | true | false S2 | lpar Expr S3 rpar | id S4 Factor~ S5
        private void FactorPushCtrl()
        {

        }
        #endregion

        #region No.34 CallType

        // 34. CallType -> Call S1 | blank S2
        private void CallTypePushCtrl()
        {

        }
        #endregion

        #region No.35 Call

        // 35. Call -> lpar Args S1 rpar
        private void CallPushCtrl()
        {

        }
        #endregion

        #region No.36 Id

        // 36. Id -> id S1
        private void IdPushCtrl()
        {

        }
        #endregion

        #region No.37 VarType

        // 37. VarType -> int | long | bool | char S1
        private void VarTypePushCtrl()
        {

        }
        #endregion

        #region No.38 Num

        // 38. Num -> integer S1
        private void NumPushCtrl()
        {

        }
        #endregion

        #region No.39 Relop

        // 39. Relop -> eq | neq | leq | geq | gre | les S1
        private void RelopPushCtrl()
        {

        }
        #endregion

        #region No.40 Array'

        // 40. Array' -> blank S1 | lsbrc Expr S2 rsbrc Array'  S3
        private void ArraySPushCtrl()
        {

        }
        #endregion

        #region No.41 Declare~

        // 41. Declare ~ -> TypeDeclare | FuncDeclare
        private void DeclareTPushCtrl()
        {

        }
        #endregion

        #region No.42 Assign~

        // 42. Assign~ -> assign Expr S1 smc | lsbrc Expr S2 rsbrc Array' S3 assign Expr S4 smc 
        private void AssignTPushCtrl()
        {

        }
        #endregion

        #region No.43 Factor~

        // 43. Factor ~ -> CallType S1 | lsbrc Expr S2 rsbrc Array' S3
        private void FactorTPushCtrl()
        {

        }
        #endregion

                

        #endregion
    }
}
