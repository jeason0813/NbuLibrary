using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Sql
{
    public class DatabaseManager
    {
        private SqlConnection conn;

        public List<Table> Tables { get; private set; }
        public List<StoredProcedure> StoredProcedures { get; private set; }

        private class FKCacheEntry
        {
            public FKCacheEntry()
            {
                Owners = new List<Table>();
                Constraints = new List<ForeignKeyConstraint>();
            }

            public List<ForeignKeyConstraint> Constraints { get; set; }
            public List<Table> Owners { get; set; }

            public void Add(ForeignKeyConstraint fk, Table owner)
            {
                Owners.Add(owner);
                Constraints.Add(fk);
            }
        }
        private Dictionary<string, FKCacheEntry> _fkCache;


        public DatabaseManager(SqlConnection connection)
        {
            conn = connection;

            Tables = new List<Table>();
            StoredProcedures = new List<StoredProcedure>();
        }

        public void Merge(List<Table> tables, List<StoredProcedure> procs)
        {
            LoadSchema();

            foreach (var table in tables)
            {
                var existing = Tables.Find(tbl => tbl.Name == table.Name);
                if (existing != null)
                    MergeTable(existing, table);
                else
                    CreateTable(table);
            }
            if (procs != null)
                foreach (var proc in procs)
                {
                    var existing = StoredProcedures.Find(sp => sp.Name.Equals(proc.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (existing != null)
                        MergeProcedure(existing, proc);
                    else
                        CreateProcedure(proc);
                }
        }

        public void CreateTable(Table table)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("CREATE TABLE [{0}] (\n", table.Name);
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];

                if (column.IsComputed)
                    sql.AppendFormat(" [{0}] AS ({1})", column.Name, column.ComputedDefinition);
                else if (column.DataType.Equals(SqlDbType.NVarChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    sql.AppendFormat(" [{0}] {1}({2}) {3} {4}", column.Name, column.DataType, column.Length, column.IsNullable ? "NULL" : "NOT NULL", column.Identity ? "IDENTITY" : "");
                else if (column.DataType.Equals(SqlDbType.Decimal.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    sql.AppendFormat(" [{0}] {1} (28, 9) {2}", column.Name, column.DataType, column.IsNullable ? "NULL" : "NOT NULL");
                else
                    sql.AppendFormat(" [{0}] {1} {2} {3}", column.Name, column.DataType, column.IsNullable ? "NULL" : "NOT NULL", column.Identity ? "IDENTITY" : "");

                if (i < table.Columns.Count - 1)
                    sql.AppendFormat(",\n");
            }

            if (table.Constraints.Count > 0)
                sql.AppendFormat(",\n");

            List<Constraint> addAfterTableCreation = new List<Constraint>();

            for (int i = 0; i < table.Constraints.Count; i++)
            {
                var cnst = table.Constraints[i];

                if (cnst.Type.Equals(Constraint.UNIQUE, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (table.Columns.Count(c => cnst.Columns.Contains(c.Name, StringComparer.InvariantCultureIgnoreCase) && c.IsNullable) > 0)
                    {
                        addAfterTableCreation.Add(cnst);
                        continue;
                    }
                    else
                    {
                        sql.AppendFormat(" CONSTRAINT [{0}] UNIQUE ({1})",
                            cnst.Name,
                            string.Join(",", cnst.Columns.Select(col => string.Format("[{0}]", col)))
                            );
                    }

                }
                else if (cnst.Type.Equals(Constraint.FOREIGN_KEY, StringComparison.InvariantCultureIgnoreCase))
                {
                    var fk = (ForeignKeyConstraint)cnst;

                    sql.AppendFormat(" CONSTRAINT [{0}] FOREIGN KEY ({1}) REFERENCES [{2}]({3})",
                        fk.Name,
                        string.Join(",", fk.Columns.Select(col => string.Format("[{0}]", col))),
                        fk.RefTable,
                        string.Join(",", fk.RefColumns.Select(refCol => string.Format("[{0}]", refCol)))
                        );
                }
                else if (cnst.Type.Equals(Constraint.PRIMARY_KEY, StringComparison.InvariantCultureIgnoreCase))
                {
                    sql.AppendFormat(" CONSTRAINT [{0}] PRIMARY KEY ({1})",
                        cnst.Name,
                        string.Join(",", cnst.Columns.Select(col => string.Format("[{0}]", col)))
                        );
                }
                else if (cnst.Type.Equals(Constraint.DEFAULT, StringComparison.InvariantCultureIgnoreCase))
                {
                    addAfterTableCreation.Add(cnst);
                    continue;
                }
                else
                    throw new NotImplementedException(string.Format("Creating constraint of type {0} not implemented.", cnst.Type));

                if (i < table.Constraints.Count - 1)
                    sql.Append(",\n");
            }
            sql.Append("\n)");

            if (!string.IsNullOrEmpty(table.FileGroup))
                sql.AppendFormat(" ON [{0}]", table.FileGroup);

            var createTbl = conn.CreateCommand();
            createTbl.CommandText = sql.ToString();
            createTbl.ExecuteNonQuery();
            addAfterTableCreation.ForEach(cnst => { AddConstraint(table, cnst); });
        }

        public void MergeTable(Table exTable, Table table)
        {
            #region upgrade columns

            table.Columns.ForEach(col =>
            {
                var exColumn = exTable.GetColumnByName(col.Name);
                if (exColumn != null)
                {
                    if (exColumn.IsComputed && col.IsComputed && exColumn.ComputedDefinition != col.ComputedDefinition)
                    {
                        DropColumn(table, col);

                        var addColumn = conn.CreateCommand();
                        addColumn.CommandText = string.Format("ALTER TABLE [{0}] ADD [{1}] AS ({2})", table.Name, col.Name, col.ComputedDefinition);

                        addColumn.ExecuteNonQuery();
                    }
                    else if (exColumn.IsNullable != col.IsNullable
                        || exColumn.Length != col.Length
                        || !exColumn.DataType.Equals(col.DataType.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        //check for computed dependencies
                        var depComputeds = exTable.Columns.Where(c => c.IsComputed && c.ComputedDefinition.ToLower().Contains(string.Format("[{0}]", col.Name.ToLower())));
                        foreach (var depCol in depComputeds)
                        {
                            DropColumn(exTable, depCol);
                        }

                        if (exColumn.Length <= col.Length)
                        {
                            var alterColumn = conn.CreateCommand();
                            if (col.DataType.Equals(SqlDbType.NVarChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
                                alterColumn.CommandText = string.Format("ALTER TABLE [{0}] ALTER COLUMN [{1}] {2}({3}) {4}", table.Name, col.Name, col.DataType, col.Length, col.IsNullable ? "NULL" : "NOT NULL");
                            else
                                alterColumn.CommandText = string.Format("ALTER TABLE [{0}] ALTER COLUMN [{1}] {2} {3}", table.Name, col.Name, col.DataType, col.IsNullable ? "NULL" : "NOT NULL");

                            alterColumn.ExecuteNonQuery();
                        }
                        else
                        {
                            var constraints = Tables.SelectMany(t => t.Constraints).Where(c => c.Columns.Contains(col.Name));

                            foreach (var cnst in constraints)
                            {
                                DropConstraint(exTable, cnst);
                            }

                            DropColumn(table, col);

                            var addColumn = conn.CreateCommand();
                            if (col.DataType.Equals(SqlDbType.NVarChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
                                addColumn.CommandText = string.Format("ALTER TABLE [{0}] ADD [{1}] {2}({3}) {4}", table.Name, col.Name, col.DataType, col.Length, col.IsNullable ? "NULL" : "NOT NULL");
                            else
                                addColumn.CommandText = string.Format("ALTER TABLE [{0}] ADD [{1}] {2} {3}", table.Name, col.Name, col.DataType, col.IsNullable ? "NULL" : "NOT NULL");

                            AddColumn(table, col);

                            foreach (var cnst in constraints)
                            {
                                AddConstraint(exTable, cnst);
                            }
                        }

                        foreach (var depCol in depComputeds)
                        {
                            AddColumn(exTable, depCol);
                        }
                    }
                }
                else
                {
                    //TODO: Default value of not nullable columns
                    AddColumn(table, col);
                }
            });

            #endregion

            table.Constraints.ForEach(cnst =>
            {
                var exCnst = exTable.GetConstraintByName(cnst.Name);
                if (exCnst != null)
                {
                    if (cnst.Type != exCnst.Type)
                        throw new NotSupportedException("Cannot change the type of constraint!");

                    bool diff = cnst.Columns.Count != exCnst.Columns.Count;
                    if (!diff)
                        cnst.Columns.ForEach(col => { if (!exCnst.Columns.Contains(col, StringComparer.InvariantCultureIgnoreCase)) diff = true; });

                    if (!diff && cnst.Type.Equals(Constraint.DEFAULT, StringComparison.InvariantCultureIgnoreCase))
                    {
                        diff = string.Format("({0})", (cnst as DefaultConstraint).Value).Equals((exCnst as DefaultConstraint).Value, StringComparison.InvariantCultureIgnoreCase);
                    }

                    if (diff) //constraing changed
                        throw new NotImplementedException(String.Format("Upgrading existing constraints is not supported({0})", cnst.Name));
                }
                else // create
                {
                    AddConstraint(exTable, cnst);
                }
            });
        }

        public void DropTable(Table table, bool recursive)
        {
            var dropCmd = conn.CreateCommand();
            dropCmd.CommandText = string.Format("DROP TABLE [{0}];", table.Name);

            if (recursive)
            {
                BuildForeignKeysCache();
                List<string> exludedCls = new List<string>();
                foreach (var col in table.Columns)
                {
                    if (exludedCls.Contains(col.Name))
                        continue;
                    string key = string.Format("{0}_{1}", table.Name, col.Name);
                    if (_fkCache.ContainsKey(key))
                    {
                        var entry = _fkCache[key];
                        for (int i = 0; i < entry.Constraints.Count; i++)
                            DropConstraint(entry.Owners[i], entry.Constraints[i]);
                    }
                }
            }

            dropCmd.ExecuteNonQuery();
            if (Tables.Contains(table))
                Tables.Remove(table);
        }

        public void DropColumn(Table table, Column col)
        {
            var dropColumn = conn.CreateCommand();
            dropColumn.CommandText = string.Format("ALTER TABLE [{0}] DROP COLUMN [{1}]", table.Name, col.Name);
            dropColumn.ExecuteNonQuery();
        }

        private void CreateProcedure(StoredProcedure proc)
        {
            var createCmd = conn.CreateCommand();
            createCmd.CommandText = proc.Definition;

            createCmd.ExecuteNonQuery();
        }

        private void MergeProcedure(StoredProcedure existing, StoredProcedure proc)
        {
            if (!existing.Definition.Equals(proc.Definition, StringComparison.InvariantCultureIgnoreCase))
            {
                var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = string.Format("DROP PROCEDURE {0}", proc.Name);
                var createCmd = conn.CreateCommand();
                createCmd.CommandText = proc.Definition;

                dropCmd.ExecuteNonQuery();
                createCmd.ExecuteNonQuery();
            }
        }

        private void CreateUniqueIndex(Table table, Constraint uc)
        {
            var nullableColumns = table.Columns.Where(c => uc.Columns.Contains(c.Name, StringComparer.InvariantCultureIgnoreCase) && c.IsNullable);
            if (nullableColumns.Count() > 0)
            {
                StringBuilder sql = new StringBuilder("create unique index ");
                var allCols = uc.Columns.ToArray();
                Array.Sort(allCols);
                sql.AppendFormat(uc.Name);

                sql.AppendFormat(" ON [{0}] (", table.Name);
                var it = allCols.GetEnumerator();
                bool hasNext = it.MoveNext();
                while (hasNext)
                {
                    sql.AppendFormat("[{0}]", it.Current);
                    hasNext = it.MoveNext();
                    if (hasNext)
                        sql.Append(",");
                }

                sql.AppendFormat(") WHERE ");
                var it2 = nullableColumns.GetEnumerator();
                hasNext = it2.MoveNext();
                while (hasNext)
                {
                    sql.AppendFormat("[{0}] IS NOT NULL", it2.Current.Name);
                    hasNext = it2.MoveNext();
                    if (hasNext)
                        sql.Append(" AND ");
                }

                var addConstraint = conn.CreateCommand();
                addConstraint.CommandText = sql.ToString();
                addConstraint.ExecuteNonQuery();
            }
        }

        private void AddConstraint(Table table, Constraint cnst)
        {
            if (cnst.Type.Equals(Constraint.UNIQUE, StringComparison.InvariantCultureIgnoreCase))
            {
                var nullableColumns = table.Columns.Where(c => cnst.Columns.Contains(c.Name, StringComparer.InvariantCultureIgnoreCase) && c.IsNullable);
                if (nullableColumns.Count() > 0)
                {
                    CreateUniqueIndex(table, cnst as UniqueConstraint);
                }
                else
                {
                    var addConstraint = conn.CreateCommand();
                    addConstraint.CommandText = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] UNIQUE ({2})",
                        table.Name,
                        cnst.Name,
                        string.Join(",", cnst.Columns.Select(col => string.Format("[{0}]", col)))
                        );

                    addConstraint.ExecuteNonQuery();
                }
                table.Constraints.Add(cnst);
            }
            else if (cnst.Type.Equals(Constraint.FOREIGN_KEY, StringComparison.InvariantCultureIgnoreCase))
            {
                var fk = (ForeignKeyConstraint)cnst;

                var addFKConstraint = conn.CreateCommand();
                addFKConstraint.CommandText = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] FOREIGN KEY ({2}) REFERENCES [{3}]({4})",
                    table.Name,
                    fk.Name,
                    string.Join(",", fk.Columns.Select(col => string.Format("[{0}]", col))),
                    fk.RefTable,
                    string.Join(",", fk.RefColumns.Select(refCol => string.Format("[{0}]", refCol)))
                    );

                addFKConstraint.ExecuteNonQuery();
                table.Constraints.Add(cnst);
            }
            else if (cnst.Type.Equals(Constraint.DEFAULT, StringComparison.InvariantCultureIgnoreCase))
            {
                var defCnstr = cnst as DefaultConstraint;
                var addDefaultConstraint = conn.CreateCommand();
                addDefaultConstraint.CommandText = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT DFLT_{0}_{1} DEFAULT ({2}) FOR [{1}]",
                    table.Name,
                    defCnstr.Column,
                    defCnstr.Value);

                addDefaultConstraint.ExecuteNonQuery();
                table.Constraints.Add(defCnstr);
            }
            else
                throw new NotImplementedException(string.Format("Adding constraint of type {0} not implemented.", cnst.Type));
        }

        private void DropConstraint(Table table, Constraint cnst)
        {
            if (cnst.Type == Constraint.UNIQUE)
            {
                var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = string.Format("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]", table.Name, cnst.Name);
                dropCmd.ExecuteNonQuery();
                table.Constraints.Remove(cnst);
            }
            else if (cnst.Type == Constraint.FOREIGN_KEY)
            {
                var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = string.Format("ALTER TABLE [{0}] DROP CONSTRAINT [{1}]", table.Name, cnst.Name);
                dropCmd.ExecuteNonQuery();
                table.Constraints.Remove(cnst);
            }
            else
                throw new NotImplementedException(string.Format("Dropping constraint of type {0} not implemented.", cnst.Type));
        }

        private void AddColumn(Table table, Column column)
        {
            var addColumn = conn.CreateCommand();
            if (column.IsComputed)
                addColumn.CommandText = string.Format("ALTER TABLE [{0}] ADD [{1}] AS ({2})", table.Name, column.Name, column.ComputedDefinition);
            else if (column.DataType.Equals(SqlDbType.NVarChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
                addColumn.CommandText = string.Format("ALTER TABLE [{0}] ADD [{1}] {2}({3}) {4}", table.Name, column.Name, column.DataType, column.Length, column.IsNullable ? "NULL" : "NOT NULL");
            else
                addColumn.CommandText = string.Format("ALTER TABLE [{0}] ADD [{1}] {2} {3}", table.Name, column.Name, column.DataType, column.IsNullable ? "NULL" : "NOT NULL");

            addColumn.ExecuteNonQuery();
        }

        public void LoadSchema()
        {
            Tables.Clear();
            StoredProcedures.Clear();

            var selectTables = conn.CreateCommand();
            selectTables.CommandText = "select * from INFORMATION_SCHEMA.TABLES;";
            using (var reader = selectTables.ExecuteReader())
            {
                while (reader.Read())
                {
                    Tables.Add(new NbuLibrary.Core.Sql.Table(reader));
                }
            }

            var selectProcs = conn.CreateCommand();
            selectProcs.CommandText = string.Format("select * from INFORMATION_SCHEMA.ROUTINES");
            using (var reader = selectProcs.ExecuteReader())
            {
                while (reader.Read())
                {
                    StoredProcedures.Add(new NbuLibrary.Core.Sql.StoredProcedure(reader));
                }
            }

            foreach (var table in Tables)
            {
                var selectCols = conn.CreateCommand();
                selectCols.CommandText = string.Format(@"
                    select cc.definition, ic.* from INFORMATION_SCHEMA.COLUMNS ic
                    INNER JOIN sys.columns c ON ic.COLUMN_NAME = c.name AND c.object_id = OBJECT_ID('{0}')
                    LEFT JOIN sys.computed_columns cc ON c.column_id = cc.column_id
                    WHERE TABLE_NAME = '{0}'", table.Name);
                using (var reader = selectCols.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var col = new NbuLibrary.Core.Sql.Column(reader);
                        table.Columns.Add(col);
                        if (!reader.IsDBNull(reader.GetOrdinal(Consts.COLUMN_DEFAULT)))
                            table.Constraints.Add(new DefaultConstraint(table.Name, col.Name, reader[Consts.COLUMN_DEFAULT] as string));
                    }
                }

                var selectConstraints = conn.CreateCommand();
                selectConstraints.CommandText = string.Format("SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = '{0}';", table.Name);
                using (var reader = selectConstraints.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if ((string)reader[Consts.CONSTRAINT_TYPE] == Constraint.FOREIGN_KEY)
                        {
                            table.Constraints.Add(new NbuLibrary.Core.Sql.ForeignKeyConstraint(reader));
                        }
                        else
                            table.Constraints.Add(new NbuLibrary.Core.Sql.Constraint(reader));

                    }
                }

                foreach (var con in table.Constraints)
                {
                    var selectConstraintColumns = conn.CreateCommand();
                    selectConstraintColumns.CommandText = string.Format("SELECT * FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE WHERE {0}='{1}';", NbuLibrary.Core.Sql.Consts.CONSTRAINT_NAME, con.Name);
                    using (var reader = selectConstraintColumns.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            con.Columns.Add((string)reader[NbuLibrary.Core.Sql.Consts.COLUMN_NAME]);
                        }
                    }

                    if (con is ForeignKeyConstraint)
                    {
                        var fk = con as ForeignKeyConstraint;
                        var selectRefCols = conn.CreateCommand();
                        selectRefCols.CommandText = string.Format(@"select cu.COLUMN_NAME, cu.TABLE_NAME
                                                                    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
	                                                                    INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu ON rc.UNIQUE_CONSTRAINT_NAME = cu.CONSTRAINT_NAME 
                                                                    WHERE rc.CONSTRAINT_NAME = '{0}';", fk.Name);
                        using (var reader = selectRefCols.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (string.IsNullOrEmpty(fk.RefTable))
                                    fk.RefTable = (string)reader[Consts.TABLE_NAME];

                                fk.RefColumns.Add((string)reader[Consts.COLUMN_NAME]);
                            }
                        }
                    }
                }

                var selectUniqueIndexes = conn.CreateCommand();
                selectUniqueIndexes.CommandText = string.Format(@"
select name, filter_definition, index_id from sys.indexes
where object_id = OBJECT_ID('[dbo].[{0}]') and is_unique=1 and is_primary_key = 0", table.Name);
                List<object[]> idxs = new List<object[]>();
                using (var reader = selectUniqueIndexes.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        object[] idx = new object[3];
                        reader.GetValues(idx);
                        idxs.Add(idx);
                    }
                }

                foreach (var idx in idxs)
                {
                    List<string> cols = new List<string>();
                    var selectIdxCols = conn.CreateCommand();
                    selectIdxCols.CommandText = string.Format(@"
select c.name
from sys.index_columns ic
inner join sys.columns c on c.column_id = ic.column_id and c.object_id = ic.object_id
where ic.index_id={0} and ic.object_id = OBJECT_ID('[dbo].[{1}]') ", idx[2], table.Name);
                    using (var reader = selectIdxCols.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        while (reader.Read())
                        {
                            cols.Add(reader.GetString(0));
                        }
                    }

                    table.Constraints.Add(new UniqueConstraint(table.Name, cols.ToArray()) { Filter = idx[1] as string });
                }
            }
        }

        private void BuildForeignKeysCache()
        {
            _fkCache = new Dictionary<string, FKCacheEntry>();
            foreach (var tbl in Tables)
            {
                foreach (var cnst in tbl.Constraints)
                {
                    if (cnst is ForeignKeyConstraint == false)
                        continue;

                    var fk = cnst as ForeignKeyConstraint;
                    foreach (var col in fk.RefColumns)
                    {
                        var refTable = Tables.Find(t => t.Name == fk.RefTable);
                        if (refTable == null)
                            continue;
                        var key = string.Format("{0}_{1}", refTable.Name, col);
                        if (!_fkCache.ContainsKey(key))
                        {
                            var entry = new FKCacheEntry();
                            entry.Add(fk, tbl);
                            _fkCache.Add(key, entry);
                        }
                        else
                            _fkCache[key].Add(fk, tbl);
                    }
                }
            }
        }
    }
}
