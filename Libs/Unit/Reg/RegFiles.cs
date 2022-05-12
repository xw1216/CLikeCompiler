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
        internal List<Regs> CallerSaveList { get; }
        internal List<Regs> CalleeSaveList { get; }

        internal RegFiles()
        {
            regs = new List<Regs>();
            CallerSaveList = new List<Regs>();
            CalleeSaveList = new List<Regs>();
            for (int i = 0; i < RegStdList.Count; i++)
            {
                Regs reg = new (i);
                regs.Add(reg);
                if (RegNumCallerSaveList.Contains(i))
                {
                    CallerSaveList.Add(reg);
                }

                if (RegNumCalleeSaveList.Contains(i))
                {
                    CalleeSaveList.Add(reg);
                }
            }
        }

        internal static string GetRegName(int index)
        {
            return RegStdList[index];
        }

        internal Regs FindRegs(int index)
        {
            if (index < regs.Count && index >= 0)
            { return regs[index]; }
            return null;
        }

        internal Regs FindRegs(string name)
        {
            int index = RegStdList.IndexOf(name);
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

        private static readonly List<string> RegStdList = new()
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

        private static readonly List<int> RegNumCallerSaveList = new()
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

        private static readonly List<int> RegNumCalleeSaveList = new()
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
