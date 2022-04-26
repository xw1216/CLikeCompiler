using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit;
using CLikeCompiler.Libs.Unit.Reg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record
{
    internal abstract class CallSaver
    {
        internal List<Regs> usedRegs = new();
        internal int usedRegCnt { get { return usedRegs.Count; } }

        internal List<Regs> saveRegs = new();
        internal int saveRegCnt { get { return saveRegs.Count; } }

        internal List<ImmRecord> updateList = new();

        internal abstract void AddUsedReg(Regs reg);
        internal void AddUpdateItem(ImmRecord rec) { updateList.Add(rec); }

        internal void Clear()
        {
            updateList.Clear();
            usedRegs.Clear();
            saveRegs.Clear();
        }
    }

    internal class CalleeSaver : CallSaver
    {
        private readonly List<int> calleeSaveList = new() { 1, 2, 8, 9, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27 };

        internal int Length { get { return saveRegs.Count * FuncRecord.dword; } }

        internal CalleeSaver()
        {
            TargGenServer server = Compiler.targGen;
            AddUsedReg(server.regFile.FindRegs("ra"));
            AddUsedReg(server.regFile.FindRegs("fp"));
        }

        internal override void AddUsedReg(Regs reg)
        {
            if(usedRegs.Contains(reg)) { return; }
            usedRegs.Add(reg);
            if (calleeSaveList.Contains(reg.Index))
            {
                saveRegs.Add(reg);
                ImmRecord imm = new();
                imm.Name = reg.Name;
                imm.IsSpRel = true;
                updateList.Add(imm);
            }
        }

        internal void UpdateFrameOffset(int frameSize)
        {
            int offset = -FuncRecord.dword;
            foreach(ImmRecord imm in updateList)
            {
                imm.Offset = offset;
                imm.Initial = frameSize;
                offset -= frameSize;
            }
        }
    }

    internal class CallerSaver : CallSaver
    {
        private readonly List<int> callerSaveList = new() { 5, 6, 7, 12, 13, 14, 15, 16, 17, 28, 29, 31 };

        internal int Length { get; private set; } = 0;

        internal void SetSubFuncUsedReg(List<Regs> regs)
        {
            foreach(Regs reg in regs)
            {
                AddUsedReg(reg);
            }
        }

        internal override void AddUsedReg(Regs reg)
        {
            if (usedRegs.Contains(reg)) { return; }
            usedRegs.Add(reg);
            if (callerSaveList.Contains(reg.Index))
            {
                saveRegs.Add(reg);
                ImmRecord imm = new();
                imm.Name = reg.Name;
                imm.IsSpRel = true;
                updateList.Add(imm);
            }
        }

        internal void SetCallerArgument(List<VarRecord> args)
        {
            foreach (VarRecord arg in args)
            {
                ImmRecord rec = new();
                rec.Name = arg.Name;
                rec.IsSpRel = true;
                updateList.Add(rec);
            }

            int offset = 0;
            for (int i = updateList.Count - 1, j = 0; i >= 0; i--, j++)
            {
                if (j < args.Count)
                {
                    updateList[i].Offset = offset;
                    offset += args[j].Width;
                }
                else
                {
                    updateList[i].Offset = offset;
                    offset += FuncRecord.dword;
                }
            }
            Length = offset;
        }
    }


    
}
