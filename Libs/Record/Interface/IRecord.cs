using CLikeCompiler.Libs.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Record.Interface
{
    public interface IRecord
    {
        string Name { get; set; }

        RecordType GetRecordType();
    }
}
