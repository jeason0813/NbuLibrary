using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Sql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Infrastructure
{
    public class SequenceProvider : ISequenceProvider
    {
        private IDatabaseService _dbService;
        public SequenceProvider(IDatabaseService dbService)
        {
            _dbService = dbService;
        }

        public object GetNext(SequencePropertyModel pm)
        {
            switch (pm.SequenceType)
            {
                case SequenceType.Guid:
                    return Guid.NewGuid();
                case SequenceType.Uri:
                    return GetNextUri(pm.SequenceId, DateTime.Now.Year);
                default:
                    throw new NotImplementedException(string.Format("SequenceProvider.GetNext not implemented for sequence of type \"{0}\"", pm.SequenceType));
            }
        }

        private object GetNextUri(string sequence, int year)
        {
            var cmd = new SqlCommand("_SysUris_GetNext");
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("sequence", sequence);
            cmd.Parameters.AddWithValue("year", year);
            var next = new SqlParameter("next", System.Data.SqlDbType.Int);
            next.Direction = System.Data.ParameterDirection.Output;
            cmd.Parameters.Add(next);
            using (var ctx = _dbService.GetDatabaseContext(true))
            {
                cmd.Connection = ctx.Connection;
                cmd.ExecuteNonQuery();
                ctx.Complete();
                return string.Format("{0}-{1}-{2}", sequence, next.Value, year);
            }
        }

        public static void Initialize(DatabaseManager dbManager)
        {
            Table table = new Table("_SysUris");
            table.Columns.Add(new Column("Name", System.Data.SqlDbType.NVarChar, 128, false));
            table.Columns.Add(new Column("Number", System.Data.SqlDbType.Int, nullable: false));
            table.Columns.Add(new Column("Year", System.Data.SqlDbType.Int, nullable: false));

            table.Constraints.Add(new UniqueConstraint(table.Name, "Name", "Number", "Year"));

            var sp = new StoredProcedure("_SysUris_GetNext", @"
CREATE PROCEDURE [_SysUris_GetNext]
	@sequence nvarchar(128),
	@year int,
	@next int output
AS
BEGIN
	DECLARE @t TABLE([NUMBER] int);
	merge [_SysUris] T
	USING (select @sequence as [Name], @year as [Year]) S
	ON T.[Name] = S.[Name] AND T.[Year] = S.[Year]
	WHEN MATCHED THEN
	UPDATE SET [NUMBER] = [NUMBER]+1
	WHEN NOT MATCHED THEN
	INSERT ([Name], [Number], [Year]) VALUES (S.[Name], 1, S.[Year])
	OUTPUT inserted.[Number] into @t;
	select @next = [Number] from @t;
END");

            dbManager.Merge(new List<Table>() { table }, new List<StoredProcedure>() { sp });
        }
    }
}
