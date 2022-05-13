using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.CodeRecord;
using CLikeCompiler.Libs.Record.DataRecord;
using CLikeCompiler.Libs.Record.Interface;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Runtime
{
    internal class RecordTable
    {
        private FuncRecord mainFunc = null;

        #region Code Assist Records
        private readonly List<FuncRecord> funcTable;
        private readonly List<LabelRecord> labelTable;
        private readonly List<CallRecord> callTable;
        #endregion

        #region Data Records
        private readonly ScopeTable consTable;
        private readonly List<ScopeTable> scopeTables;
        private readonly ScopeTable globalTable;
        #endregion
        
        private ScopeTable currentTable;
        private FuncRecord currentFunc;
        private bool isFuncScopeSkip = false;


        #region Init

        internal RecordTable()
        {
            scopeTables = new List<ScopeTable>();
            funcTable = new List<FuncRecord>();
            labelTable = new List<LabelRecord>();
            callTable = new List<CallRecord>();

            consTable = new ScopeTable();
            globalTable = new ScopeTable
            {
                Parent = null
            };

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
            currentFunc = null;
            isFuncScopeSkip = false;
        }

        internal List<FuncRecord> GetFuncList()
        {
            return funcTable;
        }

        internal List<CallRecord> GetCallList()
        {
            return callTable;
        }

        internal ScopeTable GetGlobalTable()
        {
            return globalTable;
        }

        internal void MarkGlobalDataRecord()
        {
            for (int i = 0; i < globalTable.Count; i++)
            {
                globalTable[i].IsGlobal = true;
            }
        }

        #endregion


        #region Scope Related

        internal FuncRecord GetFuncRecord()
        {
            return currentFunc;
        }

        internal bool IsGlobalScope()
        {
            return currentTable == globalTable;
        }

        // 在本函数内进入新的作用域（新建函数时不调用）
        internal ScopeTable EnterScope()
        {
            if (isFuncScopeSkip)
            {
                isFuncScopeSkip = false;
                return currentTable;
            }
            ScopeTable newTable = new();
            currentTable.AddChildTable(newTable);
            newTable.Parent = currentTable;
            currentTable = newTable;
            return newTable;
        }

        // 离开本函数内的当前作用域
        internal ScopeTable LeaveScope()
        {
            if(currentTable == null || currentTable.Parent == null)
            {
                throw new ArgumentNullException();
            }

            if (currentTable == globalTable)
            {
                currentFunc = null;
                return globalTable;
            }

            currentTable = currentTable.Parent;
            if (currentTable == globalTable)
            {
                currentFunc = null;
            }
            return currentTable;
        }

        #endregion

        #region Label Related

        // ------------------------- 跳转标签相关 ---------------------------------

        internal LabelRecord CreateLabelRecord(Quad quad, string name)
        {
            if (FindLabelRecord(name) != null)
            {
                return null;
            }
            LabelRecord label = new(quad, name);
            if(quad.Label == null ) { quad.Label = label; }
            label.ToQuad = quad;
            labelTable.Add(label);
            return label;
        }

        internal LabelRecord CreateTmpLabelRecord(Quad quad)
        {
            if (quad.Label != null)
            {
                return quad.Label;
            }
            LabelRecord tmpLabel = new(quad, LabelRecord.GetTmpLabelName());
            quad.Label = tmpLabel;
            tmpLabel.ToQuad = quad;
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

        #endregion

        #region Func Related
        
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

        internal FuncRecord CreateFuncRecord(VarType returnType, string name, Dictionary<string, VarType> paramDict, Quad quad)
        {
            List<VarRecord> vars = new();

            FuncRecord func = new()
            {
                Name = name,
                ReturnType = returnType
            };
            currentTable = func.LocalTable;
            foreach (KeyValuePair<string, VarType> pair in paramDict)
            {
                VarRecord var = CreateArgRecord(pair.Key, pair.Value);
                if (var == null)
                {
                    throw new ArgumentException("重复的函数参数名");
                }
                vars.Add(var);
            }

            FuncRecord findFunc = FindFuncRecord(name, vars);
            if (findFunc != null) { return null; }

            func.ArgsList = vars;

            isFuncScopeSkip = true;
            // 设置主入口函数
            if (mainFunc == null && func.Name == "main")
            { mainFunc = func; }
            // 设置函数跳转标签
            LabelRecord label = CreateLabelRecord(quad, "Func_" + name + GetArgsAbbr(vars));
            func.Label = label;
            // 设置第一条四元式位置
            func.QuadStart = quad;
            // 设置函数作用域
            func.LocalTable.Parent = globalTable;
            globalTable.AddChildTable(func.LocalTable);
            // 设置当前函数 
            currentFunc = func;

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

            CallRecord call = new(caller, callee);
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

        #endregion

        #region Const Related

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
                case VarType.VOID:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
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
            if (result == null) throw new ArgumentNullException(nameof(result));
            Type type = typeof(T);
            var parse = type.GetMethod("TryParse");
            if (parse == null)
            {
                throw new MissingMethodException("");
            }
            result = new List<object>();

            foreach (string str in strList)
            {
                var args = new[] { str, Activator.CreateInstance(type) };
                parse.Invoke(null, args);
                result.Add((T)args[1]);
            }
        }

        // 创建常量

        internal ConsVarRecord CreateConsRecord(VarType type, string cont)
        {
            ConsVarRecord consRecord = (ConsVarRecord)FindConsRecord(cont);
            if (consRecord != null) { return consRecord; }
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

        #endregion


        #region Var Related

        // ------------------------------ 变量部分 -------------------------------

        internal VarRecord CreateArgRecord(string name, VarType type)
        {
            if (!IsLocalRecordExist(name))
            {
                VarRecord record = new()
                {
                    Name = name,
                    Type = type,
                    Pos = RecordPos.REG
                };
                return record;
            }
            return null;
        }

        internal VarRecord CreateLocalVarRecord(string name, VarType type)
        {
            if (!IsLocalRecordExist(name))
            {
                VarRecord record = new()
                {
                    Name = name,
                    Type = type,
                    Pos = RecordPos.MEM
                };
                currentTable.AddRecord(record);
                return record;
            }
            return null;
        }

        internal ArrayRecord CreateLocalArrayRecord(string name, VarType type, List<int> dimList)
        {
            if (!IsLocalRecordExist(name))
            {
                ArrayRecord record = new()
                {
                    Name = name,
                    Type = type,
                    Pos = RecordPos.MEM
                };
                record.SetDimList(dimList);
                currentTable.AddRecord(record);
                return record;
            }
            return null;
        }

        internal VarTempRecord CreateTempVarRecord(VarType type)
        {
            VarTempRecord record = new()
            {
                Type = type,
                Pos = RecordPos.MEM
            };
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

        #endregion
    }
}
