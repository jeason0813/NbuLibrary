using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.Services;
using Ninject.Modules;
using NbuLibrary.Core.Services.tmp;
using NbuLibrary.Core.DataModel;
using System.Configuration;
using System.Data.SqlClient;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Sql;
using System.Transactions;

namespace NbuLibrary.Core.Infrastructure
{
    public class DefaultBindings : NinjectModule
    {
        public override void Load()
        {
            Bind<IEntityOperationService>().To<EntityOperationService>();
            Bind<ISecurityService>().To<SecurityService>();
            Bind<IBusinessLogic>().To<AdminBusinessLogic>();
            Bind<INotificationService>().To<NotificationServiceImpl>();

            Bind<IUIDefinitionService>().To<UIDefinitionServiceImpl>();

            Bind<IEntityRepository>().To<EntityRepository>().InThreadScope();//TODO: check for issues5
            Bind<IDomainModelService>().To<DomainModelService>();
            Bind<IDatabaseService>().To<TempDbService>();
            Bind<ILoggingService>().To<LoggingService>();
            Bind<ITemplateService>().To<TemplateServiceImpl>();

            Bind<IDomainChangeListener>().To<EntityRepositoryDomainListener>();

            Bind<IEntityQueryInspector>().To<SecurityService>();
            Bind<IEntityOperationInspector>().To<SecurityService>();
            Bind<ISequenceProvider>().To<SequenceProvider>();
            Bind<IModule>().To<CoreInfrastructure>();

        }
    }

    //TODO: Database service
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

    public class TempDbService : IDatabaseService
    {
        public System.Data.SqlClient.SqlConnection GetSqlConnection()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString);
        }


        public IDatabaseContext GetDatabaseContext(bool useTransaction)
        {
            return new DatabaseContext(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString, useTransaction);
        }


        public string GetRootPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }

    public class CoreInfrastructure : IModule, IUIProvider
    {
        public const int ID = 0;
        private IDatabaseService _dbService;

        public CoreInfrastructure(IDatabaseService dbService)
        {
            _dbService = dbService;
        }

        public int Id
        {
            get { return ID; }
        }

        public decimal Version
        {
            get { return 1.1m; }
        }

        public string Name
        {
            get { return "CoreInfrastructure"; }
        }

        public IUIProvider UIProvider
        {
            get { return this; }
        }

        public ModuleRequirements Requirements
        {
            get { return new ModuleRequirements(); }
        }

        public void Initialize()
        {
            using (var conn = _dbService.GetSqlConnection())
            {
                var dbManager = new DatabaseManager(conn);
                conn.Open();
                SequenceProvider.Initialize(dbManager);
                TemplateServiceImpl.Install(dbManager);
            }
        }

        public IEnumerable<ClientScript> GetClientScripts(Domain.UserTypes type)
        {
            return new ClientScript[0];
        }

        public string GetClientTemplates(Domain.UserTypes type)
        {
            return string.Empty;
        }
    }

}
