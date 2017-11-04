using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Sql
{
    public class Table
    {

        public Table()
        {
            Columns = new List<Column>();
            Constraints = new List<Constraint>();
        }

        public Table(string name)
            : this()
        {
            Name = name;
        }

        public Table(IDataRecord record)
            : this()
        {
            Name = (string)record[Consts.TABLE_NAME];
        }

        public string FileGroup { get; set; }
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
        public List<Constraint> Constraints { get; set; }

        public Column GetColumnByName(string name)
        {
            return Columns.Find(col => col.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public Constraint GetConstraintByName(string name)
        {
            return Constraints.Find(cnst => cnst.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
