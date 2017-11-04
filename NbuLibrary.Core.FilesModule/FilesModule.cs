using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.FilesModule
{
    public class Permissions
    {
        public const string ManageAll = "ManageAll";
        public const string ViewOwn = "ViewOwn";
        public const string ManageOwn = "ManageOwn";
        public const string Upload = "Upload";
    }

    public class FilesNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<FilesModule>();
            Bind<IFileService>().To<FileService>();
            Bind<IEntityOperationLogic>().To<FilesOperationLogic>();
            Bind<IEntityOperationInspector>().To<FilesOperationInspector>();
            Bind<IEntityQueryInspector>().To<FilesQueryInspector>();
        }
    }

    public class FilesModule : IModule
    {
        public FilesModule(IDatabaseService dbService)
        {
            _dbService = dbService;
        }

        public const int Id = 4;
        private IDatabaseService _dbService;

        int IModule.Id
        {
            get { return Id; }
        }

        decimal IModule.Version
        {
            get { return 1.2m; }
        }

        string IModule.Name
        {
            get { return "FilesModule"; }
        }

        IUIProvider IModule.UIProvider
        {
            get { return new FilesUIProvider(); }
        }

        ModuleRequirements IModule.Requirements
        {
            get
            {
                ModelBuilder mbFile = new ModelBuilder("File");
                mbFile.AddString("Name", 256);
                mbFile.AddString("Extension", 32);
                mbFile.AddString("ContentType", 128);
                mbFile.AddString("ContentPath", 512);
                mbFile.AddInteger("Size");
                mbFile.Rules.AddRequired("Name");
                mbFile.Rules.AddRequired("ContentPath");

                ModelBuilder mbUser = new ModelBuilder("User");
                mbUser.AddInteger("DiskUsageLimit", 25 * 1024 * 1024);

                var fileAccess = mbFile.AddRelationTo(mbUser.EntityModel, RelationType.ManyToMany, Roles.Access);
                ModelBuilder mbAccess = new ModelBuilder(fileAccess);
                mbAccess.AddEnum<FileAccessType>("Type");
                mbAccess.AddGuid("Token");
                mbAccess.AddDateTime("Expire");

                var dm = new DomainModel();
                dm.Entities.Add(mbFile.EntityModel);
                dm.Entities.Add(mbUser.EntityModel);

                return new ModuleRequirements()
                {
                    RequredModules = new[] { 1 },
                    Domain = dm,
                    Permissions = new string[] { Permissions.ManageAll, Permissions.ManageOwn, Permissions.ViewOwn, Permissions.Upload },
                    UIDefinitions = new List<UIDefinition>() {
                        new GridDefinition(){ 
                            Label = "Files_Grid",
                            Name = "Files_Grid",
                            Entity = NbuLibrary.Core.Domain.File.ENTITY,
                            Fields= new List<ViewField>(){
                                new Textfield(){ Property = "Name", Label = "Name", Length = 256, Order = 1 },
                                new Textfield(){ Property = "Extension", Label = "Extension", Length = 32, Order = 2 },
                                new Numberfield(){ Property = "Size", Label = "Size", Order = 3 }
                        }}
                    }
                };
            }
        }

        void IModule.Initialize()
        {
            FileServiceConfigurationSection config = ConfigurationManager.GetSection("fileService") as FileServiceConfigurationSection;
            //if (!Directory.Exists(config.TemporaryStoragePath))
            //    Directory.CreateDirectory(config.TemporaryStoragePath);
            try
            {
                if (!Directory.Exists(config.PermanentStoragePath))
                    Directory.CreateDirectory(config.PermanentStoragePath);

                using (var dbContext = _dbService.GetDatabaseContext(true))
                {
                    SqlCommand cmd = new SqlCommand("UPDATE [User] SET [DiskUsageLimit]=@limit where [DiskUsageLimit] is null", dbContext.Connection);
                    cmd.Parameters.AddWithValue("@limit", 25 * 1024 * 1024);
                    cmd.ExecuteNonQuery();
                    dbContext.Complete();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not create file directory for permanent file storage.", ex);
            }
            try
            {
                string test = "test";
                System.IO.File.WriteAllText(Path.Combine(config.PermanentStoragePath, "test.txt"), test);
                System.IO.File.Delete(Path.Combine(config.PermanentStoragePath, "test.txt"));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("The application cannot write into the permanent file storage directory: {0}", config.PermanentStoragePath), ex);
            }

        }
    }

    public class FilesUIProvider : IUIProvider
    {
        public string GetClientTemplates(Domain.UserTypes type)
        {
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Core.FilesModule.{0}", resourceFileName)))
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    resourceContent = reader.ReadToEnd();
                }
            }

            return resourceContent;
        }

        IEnumerable<ClientScript> IUIProvider.GetClientScripts(UserTypes type)
        {
            return new List<ClientScript>()
            {
                new ClientScript(){ Name = "filesmodule", Content = GetContent("Scripts.filesmodule.js") },
                new ClientScript(){ Name = "vm.files", Content = GetContent("Scripts.vm.files.js") },
                new ClientScript(){ Name = "vm.file", Content = GetContent("Scripts.vm.file.js") }
            };
        }
    }

    public class FilesQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        private IFileService _fileService;
        public FilesQueryInspector(ISecurityService securityService, IFileService fileService)
        {
            _securityService = securityService;
            _fileService = fileService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            //TODO: filequeryinspector - inspect query
            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities)
        {
            if (query.IsForEntity(NbuLibrary.Core.Domain.File.ENTITY))
            {
                foreach (var e in entities)
                {
                    if (_fileService.HasAccess(_securityService.CurrentUser, e.Id))
                        return InspectionResult.Allow;
                }
            }

            return InspectionResult.None;
        }
    }


    public class FilesOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        private IFileService _fileService;
        public FilesOperationInspector(ISecurityService securityService, IFileService fileService)
        {
            _securityService = securityService;
            _fileService = fileService;
        }

        public InspectionResult Inspect(EntityOperation operation)
        {
            if (operation.IsEntity(NbuLibrary.Core.Domain.File.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate() && _securityService.HasModulePermission(_securityService.CurrentUser, FilesModule.Id, Permissions.Upload))
                    return InspectionResult.Allow;
                else if ((_securityService.HasModulePermission(_securityService.CurrentUser, FilesModule.Id, Permissions.ManageOwn) || _securityService.HasModulePermission(_securityService.CurrentUser, FilesModule.Id, Permissions.ManageAll))
                    && _fileService.HasAccess(_securityService.CurrentUser, update.Id.Value, FileAccessType.Owner))
                    return InspectionResult.Allow;
                else if (_securityService.HasModulePermission(_securityService.CurrentUser, FilesModule.Id, Permissions.ManageAll)
                    && _fileService.HasAccess(_securityService.CurrentUser, update.Id.Value, FileAccessType.Full))
                    return InspectionResult.Allow;
            }
            else if (operation.IsEntity(NbuLibrary.Core.Domain.File.ENTITY) && operation is EntityDelete)
            {
                //TODO: file delete permission
                if (_fileService.HasAccess(_securityService.CurrentUser, operation.Id.Value, FileAccessType.Owner))
                    return InspectionResult.Allow;
            }
            else if (operation.IsEntity(User.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.ContainsProperty("DiskUsageLimit") && _securityService.CurrentUser.UserType != UserTypes.Admin)
                {
                    return InspectionResult.Deny;
                }
            }

            return InspectionResult.None;
        }
    }


    public class FilesOperationLogic : IEntityOperationLogic
    {
        private const string CTXKEY_FILEDELETION = "Files:FileDeletion";

        private ISecurityService _securityService;
        private IFileService _fileService;
        public FilesOperationLogic(ISecurityService securityService, IFileService fileService)
        {
            _securityService = securityService;
            _fileService = fileService;
        }

        public void Before(Services.tmp.EntityOperation operation, EntityOperationContext context)
        {
            if (operation.IsEntity(NbuLibrary.Core.Domain.File.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate())
                {
                    var access = update.Attach(NbuLibrary.Core.Domain.User.ENTITY, Roles.Access, _securityService.CurrentUser.Id);
                    access.Set("Type", FileAccessType.Owner);
                }
                else
                {
                    var accessUpdates = update.GetMultipleRelationUpdates(User.ENTITY, Roles.Access);
                    if (accessUpdates != null)
                        foreach (var ac in accessUpdates)
                        {
                            if (ac.Operation == RelationOperation.Detach)
                                continue;

                            if (ac.ContainsProperty("Type") && ac.Get<FileAccessType>("Type") == FileAccessType.Token)
                                ac.Set("Token", Guid.NewGuid());//TODO: token based file access
                        }
                }
            }
            else if (operation.IsEntity(NbuLibrary.Core.Domain.File.ENTITY) && operation is EntityDelete)
            {
                var file = _fileService.GetFile(operation.Id.Value);
                context.Set<NbuLibrary.Core.Domain.File>(CTXKEY_FILEDELETION, file);
            }
        }

        public void After(Services.tmp.EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (operation.IsEntity(NbuLibrary.Core.Domain.File.ENTITY)
                && operation is EntityDelete
                && result.Success)
            {
                var file = context.Get<NbuLibrary.Core.Domain.File>(CTXKEY_FILEDELETION);
                if (file != null)
                    _fileService.DeleteFileContent(Guid.Parse(file.ContentPath));
            }
        }
    }
}
