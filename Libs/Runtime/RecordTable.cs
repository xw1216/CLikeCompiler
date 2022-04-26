using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Runtime;
using CLikeCompiler.Libs.Unit.Quad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class RecordTable
    {
        private FuncRecord mainFunc = null;

        private readonly List<ScopeTable> scopeTables;
        private readonly List<FuncRecord> funcTable;
        private readonly List<LabelRecord> labelTable;
        private readonly List<CallRecord> callTable;

        private readonly ScopeTable consTable;
        private readonly ScopeTable globalTable;

        private ScopeTable currentTable;

        internal RecordTable()
        {
            scopeTables = new();
            funcTable = new();
            labelTable = new();
            callTable = new();

            consTable = new();
            globalTable = new();

            // TODO 检查全局表父指针的必要性
            globalTable.Parent = globalTable;
            scopeTables.Add(consTable);
            scopeTables.Add(globalTable);

            currentTable = globalTable;
        }

        internal void ResetRecordTable()
        {
            mainFunc = null;
            funcTable.Clear();
            consTable.Clear();
            labelTable.Clear();
            callTable.Clear();

            scopeTables.Clear();
            globalTable.Clear();
            currentTable = null;
        }

        // 在本函数内进入新的作用域
        internal ScopeTable EnterScope()
        {
            ScopeTable newTable = new();
            currentTable.AddChildTable(newTable);
            newTable.Parent = currentTable;
            currentTable = newTable;
            return newTable;
        }

        // 离开本函数当前作用域
        internal ScopeTable LeaveScope()
        {
            // TODO 记录函数四元式开始与结束位置
            if (currentTable == null || currentTable == globalTable)
            { return globalTable; }

            currentTable = currentTable.Parent;
            return currentTable;
        }

        // ------------------------- 跳转标签相关 ---------------------------------

        internal LabelRecord CreateLabelRecord(Quad quad, string name)
        {
            foreach (LabelRecord item in labelTable)
            {
                if (item.Name == name) { return null; }
            }
            LabelRecord label = new(quad, name);
            if(quad == null ) { quad.Label = label; }
            labelTable.Add(label);
            return label;
        }

        internal LabelRecord CreateTmpLabelRecord(Quad quad)
        {
            LabelRecord tmpLabel = new(quad, LabelRecord.GetTmpLabelName());
            if (quad == null) { quad.Label = tmpLabel; }
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

        // -------------------------- 函数相关 ---------------------------------

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

        internal FuncRecord CreateFuncRecord(VarType returnType, string name, List<VarRecord> vars, Quad quad)
        {
            FuncRecord func = FindFuncRecord(name, vars);
            if (func != null) { return null; }

            func = new();
            func.Name = name;
            func.ReturnType = returnType;
            func.ArgsList = vars;

            // 设置主入口函数
            if (mainFunc == null && func.Name == "main")
            { mainFunc = func; }

            LabelRecord label = CreateLabelRecord(quad, "Func_" + name + GetArgsAbbr(vars));
            func.Label = label;
            label.ToQuad = quad;
            if(quad.Label == null) { quad.Label = label; }

            func.LocalTable.Parent = globalTable;
            globalTable.AddChildTable(func.LocalTable);

            funcTable.Add(func);
            return func;
        }

        internal FuncRecord FindFuncRecord(string name, List<VarRecord> vars)
        {
            foreach (FuncRecord func in funcTable)
            {
                if (func.IsSignSame(name, vars)) { return func; }
            }
            return null;
        }

        // 函数调用关系记录
        internal CallRecord CreateCallRecord(FuncRecord caller, FuncRecord callee)
        {
            if(FindCallRecord(caller, callee) != null) { return null; }
            CallRecord call = new();
            call.Caller = caller;
            call.Callee = callee;
            callTable.Add(call);
            return call;
        }

        internal CallRecord FindCallRecord(FuncRecord caller, FuncRecord callee)
        {
            foreach(CallRecord call in callTable)
            {
                if(call.Callee == callee && call.Caller == caller)
                {
                    return call;
                }
            }
            return null;
        }


        // ----------------------------- 常量相关 ------------------------------------

        // 解析常量
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
                List<string> strList = cont.Split( ',' , 
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
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

        // 创建常量

        internal ConsVarRecord CreateConsRecord(VarType type, string cont)
        {
            if (FindConsRecord(cont) == null) { return null; }
            ConsVarRecord consVar = new();
            ParseConsRecordInput(consVar, type, cont);
            consTable.AddRecord(consVar);
            return consVar;
        }

        internal ConsArrayRecord CreateConsArrayRecord(VarType type, string cont)
        {
            if (FindConsRecord(cont) == null) { return null; }
            ConsArrayRecord consArray = new();
            ParseConsRecordInput(consArray, type, cont);
            consTable.AddRecord(consArray);
            return consArray;
        }

        private IDataRecord FindConsRecord(string cont)
        {
            for (int i = 0; i < consTable.Count; i++)
            {
                IDataRecord rec = consTable[i];
                if (rec.GetRecordType() == RecordType.ARRAY
                    && ((ConsArrayRecord)rec).OriginCont == cont)
                { return rec; }
                else if (rec.GetRecordType() == RecordType.VAR
                    && ((ConsVarRecord)rec).OriginCont == cont)
                { return rec; }
            }
            return null;
        }

        // ------------------------------ 变量部分 -------------------------------

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

        internal VarTempReocrd CreateTempVarRecord(VarType type)
        {
            VarTempReocrd record = new();
            record.Type = type;
            record.Pos = RecordPos.MEM;
            currentTable.AddRecord(record);
            return record;
        }
        
        // 本作用域内重名检查
        private bool IsLocalRecordExist(string name)
        {
            for (int i = 0; i < currentTable.Count; i++)
            {
                if (name == currentTable[i].Name) { return true; }
            }
            return false;
        }

        internal IDataRecord FindRecord(string name)
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
