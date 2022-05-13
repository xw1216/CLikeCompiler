using System;
using System.Collections.Generic;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Quads;
using CLikeCompiler.Libs.Unit.Reg;
using CLikeCompiler.Libs.Unit.Target;
using CLikeCompiler.Libs.Util.LogItem;

namespace CLikeCompiler.Libs.Component
{
    internal class TargetGenServer
    {
        private List<Target> targetCodeList = new();
        private FuncRecord funcNow = null;
        private List<FuncRecord> funcList;  
        private List<CallRecord> callList;

        // register at compiler from mid code generator
        private RecordTable recordTable;
        private RegFiles regFiles;
        private QuadTable quadTable;

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

        internal void StartCodeGen()
        {
            FuncPreProcess();
            CallRecordPrePrecess();
        }

        internal void Reset()
        {

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

        

        #endregion

        #region Text Segment

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
                Target target = new()  {  Op = quad.Name, };

                // 跳转操作 出现 Reg Label 记录
                if (Quad.IsJumpOp(op)) { JumpQuadHandler(quad, target); }

                // 调用操作 出现 Func Call 记录
                if (Quad.IsCallOp(op)) { CallQuadHandler(quad, target); }

                // 内存操作 出现 Type Var Reg Array ConsVar TempVar记录
                if (Quad.IsMemOp(op)) { MemQuadHandler(quad, target); }

                // 复制操作 出现 Reg Imm 记录
                if (Quad.IsCopyOp(op)) { CopyQuadHandler(quad, target); }

                // 普通操作 出现 Reg Imm 记录
                StdQuadHandler(quad, target);

                targetCodeList.Add(target);
            }
        }

        private void JumpQuadHandler(Quad quad, Target target)
        {
            if (quad.Lhs != null)
            {

            }

        }

        private void CallQuadHandler(Quad quad, Target target)
        {

        }

        private void MemQuadHandler(Quad quad, Target target)
        {

        }

        private void CopyQuadHandler(Quad quad, Target target)
        {

        }

        private void StdQuadHandler(Quad quad, Target target)
        {

        }
        #endregion


    }
}
