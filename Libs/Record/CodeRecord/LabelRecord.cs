using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class LabelRecord : IRecord
    {
        private static int tmpCnt;
        public string Name { get; set; }

        internal Quad ToQuad { get; set; }

        internal LabelRecord(Quad quad, string name)
        {
            ToQuad = quad;
            Name = name;
        }

        public RecordType GetRecordType()
        {
            return RecordType.LABEL;
        }

        internal static string GetTmpLabelName()
        {
            return ".L" + tmpCnt++;
        }
    }
}
