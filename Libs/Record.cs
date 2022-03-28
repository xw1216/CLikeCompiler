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
        REGS
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
        int Offset { get; set; }
        int Width { get; }
        VarType Type { get; set; }
        RecordPos Pos { get; set; }

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
        public int Offset { get; set; }
        public int Width { get { return IDataRecord.GetWidth(Type); } }
        public VarType Type { get; set ; }
        public RecordPos Pos { get; set; }

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
        public int Offset { get; set; }
        public int Width { get { return IDataRecord.GetWidth(Type); } }
        public VarType Type { get; set; }
        public RecordPos Pos { get; set; }

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


    internal class FuncRecord : IRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.FUNC;
        }

        private static readonly int fpBaseOffset = 16;
        private static readonly int dword = 8;

        public string Name { get; set; } = "";
        internal VarType ReturnType { get; set; }
        internal LabelRecord Label { get; set; }
        internal ScopeTable LocalTable { get; set; } = new();

        // TODO : 怎么去确定需要保护现场的寄存器以便确定栈帧长度
        internal List<Regs> savedRegs = new();
        internal int SaveRegCnt { get { return savedRegs.Count; } }

        internal int FrameSize { get; private set; } = fpBaseOffset;
        internal int OldFpRelPos { get { return FrameSize - 2 * dword; } }
        internal int RetAddrRelPos { get { return FrameSize - dword; } }

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

        private void ScopeSortRecur(List<ScopeTable> queue, ScopeTable table)
        {
            if(table == null) { return; }
            queue.Add(table);
            for(int i = 0; i < table.Children.Count; i++)
            {
                ScopeSortRecur(queue, table.Children[i]);
            }
        }
        
        internal void CalcuFrameSize()
        {
            FrameSize = SaveRegCnt * dword + LocalVarOffset + fpBaseOffset;
            CalcuArgsOffset();
            CalcuVarsOffset();
        }

        private void CalcuArgsOffset()
        {
            ArgOffset = FrameSize;
            for (int i = 0; i < argsList.Count; i++)
            {
                // former 8 arguments are in register a0 ~ a7
                argsList[i].Pos = (i < 8) ? RecordPos.REG : RecordPos.MEM;
                argsList[i].Offset = ArgOffset;
                ArgOffset += argsList[i].Width;
            }
            ArgOffset -= FrameSize;
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
