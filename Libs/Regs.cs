using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs
{
    internal class RegFiles
    {
        private List<Regs> regs;

        private static readonly List<string> regStdList = new()
        {
            "zero",     "ra" ,  "sp",   "gp",
            "tp",   "t0",   "t1",   "t2",
            "fp",   "s1",   "a0",   "a1",
            "a2",   "a3",   "a4",   "a5",
            "a6",   "a7",   "s2",   "s3",
            "s4",   "s5",   "s6",   "s7",
            "s8",   "s9",   "s10",  "s11",
            "t3",   "t4",   "t5",   "t6"
        };
        internal RegFiles()
        {
            regs = new();
            for (int i = 0; i < regStdList.Count; i++)
            {
                regs.Add(new Regs(i));
            }
        }

        internal static string GetRegName(int index) 
        {
            return regStdList[index];
        }

        internal Regs FindRegs(int index)
        {
            if(index < regs.Count && index >= 0) 
                { return regs[index]; }
            return null;
        }

        internal Regs FindRegs(string name)
        {
            int index = regStdList.IndexOf(name);
            if(index >= 0) { return regs[index]; }
            else { return null; }
        }
    }

    internal class Regs : IRecord
    {
        public RecordType GetRecordType()
        {
            return RecordType.REGS;
        }

        internal int Index { get; private set; }
        public string Name { get => RegFiles.GetRegName(Index); set {; } }
        internal IRecord Cont { get; set; }

        internal Regs(int index)
        {
            Index = index;
        }

    }
}
