using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Analy;
using CLikeCompiler.Libs.Unit.Symbol;
using CLikeCompiler.Libs.Util.LogItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class MidGenServer
    {
        private AnalyStack stack;

        private int predictRow;
        private int predictCol;
        private LexUnit inputNow;
        private LexUnit inputLast;

        List<Symbols> engageQueue;
        QuadTable quadTable;
        RecordTable recordTable;

        internal MidGenServer(GramServer gram)
        {
            stack = gram.GetStack();
        }

        internal void SetTable(QuadTable quadTable, RecordTable recordTable)
        {
            this.quadTable = quadTable;
            this.recordTable = recordTable;
        }

        private void SendFrontMessage(string msg, LogMsgItem.MsgType type, int linePos)
        {
            LogReportArgs args = new(type, msg, linePos);
            Compiler.Instance().ReportFrontInfo(this, args);
        }

        private void SendBackMessage(string msg, LogMsgItem.MsgType type)
        {
            LogReportArgs args = new(type, msg);
            Compiler.Instance().ReportBackInfo(this, args);
        }

        internal void StackTopProp(out dynamic prop)
        {
            stack.Top(out _, out prop);
        }

        internal void StackPop()
        {
            stack.Pop();
        }

        internal void AutoPush(int cnt)
        {
            if(cnt < engageQueue.Count) { return; }
            for(; cnt > 0; cnt--)
            {
                stack.Push(engageQueue[0]);
                engageQueue.RemoveAt(0);
            }
        }

        internal void AutoPush(DynamicProperty prop)
        {
            if(engageQueue.Count < 1) { return; }
            stack.Push(engageQueue[0], prop);
            engageQueue.RemoveAt(0);
        }

        private GramAction CreateBindActions(string name)
        {
            string methodName = name.Replace(" ", "Action");
            Type server =  this.GetType();
            MethodInfo method = server.GetMethod(methodName);
            if(method == null)
            {
                SendBackMessage("未找到成员函数：" + method, LogMsgItem.MsgType.ERROR);
                throw new Exception();
            }

            GramAction action = new(name);
            Delegate dele =  Delegate.CreateDelegate(typeof(GramAction.ActionHandler), method);
            action.AddHandler(dele as GramAction.ActionHandler);
            return action;
        }

        private void DefaultDeriveNoProp()
        {
            StackPop();
            while(engageQueue.Count > 0)
            {
                stack.Push(engageQueue[0], new DynamicProperty());
                engageQueue.RemoveAt(0);
            };
        }

        internal void DeriveHandler(PredictTableItem item, LexUnit unit)
        {
            predictRow = item.pos[0];
            predictCol = item.pos[1];
            inputNow = unit;
            inputLast = Compiler.gram.GetLastInput();
            engageQueue = new(item.prod.GetRhs().First());
            engageQueue.Reverse();
            DeriveDispatcher();
        }

        internal void DeriveDispatcher()
        {
            switch(predictRow)
            {
                case 0: ProgramPushCtrl(); break;
                case 1: DeclareClusterPushCtrl();  break;
                case 2: DeclareClusterLoopPushCtrl();  break;
                case 3: DeclarePushCtrl(); break;
                case 4: TypeDeclarePushCtrl(); break;
                case 5: VarDeclarePushCtrl(); break;
                case 6: FuncDeclarePushCtrl(); break;
                case 7: break;
                case 8: break;
                case 9: break;
                case 10: break;
                case 11: break;
                case 12: break;
                case 13: break;
                case 14: break;
                case 15: break;
                case 16: break;
                case 17: break;
                case 18: break;
                case 19: break;
                case 20: break;
                case 21: break;
                case 22: break;
                case 23: break;
                case 24: break;
                case 25: break;
                case 26: break;
                case 27: break;
                case 28: break;
                case 29: break;
                case 30: break;
                case 31: break;
                case 32: break;
                case 33: break;
                case 34: break;
                case 35: break;
                case 36: break;
                case 37: break;
                case 38: break;
                case 39: break;
                case 40: break;
                case 41: break;
                case 42: break;
                case 43: break;
                default: DefaultDeriveNoProp();  break;
            }
        }

        // 0 Program -> DeclareCluster
        private void ProgramPushCtrl()
        {
            StackPop();
            AutoPush(engageQueue.Count);
        }

        // 1. DeclareCluster -> S1 Declare DeclareClusterLoop
        private void DeclareClusterPushCtrl()
        {
            StackPop();
            AutoPush(engageQueue.Count);
        }

        // 2. DeclareClusterLoop -> Declare DeclareClusterLoop | blank

        private void DeclareClusterLoopPushCtrl()
        {
            StackPop();
            AutoPush(engageQueue.Count);
        }

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
            stack.Top(out _, out dynamic S1Prop);
            stack.RelativeFetch(1, out dynamic backProp);
            backProp.returnType = VarType.VOID;
            backProp.funcName = S1Prop.name;
            return true;
        }

        private bool DeclareActionS2()
        {
            stack.Top(out _, out dynamic S2Prop);
            stack.RelativeFetch(3, out dynamic DeclareProp);
            DeclareProp.type = S2Prop.varType;
            return true;
        }

        private bool DeclareActionS3()
        {
            stack.Top(out _, out dynamic S3Prop);
            stack.RelativeFetch(1, out dynamic DeclareProp);
            DeclareProp.type = S3Prop.varType;
            return true;
        }

        // 4. TypeDeclare -> VarDeclare | ArrayDeclare

        private void TypeDeclarePushCtrl()
        {
            stack.Top(out _, out dynamic TypeDeclareProp);
            dynamic backProp = DynamicProperty.CreateByDynamic(TypeDeclareProp);

            StackPop();
            AutoPush(backProp);
        }

        // 5. VarDeclare -> smc
        private void VarDeclarePushCtrl()
        {
            stack.Top(out _, out dynamic VarDeclareProp);
            recordTable.CreateLocalVarRecord(VarDeclareProp.name, VarDeclareProp.type);

            StackPop();
            AutoPush(1);
        }

        // 6. FuncDeclare -> lpar FormParam S1 rpar S2 StateBlock S3
        private void FuncDeclarePushCtrl()
        {
            stack.Top(out _, out dynamic FuncDeclareProp);
            StackPop();

            dynamic StateBlockProp = new DynamicProperty();
            dynamic S2Prop = new DynamicProperty();
            StateBlockProp.returnType = FuncDeclareProp.returnType;
            S2Prop.returnType = FuncDeclareProp.returnType;
            S2Prop.name = FuncDeclareProp.name;

            GramAction actionS1 = CreateBindActions("FuncDeclare S1");
            GramAction actionS2 = CreateBindActions("FuncDeclare S2");
            GramAction actionS3 = CreateBindActions("FuncDeclare S3");
            stack.Push(actionS3);
            AutoPush(StateBlockProp);
            stack.Push(actionS2, S2Prop);
            AutoPush(1);
            stack.Push(actionS1);
            AutoPush(2);
        }

        private bool FuncDeclareActionS1()
        {
            stack.Top(out _, out dynamic S1Prop);
            stack.RelativeFetch(2, out dynamic S2Prop);
            S2Prop.paramDict = S1Prop.paramDict;
            return true;
        }

        private bool FuncDeclareActionS2()
        {
            int funcAddr = quadTable.NextQuadAddr();


        }

        private bool FuncDeclareActionS3()
        {

        }

        // 7. ArrayDeclare -> lsbrc Num S1 rsbrc ArrayDeclareLoop S2 smc
        private void ArrayDeclarePushCtrl()
        {

        }

        // 8. ArrayDeclareLoop -> lsbrc Num S1 rsbrc ArrayDeclareLoop S2 | blank S3
        private void ArrayDeclareLoopPushCtrl()
        {

        }

        // 9. FormParam -> ParamList S1 | void S2
        private void FormParamPushCtrl()
        {

        }

        // 10. ParamList -> Param S1 ParamListLoop S2
        private void ParamListPushCtrl()
        {

        }

        // 11. ParamListLoop -> cma Param S1 ParamListLoop S2 | blank S3
        private void ParamListLoopPushCtrl()
        {

        }

        // 12. Param -> VarType S1 Id S2
        private void ParamPushCtrl()
        {

        }

        // 13. Args -> S1 ArgsList S2 | blank S3
        private void ArgsPushCtrl()
        {

        }

        // 14. ArgsList -> Expr S1 ArgListLoop S2
        private void ArgsListPushCtrl()
        {

        }

        // 15. ArgListLoop -> cma Expr S1 ArgListLoop S2 | blank S3
        private void ArgListLoopPushCtrl()
        {

        }

        // 16. StateBlock -> lbrc InnerDeclare StateCluster rbrc
        private void StateBlockPushCtrl()
        {

        }

        // 17. InnerDeclare -> blank | InnerVarDeclare InnerDeclare
        private void InnerDeclarePushCtrl()
        {

        }

        // 18. InnerVarDeclare -> VarType Id TypeDeclare
        private void InnerVarDeclarePushCtrl()
        {

        }

        // 19. StateCluster -> blank | State StateCluster
        private void StateClusterPushCtrl()
        {

        }

        // 20. State -> If | While | Return | Assign
        private void StatePushCtrl()
        {

        }

        // 21. Assign -> id S1 Assign~
        private void AssignPushCtrl()
        {

        }

        // 22. Return -> return ReturnAlter S1 smc
        private void ReturnPushCtrl()
        {

        }

        // 23. ReturnAlter -> Expr S1 | blank S2
        private void ReturnAlterPushCtrl()
        {

        }

        // 24. While -> while lpar S1 Expr S2 rpar StateBlock S3
        private void WhilePushCtrl()
        {

        }

        // 25. If -> if lpar Expr S1 rpar StateBlock S2 IfAlter S3
        private void IfPushCtrl()
        {

        }

        // 26. IfAlter -> else S1 StateBlock S2 | blank S3
        private void IfAlterPushCtrl()
        {

        }

        // 27. Expr -> AddExpr S1 ExprLoop S2
        private void ExprPushCtrl()
        {

        }

        // 28. ExprLoop -> Relop S1 AddExpr S2 ExprLoop S3 | blank S4
        private void ExprLoopPushCtrl()
        {

        }

        // 29. AddExpr -> Item S1 AddExprLoop S2
        private void AddExprPushCtrl()
        {

        }

        // 30. AddExprLoop -> plus Item S1 AddExprLoop S2 | sub Item S3 AddExprLoop S4 | blank S5
        private void AddExprLoopPushCtrl()
        {

        }

        // 31. Item -> Factor S1 ItemLoop S2
        private void ItemPushCtrl()
        {

        }

        // 32. ItemLoop -> mul Factor S1 ItemLoop S2 | div Factor S3 ItemLoop S4 | blank S5
        private void ItemLoopPushCtrl()
        {

        }

        // 33. Factor -> Num S1 | str | ch | true | false S2 | lpar Expr S3 rpar | id S4 Factor~ S5
        private void FactorPushCtrl()
        {

        }

        // 34. CallType -> Call S1 | blank S2
        private void CallTypePushCtrl()
        {

        }

        // 35. Call -> lpar Args S1 rpar
        private void CallPushCtrl()
        {

        }

        // 36. Id -> id S1
        private void IdPushCtrl()
        {

        }

        // 37. VarType -> int | long | bool | char S1
        private void VarTypePushCtrl()
        {

        }

        // 38. Num -> integer S1
        private void NumPushCtrl()
        {

        }

        // 39. Relop -> eq | neq | leq | geq | gre | les S1
        private void RelopPushCtrl()
        {

        }

        // 40. Array' -> blank S1 | lsbrc Expr S2 rsbrc Array'  S3
        private void ArraySPushCtrl()
        {

        }

        // 41. Declare ~ -> TypeDeclare | FuncDeclare
        private void DeclareTPushCtrl()
        {

        }

        // 42. Assign~ -> assign Expr S1 smc | lsbrc Expr S2 rsbrc Array' S3 assign Expr S4 smc 
        private void AssignTPushCtrl()
        {

        }

        // 43. Factor ~ -> CallType S1 | lsbrc Expr S2 rsbrc Array' S3
        private void FactorTPushCtrl()
        {

        }
    }
}
