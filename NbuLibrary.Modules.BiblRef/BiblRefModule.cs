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

namespace NbuLibrary.Modules.BiblRef
{
    public class NotificationTemplates
    {
        public const string QUERY_COMPLETED = "ed3fb822-84cc-43c4-90ee-7653d1f3ca0e";
        public const string PAYMENT_COMPLETED = "7db76fc8-8a64-427c-ac88-45b6fc901049";
    }

    public class EntityConsts
    {
        public const string Bibliography = "Bibliography";
        public const string BibliographicDocument = "BibliographicDocument";
        public const string Language = "Language";
        public const string BibliographicQuery = "BibliographicQuery";
    }

    public class Roles
    {
        public const string Included = "Included";
        public const string Keywords = "Keywords";
        public const string Query = "Query";
        public const string File = "File";
        public const string Arguments = "Arguments";
        public const string Payment = "Payment";
        public const string Customer = "Customer";
        public const string ProcessedBy = "ProcessedBy";
    }

    public class Permissions
    {
        public const string Use = "Use";
    }


    public class BiblRefNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<BiblRefModule>();
            Bind<IEntityQueryInspector>().To<BiblRefQueryInspector>();
            Bind<IEntityOperationInspector>().To<BiblRefOperationInspector>();
            Bind<IEntityOperationLogic>().To<BiblRefOperationLogic>();
        }
    }


    public class BiblRefModule : IModule
    {
        public const int Id = 103;

        int IModule.Id
        {
            get { return Id; }
        }

        public decimal Version
        {
            get { return 1.9m; }
        }

        public string Name
        {
            get { return "BibliographicReferences"; }
        }

        public IUIProvider UIProvider
        {
            get { return new BiblRefUIProvider(); }
        }

        public ModuleRequirements Requirements
        {
            get
            {
                var payment = new EntityModel(Payment.ENTITY);
                var user = new EntityModel(User.ENTITY);
                var notification = new EntityModel(Notification.ENTITY);
                var language = new NomenclatureModel(EntityConsts.Language);
                var document = new NomenclatureModel(EntityConsts.BibliographicDocument);
                var arguments = new NomenclatureModel("Arguments");
                var file = new EntityModel(File.ENTITY);

                ModelBuilder mbBibliography = new ModelBuilder(EntityConsts.Bibliography);
                mbBibliography.AddString("Subject", 512);
                mbBibliography.AddInteger("FromYear");
                mbBibliography.AddInteger("ToYear");
                mbBibliography.AddBoolean("Complete", false);

                mbBibliography.Rules.AddRequired("Subject");
                mbBibliography.Rules.AddRequired("FromYear");
                mbBibliography.Rules.AddRequired("ToYear");
                mbBibliography.Rules.AddRequired("Complete");

                mbBibliography.AddRelationTo(document, RelationType.ManyToMany, Roles.Included);
                var keywords = mbBibliography.AddRelationTo(language, RelationType.ManyToMany, Roles.Keywords);
                ModelBuilder mbKeywords = new ModelBuilder(keywords);
                mbKeywords.AddString("Keywords", 1024);
                mbKeywords.Rules.AddRequired("Keywords");
                mbBibliography.AddRelationTo(file, RelationType.OneToOne, Roles.File);
                mbBibliography.AddRelationTo(notification, RelationType.OneToMany, Notification.ROLE);

                ModelBuilder mbQuery = new ModelBuilder(EntityConsts.BibliographicQuery);
                mbQuery.AddUri("Number", "SB");
                mbQuery.AddBoolean("ForNew");
                mbQuery.AddEnum<QueryStatus>("Status", QueryStatus.New);
                mbQuery.AddDateTime("ReplyBefore");
                mbQuery.AddEnum<ReplyMethods>("ReplyMethod", ReplyMethods.ByNotification);
                mbQuery.AddEnum<PaymentMethod>("PaymentMethod");

                mbQuery.Rules.AddRequired("ForNew");
                mbQuery.Rules.AddRequired("ReplyBefore");
                mbQuery.Rules.AddFutureDate("ReplyBefore", TimeSpan.FromDays(5.0));
                mbQuery.Rules.AddRequired("ReplyMethod");
                mbQuery.Rules.AddRequired("PaymentMethod");

                mbQuery.AddRelationTo(arguments, RelationType.ManyToOne, Roles.Arguments);
                mbQuery.AddRelationTo(mbBibliography.EntityModel, RelationType.ManyToOne, Roles.Query);
                mbQuery.AddRelationTo(payment, RelationType.OneToOne, Roles.Payment);
                mbQuery.AddRelationTo(user, RelationType.ManyToOne, Roles.Customer);
                mbQuery.AddRelationTo(user, RelationType.ManyToOne, Roles.ProcessedBy);
                mbQuery.AddRelationTo(notification, RelationType.OneToMany, Notification.ROLE);

                var dm = new DomainModel();
                dm.Entities.Add(payment);
                dm.Entities.Add(user);
                dm.Entities.Add(language);
                dm.Entities.Add(document);
                dm.Entities.Add(arguments);
                dm.Entities.Add(file);
                dm.Entities.Add(mbBibliography.EntityModel);
                dm.Entities.Add(mbQuery.EntityModel);

                return new ModuleRequirements()
                {
                    RequredModules = new[] { 0, 1, 2, 3, 4, 5 },
                    Domain = dm,
                    Permissions = new string[] { Permissions.Use },
                    UIDefinitions = new UIDefinition[]{
                        new FormDefinition(){ Entity = EntityConsts.BibliographicQuery, Name = "BiblRef_Customer_BibliographicQuery_Form", Label = "Bibliographic query form" },
                        new FormDefinition(){ Entity = EntityConsts.BibliographicQuery, Name = "BiblRef_Librarian_BibliographicQuery_Form", Label = "Bibliographic query form" },
                        new FormDefinition(){ Entity = EntityConsts.BibliographicQuery, Name = "BiblRef_BibliographicQuery_Process", Label = "Bibliographic query process form" },
                        new FormDefinition(){ Entity = EntityConsts.Bibliography, Name = "BiblRef_Bibliography_Form", Label = "Bibliography form" },
                        new FormDefinition(){ Entity = EntityConsts.Bibliography, Name = "BiblRef_Librarian_Bibliography_Form", Label = "Bibliography form" },
                        new ViewDefinition(){ Entity = EntityConsts.Bibliography, Name = "BiblRef_Customer_Bibliography_Details", Label = "Bibliography details" },
                        new ViewDefinition(){ Entity = EntityConsts.Bibliography, Name = "BiblRef_Librarian_Bibliography_Details", Label = "Bibliography details" },
                        new FormDefinition(){ Entity = EntityConsts.Bibliography, Name = "BiblRef_Bibliography_CompletionForm", Label = "Bibliography's completion form" },
                        new GridDefinition(){ Entity = EntityConsts.Bibliography, Name = "BiblRef_Customer_BibliographyGrid", Label = "Bibliographies grid" },
                        new GridDefinition(){ Entity = EntityConsts.Bibliography, Name = "BiblRef_Librarian_BibliographyGrid", Label = "Bibliographies grid" },
                        new GridDefinition(){ Entity = EntityConsts.BibliographicQuery, Name = "BiblRef_Customer_BibliographicQuery_Grid", Label = "BibliographicQuery grid" },
                        new GridDefinition(){ Entity = EntityConsts.BibliographicQuery, Name = "BiblRef_Librarian_BibliographicQuery_Grid", Label = "BibliographicQuery grid" },
                        new ViewDefinition(){ Entity = EntityConsts.BibliographicQuery, Name = "BiblRef_Librarian_BibliographicQuery_Details", Label = "BibliographicQuery Details" },
                        new ViewDefinition(){ Entity = EntityConsts.BibliographicQuery, Name = "BiblRef_Customer_BibliographicQuery_Details", Label = "BibliographicQuery Details" }
                    },
                    Templates = new[]{
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.QUERY_COMPLETED),
                            Name="Bibliographic Reference - Query Completed",
                            SubjectTemplate="Заявка {Query.Number} е завършена",
                            BodyTemplate=@"Здравейте {Customer.FirstName} {Customer.LastName},
<p>Уведомяваме Ви, че обработката на заявка {Query.Number} за услуга ""Тематична библиография"" приключи успешно. Щом платите своето задължение ще получите потвърждение и възможност за изтегляне на файла.</p>
Библиотека на НБУ"
                        },
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.PAYMENT_COMPLETED),
                            Name="Bibliographic Reference - Payment Completed",
                            SubjectTemplate="Заявка {Query.Number} е завършена",
                            BodyTemplate=@"Здравейте {Customer.FirstName} {Customer.LastName},
<p>Заявка {Query.Number} за услуга ""Тематична библиография"" е платена. Може да изтеглите получения файл.</p>
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

    public class BiblRefUIProvider : IUIProvider
    {
        public string GetClientTemplates(NbuLibrary.Core.Domain.UserTypes type)
        {
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Modules.BiblRef.{0}", resourceFileName)))
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
                new ClientScript(){ Name = "biblref", Content = GetContent("Scripts.biblref.js") },
                new ClientScript(){ Name = "vm.bibliography", Content = GetContent("Scripts.vm.bibliography.js") },
                new ClientScript(){ Name = "vm.biblqueries", Content = GetContent("Scripts.vm.biblqueries.js") },
                new ClientScript(){ Name = "vm.biblquery", Content = GetContent("Scripts.vm.biblquery.js") },
                new ClientScript(){ Name = "vm.bibliographies", Content = GetContent("Scripts.vm.bibliographies.js") }
            };
        }
    }

    public class BiblRefQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        public BiblRefQueryInspector(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (query.IsForEntity("Arguments") && _securityService.HasModulePermission(_securityService.CurrentUser, BiblRefModule.Id, Permissions.Use))
                return InspectionResult.Allow;
            if (query.IsForEntity(EntityConsts.BibliographicDocument)
                || query.IsForEntity(EntityConsts.BibliographicQuery)
                || query.IsForEntity(EntityConsts.Bibliography)
                || query.IsForEntity(EntityConsts.Language))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, BiblRefModule.Id, Permissions.Use))
                {
                    if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                        return InspectionResult.Allow;
                    else if (_securityService.CurrentUser.UserType == UserTypes.Customer && query.IsForEntity(EntityConsts.BibliographicQuery))
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
                && _securityService.HasModulePermission(_securityService.CurrentUser, BiblRefModule.Id, Permissions.Use))
            {
                if (query.GetRelatedQuery(EntityConsts.BibliographicQuery, Roles.Payment) != null)
                    return InspectionResult.Allow;
                else if (!query.HasInclude(EntityConsts.BibliographicQuery, Roles.Payment))
                    query.Include(EntityConsts.BibliographicQuery, Roles.Payment);
            }

            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities)
        {
            if (query.IsForEntity(EntityConsts.BibliographicQuery)
                && _securityService.CurrentUser.UserType == UserTypes.Customer
                && _securityService.HasModulePermission(_securityService.CurrentUser, BiblRefModule.Id, Permissions.Use)
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
            else if (query.IsForEntity(Payment.ENTITY) && query.HasInclude(EntityConsts.BibliographicQuery, Roles.Payment))
            {
                bool ok = true;
                foreach (var e in entities)
                {
                    var rel = e.GetSingleRelation(EntityConsts.BibliographicQuery, Roles.Payment);
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

    public class BiblRefOperationLogic : IEntityOperationLogic
    {
        private ISecurityService _securityService;
        private IEntityRepository _repository;
        private INotificationService _notificationService;
        private ITemplateService _templateService;
        //private IFileService _fileService;
        public BiblRefOperationLogic(ISecurityService securityService, IEntityRepository repository, INotificationService notificationService, ITemplateService templateService)
        {
            _securityService = securityService;
            _repository = repository;
            _notificationService = notificationService;
            _templateService = templateService;
            //_fileService = fileService;
        }

        public void Before(Core.Services.tmp.EntityOperation operation, EntityOperationContext context)
        {
            if (operation.IsEntity(EntityConsts.BibliographicQuery))
            {
                if (operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    if (update.IsCreate() && _securityService.CurrentUser.UserType == UserTypes.Customer)
                        update.Attach(User.ENTITY, Roles.Customer, _securityService.CurrentUser.Id);
                    else if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                    {
                        bool attach = false;
                        if (update.IsCreate())
                            attach = true;
                        else
                        {
                            var q = new EntityQuery2(User.ENTITY);
                            q.WhereRelated(new RelationQuery(EntityConsts.BibliographicQuery, Roles.ProcessedBy, update.Id.Value));
                            var user = _repository.Read(q);
                            if (user == null)
                                attach = true;
                            else if (user.Id != _securityService.CurrentUser.Id)
                            {
                                update.Detach(User.ENTITY, Roles.ProcessedBy, user.Id);
                                attach = true;
                            }
                        }

                        if (attach)
                            update.Attach(User.ENTITY, Roles.ProcessedBy, _securityService.CurrentUser.Id);
                    }
                }
            }
        }

        public void After(Core.Services.tmp.EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (!result.Success)
                return;

            var update = operation as EntityUpdate;
            if (operation.IsEntity(EntityConsts.BibliographicQuery) && update != null && update.ContainsProperty("Status") && update.Get<QueryStatus>("Status") == QueryStatus.Completed)
            {
                var q = new EntityQuery2(EntityConsts.BibliographicQuery, update.Id.Value) { AllProperties = true };
                q.Include(User.ENTITY, Roles.Customer);
                q.Include(EntityConsts.Bibliography, Roles.Query);
                var biblQuery = _repository.Read(q);
                var user = new User(biblQuery.GetSingleRelation(User.ENTITY, Roles.Customer).Entity);
                var bibl = biblQuery.GetSingleRelation(EntityConsts.Bibliography, Roles.Query).Entity;

                var template = _templateService.Get(new Guid(NotificationTemplates.QUERY_COMPLETED));
                string subject = null, body = null;
                Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
                templateContext.Add("Customer", user);
                templateContext.Add("Query", biblQuery);

                templateContext.Add("Bibliography", bibl);

                _templateService.Render(template, templateContext, out subject, out body);
                var withEmail = biblQuery.GetData<ReplyMethods>("ReplyMethod") == ReplyMethods.ByEmail;
                _notificationService.SendNotification(withEmail, new User[] { user }, subject, body, null, new Relation[] { new Relation(Notification.ROLE, biblQuery), new Relation(Notification.ROLE, bibl) });
            }
            else if (operation.IsEntity(Payment.ENTITY) && update != null && update.ContainsProperty("Status") && update.Get<PaymentStatus>("Status") == PaymentStatus.Paid)
            {
                var q = new EntityQuery2(EntityConsts.BibliographicQuery);
                q.AllProperties = true;
                q.WhereRelated(new RelationQuery(Payment.ENTITY, Roles.Payment, update.Id.Value));
                q.Include(User.ENTITY, Roles.Customer);
                var biblQuery = _repository.Read(q);
                if (biblQuery != null)
                {
                    var q2 = new EntityQuery2(EntityConsts.Bibliography);
                    q2.AddProperties("Subject");
                    q2.WhereRelated(new RelationQuery(EntityConsts.BibliographicQuery, Roles.Query, biblQuery.Id));
                    q2.Include(File.ENTITY, Roles.File);
                    var bibl = _repository.Read(q2);
                    var file = new File(bibl.GetSingleRelation(File.ENTITY, Roles.File).Entity);
                    var user = new User(biblQuery.GetSingleRelation(User.ENTITY, Roles.Customer).Entity);
                    var template = _templateService.Get(new Guid(NotificationTemplates.PAYMENT_COMPLETED));

                    string subject = null, body = null;
                    Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
                    templateContext.Add("Customer", user);
                    templateContext.Add("Query", biblQuery);

                    templateContext.Add("Bibliography", bibl);

                    _templateService.Render(template, templateContext, out subject, out body);
                    var withEmail = biblQuery.GetData<ReplyMethods>("ReplyMethod") == ReplyMethods.ByEmail;
                    _notificationService.SendNotification(withEmail, new User[] { user }, subject, body, new File[] { file }, new Relation[] { new Relation(Notification.ROLE, biblQuery), new Relation(Notification.ROLE, bibl) });
                    //_fileService.GrantAccess(file.Id, FileAccessType.Read, new User(biblQuery.GetSingleRelation(User.ENTITY, Roles.Customer).Entity));

                }
            }
        }
    }

}
