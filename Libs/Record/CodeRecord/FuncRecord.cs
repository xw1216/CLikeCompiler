using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Reg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class FuncRecord : IRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.FUNC;
        }

        internal FuncRecord()
        {
            calleePartialFrame = new();
            calleePartialFrame.Initial = fpInitOffset;
            callerPartialFrame = new();
        }

        private static readonly int fpInitOffset = 16;
        internal static readonly int dword = 8;

        public string Name { get; set; } = "";
        internal VarType ReturnType { get; set; }
        internal LabelRecord Label { get; set; }
        internal ScopeTable LocalTable { get; set; } = new();

        //  callee regs  由本函数负责跨调用一致
        //  在本函数的寄存器分配完成后即可确定并不再更改
        internal readonly CalleeSaver calleeSaver = new();
        internal readonly ImmRecord calleePartialFrame;

        // caller regs 跨调用一致不受保护
        // 故在本函数内调用其他函数时需要
        // 依据子函数使用的寄存器动态保护
        internal readonly CallerSaver callerSaver = new();
        internal readonly ImmRecord callerPartialFrame;

        internal int LocalVarOffset { get; private set; } = 0;
        internal int ArgOffset { get; private set; } = 0;
        private List<VarRecord> argsList;

        internal List<VarRecord> ArgsList
        {
            get => argsList;
            set
            {
                if (value == null) { return; }
                argsList = value;
                ArgOffset = 0;
                for (int i = 0; i < argsList.Count; i++)
                {
                    LocalTable.AddRecord(argsList[i]);
                }
            }
        }

        internal bool IsSignSame(FuncRecord func)
        {
            if (func == null) { return false; }
            if (func.argsList.Count != argsList.Count) { return false; }
            for (int i = 0; i < argsList.Count; i++)
            {
                if (func.argsList[i].Type != argsList[i].Type) { return false; }
            }
            return true;
        }

        internal bool IsSignSame(string name, List<VarRecord> vars)
        {
            if (name != this.Name) { return false; }
            if (vars.Count != this.argsList.Count) { return false; }
            for (var i = 0; i < vars.Count; i++)
            {
                if (vars[i].Type != argsList[i].Type) { return false; }
            }
            return true;
        }

        internal int CalcuCallerFrameSize(FuncRecord func)
        {
            List<VarRecord> argsList = func.argsList;
            List<Regs> saveRegs = func.calleeSaver.saveRegs;
            callerSaver.SetSubFuncUsedReg(saveRegs);
            callerSaver.SetCallerArgument(argsList);
            callerPartialFrame.Initial = callerSaver.Length;
            return callerPartialFrame.Initial;
        }

        // 在堆栈临时扩充与缩小时使用以修正相对 SP 偏移量
        // 调用前先完成 CalcuCallerFrameSize 与 sub sp, sp, callerFrameSize.Initial
        internal void CalcuNewCallOffset(int shift)
        {
            calleePartialFrame.Initial += shift;
            foreach (ImmRecord imm in calleeSaver.updateList)
            {
                imm.Initial += shift;
            }

            List<ScopeTable> queue = new();
            ScopeSortRecur(queue, LocalTable);
            foreach (ScopeTable table in queue)
            {
                for (int i = 0; i < table.Count; i++)
                {
                    ((IDataRecord)table[i]).Offset += shift;
                }
            }
        }

        internal void ResetCallerFrame()
        {
            callerSaver.Clear();
        }

        // TODO 执行前确保所有本函数使用的寄存器已经记录好
        internal int CalcuCalleeFrameSize()
        {
            CalcuVarsOffset();
            calleePartialFrame.Initial = LocalVarOffset + calleeSaver.Length;
            CalcuArgsOffset(calleePartialFrame.Initial);
            calleeSaver.UpdateFrameOffset(calleePartialFrame.Initial);
            return calleePartialFrame.Initial;
        }

        private void ScopeSortRecur(List<ScopeTable> queue, ScopeTable table)
        {
            if (table == null) { return; }
            queue.Add(table);
            for (int i = 0; i < table.Children.Count; i++)
            {
                ScopeSortRecur(queue, table.Children[i]);
            }
        }

        private void CalcuArgsOffset(int frameSize)
        {
            ArgOffset = frameSize;
            for (int i = 0; i < argsList.Count; i++)
            {
                // former 8 arguments are in register a0 ~ a7
                argsList[i].Pos = (i < 8) ? RecordPos.REG : RecordPos.MEM;
                argsList[i].Offset = ArgOffset;
                ArgOffset += argsList[i].Width;
            }
            ArgOffset -= frameSize;
        }

        private void CalcuVarsOffset()
        {
            List<ScopeTable> queue = new();
            List<IRecord> localVars = new();
            ScopeSortRecur(queue, LocalTable);

            int offset = 0;
            for (int i = 0; i < queue.Count; i++)
            {
                ScopeTable scope = queue[i];
                for (int j = 0; j < scope.Count; j++)
                {
                    IRecord rec = scope[j];
                    // Temp Vars under SP firstly
                    if (rec.GetRecordType() == RecordType.VAR
                            && ((VarRecord)rec).IsTemp())
                    {
                        VarRecord varRecord = (VarRecord)rec;
                        varRecord.Offset = offset;
                        varRecord.Pos = RecordPos.MEM;
                        offset += varRecord.Width;
                    }
                    else
                    {
                        localVars.Add(rec);
                    }
                }
            }
            this.LocalVarOffset = offset;
            CalcuLocalVarOffset(localVars);
        }

        private void CalcuLocalVarOffset(List<IRecord> localVars)
        {
            int offset = this.LocalVarOffset;
            for (int i = localVars.Count - 1; i >= 0; i--)
            {
                if (localVars[i].GetRecordType() == RecordType.VAR)
                {
                    VarRecord rec = (VarRecord)localVars[i];
                    rec.Offset = offset;
                    rec.Pos = RecordPos.MEM;
                    offset += rec.Width;
                }
                else
                {
                    ArrayRecord rec = (ArrayRecord)localVars[i];
                    rec.Offset = offset;
                    rec.Pos = RecordPos.MEM;
                    offset += rec.Length;
                }
            }
            this.LocalVarOffset = offset;
        }
    }
}
