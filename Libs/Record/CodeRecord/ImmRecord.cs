using CLikeCompiler.Libs.Enum;
using CLikeCompiler.Libs.Record.Interface;

namespace CLikeCompiler.Libs.Record.CodeRecord
{
    internal class ImmRecord : IRecord
    {
        public string Name { get; set; }

        internal string Value { get; set; }

        public ImmRecord(string name, long num)
        {
            Name = name;
            Value = num.ToString();
        }

        public ImmRecord(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public RecordType GetRecordType()
        {
            return RecordType.IMM;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
