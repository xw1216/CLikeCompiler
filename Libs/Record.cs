using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    public enum VarType
    {
        VOID,
        INT,
        LONG,
        BOOL,
        CHAR
    }
    public enum RecordType
    {
        FUNC,
        VAR,
        ARRAY,
        LABEL,
        REGS,
        IMM
    }

    public enum RecordPos
    {
        TEXT,
        DATA,
        MEM,
        REG
    }

    public interface IDataRecord
    {
        VarType Type { get; set; }
        int Width { get; }

        RecordPos Pos { get; set; }
        int Offset { get; set; }
        Regs Reg { get; set; }

        static int GetWidth(VarType type)
        {
            switch (type)
            {
                case VarType.INT: return 4;
                case VarType.LONG: return 8;
                case VarType.BOOL: return 1;
                case VarType.CHAR: return 1;
                default: return 0;
            }
        }
    }

    public interface IRecord
    {
        string Name { get; set; }
        
        RecordType GetRecordType();
    }

    internal class ImmRecord : IRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.IMM;
        }
        public string Name { get; set; }

        internal int Init { get; set; } = 0;
        internal int Offset { get; set; } = 0;
        internal bool IsSpRel { get; set; } = false;

        internal int Value { get { return Init + Offset; } }
    }

    internal class LabelRecord : IRecord
    {
        public string Name { get; set; } = "";
        public RecordType GetRecordType()
        {
            return RecordType.LABEL;
        }

        private static int tmpCnt = 0;

        internal int Addr { get; set; } = 0;
        internal LabelRecord(int addr, string name)
        {
            Addr = addr;
            Name = name;
        }

        internal static string GetTmpLabelName()
        {
            return ".L" + tmpCnt++;
        }
    }

    internal class VarRecord : IRecord, IDataRecord
    {
        public string Name { get; set; } = "";
        public int Width { get { return IDataRecord.GetWidth(Type); } }
        public VarType Type { get; set ; }
        public int Offset { get; set; }
        public RecordPos Pos { get; set; }
        public Regs Reg { get; set; }

        internal VarRecord() { }

        internal VarRecord(string name, VarType type) 
            { this.Name = name; this.Type = type; }

        internal virtual bool IsTemp() { return false; }
        internal virtual bool IsCons() { return false; }

        public RecordType GetRecordType()
        {
            return RecordType.VAR;
        }

    }

    internal class TempVarReocrd : VarRecord
    {
        private static long tmpCnt = 0;

        internal TempVarReocrd()
        {
            this.Name = "~Tmp" + tmpCnt;
            tmpCnt++;
        }

        internal override bool IsTemp() { return true; }
    }

    internal class ConsVarRecord : VarRecord
    {
        private static long conCnt = 0;

        internal ConsVarRecord()
        {
            this.Name = "~Con" + conCnt;
            conCnt++;
        }

        internal object Val { get; set; }
        internal string OriginCont { get; set; }

        internal override bool IsCons() { return true; }
    }

    internal class ArrayRecord : IRecord, IDataRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.ARRAY;
        }

        private List<int> dimList = new();

        public string Name { get; set; } = "";
        public int Width { get { return IDataRecord.GetWidth(Type); } }
        public VarType Type { get; set; }
        public RecordPos Pos { get; set; }
        public int Offset { get; set; }
        public Regs Reg { get; set; }

        internal int Dim { get
            {
                if(dimList == null) { return 0; }
                else { return dimList.Count; }
            } 
        }

        internal int Length
        {
            get
            {
                int len = 0;
                for (int i = 0; i < dimList.Count; i++) { len += dimList[i]; }
                len *= this.Width;
                return len;
            }
        }


        internal virtual bool IsCons() { return false; }

        internal void  SetDimList(List<int> list) { dimList = list; }

        internal int GetDimLen(int index)
        {
            if(dimList == null || index <  0 || index > dimList.Count - 1) { return 0; }
            else { return dimList[index]; }
        }

    }

    internal class ConsArrayRecord : ArrayRecord
    {
        internal readonly List<object> list = new();
        internal string OriginCont { get; set; }

        internal override bool IsCons() { return true; }
    }


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
                imm.Init = frameSize;
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


    internal class FuncRecord : IRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.FUNC;
        }

        internal FuncRecord()
        {
            calleePartialFrame = new();
            calleePartialFrame.Init = fpInitOffset;
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
            if(func == null) { return false; }
            if(func.argsList.Count != argsList.Count) { return false; }
            for(int i = 0; i < argsList.Count; i++)
            {
                if(func.argsList[i].Type != argsList[i].Type) { return false; }
            }
            return true;
        }

        internal bool IsSignSame(string name, List<VarRecord> vars)
        {
            if(name != this.Name) { return false; }
            if(vars.Count != this.argsList.Count) { return false; }
            for(var i = 0; i < vars.Count; i++)
            {
               if( vars[i].Type != argsList[i].Type) { return false; }
            }
            return true;
        }

        internal int CalcuCallerFrameSize(FuncRecord func)
        {
            List<VarRecord> argsList = func.argsList;
            List<Regs> saveRegs = func.calleeSaver.saveRegs ;
            callerSaver.SetSubFuncUsedReg(saveRegs);
            callerSaver.SetCallerArgument(argsList);
            callerPartialFrame.Init = callerSaver.Length;
            return callerPartialFrame.Init;
        }

        // 在堆栈临时扩充与缩小时使用以修正相对 SP 偏移量
        // 调用前先完成 CalcuCallerFrameSize 与 sub sp, sp, callerFrameSize.Init
        internal void CalcuNewCallOffset(int shift) 
        {
            calleePartialFrame.Init += shift;
            foreach(ImmRecord imm in calleeSaver.updateList)
            {
                imm.Init += shift;
            }

            List<ScopeTable> queue = new();
            ScopeSortRecur(queue, LocalTable);
            foreach(ScopeTable table in queue)
            {
                for(int i = 0; i < table.Count; i++)
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
            calleePartialFrame.Init = LocalVarOffset + calleeSaver.Length;
            CalcuArgsOffset(calleePartialFrame.Init);
            calleeSaver.UpdateFrameOffset(calleePartialFrame.Init);
            return calleePartialFrame.Init;
        }

        private void ScopeSortRecur(List<ScopeTable> queue, ScopeTable table)
        {
            if (table == null) { return; }
            queue.Add(table);
            for(int i = 0; i < table.Children.Count; i++)
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
            for(int i = 0; i < queue.Count; i++)
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
                } else
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
