using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.Services.tmp;
using NbuLibrary.Core.Domain;

namespace NbuLibrary.Core.EntityLogic
{
    public class DatabaseBuilder
    {
        private List<IDbCommand> _batch = new List<IDbCommand>();

        public void CreateTable(string name, ColumnInfo[] columns, ConstraintBase[] constraints)
        {
            StringBuilder sql = new StringBuilder("CREATE TABLE ");
            sql.AppendFormat("[{0}] (\n", name);

            for (int i = 0; i < columns.Length; i++)
            {
                var sqlType = GetSqlType(columns[i].DataType);
                if (sqlType == SqlDbType.NVarChar.ToString())
                    sqlType = string.Format("{0}({1})", sqlType, columns[i].Size);
                else if (sqlType == SqlDbType.Decimal.ToString())
                    sqlType = string.Format("{0}(18, {1})", sqlType, 9);
                sql.AppendFormat("[{0}] {1} {2} {3}", columns[i].Name, sqlType, columns[i].Nullable ? "NULL" : "NOT NULL", columns[i].Identity ? "IDENTITY" : "");
                if (i < columns.Length - 1)
                    sql.Append(",\n");
            }

            for (int i = 0; i < constraints.Length; i++)
            {
                if (constraints[i].Type == ConstraintBase.ConstraintType.PrimaryKey)
                {
                    sql.AppendFormat("\n, CONSTRAINT PK_{0} {1}", name, constraints[i].Sql);
                }
                else if (constraints[i].Type == ConstraintBase.ConstraintType.ForeignKey)
                {
                    sql.AppendFormat("\n, CONSTRAINT FK_{0}_{1} {2}", name, ((ForeignKeyConstraint)constraints[i]).RefTable, constraints[i].Sql);
                }
                else if (constraints[i].Type == ConstraintBase.ConstraintType.Unique)
                    sql.AppendFormat("\n, CONSTRAINT UK_{0} {1}", name, constraints[i].Sql);
                //if (i < constraints.Length - 1)
                //    sql.Append(",\n");
            }

            sql.Append(")\n");

            _batch.Add(new SqlCommand(sql.ToString()));
        }
        //public void AddConstraints(string tableName, ConstraintBase[] constraints)
        //{
        //    StringBuilder sql = new StringBuilder();

        //    foreach (var constraint in constraints)
        //    {
        //        sql.AppendFormat("ALTER TABLE {0}\n", tableName);
        //        sql.AppendFormat("ADD CONSTRAINT {0};\n", constraint.Sql);
        //    }
        //}
       

        public void DropTable(string name)
        {
            throw new NotImplementedException();
        }

        public void CreateProcedure(string name, ParameterInfo[] parameters, string body)
        {
            StringBuilder sql = new StringBuilder("CREATE PROC ");
            sql.AppendFormat("[{0}] \n", name);

            for (int i = 0; i < parameters.Length; i++)
            {
                string sqlType = GetSqlType(parameters[i].DataType);
                if (sqlType == SqlDbType.NVarChar.ToString())
                    sqlType = string.Format("{0}({1})", sqlType, parameters[i].Size);
                else if (sqlType == SqlDbType.Decimal.ToString())
                    sqlType = string.Format("{0}(18, {1})", sqlType, 9);
                sql.AppendFormat("@{0} {1} {2}", parameters[i].Name, sqlType, parameters[i].Output ? "OUTPUT" : "");
                if (i < parameters.Length - 1)
                    sql.Append(",\n");
            }
            sql.Append("\nAS \nBEGIN \n");
            sql.Append(body);
            sql.Append("\nEND\n");

            _batch.Add(new SqlCommand(sql.ToString()));
        }
        public void DropProcedure(string name)
        {
            throw new NotImplementedException();
        }

        public string BuildInsertStatement(string table, string[] columns, string[] values)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("INSERT INTO [{0}] \n(", table);
            for (int i = 0; i < columns.Length; i++)
            {
                sql.AppendFormat("[{0}]", columns[i]);
                if (i < columns.Length - 1)
                    sql.Append(",\n");
            }
            sql.Append(") VALUES \n (");

            for (int i = 0; i < values.Length; i++)
            {
                sql.Append(values[i]);
                if (i < columns.Length - 1)
                    sql.Append(",\n");
            }
            sql.Append(")\n");

            return sql.ToString();
        }

        public static string GetTableFor(EntityRelation relation)
        {
            return string.Format("{0}_{1}_{2}", relation.LeftEntity, relation.RightEntity, relation.Role);
        }

        public IEnumerable<IDbCommand> GetSqlBatch()
        {
            return _batch;
        }

        private string GetSqlType(PropertyTypes dataType)
        {
            switch (dataType)
            {
                case PropertyTypes.Integer:
                    return SqlDbType.Int.ToString();
                case PropertyTypes.Number:
                    return SqlDbType.Decimal.ToString();
                case PropertyTypes.String:
                    return SqlDbType.NVarChar.ToString();
                case PropertyTypes.EnumValue:
                    return SqlDbType.TinyInt.ToString();
                case PropertyTypes.Date:
                    return SqlDbType.DateTime.ToString();
                case PropertyTypes.Boolean:
                    return SqlDbType.Bit.ToString();
                default:
                    throw new NotImplementedException(string.Format("GetSqlType for type {0} is not yet implemented.", dataType));
            }
        }

        //TODO: TMP class holder
        public static string BuildCondition(Condition cond, EntityDefinition definition, string table)
        {
            string sqlCondition = null;

            var property = definition.Properties.Find(p => p.Name == cond.Property);
            if (property == null && cond.Property == "Id")
                property = new PropertyDefinition("Id", PropertyTypes.Integer, false);
            else if (property == null)
                throw new ArgumentException("Invalid property!");

            var column = string.Format("{0}.{1}", table, property.Name);

            string value = null;
            if (cond.Values.Single() is decimal)
                value = ((decimal)(object)cond.Values.Single()).ToString(System.Globalization.CultureInfo.InvariantCulture);
            else if (property.Type == PropertyTypes.EnumValue)
                value = (Convert.ToByte(cond.Values.Single())).ToString();
            else if (property.Type == PropertyTypes.Boolean)
                value = Convert.ToBoolean(cond.Values.Single()) ? "1" : "0";
            else
                value = cond.Values.Single().ToString();

            if (cond.Operator == Condition.Is)
            {
                if (property.Type == PropertyTypes.String)
                    sqlCondition = string.Format("{0} = '{1}'", column, value);
                else
                    sqlCondition = string.Format("{0} = {1}", column, value);
            }
            else if (cond.Operator == Condition.Not)
            {
                if (property.Type == PropertyTypes.String)
                    sqlCondition = string.Format("{0} <> '{1}'", column, value);
                else
                    sqlCondition = string.Format("{0} <> {1}", column, value);
            }
            else if (cond.Operator == Condition.LessThen)
                sqlCondition = string.Format("{0} < {1}", column, value);
            else if (cond.Operator == Condition.LessThenOrEqual)
                sqlCondition = string.Format("{0} <= {1}", column, value);
            else if (cond.Operator == Condition.GreaterThen)
                sqlCondition = string.Format("{0} > {1}", column, value);
            else if (cond.Operator == Condition.GreaterThenOrEqual)
                sqlCondition = string.Format("{0} >= {1}", column, value);
            else if (cond.Operator == Condition.StartsWith && property.Type == PropertyTypes.String)
                sqlCondition = string.Format("{0} LIKE '{1}%'", column, value);
            else
                throw new NotImplementedException("Unrecognized operator!");

            return sqlCondition;
        }

    }

    public abstract class ConstraintBase
    {
        public enum ConstraintType
        {
            PrimaryKey,
            ForeignKey,
            Unique
        }
        public abstract ConstraintType Type { get; }
        public string Sql { get; protected set; }
    }

    public class PrimaryKeyConstraint : ConstraintBase
    {
        public PrimaryKeyConstraint(params string[] columns)
        {
            StringBuilder sb = new StringBuilder("PRIMARY KEY(");
            for (int i = 0; i < columns.Length; i++)
            {
                sb.Append(columns[i]);
                if (i < columns.Length - 1)
                    sb.Append(", ");
                else
                    sb.Append(")");
            }

            Sql = sb.ToString();
        }

        public override ConstraintBase.ConstraintType Type
        {
            get { return ConstraintType.PrimaryKey; }
        }
    }

    public class ForeignKeyConstraint : ConstraintBase
    {
        public string RefTable { get; private set; }

        public ForeignKeyConstraint(string[] columns, string refTable, string[] refColumns)
        {
            RefTable = refTable;
            if (columns.Length != refColumns.Length)
                throw new ArgumentException("The number of columns in the foreign key constraint must match the number of referenced columns.");

            StringBuilder sbCols = new StringBuilder();
            StringBuilder sbRefCols = new StringBuilder();
            for (int i = 0; i < columns.Length; i++)
            {
                sbCols.Append(columns[i]);
                sbRefCols.Append(refColumns[i]);
                if (i < columns.Length - 1)
                {
                    sbCols.Append(", ");
                    sbRefCols.Append(", ");
                }
            }

            Sql = string.Format("FOREIGN KEY({0}) REFERENCES [{1}]({2})", sbCols.ToString(), refTable, sbRefCols.ToString());
        }

        public override ConstraintBase.ConstraintType Type
        {
            get { return ConstraintType.ForeignKey; }
        }
    }

    public class UniqueConstraint : ConstraintBase
    {
        public UniqueConstraint(params string[] columns)
        {
            StringBuilder sb = new StringBuilder("UNIQUE (");
            for (int i = 0; i < columns.Length; i++)
            {
                sb.Append(columns[i]);
                if (i < columns.Length - 1)
                    sb.Append(", ");
                else
                    sb.Append(")");
            }

            Sql = sb.ToString();
        }

        public override ConstraintBase.ConstraintType Type
        {
            get { return ConstraintType.Unique; }
        }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public PropertyTypes DataType { get; set; }
        public int Size { get; set; }
        public bool Output { get; set; }

        public ParameterInfo(string name, PropertyTypes dataType, int size = 0, bool output = false)
        {
            Name = name;
            DataType = dataType;
            Size = size;
            Output = output;
        }
    }

    public class ColumnInfo
    {
        public static ColumnInfo IdentifierColumnInfo
        {
            get
            {
                return new ColumnInfo("Id", PropertyTypes.Integer, 0, false, true);
            }
        }

        public string Name { get; set; }
        public PropertyTypes DataType { get; set; }
        public bool Nullable { get; set; }
        public bool Identity { get; set; }
        public int Size { get; set; }

        public ColumnInfo(string name, PropertyTypes dataType, int size = 0, bool nullable = true, bool identity = false)
        {
            Name = name;
            DataType = dataType;
            Nullable = nullable;
            Identity = identity;
            Size = size;
        }
    }
}
