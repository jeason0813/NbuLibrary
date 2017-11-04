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

namespace NbuLibrary.Modules.MyMagazines
{
    public class MyMagazinesNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<MyMagazinesModule>();
            Bind<IEntityQueryInspector>().To<MyMagazinesQueryInspector>();
            Bind<IEntityOperationInspector>().To<MyMagazinesOperationInspector>();
            Bind<IEntityOperationLogic>().To<MyMagazinesOperationLogic>();
        }
    }

    public class NotificationTemplates
    {
        public const string NEW_ISSUE = "25119940-f37c-418c-92a1-13d8f9a79d37";
        public const string DEACTIVATED_MAGAZINE = "a430b335-f3e4-4b7a-ae4e-37ece403fcbe";
    }

    public class Permissions
    {
        public const string Use = "Use";
    }

    public class EntityConsts
    {
        public const string Magazine = "Magazine";
        public const string Issue = "Issue";
        public const string MagazineCategory = "MagazineCategory";
        public const string Language = "Language";
        public const string Department = "Department";
    }

    public class MyMagazinesModule : IModule, IUIProvider
    {
        public const int Id = 102;

        public decimal Version
        {
            get { return 1.8m; }
        }

        int IModule.Id
        {
            get { return Id; }
        }

        public string Name
        {
            get { return "My Magazines"; }
        }

        public IUIProvider UIProvider
        {
            get { return this; }
        }

        public ModuleRequirements Requirements
        {
            get
            {
                var user = new EntityModel(User.ENTITY);
                var file = new EntityModel(NbuLibrary.Core.Domain.File.ENTITY);
                var notification = new EntityModel(Notification.ENTITY);
                var category = new NomenclatureModel(EntityConsts.MagazineCategory);
                var language = new NomenclatureModel(EntityConsts.Language);
                var departments = new NomenclatureModel(EntityConsts.Department);

                ModelBuilder mbMagazine = new ModelBuilder(EntityConsts.Magazine);
                mbMagazine.AddString("Title", 256);
                mbMagazine.AddString("ISSN", 64);
                mbMagazine.AddBoolean("IsActive", true);

                mbMagazine.Rules.AddRequired("Title");
                mbMagazine.Rules.AddUnique("Title");
                //mbMagazine.Rules.AddUnique("ISSN"); //TODO: make ISSN unique (?and required?)
                mbMagazine.Rules.AddRequired("IsActive");

                ModelBuilder mbIssue = new ModelBuilder(EntityConsts.Issue);
                mbIssue.AddInteger("Year");
                mbIssue.AddString("Name", 256);
                mbIssue.AddBoolean("Sent", false);

                mbIssue.Rules.AddRequired("Year");
                mbIssue.Rules.AddRequired("Name");
                mbIssue.Rules.AddRequired("Sent");

                mbMagazine.AddRelationTo(mbIssue.EntityModel, RelationType.OneToMany, Roles.Issue);
                mbMagazine.AddRelationTo(category, RelationType.ManyToMany, Roles.Category);
                mbMagazine.AddRelationTo(language, RelationType.ManyToMany, Roles.Language);
                mbMagazine.AddRelationTo(departments, RelationType.ManyToMany, Roles.Department);
                mbMagazine.AddRelationTo(notification, RelationType.OneToMany, Notification.ROLE);

                var subscription = mbMagazine.AddRelationTo(user, RelationType.ManyToMany, Roles.Subscriber);
                mbIssue.AddRelationTo(file, RelationType.OneToMany, Roles.Content);
                mbIssue.AddRelationTo(notification, RelationType.OneToMany, Notification.ROLE);


                var mbSubscription = new ModelBuilder(subscription);
                mbSubscription.AddBoolean("IsActive", true);
                mbSubscription.AddUri("Number", "TA");

                var dm = new DomainModel();
                dm.Entities.Add(mbMagazine.EntityModel);
                dm.Entities.Add(mbIssue.EntityModel);
                dm.Entities.Add(user);
                dm.Entities.Add(file);
                dm.Entities.Add(notification);
                dm.Entities.Add(category);
                dm.Entities.Add(language);
                dm.Entities.Add(departments);

                return new ModuleRequirements()
                {
                    RequredModules = new[] { 0, 1, 2, 3, 4 },
                    Domain = dm,
                    Permissions = new string[] { Permissions.Use },
                    //Definitions = new EntityDefinition[]{
                    //     new EntityDefinition("Magazine"){
                    //         Properties = new List<PropertyDefinition>(){
                    //             new StringProperty("Title", 256, false, true),
                    //             new PropertyDefinition("IsActive", PropertyTypes.Boolean, false)
                    //         }
                    //     },
                    //     new EntityDefinition("Issue"){
                    //         Properties = new List<PropertyDefinition>(){
                    //             new PropertyDefinition("Year", PropertyTypes.Integer, false),
                    //             new StringProperty("IssueNumber")
                    //         }
                    //     },
                    //     new EntityDefinition("Subscription")
                    //     {
                    //         Properties = new List<PropertyDefinition>(){
                    //             new PropertyDefinition("IsActive", PropertyTypes.Boolean, false)
                    //         }
                    //     }
                    // },
                    // Relations = new EntityRelation[] {
                    //     new EntityRelation("Magazine", "Issue", Roles.Issue, RelationTypes.OneToMany),
                    //     new EntityRelation("Subscription", "Magazine", Roles.Subscription, RelationTypes.OneToOne),
                    //     new EntityRelation("Subscription", "User", Roles.Subscriber, RelationTypes.OneToOne),
                    // },

                    UIDefinitions = new UIDefinition[]{
                         new GridDefinition(){ Name = "MyMagazines_Librarian_MagazinesGrid", Entity = EntityConsts.Magazine, Label = "Magazines' Grid" },
                         new GridDefinition(){ Name = "MyMagazines_Customer_MagazinesGrid", Entity = EntityConsts.Magazine, Label = "Magazines' Grid" },
                         new FormDefinition(){ Name = "MyMagazines_Librarian_MagazineForm", Entity=EntityConsts.Magazine, Label = "New magazine form (Librarian)"},
                         new GridDefinition(){ Name = "MyMagazines_Librarian_SubscriptionsGrid", Entity=User.ENTITY, Label = "Subscriptions' grid"},
                         new GridDefinition(){ Name = "MyMagazines_Customer_SubscriptionsGrid", Entity=EntityConsts.Magazine, Label = "Subscriptions' grid"},
                         new GridDefinition(){ Name = "MyMagazines_Librarian_IssuesGrid", Entity=EntityConsts.Issue, Label = "Issues' grid"},
                         new GridDefinition(){ Name = "MyMagazines_Customer_IssuesGrid", Entity=EntityConsts.Issue, Label = "Issues' grid"},
                         new GridDefinition(){ Name = "MyMagazines_Librarian_UsersGrid", Entity=User.ENTITY, Label = "Customers' grid"},
                         new FormDefinition(){ Name = "MyMagazines_Librarian_IssueForm", Entity=EntityConsts.Issue, Label = "Magazine's issue form (Librarian)"},
                         new FormDefinition(){ Name = "MyMagazines_Librarian_UserForm", Entity=User.ENTITY, Label = "User form (Librarian)"},
                     },
                    Templates = new HtmlTemplate[]{
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.NEW_ISSUE),
                            Name="My Magazines - New Issue",
                            SubjectTemplate="{Magazine.Title} {Issue.Name} {Issue.Year}",
                            BodyTemplate=@"Здравейте,
<p>Тъй като сте абониран за получаване на списание {Magazine.Title} Ви информираме, че разполагаме с новия брой: {Issue.Name}.</p>
Библиотека на НБУ"
                        },
                        new HtmlTemplate(){
                            Id = new Guid(NotificationTemplates.DEACTIVATED_MAGAZINE),
                            Name="My Magazines - Magazine deactivated",
                            SubjectTemplate="Преустановено списание {Magazine.Title}",
                            BodyTemplate=@"Здравейте,
<p>Списание {Magazine.Title} вече не е част от абонамента на библиотеката. По тази причина известията за новопристигнали броеве са преустановени.</p>
<p>С поздрав!</p>
<br />
НБУ Библиотека"
                        }
                     }
                };
            }
        }

        public void Initialize()
        {
        }

        public string GetClientTemplates(UserTypes type)
        {
            //if (type == UserTypes.Admin)
            //    return GetContent("Templates.AdminUI.html");
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Modules.MyMagazines.{0}", resourceFileName)))
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
                new ClientScript(){ Name="mymagazines", Content = GetContent("Scripts.mymagazines.js") },
                new ClientScript(){ Name="vm.magazinemgr", Content = GetContent("Scripts.vm.magazinemgr.js") },
                new ClientScript(){ Name="vm.magazines", Content = GetContent("Scripts.vm.magazines.js") },
                new ClientScript(){ Name="vm.subscriptions", Content = GetContent("Scripts.vm.subscriptions.js") },
                new ClientScript(){ Name="vm.mysubscriptions", Content = GetContent("Scripts.vm.mysubscriptions.js") },
                new ClientScript(){ Name="vm.mymagcustomers", Content = GetContent("Scripts.vm.mymagcustomers.js") }
            };
        }
    }

    public class MyMagazinesQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        public MyMagazinesQueryInspector(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (query.IsForEntity(EntityConsts.Magazine)
                || query.IsForEntity(EntityConsts.Issue)
                || query.IsForEntity(EntityConsts.MagazineCategory)
                || (query.IsForEntity(User.ENTITY) && _securityService.CurrentUser.UserType == UserTypes.Librarian))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, MyMagazinesModule.Id, Permissions.Use))
                {
                    return InspectionResult.Allow;//TODO: MyMagazines inspect query
                }
            }
            else if (query.IsForEntity(Notification.ENTITY)
                && _securityService.CurrentUser.UserType == UserTypes.Librarian 
                && _securityService.HasModulePermission(_securityService.CurrentUser, MyMagazinesModule.Id, Permissions.Use))
            {
                query.Include(EntityConsts.Issue, Notification.ROLE);
            }
            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities)
        {
            if (query.IsForEntity(Notification.ENTITY) 
                && _securityService.CurrentUser.UserType == UserTypes.Librarian 
                && _securityService.HasModulePermission(_securityService.CurrentUser, MyMagazinesModule.Id, Permissions.Use))
            {
                bool allowed = true;
                foreach (var entity in entities)
                {
                    var issue = entity.GetSingleRelation(EntityConsts.Issue, Notification.ROLE);
                    if (issue == null)
                    {
                        allowed = false;
                        break;
                    }
                }
                if (allowed)
                    return InspectionResult.Allow;
            }
            return InspectionResult.None;
        }
    }

    public class MyMagazinesOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        public MyMagazinesOperationInspector(ISecurityService securityService)
        {
            _securityService = securityService;
        }
        public InspectionResult Inspect(EntityOperation operation)
        {
            if (operation.IsEntity(EntityConsts.Magazine) || operation.IsEntity(EntityConsts.Issue))
            {
                if (_securityService.CurrentUser.UserType == UserTypes.Librarian
                    && _securityService.HasModulePermission(_securityService.CurrentUser, MyMagazinesModule.Id, Permissions.Use)
                    && operation is EntityUpdate)
                {
                    return InspectionResult.Allow;
                }
            }
            else if (operation is EntityUpdate && operation.IsEntity(User.ENTITY))
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate()
                    && _securityService.CurrentUser.UserType == UserTypes.Librarian
                    && _securityService.HasModulePermission(_securityService.CurrentUser, MyMagazinesModule.Id, Permissions.Use))
                    return InspectionResult.Allow;
            }

            return InspectionResult.None;
        }
    }

    public class MyMagazinesOperationLogic : IEntityOperationLogic
    {
        private const string CTXKEY_SEND_ISSUE = "MyMagazines:SendIssue";
        private const string CTXKEY_ISACTIVEOLD = "MyMagazines:IsActiveOldValue";
        private INotificationService _notificationService;
        private IEntityRepository _repository;
        private IFileService _fileService;
        private ISecurityService _securityService;
        private ITemplateService _templateService;

        public MyMagazinesOperationLogic(IEntityRepository repository, INotificationService notificationService, IFileService fileService, ISecurityService securityService, ITemplateService templateService)
        {
            _repository = repository;
            _notificationService = notificationService;
            _fileService = fileService;
            _securityService = securityService;
            _templateService = templateService;
        }

        public void Before(EntityOperation operation, EntityOperationContext context)
        {
            var update = operation as EntityUpdate;
            if (update != null)
            {
                if (operation.IsEntity(EntityConsts.Issue))
                {
                    if (update.IsCreate() && !update.ContainsProperty("Year"))
                        update.Set("Year", DateTime.Now.Year);

                    if (update.ContainsProperty("Sent"))//TODO: mymagazines issue send (sent flag used)
                    {
                        context.Set<bool>(CTXKEY_SEND_ISSUE, true);
                    }
                }
                else if (operation.IsEntity(EntityConsts.Magazine) && update.ContainsProperty("IsActive") && update.Id.HasValue)
                {
                    if (update.Get<bool>("IsActive") == false)
                    {
                        var q = new EntityQuery2(EntityConsts.Magazine, update.Id.Value);
                        q.AddProperty("IsActive");
                        var magazine = _repository.Read(q);
                        context.Set<bool>(CTXKEY_ISACTIVEOLD, magazine.GetData<bool>("IsActive"));
                    }
                }
            }
        }

        public void After(EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (!result.Success)
                return;

            if (operation.IsEntity(EntityConsts.Issue) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (context.Get<bool>(CTXKEY_SEND_ISSUE))
                {
                    SendIssueToSubscribers(operation as EntityUpdate);
                }

                if (update.ContainsRelation(File.ENTITY, Roles.Content))
                {
                    var filesAttached = update.GetMultipleRelationUpdates(File.ENTITY, Roles.Content).Where(fu => fu.Operation == RelationOperation.Attach);
                    if (filesAttached.Count() > 0)
                    {
                        var issue = update.ToEntity();
                        var q = new EntityQuery2(EntityConsts.Magazine);
                        q.WhereRelated(new RelationQuery(EntityConsts.Issue, Roles.Issue, issue.Id));
                        q.Include(User.ENTITY, Roles.Subscriber);
                        var mag = _repository.Read(q);
                        var subscribers = mag.GetManyRelations(User.ENTITY, Roles.Subscriber).Select(r => new User(r.Entity));
                        foreach (var subscriber in subscribers)
                        {
                            foreach (var fileUpdate in filesAttached)
                            {
                                if (!_fileService.HasAccess(subscriber, fileUpdate.Id.Value))
                                    _fileService.GrantAccess(fileUpdate.Id.Value, FileAccessType.Read, subscriber);
                            }
                        }
                    }
                }
            }
            else if (operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsEntity(User.ENTITY) && update.ContainsRelation(EntityConsts.Magazine, Roles.Subscriber))
                {
                    var rus = update.GetMultipleRelationUpdates(EntityConsts.Magazine, Roles.Subscriber).Where(ru => ru.Operation == RelationOperation.Attach);
                    foreach (var ru in rus)
                    {
                        var q = new EntityQuery2(EntityConsts.Issue);
                        q.WhereRelated(new RelationQuery(EntityConsts.Magazine, Roles.Issue, ru.Id.Value));
                        q.Include(File.ENTITY, Roles.Content);
                        var issues = _repository.Search(q);
                        foreach (var issue in issues)
                        {
                            //The user cannot give himself an access to file - only owner or administrator can.
                            using (_securityService.BeginSystemContext())
                            {
                                GiveFileAccessForIssue(issue, new User(update.ToEntity()));
                            }
                        }
                    }
                }
                else if (update.IsEntity(EntityConsts.Magazine) && update.ContainsRelation(User.ENTITY, Roles.Subscriber))
                {
                    var rus = update.GetMultipleRelationUpdates(User.ENTITY, Roles.Subscriber).Where(ru => ru.Operation == RelationOperation.Attach);

                    if (rus.Count() > 0)
                    {
                        var q = new EntityQuery2(EntityConsts.Issue);
                        q.WhereRelated(new RelationQuery(EntityConsts.Magazine, Roles.Issue, update.Id.Value));
                        q.Include(File.ENTITY, Roles.Content);
                        var issues = _repository.Search(q);
                        foreach (var ru in rus)
                        {
                            foreach (var issue in issues)
                                GiveFileAccessForIssue(issue, new User(ru.Id.Value));
                        }
                    }
                }
                else if (update.IsEntity(EntityConsts.Magazine) && update.ContainsProperty("IsActive"))
                {
                    var isActiveNew = update.Get<bool>("IsActive");
                    if (isActiveNew == false && context.Get<bool>(CTXKEY_ISACTIVEOLD))
                    {
                        SendMagazineNotActiveToSubscribers(update);
                    }
                }
            }
        }

        private void SendIssueToSubscribers(EntityUpdate update)
        {
            var issueQuery = new EntityQuery2(EntityConsts.Issue, update.Id.Value);
            issueQuery.AllProperties = true;
            issueQuery.Include(EntityConsts.Magazine, Roles.Issue);
            issueQuery.Include(NbuLibrary.Core.Domain.File.ENTITY, Roles.Content);
            var issue = _repository.Read(issueQuery);
            var magazine = issue.GetSingleRelation(EntityConsts.Magazine, Roles.Issue).Entity;
            var subscribersQuery = new EntityQuery2(User.ENTITY);
            var relQuery = new RelationQuery(EntityConsts.Magazine, Roles.Subscriber, magazine.Id);
            relQuery.RelationRules.Add(new Condition("IsActive", Condition.Is, true));
            subscribersQuery.WhereRelated(relQuery);
            subscribersQuery.AllProperties = true;
            var subscribers = _repository.Search(subscribersQuery).Select(e => new User(e));

            var contents = issue.GetManyRelations(NbuLibrary.Core.Domain.File.ENTITY, Roles.Content).Select(r => new File(r.Entity));

            var template = _templateService.Get(new Guid(NotificationTemplates.NEW_ISSUE));

            string subject = null, body = null;
            Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
            templateContext.Add("Magazine", magazine);
            templateContext.Add("Issue", issue);

            _templateService.Render(template, templateContext, out subject, out body);


            _notificationService.SendNotification(true, subscribers, subject, body, contents, new Relation[] { new Relation(Notification.ROLE, issue) });
        }

        private void SendMagazineNotActiveToSubscribers(EntityUpdate update)
        {
            var magazineQuery = new EntityQuery2(EntityConsts.Magazine, update.Id.Value);
            magazineQuery.AllProperties = true;
            magazineQuery.Include(User.ENTITY, Roles.Subscriber);

            var magazine = _repository.Read(magazineQuery);

            var subscribers = magazine.GetManyRelations(User.ENTITY, Roles.Subscriber).Select(r => new User(r.Entity));

            var template = _templateService.Get(new Guid(NotificationTemplates.DEACTIVATED_MAGAZINE));

            string subject = null, body = null;
            Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
            templateContext.Add("Magazine", magazine);
            _templateService.Render(template, templateContext, out subject, out body);
            _notificationService.SendNotification(true, subscribers, subject, body, null, new Relation[] { new Relation(Notification.ROLE, magazine) });
        }

        private void GiveFileAccessForIssue(Entity issue, User user)
        {
            foreach (var fileAttach in issue.GetManyRelations(File.ENTITY, Roles.Content))
            {
                if (!_fileService.HasAccess(user, fileAttach.Entity.Id))
                    _fileService.GrantAccess(fileAttach.Entity.Id, FileAccessType.Read, user);
            }
        }
    }

}
