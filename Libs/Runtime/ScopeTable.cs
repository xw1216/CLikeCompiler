using CLikeCompiler.Libs.Record.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLikeCompiler.Libs.Runtime
{
    internal class ScopeTable
    {
        internal ScopeTable Parent { get; set; } = null;
        internal List<ScopeTable> Children = new();

        private readonly List<IDataRecord> records = new();
        internal int Count { get { return records.Count; } }

        internal IDataRecord this[int i]
        {
            get { return records[i]; }
            set { records[i] = value; }
        }

        internal void Clear()
        {
            Parent = null;
            Children.Clear();
            records.Clear();
        }

        internal void AddRecord(IDataRecord rec)
        {
            records.Add(rec);
        }

        internal void AddChildTable(ScopeTable table)
        {
            Children.Add(table);
        }

        internal bool ContainsRecord(IDataRecord rec)
        {
            return records.Contains(rec);
        }

        internal bool CreateRecord(IDataRecord rec)
        {
            // We can create record when
            // no same name record is included in state block table
            if (rec == null) { return false; }
            foreach (IDataRecord lhs in records)
            {
                if (lhs.Name == rec.Name) { return false; }
            }
            records.Add(rec);
            return true;
        }
    }
}
