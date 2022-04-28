using CLikeCompiler.Libs.Record.CodeRecord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Unit.Reg
{
    internal class RegFiles
    {
        private readonly List<Regs> regs;

        internal RegFiles()
        {
            regs = new List<Regs>();
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
            if (index < regs.Count && index >= 0)
            { return regs[index]; }
            return null;
        }

        internal Regs FindRegs(string name)
        {
            int index = regStdList.IndexOf(name);
            if (index >= 0) { return regs[index]; }
            else { return null; }
        }

        internal Regs FindFuncArgRegs(int index)
        {
            if(index < 0 || index > 7)
            {
                return null;
            } else
            {
                return FindRegs("a" + index);
            }
        }

        internal static List<Regs> CalcuCallerSaveList(FuncRecord caller, FuncRecord callee)
        {
            List<Regs> saveList = new();
            List<Regs> callerUseList = caller.UsedRegList;
            List<Regs> calleeUseList = callee.UsedRegList;

            ListIntersect(callerUseList, calleeUseList, out List<Regs> list);
            ListIntersect(list, saveList, out List<Regs> result);
            return result;
        }

        internal static List<Regs> CalcuCalleeSaveList(FuncRecord callee)
        {
            List<Regs> saveList = new();
            List<Regs> calleeUseList = callee.UsedRegList;
            ListIntersect(calleeUseList, saveList, out List<Regs> result);
            return saveList;
        }

        private static void ListIntersect(List<Regs> lhs, List<Regs> rhs, out List<Regs> result)
        {
             result = new List<Regs>();
            foreach(Regs r in lhs)
            {
                if(rhs.Contains(r))
                {
                    result.Add(r);
                }
            }
        }

        private static readonly List<string> regStdList = new()
        {
            "zero",
            "ra",
            "sp",
            "gp",
            "tp",
            "t0",
            "t1",
            "t2",
            "fp",
            "s1",
            "a0",
            "a1",
            "a2",
            "a3",
            "a4",
            "a5",
            "a6",
            "a7",
            "s2",
            "s3",
            "s4",
            "s5",
            "s6",
            "s7",
            "s8",
            "s9",
            "s10",
            "s11",
            "t3",
            "t4",
            "t5",
            "t6"
        };

        private static readonly List<int> regCallerSaveList = new()
        {
            5,
            6,
            7,
            12,
            13,
            14,
            15,
            16,
            17,
            28,
            29,
            30,
            31
        };

        private static readonly List<int> regCalleeSaveList = new()
        {
            9,
            18,
            19,
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27
        };
    }
}
