using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using Ninject.Modules;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Domain;
using System.Security.Cryptography;
using System.Web.Security;
using NbuLibrary.Core.Services.tmp;
using System.Xml.Serialization;
using NbuLibrary.Core.Service.tmp;
using System.Threading;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using NbuLibrary.Core.Sql;
using System.Data.SqlClient;

namespace NbuLibrary.Core.Infrastructure
{
    public class UIDefinitionServiceImpl : IUIDefinitionService
    {
        private Dictionary<string, UIDefinition> _definitions;
        private string _path;

        private static ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private IDatabaseService _storageService;

        public UIDefinitionServiceImpl(IDatabaseService storageService)
        {
            _storageService = storageService;
            _path = Path.Combine(_storageService.GetRootPath(), "viewdefinitionstore.xml");

            _rwLock.EnterUpgradeableReadLock();
            try
            {
                if (!System.IO.File.Exists(_path))
                {
                    _rwLock.EnterWriteLock();
                    try
                    {
                        _definitions = new Dictionary<string, UIDefinition>();
                        WriteDefinitionsToFile(FileMode.Create);

                    }
                    finally
                    {
                        _rwLock.ExitWriteLock();
                    }
                }
                else
                {
                    XmlSerializer ser = new XmlSerializer(typeof(List<UIDefinition>));

                    List<UIDefinition> list = null;
                    using (FileStream fs = new FileStream(_path, FileMode.Open))
                    {
                        list = ser.Deserialize(fs) as List<UIDefinition>;
                    }
                    _definitions = new Dictionary<string, UIDefinition>();
                    list.ForEach(uidef => _definitions.Add(uidef.Name, uidef));
                }
            }
            finally
            {
                _rwLock.ExitUpgradeableReadLock();
            }
        }

        public Service.tmp.UIDefinition GetByName(string name)
        {
            _rwLock.EnterReadLock();
            UIDefinition def = null;
            try
            {
                _definitions.TryGetValue(name, out def);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return def;
        }

        public T GetByName<T>(string name) where T : Service.tmp.UIDefinition
        {
            _rwLock.EnterReadLock();
            UIDefinition def = null;
            try
            {
                _definitions.TryGetValue(name, out def);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return def as T;
        }

        public void Add(Service.tmp.UIDefinition definition)
        {
            PrepareDefinition(definition);
            _rwLock.EnterWriteLock();
            try
            {
                _definitions.Add(definition.Name, definition);
                WriteDefinitionsToFile(FileMode.Truncate);

            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void Remove(Service.tmp.UIDefinition definition)
        {
            _rwLock.EnterWriteLock();
            try
            {
                _definitions.Remove(definition.Name);
                WriteDefinitionsToFile(FileMode.Truncate);

            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void Update(Service.tmp.UIDefinition definition)
        {
            PrepareDefinition(definition);
            _rwLock.EnterWriteLock();
            try
            {
                if (!_definitions.ContainsKey(definition.Name))
                    throw new ArgumentException(string.Format("The definition {0} does not exist!", definition.Name));

                _definitions[definition.Name] = definition;
                WriteDefinitionsToFile(FileMode.Truncate);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public IEnumerable<Service.tmp.UIDefinition> GetAll()
        {
            _rwLock.EnterReadLock();
            IEnumerable<UIDefinition> all = null;
            try
            {
                all = _definitions.Values.OrderBy(f => f.Name).ToArray();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            return all;
        }

        private void WriteDefinitionsToFile(FileMode mode)
        {
            foreach (var def in _definitions.Values)
                PrepareDefinition(def);
            XmlSerializer ser = new XmlSerializer(typeof(List<UIDefinition>));
            using (MemoryStream ms = new MemoryStream())
            {
                ser.Serialize(ms, _definitions.Values.ToList());
                ms.Flush();
                ms.Position = 0;
                using (FileStream fs = new FileStream(_path, mode))
                {
                    ms.CopyTo(fs);
                }
            }
        }

        private void PrepareDefinition(UIDefinition definition)
        {
            if (definition is GridDefinition)
            {
                var gridDef = definition as GridDefinition;
                if (gridDef.Fields == null)
                    gridDef.Fields = new List<ViewField>();
                gridDef.Fields = gridDef.Fields.OrderBy(f => f.Order).ToList();
                //TODO: Filters
            }
            else if (definition is ViewDefinition)
            {
                var viewDef = definition as ViewDefinition;
                if (viewDef.Fields == null)
                    viewDef.Fields = new List<ViewField>();
                viewDef.Fields = viewDef.Fields.OrderBy(f => f.Order).ToList();
            }
            else if (definition is FormDefinition)
            {
                var formDef = definition as FormDefinition;
                if (formDef.Fields == null)
                    formDef.Fields = new List<EditField>();
                formDef.Fields = formDef.Fields.OrderBy(f => f.Order).ToList();
            }
        }
    }


    public class AdminBusinessLogic : IBusinessLogic
    {
        private ISecurityService _securityService;
        public AdminBusinessLogic(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public OperationResolution Resolve(EntityOperation operation)
        {
            if (_securityService.CurrentUser.UserType == UserTypes.Admin)
                return OperationResolution.Allowed;
            else
                return OperationResolution.None;
        }


        public void Prepare(EntityOperation operation, ref BLContext context)
        {
        }

        public void Complete(EntityOperation operation, ref BLContext context, ref BLResponse response)
        {
        }
    }

    public class TemplateServiceImpl : ITemplateService
    {
        private Regex regex = new Regex("{\\w+.\\w+}", RegexOptions.Compiled);
        private IDatabaseService _storageService;

        public TemplateServiceImpl(IDatabaseService storageService)
        {
            _storageService = storageService;
        }

        public void Save(HtmlTemplate template)
        {
            var cmd = new SqlCommand("_HtmlTemplates_Upsert");
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("id", template.Id);
            cmd.Parameters.AddWithValue("name", template.Name);
            cmd.Parameters.AddWithValue("subject", template.SubjectTemplate);
            cmd.Parameters.AddWithValue("body", template.BodyTemplate);
            using (var ctx = _storageService.GetDatabaseContext(true))
            {
                cmd.Connection = ctx.Connection;
                cmd.ExecuteNonQuery();
                ctx.Complete();
            }
        }

        public HtmlTemplate Get(Guid id)
        {
            var cmd = new SqlCommand("_HtmlTemplates_GetByID");
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("id", id);
            using (var ctx = _storageService.GetDatabaseContext(false))
            {
                cmd.Connection = ctx.Connection;
                using (var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
                {
                    if (reader.Read())
                        return new HtmlTemplate() { Id = id, Name = reader.GetString(0), SubjectTemplate = reader.GetString(1), BodyTemplate = reader.GetString(2) };
                    else
                        return null;
                }
            }
        }

        public void Render(HtmlTemplate htmlTemplate, Dictionary<string, Entity> context, out string subject, out string body)
        {
            subject = RenderTemplate(htmlTemplate.SubjectTemplate, context);
            body = RenderTemplate(htmlTemplate.BodyTemplate, context);
        }

        public static void Install(DatabaseManager dbManager)
        {
            Table table = new Table("_HtmlTemplates");
            table.Columns.Add(new Column("ID", System.Data.SqlDbType.UniqueIdentifier, nullable: false));
            table.Columns.Add(new Column("Name", System.Data.SqlDbType.NVarChar, 256, nullable: false));
            table.Columns.Add(new Column("Subject", System.Data.SqlDbType.NVarChar, 500, nullable: false));
            table.Columns.Add(new Column("Body", System.Data.SqlDbType.NVarChar, 4000, nullable: false));
            table.Constraints.Add(new Constraint("PK__HtmlTemplates", Constraint.PRIMARY_KEY, "ID"));

            var spGetByID = new StoredProcedure("_HtmlTemplates_GetByID",
                string.Format(@"
                    CREATE PROCEDURE [_HtmlTemplates_GetByID]
	                    @id uniqueidentifier
                    AS
                    BEGIN
                        SELECT [{2}], [{3}], [{4}] FROM [{0}] WHERE [{1}] = @id;
                    END", table.Name, table.Columns[0].Name, table.Columns[1].Name, table.Columns[2].Name, table.Columns[3].Name));

            var spUpsert = new StoredProcedure("_HtmlTemplates_Upsert",
                string.Format(@"
CREATE PROCEDURE [_HtmlTemplates_Upsert]
    @id uniqueidentifier,
    @name nvarchar(256),
    @subject nvarchar(500),
    @body nvarchar(4000)
AS
    merge [{0}] T
	USING (select @id as [{1}], @name as [{2}], @subject as [{3}], @body as [{4}]) S
	ON T.[{1}] = S.[{1}]
	WHEN MATCHED THEN
	UPDATE SET [{2}] = S.[{2}],[{3}] = S.[{3}], [{4}] = S.[{4}]
	WHEN NOT MATCHED THEN
	INSERT ([{1}], [{2}], [{3}], [{4}]) VALUES (S.[{1}], S.[{2}], S.[{3}], S.[{4}]);", table.Name, table.Columns[0].Name, table.Columns[1].Name, table.Columns[2].Name, table.Columns[3].Name));

            dbManager.Merge(new List<Table>() { table }, new List<StoredProcedure>() { spGetByID, spUpsert });
        }

        private string RenderTemplate(string template, Dictionary<string, Entity> context)
        {
            StringBuilder sb = new StringBuilder();
            var matches = regex.Matches(template);

            if (matches.Count == 0)
                return template;

            int idx = 0;
            foreach (Match m in matches)
            {
                sb.Append(template.Substring(idx, m.Index - idx));
                string[] ctxValuePath = m.Value.TrimStart('{').TrimEnd('}').Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (ctxValuePath.Length == 2)
                {
                    if (context.ContainsKey(ctxValuePath[0]) && context[ctxValuePath[0]].Data.ContainsKey(ctxValuePath[1]))
                    {
                        var rawValue = context[ctxValuePath[0]].Data[ctxValuePath[1]];
                        if (rawValue != null)
                        {
                            if (rawValue is DateTime)
                                sb.Append(((DateTime)rawValue).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture));
                            else
                                sb.Append(rawValue);
                        }
                    }
                    else
                        sb.Append(m.Value);
                }
                else
                    System.Diagnostics.Trace.WriteLine("TemplateService Warning: invalid context value format \"{0}\". Only the format \"EntityContextKey.Property\" is supported.");
                idx = m.Index + m.Length;
            }
            sb.Append(template.Substring(idx, template.Length - idx));

            return sb.ToString();
        }


        public IEnumerable<HtmlTemplate> GetAllTemplatesInfo()
        {
            var cmd = new SqlCommand("SELECT ID, Name FROM [_HtmlTemplates];");
            using (var ctx = _storageService.GetDatabaseContext(false))
            {
                cmd.Connection = ctx.Connection;
                using (var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                        yield return new HtmlTemplate() { Id = reader.GetGuid(0), Name = reader.GetString(1) };
                }
            }
        }
    }

}
