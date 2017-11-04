using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.Sql
{
    public class Column
    {
        public Column()
        {

        }

        public Column(string name, SqlDbType dataType, int length = 0, bool nullable = true, bool identity = false, string computed = null)
        {
            Name = name;
            DataType = dataType.ToString();
            Length = length;
            IsNullable = nullable;
            Identity = identity;
            ComputedDefinition = computed;
        }

        public Column(IDataRecord record)
        {
            Name = (string)record[Consts.COLUMN_NAME];
            DataType = (string)record[Consts.DATA_TYPE];
            Length = record[Consts.CHAR_MAX_LENGTH] is DBNull ? 0 : Convert.ToInt32(record[Consts.CHAR_MAX_LENGTH]);
            IsNullable = (string)record[Consts.IS_NULLABLE] == "YES" ? true : false;
            ComputedDefinition = record[Consts.COLUMN_COMPUTED_DEFINITION] is DBNull ? null : (string)record[Consts.COLUMN_COMPUTED_DEFINITION];
        }

        public string Name { get; set; }
        public string DataType { get; set; }
        public int Length { get; set; }
        public bool IsNullable { get; set; }
        public bool Identity { get; set; }
        public string ComputedDefinition { get; set; }
        public bool IsComputed
        {
            get
            {
                return !string.IsNullOrEmpty(ComputedDefinition);
            }
        }
    }
}
