//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace NbuLibrary.Core.Sql
//{
//    public class Select
//    {
//        public Select()
//        {
//            Where = new WhereClause();
//        }

//        public WhereClause Where { get; set; }

//        public Select(string table, params string[] columns)
//        {
//        }
//    }

//    public class WhereClause
//    {
//        public WhereClause()
//        {
//            Conditions = new List<string>();
//        }

//        public List<string> Conditions { get; set; }

//        public void IsWithValue(string column, object value)
//        {
//            throw new NotImplementedException();
//        }

//        public void IsWithParameter(string column, string parameter)
//        {
//            Conditions.Add(string.Format(""));
//        }

//        public override string ToString()
//        {

//        }
//    }

//    public class UpdateSql
//    {
//        public UpdateSql(string table)
//        {
//            this.Table = table;
//            Columns = new List<string>();
//            Values = new List<object>();
//            Where = new WhereClause();
//        }

//        public List<string> Columns { get; set; }
//        public List<object> Values { get; set; }
//        public string Table { get; set; }
//        public WhereClause Where { get; set; }

//        public void SetWithValue(string column, object value)
//        {
//            Columns.Add(column);
//            Values.Add(value);
//        }

//        public SqlCommand ToSqlCommand()
//        {
//            SqlCommand cmd = new SqlCommand(this.ToString());
//            int idx = 0;
//            foreach (var col in Columns)
//            {
//                cmd.Parameters.AddWithValue(col, Values[idx++]);
//            }

//            return cmd;
//        }

//        public override string ToString()
//        {
//            StringBuilder sb = new StringBuilder();
//            sb.AppendFormat("UPDATE [{0}] SET ", Table);
//            for (int i = 0; i < Columns.Count; i++)
//            {
//                sb.AppendFormat("\t[{0}] = @{0}", Columns[i]);
//                if (i < Columns.Count - 1)
//                    sb.Append(",\n");
//            }
//            sb.Append(Where.ToString());

//            return sb.ToString();
//        }
//    }
//}
