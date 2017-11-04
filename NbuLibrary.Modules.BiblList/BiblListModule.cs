using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Modules.BiblList
{
    public class NotificationTemplates
    {
        public const string QUERY_COMPLETED = "925b6b76-2a54-4d0a-b827-b109f17f43c9";
        public const string PAYMENT_COMPLETED = "41e65aac-ee0b-482d-837d-eb254f66920c";
    }

    public class EntityConsts
    {
        public const string BibliographicListQuery = "BibliographicListQuery";
        public const string BibliographicListStandart = "BibliographicListStandart";
    }

    public class Roles
    {
        public const string Standart = "Standart";
        public const string File = "File";
        public const string Notification = "Notification";
        public const string Payment = "Payment";
        public const string Customer = "Customer";
        public const string ProcessedBy = "ProcessedBy";
    }

    public class Permissions
    {
        public const string Use = "Use";
    }

    public class BiblListNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<BiblListModule>();
            Bind<IEntityQueryInspector>().To<BiblListQueryInspector>();
            Bind<IEntityOperationInspector>().To<BiblListOperationInspector>();
            Bind<IEntityOperationLogic>().To<BiblListOperationLogic>();
        }
    }


    public class BiblListModule : IModule
    {
        public const int Id = 104;

        int IModule.Id
        {
            get { return Id; }
        }

        public decimal Version
        {
            get { return 1.3m; }
        }

        public string Name
        {
            get { return "Bibliographic list module"; }
        }

        public IUIProvider UIProvider
        {
            get
            {
                return new BiblListUIProvider();
            }
        }

        public ModuleRequirements Requirements
        {
            get
            {
                var payment = new EntityModel(Payment.ENTITY);
                var user = new EntityModel(User.ENTITY);
                var notification = new EntityModel(Notification.ENTITY);
                var file = new EntityModel(File.ENTITY);

                var standart = new NomenclatureModel(EntityConsts.BibliographicListStandart);

                var mbQuery = new ModelBuilder(EntityConsts.BibliographicListQuery);
                mbQuery.AddUri("Number", "ER");
                mbQuery.AddEnum<QueryStatus>("Status", QueryStatus.New);
                mbQuery.AddDateTime("ReplyBefore");
                mbQuery.AddEnum<ReplyMethods>("ReplyMethod");
                mbQuery.AddEnum<PaymentMethod>("PaymentMethod");

                mbQuery.Rules.AddRequired("Status");
                mbQuery.Rules.AddRequired("ReplyBefore");
                mbQuery.Rules.AddFutureDate("ReplyBefore", TimeSpan.FromDays(5.0));
                mbQuery.Rules.AddRequired("ReplyMethod");
                mbQuery.Rules.AddRequired("PaymentMethod");

                mbQuery.AddRelationTo(standart, RelationType.ManyToOne, Roles.Standart);
                mbQuery.AddRelationTo(file, RelationType.OneToOne, Roles.File);
                mbQuery.AddRelationTo(notification, RelationType.OneToMany, Roles.Notification);
                mbQuery.AddRelationTo(payment, RelationType.OneToOne, Roles.Payment);
                mbQuery.AddRelationTo(user, RelationType.ManyToOne, Roles.Customer);
                mbQuery.AddRelationTo(user, RelationType.ManyToOne, Roles.ProcessedBy);

                var dm = new DomainModel();
                dm.Entities.Add(payment);
                dm.Entities.Add(user);
                dm.Entities.Add(notification);
                dm.Entities.Add(file);
                dm.Entities.Add(standart);
                dm.Entities.Add(mbQuery.EntityModel);

                return new ModuleRequirements()
                {
                    RequredModules = new[] { 0, 1, 2, 3, 4, 5 },
                    Domain = dm,
                    Permissions = new string[] { Permissions.Use },
                    UIDefinitions = new UIDefinition[]{
                        new GridDefinition(){ Entity=EntityConsts.BibliographicListQuery, Name = "BiblList_Customer_BibliographicListQuery_Grid", Label= "Bibliographic list queries (Customer)" },
                        new GridDefinition(){ Entity=EntityConsts.BibliographicListQuery, Name = "BiblList_Librarian_BibliographicListQuery_Grid", Label= "Bibliographic list queries (Librarian)" },
                        new FormDefinition(){Entity=EntityConsts.BibliographicListQuery, Name = "BiblList_Customer_BibliographicListQuery_Form", Label= "Bibliographic list query form (Customer)"},
                        new FormDefinition(){Entity=EntityConsts.BibliographicListQuery, Name = "BiblList_Librarian_BibliographicListQuery_Form", Label= "Bibliographic list query form (Librarian)"},
                        new FormDefinition(){Entity=EntityConsts.BibliographicListQuery, Name = "BiblList_BibliographicListQuery_Process", Label= "Bibliographic list query process form (Librarian)"},
                        new ViewDefinition(){Entity=EntityConsts.BibliographicListQuery, Name = "BiblList_Customer_BibliographicListQuery_Details", Label= "Bibliographic list query details (Customer)"},
                        new ViewDefinition(){Entity=EntityConsts.BibliographicListQuery, Name = "BiblList_Librarian_BibliographicListQuery_Details", Label= "Bibliographic list query details (Librarian)"}
                    },
                    Templates = new[]{
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.QUERY_COMPLETED),
                            Name="Bibliographic List - Query Completed",
                            SubjectTemplate="Заявка {Query.Number} е завършена",
                            BodyTemplate=@"Здравейте {Customer.FirstName} {Customer.LastName},
<p>Уведомяваме Ви, че обработката на заявка {Query.Number} за услуга ""Редакция на библиографски списък"" приключи успешно. Щом платите своето задължение ще получите потвърждение и възможност за изтегляне на файла.</p>
Библиотека на НБУ"
                        },
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.PAYMENT_COMPLETED),
                            Name="Bibliographic List - Payment Completed",
                            SubjectTemplate="Заявка {Query.Number} е завършена",
                            BodyTemplate=@"Здравейте {Customer.FirstName} {Customer.LastName},
<p>Заявка {Query.Number} за услуга ""Редакция на библиографски списък"" е платена. Може да изтеглите получения файл.</p>
Библиотека на НБУ"
                        }
                    }
                };
            }
        }

        public void Initialize()
        {
        }
    }

    public class BiblListUIProvider : IUIProvider
    {
        public string GetClientTemplates(NbuLibrary.Core.Domain.UserTypes type)
        {
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Modules.BiblList.{0}", resourceFileName)))
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
                new ClientScript(){ Name = "bibllist", Content = GetContent("Scripts.bibllist.js") },
                new ClientScript(){ Name = "vm.bibllistqueries", Content = GetContent("Scripts.vm.bibllistqueries.js") },
                new ClientScript(){ Name = "vm.bibllistquery", Content = GetContent("Scripts.vm.bibllistquery.js") }
            };
        }
    }

    public class BiblListQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        public BiblListQueryInspector(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (query.IsForEntity("Arguments") && _securityService.HasModulePermission(_securityService.CurrentUser, BiblListModule.Id, Permissions.Use))
                return InspectionResult.Allow;
            if (query.IsForEntity(EntityConsts.BibliographicListQuery)
                || query.IsForEntity(EntityConsts.BibliographicListStandart))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, BiblListModule.Id, Permissions.Use))
                {
                    if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                        return InspectionResult.Allow;
                    else if (_securityService.CurrentUser.UserType == UserTypes.Customer && query.IsForEntity(EntityConsts.BibliographicListQuery))
                    {
                        var relToMe = query.GetRelatedQuery(User.ENTITY, Roles.Customer);
                        if (relToMe != null && relToMe.GetSingleId().HasValue && relToMe.GetSingleId().Value == _securityService.CurrentUser.Id)
                            return InspectionResult.Allow;
                        else if (!query.HasInclude(User.ENTITY, Roles.Customer))
                            query.Include(User.ENTITY, Roles.Customer);
                    }
                    else
                        return InspectionResult.Allow;
                }
            }
            else if (query.IsForEntity(Payment.ENTITY)
                && _securityService.CurrentUser.UserType == UserTypes.Librarian
                && _securityService.HasModulePermission(_securityService.CurrentUser, BiblListModule.Id, Permissions.Use))
            {
                if (query.GetRelatedQuery(EntityConsts.BibliographicListQuery, Roles.Payment) != null)
                    return InspectionResult.Allow;
                else if (!query.HasInclude(EntityConsts.BibliographicListQuery, Roles.Payment))
                    query.Include(EntityConsts.BibliographicListQuery, Roles.Payment);
            }

            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities)
        {
            if (query.IsForEntity(EntityConsts.BibliographicListQuery)
                && _securityService.CurrentUser.UserType == UserTypes.Customer
                && _securityService.HasModulePermission(_securityService.CurrentUser, BiblListModule.Id, Permissions.Use)
                && query.HasInclude(User.ENTITY, Roles.Customer))
            {
                bool isMe = true;
                foreach (var e in entities)
                {
                    var rel = e.GetSingleRelation(User.ENTITY, Roles.Customer);
                    if (rel == null || rel.Entity.Id != _securityService.CurrentUser.Id)
                    {
                        isMe = false;
                        break;
                    }
                }
                if (isMe)
                    return InspectionResult.Allow;
            }
            else if (query.IsForEntity(Payment.ENTITY) && query.HasInclude(EntityConsts.BibliographicListQuery, Roles.Payment))
            {
                bool ok = true;
                foreach (var e in entities)
                {
                    var rel = e.GetSingleRelation(EntityConsts.BibliographicListQuery, Roles.Payment);
                    if (rel == null)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                    return InspectionResult.Allow;
            }

            return InspectionResult.None;
        }
    }

    public class BiblListOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        private IEntityRepository _repository;
        public BiblListOperationInspector(ISecurityService securityService, IEntityRepository repository)
        {
            _securityService = securityService;
            _repository = repository;
        }

        public InspectionResult Inspect(Core.Services.tmp.EntityOperation operation)
        {
            if (operation.IsEntity(EntityConsts.BibliographicListQuery)
                && _securityService.HasModulePermission(_securityService.CurrentUser, BiblListModule.Id, Permissions.Use))
            {
                if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                    return InspectionResult.Allow;
                else if (operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    if (update.IsCreate())
                        return InspectionResult.Allow;
                    else if (update.IsEntity(EntityConsts.BibliographicListQuery))
                    {
                        var q = new EntityQuery2(User.ENTITY, _securityService.CurrentUser.Id);
                        q.WhereRelated(new RelationQuery(EntityConsts.BibliographicListQuery, Roles.Customer, update.Id.Value));
                        if (_repository.Read(q) != null)
                            return InspectionResult.Allow;
                    }
                }
            }

            return InspectionResult.None;
        }
    }

    public class BiblListOperationLogic : IEntityOperationLogic
    {
        private ISecurityService _securityService;
        private IEntityRepository _repository;
        private INotificationService _notificationService;
        private IFileService _fileService;
        private ITemplateService _templateService;
        public BiblListOperationLogic(ISecurityService securityService, IEntityRepository repository, INotificationService notificationService, IFileService fileService, ITemplateService templateService)
        {
            _securityService = securityService;
            _repository = repository;
            _notificationService = notificationService;
            _fileService = fileService;
            _templateService = templateService;
        }

        public void Before(Core.Services.tmp.EntityOperation operation, EntityOperationContext context)
        {
            if (operation.IsEntity(EntityConsts.BibliographicListQuery))
            {
                if (operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    if (update.IsCreate() && _securityService.CurrentUser.UserType == UserTypes.Customer)
                        update.Attach(User.ENTITY, Roles.Customer, _securityService.CurrentUser.Id);
                    else if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                    {
                        bool attach = false;
                        int? fileId = null;
                        if (update.IsCreate())
                            attach = true;
                        else
                        {
                            var q = new EntityQuery2(EntityConsts.BibliographicListQuery, update.Id.Value);
                            q.Include(User.ENTITY, Roles.ProcessedBy);
                            q.Include(File.ENTITY, Roles.File);
                            var e = _repository.Read(q);
                            var user = e.GetSingleRelation(User.ENTITY, Roles.ProcessedBy);
                            if (user == null)
                                attach = true;
                            else if (user.Entity.Id != _securityService.CurrentUser.Id)
                            {
                                update.Detach(User.ENTITY, Roles.ProcessedBy, user.Id);
                                attach = true;
                            }

                            var file = e.GetSingleRelation(File.ENTITY, Roles.File);
                            if (file != null)
                                fileId = file.Entity.Id;
                        }

                        if (attach)
                        {
                            update.Attach(User.ENTITY, Roles.ProcessedBy, _securityService.CurrentUser.Id);
                            if (fileId.HasValue)
                            {
                                var librarian = _securityService.CurrentUser;
                                using (_securityService.BeginSystemContext())
                                {
                                    _fileService.GrantAccess(fileId.Value, FileAccessType.Full, librarian);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void After(Core.Services.tmp.EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (!result.Success)
                return;

            var update = operation as EntityUpdate;
            if (operation.IsEntity(EntityConsts.BibliographicListQuery) && update != null && update.ContainsProperty("Status") && update.Get<QueryStatus>("Status") == QueryStatus.Completed)
            {
                var q = new EntityQuery2(EntityConsts.BibliographicListQuery, update.Id.Value) { AllProperties = true };
                q.Include(User.ENTITY, Roles.Customer);
                var biblListQuery = _repository.Read(q);
                var user = new User(biblListQuery.GetSingleRelation(User.ENTITY, Roles.Customer).Entity);
                var template = _templateService.Get(new Guid(NotificationTemplates.QUERY_COMPLETED));
                string subject = null, body = null;
                Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
                templateContext.Add("Customer", user);
                templateContext.Add("Query", biblListQuery);

                _templateService.Render(template, templateContext, out subject, out body);
                var withEmail = biblListQuery.GetData<ReplyMethods>("ReplyMethod") == ReplyMethods.ByEmail;
                _notificationService.SendNotification(withEmail, new User[] { user }, subject, body, null, new Relation[] { new Relation(Notification.ROLE, biblListQuery) });
            }
            else if (operation.IsEntity(Payment.ENTITY) && update != null && update.ContainsProperty("Status") && update.Get<PaymentStatus>("Status") == PaymentStatus.Paid)
            {
                var q = new EntityQuery2(EntityConsts.BibliographicListQuery);
                q.AddProperties("Number");
                q.WhereRelated(new RelationQuery(Payment.ENTITY, Roles.Payment, update.Id.Value));
                q.Include(User.ENTITY, Roles.Customer);
                q.Include(File.ENTITY, Roles.File);
                var biblListQuery = _repository.Read(q);
                if (biblListQuery != null)
                {
                    var file = new File(biblListQuery.GetSingleRelation(File.ENTITY, Roles.File).Entity);
                    var user = new User(biblListQuery.GetSingleRelation(User.ENTITY, Roles.Customer).Entity);

                    var template = _templateService.Get(new Guid(NotificationTemplates.PAYMENT_COMPLETED));

                    string subject = null, body = null;
                    Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
                    templateContext.Add("Customer", user);
                    templateContext.Add("Query", biblListQuery);

                    _templateService.Render(template, templateContext, out subject, out body);

                    var withEmail = biblListQuery.GetData<ReplyMethods>("ReplyMethod") == ReplyMethods.ByEmail;
                    _notificationService.SendNotification(withEmail, new User[] { user }, subject, body, new File[] { file }, new Relation[] { new Relation(Notification.ROLE, biblListQuery) });
                    //_fileService.GrantAccess(file.Id, FileAccessType.Read, new User(biblQuery.GetSingleRelation(User.ENTITY, Roles.Customer).Entity));

                }
            }
        }
    }
}
