using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.NotificationModule
{
    public class NotificationNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<NotificationModule>();
            Bind<IEntityQueryInspector>().To<NotificationEntityQueryInspector>();
            Bind<IEntityOperationInspector>().To<NotifOperationInspector>();
            Bind<IEntityOperationLogic>().To<NotificationLogic>();
            Bind<IBackgroundService>().To<EmailSenderBackgroundService>();
        }
    }

    public class NotificationModule : IModule, IUIProvider
    {
        public const int Id = 3;

        private IDatabaseService _dbService;

        public NotificationModule(IDatabaseService dbService)
        {
            _dbService = dbService;
        }

        int IModule.Id
        {
            get { return Id; }
        }

        public decimal Version
        {
            get { return 2.1m; }
        }

        public string Name
        {
            get { return "NotificationModule"; }
        }

        public IUIProvider UIProvider
        {
            get { return this; }
        }

        public ModuleRequirements Requirements
        {
            get
            {
                ModelBuilder mbNotif = new ModelBuilder(Notification.ENTITY);
                mbNotif.AddString("Subject", 512);
                mbNotif.AddStringMax("Body");
                mbNotif.AddBoolean("Received", false);
                mbNotif.AddBoolean("EmailSent", false);
                mbNotif.AddEnum<ReplyMethods>("Method", ReplyMethods.ByNotification);
                mbNotif.AddDateTime("Date");
                mbNotif.AddInteger("EmailRetries", 0);
                mbNotif.AddBoolean("Archived", false);
                mbNotif.AddBoolean("ArchivedSent", false);

                mbNotif.Rules.AddRequired("Subject");
                mbNotif.Rules.AddRequired("Body");
                mbNotif.Rules.AddRequired("Method");
                mbNotif.Rules.AddRequired("Date");


                EntityModel user = new EntityModel("User");
                var file = new EntityModel(NbuLibrary.Core.Domain.File.ENTITY);
                mbNotif.AddRelationTo(user, RelationType.ManyToOne, Roles.Sender);
                mbNotif.AddRelationTo(user, RelationType.ManyToOne, Roles.Recipient);
                mbNotif.AddRelationTo(file, RelationType.ManyToMany, Roles.Attachment);
                var dm = new DomainModel();
                dm.Entities.Add(mbNotif.EntityModel);
                dm.Entities.Add(user);

                return new ModuleRequirements()
                {
                    RequredModules = new[] { 1, 4 },
                    Domain = dm,
                    //Definitions = new EntityDefinition[] {
                    //    new EntityDefinition("Notification"){
                    //        Properties = new List<PropertyDefinition>(){
                    //            new StringProperty("Subject", 512, false, false),
                    //            new StringProperty("Body", 4000, false, false),
                    //            new PropertyDefinition("Received", PropertyTypes.Boolean, false),
                    //            new PropertyDefinition("EmailSent", PropertyTypes.Boolean, false),
                    //            new EnumValueProperty("Method", typeof(ReplyMethods), false),
                    //            new PropertyDefinition("Date", PropertyTypes.Date, false)
                    //        }
                    //    }
                    //},
                    //Relations = new EntityRelation[] {
                    //    new EntityRelation(EntityNames.Notification, EntityNames.User, Roles.Sender, RelationTypes.ManyToOne),
                    //    new EntityRelation(EntityNames.Notification, EntityNames.User, Roles.Recipient, RelationTypes.ManyToOne)
                    //},

                    UIDefinitions = new UIDefinition[]{
                        new GridDefinition(){
                          Name = "Notifications_All_Inbox",
                          Label = "Inbox",
                          Entity = EntityNames.Notification,
                          Fields = new List<ViewField>(){

                              new Textfield(){ Property = "Received", Label = "Read", Order = 1, Type= ViewTypes.Checkfield },
                              new Textfield(){ Property = "Subject", Label = "Subject", Length = 512, Order = 2 },
                              new Textfield(){ Entity = EntityNames.User, Role = Roles.Sender, Property = "Email", Length = 128, Order = 3, Label="From" },
                              new Datefield(){ Property = "Date", Label= "Date", Order = 4 }
                          }
                        },
                        new GridDefinition(){
                          Name = "Notifications_All_Sent",
                          Label = "Sent",
                          Entity = EntityNames.Notification,
                          Fields = new List<ViewField>(){

                              new Textfield(){ Property = "Subject", Label = "Subject", Length = 512, Order = 1 },
                              new Textfield(){ Entity = EntityNames.User, Role = Roles.Recipient, Property = "Email", Length = 128, Order = 2, Label="To" },
                              new Textfield(){ Property = "Received", Label = "Received", Order = 3, Type= ViewTypes.Checkfield },
                              new Datefield(){ Property = "Date", Label= "Date", Order = 4 }
                          }
                        },
                        new GridDefinition(){
                          Name = "Notifications_All_Generic",
                          Label = "Notifications",
                          Entity = EntityNames.Notification,
                          Fields = new List<ViewField>(){

                              new Textfield(){ Property = "Subject", Label = "Subject", Length = 512, Order = 1 },
                              new Textfield(){ Entity = EntityNames.User, Role = Roles.Recipient, Property = "Email", Length = 128, Order = 2, Label="To" },
                              new Textfield(){ Entity = EntityNames.User, Role = Roles.Sender, Property = "Email", Length = 128, Order = 3, Label="From" },
                              new Textfield(){ Property = "Received", Label = "Received", Order = 3, Type= ViewTypes.Checkfield },
                              new Datefield(){ Property = "Date", Label= "Date", Order = 4 }
                          }
                        },
                        new ViewDefinition()
                        {
                            Name = "Notifications_All_Read",
                            Label = "Read Notification",
                            Entity = EntityNames.Notification,
                            Fields = new List<ViewField>()
                        },
                        new FormDefinition(){
                            Name = "Notifications_All_SendNew",
                            Label = "Send Notification",
                            Entity = EntityNames.Notification,
                            Fields = new List<EditField>()
                        }
                    }
                };
            }
        }

        public void Initialize()
        {
            using (var conn = _dbService.GetSqlConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("update [Notification] set Archived = 0 where Archived is null;", conn);
                var cmd2 = new SqlCommand("update [Notification] set ArchivedSent = 0 where ArchivedSent is null;", conn);
                cmd.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
            }
        }

        public string GetClientTemplates(Domain.UserTypes type)
        {
            //if (type == UserTypes.Admin)
            //    return GetContent("Templates.AdminUI.html");
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Core.NotificationModule.{0}", resourceFileName)))
            {
                using (var reader = new StreamReader(stream))
                {
                    resourceContent = reader.ReadToEnd();
                }
            }

            return resourceContent;
        }

        IEnumerable<ClientScript> IUIProvider.GetClientScripts(UserTypes type)
        {
            return new List<ClientScript>() {
                new ClientScript(){
                    Name = "notifications",
                    Content = GetContent("Scripts.notifications.js")
                },
                new ClientScript(){
                    Name = "vm.notifications",
                    Content = GetContent("Scripts.vm.notifications.js")
                },
                new ClientScript(){
                    Name = "vm.notification",
                    Content = GetContent("Scripts.vm.notification.js")
                }
            };
        }
    }

    public class NotifOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        private IEntityRepository _repository;
        public NotifOperationInspector(ISecurityService securityService, IEntityRepository repository)
        {
            _securityService = securityService;
            _repository = repository;
        }

        public InspectionResult Inspect(EntityOperation operation)
        {
            if (operation.IsEntity(Notification.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate())
                    return InspectionResult.Allow;
                else if (update.PropertyUpdates.Count == 1 && (update.ContainsProperty("Received") || update.ContainsProperty("Archived")))
                {
                    EntityQuery2 q = new EntityQuery2(Notification.ENTITY, update.Id.Value);
                    q.Include(User.ENTITY, Roles.Recipient);
                    var recipient = _repository.Read(q).GetSingleRelation(User.ENTITY, Roles.Recipient);
                    if (recipient != null && recipient.Entity.Id == _securityService.CurrentUser.Id)
                        return InspectionResult.Allow;
                }
                else if (update.PropertyUpdates.Count == 1 && update.ContainsProperty("ArchivedSent"))
                {
                    EntityQuery2 q = new EntityQuery2(Notification.ENTITY, update.Id.Value);
                    q.Include(User.ENTITY, Roles.Sender);
                    var sender = _repository.Read(q).GetSingleRelation(User.ENTITY, Roles.Sender);
                    if (sender != null && sender.Entity.Id == _securityService.CurrentUser.Id)
                        return InspectionResult.Allow;
                }
            }

            return InspectionResult.None;
        }
    }


    public class NotificationEntityQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        public NotificationEntityQueryInspector(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (query.IsForEntity(Notification.ENTITY))
            {
                var relToSender = query.GetRelatedQuery(User.ENTITY, Roles.Sender);
                if (relToSender != null)
                {
                    var id = relToSender.GetSingleId();
                    if (id.HasValue && id.Value == _securityService.CurrentUser.Id)
                        return InspectionResult.Allow;
                }
                var relToRecipient = query.GetRelatedQuery(User.ENTITY, Roles.Recipient);
                if (relToRecipient != null)
                {
                    var id = relToRecipient.GetSingleId();
                    if (id.HasValue && id.Value == _securityService.CurrentUser.Id)
                        return InspectionResult.Allow;
                }

                if (relToRecipient == null)
                {
                    query.Include(User.ENTITY, Roles.Recipient);
                }
                if (relToSender != null)
                {
                    query.Include(User.ENTITY, Roles.Sender);
                }
            }

            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities)
        {
            if (query.IsForEntity(Notification.ENTITY))
            {
                bool relatedToMe = true;
                foreach (var en in entities)
                {
                    relatedToMe = false;
                    var sender = en.GetSingleRelation(User.ENTITY, Roles.Sender);
                    if (sender != null && sender.Entity.Id == _securityService.CurrentUser.Id)
                    {
                        relatedToMe = true;
                        continue;
                    }

                    var rec = en.GetSingleRelation(User.ENTITY, Roles.Recipient);
                    if (rec != null && rec.Entity.Id == _securityService.CurrentUser.Id)
                    {
                        relatedToMe = true;
                        continue;
                    }

                    if (!relatedToMe)
                        break;
                }

                if (relatedToMe)
                    return InspectionResult.Allow;
            }
            return InspectionResult.None;
        }
    }

    public class NotificationLogic : IEntityOperationLogic
    {
        private const string CTXKEY_CREATENOTIFICATION = "Notifications:CreateNew";

        private ISecurityService _securityService;
        private INotificationService _notificationService;
        private IEntityRepository _repository;
        private IFileService _fileService;
        public NotificationLogic(ISecurityService securityService, IEntityRepository repository, INotificationService notificationService, IFileService fileService)
        {
            _securityService = securityService;
            _notificationService = notificationService;
            _repository = repository;
            _fileService = fileService;
        }

        public void Before(EntityOperation operation, EntityOperationContext context)
        {
            if (operation.IsEntity(Notification.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate())
                {
                    update.Set("Date", DateTime.Now);
                    var sender = update.GetRelationUpdate(User.ENTITY, Roles.Sender);
                    if (sender == null)
                        update.Attach(User.ENTITY, Roles.Sender, _securityService.CurrentUser.Id);
                    else
                        sender.Id = _securityService.CurrentUser.Id;
                    context.Set<bool>(CTXKEY_CREATENOTIFICATION, true);
                }

                if (update.ContainsProperty("Body"))
                {
                    var text = System.Web.HttpUtility.HtmlEncode(update.Get<string>("Body"));
                    var newText = HtmlProcessor.ProcessEncodedHtml(text);
                    update.Set("Body", newText);
                }
            }
        }

        public void After(EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (operation.IsEntity(Notification.ENTITY)
                && operation is EntityUpdate
                && context.Get<bool>(CTXKEY_CREATENOTIFICATION)
                && result.Success)
            {
                var update = operation as EntityUpdate;
                var method = ReplyMethods.ByNotification;//default
                if (update.ContainsProperty("Method"))
                    method = update.Get<ReplyMethods>("Method");



                var recipientUpdate = update.GetRelationUpdate(User.ENTITY, Roles.Recipient);
                var attachments = update.GetMultipleRelationUpdates(NbuLibrary.Core.Domain.File.ENTITY, Roles.Attachment);
                if (attachments != null)
                    foreach (var att in attachments)
                    {
                        _fileService.GrantAccess(att.Id.Value, FileAccessType.Read, new User(recipientUpdate.Id.Value));
                    }

                var recipientQuery = new EntityQuery2(User.ENTITY, recipientUpdate.Id.Value);
                recipientQuery.AddProperty("Email");
                var recipient = _repository.Read(recipientQuery);
                var to = recipient.GetData<string>("Email");
                var body = update.Get<string>("Body");
                var subject = update.Get<string>("Subject");
            }
        }
    }

    //public class BusinessLogic : IBusinessLogic
    //{
    //    private ISecurityService _securityService;
    //    private INotificationService _notificationService;
    //    private IEntityService _entityService;
    //    public BusinessLogic(ISecurityService securityService, INotificationService notificationService, IEntityService entityService)
    //    {
    //        _securityService = securityService;
    //        _notificationService = notificationService;
    //        _entityService = entityService;
    //    }

    //    public OperationResolution Resolve(EntityOperation operation)
    //    {
    //        if (operation.EntityName.Equals(EntityNames.Notification, StringComparison.InvariantCultureIgnoreCase))
    //        {
    //            return OperationResolution.Regonized;
    //        }
    //        else return OperationResolution.None;
    //    }

    //    public void Prepare(EntityOperation operation, ref BLContext context)
    //    {
    //        if (operation is EntityUpdate)
    //        {
    //            var update = operation as EntityUpdate;

    //            if (update.IsCreate)
    //            {
    //                #region Set current date

    //                if (update.PropertyUpdates.Contains("Date"))
    //                    update.PropertyUpdates.Remove("Date");

    //                update.Set("Date", DateTime.Now);

    //                #endregion

    //                #region Set sender
    //                var senderUpd = new RelationUpdate()
    //                {
    //                    Id=_securityService.CurrentUser.Id,
    //                    EntityName = EntityNames.User,
    //                    Role = Roles.Sender,
    //                    Operation = RelationOperation.Attach
    //                };
    //                update.RelationUpdates.Add(senderUpd);
    //                #endregion


    //                //TODO: Replace with common default-value functionallity
    //                PropertyUpdate receiedUpd = update.PropertyUpdates["Received"];
    //                if (receiedUpd != null)
    //                    update.PropertyUpdates.Remove(receiedUpd);
    //                receiedUpd = new PropertyUpdate("Received", false);
    //                update.Set("Received", false);

    //                PropertyUpdate sentUpd = update.PropertyUpdates["EmailSent"];
    //                if (sentUpd != null)
    //                    update.PropertyUpdates.Remove(sentUpd);

    //                update.Set("EmailSent", false);
    //            }
    //        }
    //    }

    //    public void Complete(EntityOperation operation, ref BLContext context, ref BLResponse response)
    //    {
    //        if (operation is EntityUpdate)
    //        {
    //            var update = operation as EntityUpdate;
    //            if (update.IsCreate)
    //            {
    //                var notification = new Notification();
    //                notification.LoadRawData(update.PropertyUpdates.ToDictionary(e => e.Name, e => (object)e.Value));
    //                if (notification.Method == ReplyMethods.ByEmail)
    //                {
    //                    var recipientUpd = update.RelationUpdates.Find(r => r.Role == Roles.Recipient);
    //                    var recipient = _entityService.Read<User>(recipientUpd.Id);
    //                    //TODO: Attachments
    //                    _notificationService.SendEmail(recipient.Email, notification.Subject, notification.Body, null);
    //                }
    //            }
    //        }
    //    }
    //}

}
