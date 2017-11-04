using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace NbuLibrary.Tests.Performance
{
    public class TestDatabaseService : IDatabaseService
    {
        public const string CONNECTION_STRING = "Data Source=localhost;Initial Catalog=nbulib;Integrated Security=True";
        public const string APP_ROOT = @"D:\Users\kiko\Documents\Open Source\libnbu\NbuLibrary\NbuLibrary.Web";

        public class DatabaseContext : IDatabaseContext
        {
            [ThreadStatic]
            private static int refCount = 0;

            [ThreadStatic]
            private static SqlConnection activeConnection;


            private TransactionScope _scope;
            public DatabaseContext(string connectionString, bool useTransaction)
            {
                if (useTransaction)
                    _scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(10.0) });
                if (refCount == 0)
                {
                    activeConnection = new SqlConnection(connectionString);
                    activeConnection.Open();
                }
                refCount++;
            }

            public SqlConnection Connection
            {
                get { return activeConnection; }
            }

            public void Complete()
            {
                if (_scope != null)
                    _scope.Complete();
            }

            public void Dispose()
            {
                if (_scope != null)
                    _scope.Dispose();
                refCount--;
                if (refCount == 0)
                {
                    activeConnection.Dispose();
                    activeConnection = null;
                }
            }
        }

        public SqlConnection GetSqlConnection()
        {
            return new SqlConnection(CONNECTION_STRING);
        }


        public IDatabaseContext GetDatabaseContext(bool useTransaction)
        {
            return new DatabaseContext(GetSqlConnection().ConnectionString, useTransaction);
        }


        public string GetRootPath()
        {
            return APP_ROOT;
        }
    }
}
