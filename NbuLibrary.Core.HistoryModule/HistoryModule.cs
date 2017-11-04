using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using NbuLibrary.Core.Sql;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.HistoryModule
{
    public class Properties
    {
        public const string CreatedBy = "CreatedBy";
        public const string CreatedOn = "CreatedOn";
    }

    public class SqlConsts
    {
        public class StoredProcedures
        {
            public const string LogRelationPropertyChange = "_History_LogRelationPropertyChange";
            public const string LogOperation = "_History_LogOperation";
            public const string LogPropertyChange = "_History_LogPropertyChange";
            public const string LogRelationChange = "_History_LogRelationChange";
        }
    }

    public class HistoryNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<HistoryModule>();
            Bind<IDomainChangeListener>().To<HistoryDomainChangeListener>();
            Bind<IEntityOperationLogic>().To<HistoryOperationLogic>();
        }
    }


    public class HistoryModule : IModule
    {
        public const int Id = 6;
        private IDatabaseService _dbService;

        public HistoryModule(IDatabaseService dbService)
        {
            _dbService = dbService;
        }

        int IModule.Id
        {
            get { return Id; }
        }

        public decimal Version
        {
            get { return 0.1m; }
        }

        public string Name
        {
            get { return "History"; }
        }

        public IUIProvider UIProvider
        {
            get { return new HistoryUIProvider(); }
        }

        public ModuleRequirements Requirements
        {
            get { return new ModuleRequirements() { RequredModules = new[] { 1 } }; }
        }

        public void Initialize()
        {
            using (var dbContext = _dbService.GetDatabaseContext(false))
            {
                DatabaseManager mgr = new DatabaseManager(dbContext.Connection);
                var entityTbl = new Table("_History_EntityOperations") { FileGroup = "HISTORY" };

                entityTbl.Columns.Add(new Column("Id", System.Data.SqlDbType.Int, identity: true, nullable: false));
                entityTbl.Columns.Add(new Column("Entity", System.Data.SqlDbType.NVarChar, 128, false));
                entityTbl.Columns.Add(new Column("EntityId", System.Data.SqlDbType.Int, nullable: false));
                entityTbl.Columns.Add(new Column("Operation", System.Data.SqlDbType.TinyInt, nullable: false));
                entityTbl.Columns.Add(new Column("ByUser", System.Data.SqlDbType.Int, nullable: false));
                entityTbl.Columns.Add(new Column("OnDate", System.Data.SqlDbType.DateTime, nullable: false));
                entityTbl.Constraints.Add(new Constraint("PK__History_EntityOperations", Constraint.PRIMARY_KEY, "Id"));

                var propChangeTbl = new Table("_History_PropertyChanges") { FileGroup = "HISTORY" };
                propChangeTbl.Columns.Add(new Column("Id", System.Data.SqlDbType.Int, identity: true, nullable: false));
                propChangeTbl.Columns.Add(new Column("OperationId", System.Data.SqlDbType.Int, nullable: false));
                propChangeTbl.Columns.Add(new Column("Property", System.Data.SqlDbType.NVarChar, 128, nullable: false));
                propChangeTbl.Columns.Add(new Column("Value", System.Data.SqlDbType.NVarChar, 1024));
                propChangeTbl.Constraints.Add(new Constraint("PK__History_PropertyChanges", Constraint.PRIMARY_KEY, "Id"));
                propChangeTbl.Constraints.Add(new ForeignKeyConstraint("FK__History_PropertyChanges__History_EntityOperations", new string[] { "OperationId" }, "_History_EntityOperations", new string[] { "Id" }));

                var relChangeTbl = new Table("_History_RelationChanges") { FileGroup = "HISTORY" };
                relChangeTbl.Columns.Add(new Column("Id", System.Data.SqlDbType.Int, identity: true, nullable: false));
                relChangeTbl.Columns.Add(new Column("OperationId", System.Data.SqlDbType.Int, nullable: false));
                relChangeTbl.Columns.Add(new Column("Entity", System.Data.SqlDbType.NVarChar, 128, nullable: false));
                relChangeTbl.Columns.Add(new Column("Role", System.Data.SqlDbType.NVarChar, 128, false));
                relChangeTbl.Columns.Add(new Column("EntityId", System.Data.SqlDbType.Int, nullable: false));
                relChangeTbl.Columns.Add(new Column("RelationOperation", System.Data.SqlDbType.Int, nullable: false));
                relChangeTbl.Constraints.Add(new Constraint("PK__History_RelationChanges", Constraint.PRIMARY_KEY, "Id"));
                relChangeTbl.Constraints.Add(new ForeignKeyConstraint("FK__History_RelationChanges__History_EntityOperations", new string[] { "OperationId" }, "_History_EntityOperations", new string[] { "Id" }));

                var relPropChangeTbl = new Table("_History_RelationPropertyChanges") { FileGroup = "HISTORY" };
                relPropChangeTbl.Columns.Add(new Column("Id", System.Data.SqlDbType.Int, identity: true, nullable: false));
                relPropChangeTbl.Columns.Add(new Column("RelationChangeId", System.Data.SqlDbType.Int, nullable: false));
                relPropChangeTbl.Columns.Add(new Column("Property", System.Data.SqlDbType.NVarChar, 128, nullable: false));
                relPropChangeTbl.Columns.Add(new Column("Value", System.Data.SqlDbType.NVarChar, 1024));
                relPropChangeTbl.Constraints.Add(new Constraint("PK__History_RelationPropertyChanges", Constraint.PRIMARY_KEY, "Id"));
                relPropChangeTbl.Constraints.Add(new ForeignKeyConstraint("FK__History_RelationPropertyChanges__History_RelationChanges", new string[] { "RelationChangeId" }, "_History_RelationChanges", new string[] { "Id" }));

                mgr.Merge(new List<Table>() { 
                    entityTbl, 
                    propChangeTbl, 
                    relChangeTbl, 
                    relPropChangeTbl },
                    new List<StoredProcedure>()
                    {
                        new StoredProcedure(SqlConsts.StoredProcedures.LogOperation, GetContent("Sql.LogOperation.sql")),
                        new StoredProcedure(SqlConsts.StoredProcedures.LogPropertyChange, GetContent("Sql.LogPropertyChange.sql")),
                        new StoredProcedure(SqlConsts.StoredProcedures.LogRelationChange, GetContent("Sql.LogRelationChange.sql")),
                        new StoredProcedure(SqlConsts.StoredProcedures.LogRelationPropertyChange, GetContent("Sql.LogRelationPropertyChange.sql"))
                    });//TODO: HistoryModule - stored procedures

                dbContext.Complete();
            }
        }

        internal static string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Core.HistoryModule.{0}", resourceFileName)))
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    resourceContent = reader.ReadToEnd();
                }
            }

            return resourceContent;
        }
    }

    public class HistoryUIProvider : IUIProvider
    {
        public string GetClientTemplates(NbuLibrary.Core.Domain.UserTypes type)
        {
            return string.Empty;
        }

        IEnumerable<ClientScript> IUIProvider.GetClientScripts(UserTypes type)
        {
            return new List<ClientScript>()
            {
                //TODO: History UI
            };
        }
    }

    public class HistoryDomainChangeListener : IDomainChangeListener
    {
        public void BeforeSave(DataModel.EntityModel em)
        {
            EnsureCreatedBy(em);
            EnsureCreatedOn(em);
        }

        public void AfterSave(DataModel.EntityModel em)
        {
        }

        private void EnsureCreatedBy(EntityModel em)
        {
            var createdBy = em.Properties[Properties.CreatedBy];
            if (createdBy == null)
            {
                var mb = new ModelBuilder(em);
                mb.AddInteger(Properties.CreatedBy);
                //mb.Rules.AddRequired(Properties.CreatedBy);//TODO-History: make createdby not null
            }
            else if (createdBy.Type != PropertyType.Number || createdBy.DefaultValue != null || (createdBy as NumberPropertyModel).IsInteger != true)
                throw new NotSupportedException(string.Format("Entity <{0}> contains property <{1}> which is system for the HistoryModule.", em.Name, createdBy.Name));
        }

        private void EnsureCreatedOn(EntityModel em)
        {
            var createdOn = em.Properties[Properties.CreatedOn];
            if (createdOn == null)
            {
                var mb = new ModelBuilder(em);
                mb.AddDateTime(Properties.CreatedOn);
                //mb.Rules.AddRequired(Properties.CreatedOn);//TODO-History: make createdon not null
            }
            else if (createdOn.Type != PropertyType.Datetime || createdOn.DefaultValue != null)
                throw new NotSupportedException(string.Format("Entity <{0}> contains property <{1}> which is system for the HistoryModule.", em.Name, createdOn.Name));
        }
    }

    public class HistoryOperationLogic : IEntityOperationLogic
    {
        private ISecurityService _securityService;
        private IDatabaseService _dbService;
        public HistoryOperationLogic(ISecurityService securityService, IDatabaseService dbService)
        {
            _securityService = securityService;
            _dbService = dbService;
        }

        public void Before(Services.tmp.EntityOperation operation, EntityOperationContext context)
        {
            if (operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate())
                {
                    update.Set(Properties.CreatedBy, _securityService.CurrentUser.Id);
                    update.Set(Properties.CreatedOn, DateTime.Now);
                }
            }
        }

        public void After(Services.tmp.EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (!result.Success)
                return;

            using (var dbContext = _dbService.GetDatabaseContext(true))
            {
                if (operation is EntityDelete)
                {
                    LogOperation(operation.Entity, operation.Id.Value, HistoryOperation.Deleted, DateTime.Now, dbContext.Connection);
                }
                else if (operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    bool created = result.Data.ContainsKey("Created");
                    var now = DateTime.Now;
                    int opId = LogOperation(update.Entity, update.Id.Value, created ? HistoryOperation.Created : HistoryOperation.Modified, now, dbContext.Connection);
                    foreach (var propUpdate in update.PropertyUpdates)
                    {
                        LogPropertyChange(opId, propUpdate.Key, propUpdate.Value, dbContext.Connection);
                    }

                    foreach (var relUpdate in update.RelationUpdates)
                    {
                        int relChangeId = LogRelationChange(opId, relUpdate.Entity, relUpdate.Role, relUpdate.Id.Value, relUpdate.Operation, dbContext.Connection);
                        foreach (var propUpdate in relUpdate.PropertyUpdates)
                        {
                            LogRelationPropertyChange(relChangeId, propUpdate.Key, propUpdate.Value, dbContext.Connection);
                        }
                    }
                }

                dbContext.Complete();
            }
        }

        private int LogOperation(string entity, int id, HistoryOperation op, DateTime now, SqlConnection conn)
        {
            var cmd = new SqlCommand(SqlConsts.StoredProcedures.LogOperation, conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("Entity", entity);
            cmd.Parameters.AddWithValue("EntityId", id);
            cmd.Parameters.AddWithValue("Operation", op);
            cmd.Parameters.AddWithValue("ByUser", _securityService.CurrentUser.Id);
            cmd.Parameters.AddWithValue("OnDate", now);
            cmd.Parameters.Add(new SqlParameter("Id", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output });
            cmd.ExecuteNonQuery();
            return (int)cmd.Parameters["Id"].Value;
        }

        private void LogPropertyChange(int operationId, string property, object value, SqlConnection conn)
        {
            var cmd = new SqlCommand(SqlConsts.StoredProcedures.LogPropertyChange, conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("OperationId", operationId);
            cmd.Parameters.AddWithValue("Property", property);
            string strValue = null;
            if (value != null)
            {
                strValue = string.Format("{0}", value);
                if (strValue.Length > 1024)
                    strValue = strValue.Substring(0, 1024);
            }
            if (strValue == null)
                cmd.Parameters.AddWithValue("Value", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("Value", strValue);

            cmd.ExecuteNonQuery();
        }

        private int LogRelationChange(int operationId, string entity, string role, int id, RelationOperation relOp, SqlConnection conn)
        {
            var cmd = new SqlCommand(SqlConsts.StoredProcedures.LogRelationChange, conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("OperationId", operationId);
            cmd.Parameters.AddWithValue("Entity", entity);
            cmd.Parameters.AddWithValue("Role", role);
            cmd.Parameters.AddWithValue("EntityId", id);
            cmd.Parameters.AddWithValue("RelationOperation", relOp);
            cmd.Parameters.Add(new SqlParameter("Id", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output });
            cmd.ExecuteNonQuery();
            return (int)cmd.Parameters["Id"].Value;
        }

        private void LogRelationPropertyChange(int relChangeId, string property, object value, SqlConnection conn)
        {
            var cmd = new SqlCommand(SqlConsts.StoredProcedures.LogRelationPropertyChange, conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("RelationChangeId", relChangeId);
            cmd.Parameters.AddWithValue("Property", property);
            string strValue = null;
            if (value != null)
            {
                strValue = string.Format("{0}", value);
                if (strValue.Length > 1024)
                    strValue = strValue.Substring(0, 1024);
            }
            if (strValue == null)
                cmd.Parameters.AddWithValue("Value", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("Value", strValue);

            cmd.ExecuteNonQuery();
        }
    }

    public enum HistoryOperation : byte
    {
        None = 0,
        Created = 1,
        Modified = 2,
        Deleted = 3
    }
}
