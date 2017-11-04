using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.Sql
{
    public class Constraint
    {
        public const string UNIQUE = "UNIQUE";
        public const string PRIMARY_KEY = "PRIMARY KEY";
        public const string FOREIGN_KEY = "FOREIGN KEY";
        public const string DEFAULT = "DEFAULT";

        public Constraint()
        {
            Columns = new List<string>();
        }

        public Constraint(string name, string type, params string[] columns)
            : this()
        {
            Name = name;
            Type = type;

            Columns = new List<string>(columns);
        }

        public Constraint(IDataRecord record)
            : this()
        {
            Name = (string)record[Consts.CONSTRAINT_NAME];
            Type = (string)record[Consts.CONSTRAINT_TYPE];
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Columns { get; set; }
    }

    public class ForeignKeyConstraint : Constraint
    {
        public ForeignKeyConstraint()
            : base()
        {
            RefColumns = new List<string>();
        }

        public ForeignKeyConstraint(string name, string[] columns, string refTable, string[] refColumns)
            : base(name, Constraint.FOREIGN_KEY, columns)
        {
            RefTable = refTable;
            RefColumns = new List<string>(refColumns);
        }

        public ForeignKeyConstraint(IDataRecord record)
            : base(record)
        {
            RefColumns = new List<string>();
        }

        public List<string> RefColumns { get; set; }
        public string RefTable { get; set; }
    }

    public class DefaultConstraint : Constraint
    {
        public DefaultConstraint()
            : base()
        {

        }

        public DefaultConstraint(string tableName, string column, string value)
            : base(string.Format("DFLT_{0}_{1}", tableName, column), Constraint.DEFAULT, column)
        {
            Value = value;
            Columns = new List<string>() { column };
        }

        public string Value { get; set; }
        public string Column
        {
            get
            {
                return Columns.SingleOrDefault();
            }
        }
    }

    public class UniqueConstraint : Constraint
    {
        public UniqueConstraint()
        {

        }

        public UniqueConstraint(string table, params string[] columns)
            : base(null, Constraint.UNIQUE, columns)
        {
            Array.Sort(columns);
            Name = string.Format("UK_{0}_{1}", table, string.Join("_", columns));
        }

        public UniqueConstraint(IDataReader record)
            : base(record)
        {

        }


        public string Filter { get; set; }
    }
}
