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
using CLikeCompiler.Libs.Unit.Quad;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class FuncRecord : IRecord
    {
        internal const int Dword = 8;
        internal const int SaveBaseLen = 16;

        public string Name { get; set; } = "";
        internal VarType ReturnType { get; set; }
        internal LabelRecord Label { get; set; }
        internal ScopeTable LocalTable { get; } = new();

        internal Quad QuadStart { get; set; } = null;
        internal Quad QuadEnd { get; set; } = null;

        // 经过寄存器分配后 函数实际使用的寄存器
        internal List<Regs> UsedRegList { get; set; }
        internal List<Regs> SaveRegList { get; set; }

        // 局部变量总长度
        internal int VarLength { get; private set; } = 0;
        // 保存寄存器总长度（含返回地址与 old fp)
        internal int SaveLength { get; private set; } = SaveBaseLen;
        // 栈帧长度
        internal int FrameLength => VarLength + SaveLength;

        // 参数总长度
        internal int ArgLength { get; private set; } = 0;
        private List<VarRecord> argsList;

        internal List<VarRecord> ArgsList
        {
            get => argsList;
            set
            {
                if (value == null) { return; }
                argsList = value;
                
                CalcuArgPlace();
            }
        }

        internal FuncRecord() { }

        public RecordType GetRecordType()
        {
            return RecordType.FUNC;
        }

        internal void AddUsedRegs(Regs reg)
        {
            if(!(UsedRegList.Contains(reg)))
            {
                UsedRegList.Add(reg);
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

        private void CalcuArgPlace()
        {
            ArgLength = 0;

            for (int i = 0; i < argsList.Count; i++)
            {
                LocalTable.AddRecord(argsList[i]);
                // 前 8 个函数参数在寄存器 a0 ~ a7 中
                if (i < 8)
                {
                    argsList[i].Pos = RecordPos.REG;
                    argsList[i].Reg = Compiler.regFiles.FindFuncArgRegs(i);
                }
                else
                {
                    argsList[i].Pos = RecordPos.MEM;
                }

                // 地址对齐
                int vacancy;
                if ((vacancy = ArgLength % argsList[i].Width) != 0)
                {
                    ArgLength += argsList[i].Width - vacancy;
                }
                argsList[i].Offset = ArgLength;

                ArgLength += argsList[i].Width;
            }
        }

        /// <summary>
        /// 计算本地保存的寄存器
        /// </summary>
        /// <remarks>
        ///  分配寄存器后才能调用
        /// </remarks>
        private void CalcuSavePlace()
        {
            SaveRegList = RegFiles.CalcuCalleeSaveList(this);
            SaveLength = SaveRegList.Count * Dword + SaveBaseLen;
        }

        /// <summary>
        /// 将函数内所有的局部变量按出现的先后顺序排列
        /// </summary>
        /// <param name="queue">变量队列</param>
        /// <param name="table">函数起始变量表</param>
        private static void ScopeSortRecur(ICollection<ScopeTable> queue, ScopeTable table)
        {
            if (table == null) { return; }
            queue.Add(table);
            for (int i = 0; i < table.Children.Count; i++)
            {
                ScopeSortRecur(queue, table.Children[i]);
            }
        }

        /// <summary>
        /// 计算所有局部变量的位置
        /// </summary>
        /// <remarks>
        ///  分配寄存器后才能调用
        /// </remarks>
        private void CalcuVarsPlace()
        {
            List<ScopeTable> queue = new();
            List<IDataRecord> localVars = new();
            ScopeSortRecur(queue, LocalTable);

            foreach (var table in queue)
            {
                for(int j = 0; j < table.Count; j++)
                {
                    localVars.Add(table[j]);
                }
            }

            int offset = -SaveLength;
            foreach (var data in localVars)
            {
                int vacancy;
                if((vacancy = Math.Abs(offset % data.Width)) != 0)
                {
                    offset -= (data.Width - vacancy);
                }
                offset -= data.Width;
                data.Pos = RecordPos.MEM;
                data.Offset = offset;
            }

            ArgLength = Math.Abs(offset) - SaveLength;
        }
    }
}
