using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Sql
{
    public class StoredProcedure
    {
        public StoredProcedure()
        {

        }

        public StoredProcedure(string name, string definition)
        {
            Name = name;
            Definition = definition;
        }

        public StoredProcedure(IDataRecord record)
        {
            Name = (string)record[Consts.ROUTINE_NAME];
            Definition = (string)record[Consts.ROUTINE_DEFINITION];
        }

        public string Name { get; set; }
        public string Definition { get; set; }
        //public List<Column> Parameters { get; set; }
    }

    //public class Insert : Statement
    //{
    //    public Table Table { get; set; }
    //    public List<Column> Columns { get; set; }
    //    public List<string> Values { get; set; }
    //}
}
