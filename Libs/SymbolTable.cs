using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{

    internal class MacroTable
    {
        private static List<string> globalMacros = new();
        private Dictionary<string, string> replaceMacros = new();

        public void AddDefineValue(string key, string value)
        {
            replaceMacros.Add(key, value);
            if(!globalMacros.Contains(key))
            {
                globalMacros.Add(key);
            }
        }

        public ref Dictionary<string, string> GetLocalMacros()
        {
            return ref replaceMacros;
        }

        public void ClearLocalMacros()
        {
            replaceMacros.Clear();
        }

        public void AddDefineInclude(string key)
        {
            globalMacros.Add(key);
        }

        public bool IsMacroExist(string key)
        {
            return globalMacros.Contains(key);
        }

        public void ResetMacroTable()
        {
            globalMacros.Clear();
            replaceMacros.Clear();
        }
    }

    internal enum VarType
    {
        VOID,
        INT,
        LONG,
        BOOL,
        CHAR
    }
    internal enum RecordType
    {
        FUNC,
        VAR,
        ARRAY,
        LABEL
    }

    internal abstract class Record
    {

        internal int Offset { get; set; } = 0;

        internal string Name { get; set; } = "";

        internal abstract RecordType GetRecordType();
    }

    internal class LabelRecord : Record
    {
        internal LabelRecord(int addr, string name)
        {
            Addr = addr;
            Name = name;
        }

        internal int Addr { get; set; } = 0;

        internal override RecordType GetRecordType()
        {
            return RecordType.LABEL;
        }
    }

    internal class VarRecord : Record
    {
        internal VarType Type { get; set; }
        internal int Width { get
            {
                switch(Type)
                {
                    case VarType.INT: return 4;
                    case VarType.LONG: return 8;
                    case VarType.BOOL: return 1;
                    case VarType.CHAR: return 1;
                    default: return 0;
                }
            } 
        }

        internal VarRecord() { }

        internal VarRecord(string name, VarType type) { this.Name = name; this.Type = type; }

        internal virtual bool IsTemp() { return false; }
        internal virtual bool IsCons() { return false; }

        internal override RecordType GetRecordType()
        {
            return RecordType.VAR;
        }
    }

    internal class TempVarReocrd : VarRecord
    {
        private static long tempCnt = 0;

        internal TempVarReocrd()
        {
            this.Name = "~T" + tempCnt;
            tempCnt++;
        }

        internal override bool IsTemp()
        {
            return true;
        }
    }

    internal class ConsVarRecord : VarRecord
    {
        internal object Val { get; set; }
        internal string OriginCont { get; set; }

        internal override bool IsCons()
        {
            return true;
        }
    }

    internal class ArrayRecord : Record
    {
        private VarRecord elemStd = new();
        private List<int> dimList = new();

        internal VarType Type { 
            get
            {
                return elemStd.Type;
            } 
            set
            {
                elemStd.Type = value;
            }
        }

        internal int Dim { get
            {
                if(dimList == null) { return 0; }
                else { return dimList.Count; }
            } 
        }

        internal void  SetDimList(List<int> list)
        {
            dimList = list;
        }

        internal void AddDim(int len)
        {
            dimList.Add(len);
        }

        internal virtual bool IsCons()
        {
            return false;
        }

        internal int GetDimLen(int index)
        {
            if(dimList == null || index <  0 || index > dimList.Count - 1) { return 0; }
            else { return dimList[index]; }
        }

        internal int GetArrLen()
        {
            int len = 0;
            for (int i = 0; i < dimList.Count; i++)
            {
                len += dimList[i];
            }
            len *= GetElemWidth();
            return len;
        }

        internal int GetElemWidth()
        {
            return elemStd.Width;
        }

        internal override RecordType GetRecordType()
        {
            return RecordType.ARRAY;
        }
    }

    internal class ConsArrayRecord : ArrayRecord
    {
        internal List<object> list = new();
        internal string OriginCont { get; set; }

        internal override bool IsCons()
        {
            return true;
        }
    }

    internal class FuncRecord : Record
    {
        private List<VarRecord> argsList;
        internal VarType ReturnType { get; set; }
        internal ScopeTable LocalTable { get; set; } = new();
        internal LabelRecord Label { get; set; }
        internal int ArgOffset { get; private set; } = 0;

        internal List<VarRecord> ArgsList
        {
            get => argsList;
            set
            {
                if (value == null) { return; }
                argsList = value;
                ArgOffset = 8;
                for (int i = 0; i < argsList.Count; i++)
                {
                    argsList[i].Offset = ArgOffset;
                    ArgOffset += argsList[i].Width;
                    LocalTable.AddRecord(argsList[i]);
                }
            } 
        }

        internal void SetArgs(List<VarRecord> args)
        {
            if(args == null) { return; }
            this.argsList = args;
            for(int i = 0; i < argsList.Count; i++)
            {
                ArgOffset += argsList[i].Width;
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

        internal override RecordType GetRecordType()
        {
            return RecordType.FUNC;
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

        internal void CalcuLocalVarOffset()
        {
            int offset = 0;
            List<ScopeTable> queue = new();
            List<TempVarReocrd> tempVars = new();
            ScopeSortRecur(queue, LocalTable);
            for(int i = queue.Count - 1; i >= 0; i--)
            {
                ScopeTable scope = queue[i];
                for (int j = scope.Count - 1; j >= 0; j--)
                {
                    if (scope[j].GetRecordType() == RecordType.VAR)
                    {
                        VarRecord rec = (VarRecord)scope[j];
                        if (rec.IsTemp()) 
                        { 
                            tempVars.Add((TempVarReocrd)rec); 
                            continue; 
                        }
                        offset -= rec.Width;
                        rec.Offset = offset;
                    }
                    else if (scope[j].GetRecordType() == RecordType.ARRAY)
                    {
                        ArrayRecord array = (ArrayRecord)scope[j];
                        offset -= array.GetArrLen();
                        array.Offset = offset;
                    }
                }
            }
            
            this.Offset = offset;
            CalcuTempVarOffset(tempVars);
        }

        internal void CalcuTempVarOffset(List<TempVarReocrd> tempVars)
        {
            int offset = this.Offset;
            for(int i = tempVars.Count - 1; i >= 0; i--)
            {
                offset -= tempVars[i].Width;
                tempVars[i].Offset = offset;
            }
        }
    }

    internal class ScopeTable
    {
        internal ScopeTable Parent { get; set; } = null;
        internal List<ScopeTable> Children = new();

        private List<Record> records = new();
        internal int Count { get
            {
                return records.Count;
            }
        }

        internal Record this[int i]
        {
            get { return records[i]; }
            set { records[i] = value; }
        }

        internal void Clear()
        {
            Parent = null;
            Children.Clear();
            records.Clear();
        }

        internal void AddRecord(Record rec)
        {
            records.Add(rec);
        }

        internal void AddChildTable(ScopeTable table)
        {
            Children.Add(table);
        }

        internal bool ContainsRecord(Record rec)
        {
            return records.Contains(rec);
        }

        internal bool CreateRecord(Record rec)
        {
            // We can create record when
            // no same name record is included in state block table
            if (rec == null) { return false; }
            foreach (Record lhs in records)
            {
                if (lhs.Name == rec.Name) { return false; }
            }
            records.Add(rec);
            return true;
        }
    }

    internal class RecordTable
    {
        private List<ScopeTable> scopeTables;
        private List<FuncRecord> funcTable;
        private List<LabelRecord> labelTable;

        private ScopeTable consTable;

        private ScopeTable globalTable;
        private ScopeTable currentTable;

        internal RecordTable()
        {
            funcTable = new();
            consTable = new();
            labelTable = new();

            scopeTables = new();
            globalTable = new();

            globalTable.Parent = globalTable;
            scopeTables.Add(consTable);
            scopeTables.Add(globalTable);

            currentTable = globalTable;
        }

        internal void ResetRecordTable()
        {
            funcTable.Clear();
            consTable.Clear();
            labelTable.Clear();
            scopeTables.Clear();
            globalTable.Clear();
            currentTable = null;
        }

        // Record Table Change

        internal ScopeTable NewTable()
        {
            ScopeTable newTable = new();
            currentTable.AddChildTable(newTable);
            newTable.Parent = currentTable;
            currentTable = newTable;
            return newTable;
        }

        internal ScopeTable LeaveTable()
        {
            if(currentTable == null || currentTable == globalTable) 
            { return globalTable; }

            currentTable = currentTable.Parent;
            return currentTable;
        }

        // Label Definition
        internal LabelRecord CreateLabelRecord(int addr, string name)
        {
            foreach(LabelRecord label in labelTable)
            {
                if(label.Name == name) { return null; }
            }
            LabelRecord record = new(addr, name);
            labelTable.Add(record);
            return record;
        }

        internal LabelRecord FindLabelRecord(string name) 
        {
            foreach (LabelRecord label in labelTable)
            {
                if (label.Name == name) { return label; }
            }
            return null;
        }

        // Function Definition

        internal FuncRecord CreateFuncRecord(VarType returnType, string name, List<VarRecord> vars, int addr)
        {
            FuncRecord func = FindFuncRecord(name, vars);
            if (func != null) { return func; }

            func = new();
            func.Name = name;
            func.ReturnType = returnType;
            func.SetArgs(vars);
            LabelRecord label = CreateLabelRecord(addr, name + addr);
            func.Label = label;

            func.LocalTable.Parent = globalTable;
            globalTable.AddChildTable(func.LocalTable);

            funcTable.Add(func);

            return func;
        }        

        // Function Variebles Offset Calculate
        internal void CalcuFuncVarOffset()
        {
            for(int i = 0; i < funcTable.Count; i++)
            {
                funcTable[i].CalcuLocalVarOffset();
            }
        }

        // Function Call

        internal FuncRecord FindFuncRecord(string name, List<VarRecord> vars)
        {
            foreach (FuncRecord func in funcTable)
            {
                if(func.IsSignSame(name, vars)) { return func; }
            }
            return null;
        }

        // Recognize constants

        private void ParseConsRecordInput(ConsVarRecord consVar, VarType type, string cont)
        {
            consVar.Type = type;
            consVar.OriginCont = cont;
            switch (type)
            {
                case VarType.INT:
                    consVar.Val = int.Parse(cont);
                    return;
                case VarType.LONG:
                    consVar.Val = long.Parse(cont);
                    return;
                case VarType.CHAR:
                    consVar.Val = char.Parse(cont);
                    return;
                case VarType.BOOL:
                    consVar.Val = bool.Parse(cont);
                    return;
            }
            throw new Exception();
        }

        private void ParseConsRecordInput(ConsArrayRecord consArray, VarType type, string cont)
        {
            consArray.Type = type;
            consArray.OriginCont = cont;
            if(type == VarType.CHAR)
            {
                for(int i = 0; i < cont.Length; i++) { consArray.list.Add(cont[i]); }
            } 
            else
            {
                List<string> strList = cont.Split(',', StringSplitOptions.RemoveEmptyEntries | 
                    StringSplitOptions.TrimEntries).ToList();
                if(type == VarType.INT) 
                    { ParseListInput<int>(strList, ref consArray.list); }
                else if(type == VarType.LONG) 
                    { ParseListInput<long>(strList,ref consArray.list); }
                else 
                    { ParseListInput<bool>(strList,ref consArray.list); }
            }
        }

        private void ParseListInput<T>(List<string> strList, ref List<object> result)
        {
            Type type = typeof(T);
            var parse = type.GetMethod("TryParse");
            result = new();
            
            foreach (string str in strList)
            {
                var args = new object[] { str , Activator.CreateInstance(type)};
                parse.Invoke(null, args);
                result.Add((T)args[1]);
            }
        }

        // Constant Definition

        internal ConsVarRecord CreateConsRecord(VarType type, string cont)
        {
            if(FindConsRecord(cont) == null) { return null; }
            ConsVarRecord consVar = new();
            ParseConsRecordInput(consVar, type, cont);
            consTable.AddRecord(consVar);
            return consVar;
        }

        internal ConsArrayRecord CreateConsArrayRecord(VarType type, string cont)
        {
            if (FindConsRecord(cont) == null) { return null; }
            ConsArrayRecord consArray = new();
            ParseConsRecordInput (consArray, type, cont);
            consTable.AddRecord(consArray);
            return consArray;
        }

        // Constant Find

        private Record FindConsRecord(string cont)
        {
            for (int i = 0; i < consTable.Count; i++)
            {
                Record rec = consTable[i];
                if (rec.GetRecordType() == RecordType.ARRAY
                    && ((ConsArrayRecord)rec).OriginCont == cont)
                { return rec; }
                else if (rec.GetRecordType() == RecordType.VAR
                    && ((ConsVarRecord)rec).OriginCont == cont)
                { return rec; }
            }
            return null;
        }

        // Variable Definition
        internal VarRecord CreateLocalVarRecord(string name, VarType type)
        {
            if(IsLocalRecordExist(name))
            {
                VarRecord record = new();
                record.Name = name;
                record.Type = type;
                currentTable.AddRecord(record);
                return record;
            }
            return null;
        }

        internal ArrayRecord CreateLocalArrayRecord(string name, VarType type, List<int> dimList)
        {
            if(IsLocalRecordExist(name))
            {
                ArrayRecord record = new();
                record.Name=name;
                record.Type = type;
                record.SetDimList(dimList);
                currentTable.AddRecord(record);
                return record;
            }
            return null;
        }

        internal TempVarReocrd CreateTempVarRecord(VarType type)
        {
            TempVarReocrd record = new();
            record.Type=type;
            currentTable.AddRecord(record);
            return record;
        }

        // General Record Find

        private bool IsLocalRecordExist(string name)
        {
            for (int i = 0; i < currentTable.Count; i++) { 
                if(name == currentTable[i].Name) { return true; }
            }
            return false;
        }

        internal Record FindRecord(string name)
        {
            ScopeTable scope = currentTable;
            while (true)
            {
                for (int i = 0; i < scope.Count; i++)
                {
                    if(scope[i].Name == name) { return scope[i]; }
                }
                if(scope == currentTable) { return null; }
                scope = scope.Parent;
            }
        }

    }
}
