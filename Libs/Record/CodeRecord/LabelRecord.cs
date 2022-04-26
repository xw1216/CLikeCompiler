using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
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
}
