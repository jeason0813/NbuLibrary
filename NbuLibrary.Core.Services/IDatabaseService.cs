using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public interface IDatabaseContext : IDisposable
    {
        SqlConnection Connection { get; }
        void Complete();
    }

    public interface IDatabaseService
    {
        SqlConnection GetSqlConnection();
        IDatabaseContext GetDatabaseContext(bool useTransaction);
        string GetRootPath();
    }
}
