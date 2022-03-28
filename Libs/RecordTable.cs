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
            if (!globalMacros.Contains(key))
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

    internal class ScopeTable
    {
        internal ScopeTable Parent { get; set; } = null;
        internal List<ScopeTable> Children = new();

        private readonly List<IRecord> records = new();
        internal int Count { get { return records.Count; } }

        internal IRecord this[int i]
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

        internal void AddRecord(IRecord rec)
        {
            records.Add(rec);
        }

        internal void AddChildTable(ScopeTable table)
        {
            Children.Add(table);
        }

        internal bool ContainsRecord(IRecord rec)
        {
            return records.Contains(rec);
        }

        internal bool CreateRecord(IRecord rec)
        {
            // We can create record when
            // no same name record is included in state block table
            if (rec == null) { return false; }
            foreach (IRecord lhs in records)
            {
                if (lhs.Name == rec.Name) { return false; }
            }
            records.Add(rec);
            return true;
        }
    }

    internal class RecordTable
    {
        private FuncRecord mainFunc = null;

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
            if (currentTable == null || currentTable == globalTable)
            { return globalTable; }

            currentTable = currentTable.Parent;
            return currentTable;
        }

        // Label Definition
        internal LabelRecord CreateLabelRecord(int addr, string name)
        {
            foreach (LabelRecord item in labelTable)
            {
                if (item.Name == name) { return null; }
            }
            LabelRecord label = new(addr, name);
            labelTable.Add(label);
            return label;
        }

        internal LabelRecord CreateTmpLabelRecord(int addr)
        {
            LabelRecord tmpLabel = new(addr, LabelRecord.GetTmpLabelName());
            labelTable.Add(tmpLabel);
            return tmpLabel;
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

        private string GetArgsAbbr(List<VarRecord> args)
        {
            StringBuilder builder = new();
            foreach(VarRecord item in args)
            {
                if(item.Type == VarType.CHAR) { builder.Append("_char"); }
                else if(item.Type == VarType.INT) { builder.Append("_int"); }
                else if(item.Type == VarType.BOOL) { builder.Append("_bool"); }
                else if(item.Type==VarType.LONG) { builder.Append("_long");  }
            }
            return builder.ToString();
        }

        internal FuncRecord CreateFuncRecord(VarType returnType, string name, List<VarRecord> vars, int addr)
        {
            FuncRecord func = FindFuncRecord(name, vars);
            if (func != null) { return func; }

            func = new();
            func.Name = name;
            func.ReturnType = returnType;
            func.ArgsList = vars;

            LabelRecord label = CreateLabelRecord(addr, "Func_" + name + GetArgsAbbr(vars));
            func.Label = label;

            func.LocalTable.Parent = globalTable;
            globalTable.AddChildTable(func.LocalTable);

            funcTable.Add(func);
            // Set main function entry
            if (mainFunc == null && func.Name == "main") 
                { mainFunc = func; }

            return func;
        }

        // Function Variebles Offset Calculate
        // TODO: 确保每个函数的压栈寄存器已经配置好
        internal void CalcuFuncVarOffset()
        {
            for (int i = 0; i < funcTable.Count; i++)
            {
                funcTable[i].CalcuFrameSize();
            }
        }

        // Function Call

        internal FuncRecord FindFuncRecord(string name, List<VarRecord> vars)
        {
            foreach (FuncRecord func in funcTable)
            {
                if (func.IsSignSame(name, vars)) { return func; }
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
            if (type == VarType.CHAR)
            {
                for (int i = 0; i < cont.Length; i++) { consArray.list.Add(cont[i]); }
            }
            else
            {
                List<string> strList = cont.Split(',', StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries).ToList();
                if (type == VarType.INT)
                { ParseListInput<int>(strList, consArray.list); }
                else if (type == VarType.LONG)
                { ParseListInput<long>(strList, consArray.list); }
                else
                { ParseListInput<bool>(strList, consArray.list); }
            }
        }

        private static void ParseListInput<T>(List<string> strList, List<object> result)
        {
            Type type = typeof(T);
            var parse = type.GetMethod("TryParse");
            result = new();

            foreach (string str in strList)
            {
                var args = new object[] { str, Activator.CreateInstance(type) };
                parse.Invoke(null, args);
                result.Add((T)args[1]);
            }
        }

        // Constant Definition

        internal ConsVarRecord CreateConsRecord(VarType type, string cont)
        {
            if (FindConsRecord(cont) == null) { return null; }
            ConsVarRecord consVar = new();
            consVar.Pos = RecordPos.DATA;
            ParseConsRecordInput(consVar, type, cont);
            consTable.AddRecord(consVar);
            return consVar;
        }

        internal ConsArrayRecord CreateConsArrayRecord(VarType type, string cont)
        {
            if (FindConsRecord(cont) == null) { return null; }
            ConsArrayRecord consArray = new();
            consArray.Pos = RecordPos.DATA;
            ParseConsRecordInput(consArray, type, cont);
            consTable.AddRecord(consArray);
            return consArray;
        }

        // Constant Find

        private IRecord FindConsRecord(string cont)
        {
            for (int i = 0; i < consTable.Count; i++)
            {
                IRecord rec = consTable[i];
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
            if (IsLocalRecordExist(name))
            {
                VarRecord record = new();
                record.Name = name;
                record.Type = type;
                record.Pos = RecordPos.MEM;
                currentTable.AddRecord(record);
                return record;
            }
            return null;
        }

        internal ArrayRecord CreateLocalArrayRecord(string name, VarType type, List<int> dimList)
        {
            if (IsLocalRecordExist(name))
            {
                ArrayRecord record = new();
                record.Name = name;
                record.Type = type;
                record.Pos = RecordPos.MEM;
                record.SetDimList(dimList);
                currentTable.AddRecord(record);
                return record;
            }
            return null;
        }

        internal TempVarReocrd CreateTempVarRecord(VarType type)
        {
            TempVarReocrd record = new();
            record.Type = type;
            record.Pos = RecordPos.MEM;
            currentTable.AddRecord(record);
            return record;
        }

        // General Record Find

        private bool IsLocalRecordExist(string name)
        {
            for (int i = 0; i < currentTable.Count; i++)
            {
                if (name == currentTable[i].Name) { return true; }
            }
            return false;
        }

        internal IRecord FindRecord(string name)
        {
            ScopeTable scope = currentTable;
            while (true)
            {
                for (int i = 0; i < scope.Count; i++)
                {
                    if (scope[i].Name == name) { return scope[i]; }
                }
                if (scope == globalTable) { return null; }
                scope = scope.Parent;
            }
        }
    }
}
