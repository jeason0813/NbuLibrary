using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using Ninject.Modules;
using NbuLibrary.Core.Domain;
using System.Security.Cryptography;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.DataModel;

namespace NbuLibrary.Modules.AskTheLib
{
    public class Permissions
    {
        public const string Use = "Use";
    }

    public class AskTheLibNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<AskTheLibModule>();
            Bind<IEntityOperationInspector>().To<EntityOperationInspector>();
            Bind<IEntityQueryInspector>().To<EntityQueryInspector>();
            Bind<IEntityOperationLogic>().To<AskTheLibOperationLogic>();
        }
    }


    public class AskTheLibModule : IModule
    {
        private IUIProvider _uiProvider;

        public const int Id = 101;

        public AskTheLibModule()
        {
        }

        int IModule.Id
        {
            get { return Id; }
        }

        public decimal Version
        {
            get { return 2.2m; }
        }

        public string Name
        {
            get { return "Ask the Librarian"; }
        }

        public IUIProvider UIProvider
        {
            get
            {
                if (_uiProvider == null)
                    _uiProvider = new AskTheLibUIProvider();

                return _uiProvider;
            }
        }

        public void Initialize()
        {

        }


        public ModuleRequirements Requirements
        {
            get
            {
                var dm = new DomainModel();
                ModelBuilder mbInquery = new ModelBuilder("Inquery");
                mbInquery.AddString("Question", 2000);
                mbInquery.AddDateTime("ReplyBefore");
                mbInquery.AddEnum<ReplyMethods>("ReplyMethod");
                mbInquery.AddEnum<QueryStatus>("Status", QueryStatus.New);
                mbInquery.AddUri("Number", "AL");

                mbInquery.Rules.AddRequired("Question");
                mbInquery.Rules.AddRequired("ReplyBefore");
                mbInquery.Rules.AddFutureDate("ReplyBefore", TimeSpan.FromDays(1.0));
                mbInquery.Rules.AddRequired("ReplyMethod");
                mbInquery.Rules.AddRequired("Status");


                var arguments = new NomenclatureModel("Arguments");
                var user = new EntityModel("User");

                var notification = new EntityModel(Notification.ENTITY);
                mbInquery.AddRelationTo(arguments, RelationType.ManyToOne, RelationConsts.Argument);
                mbInquery.AddRelationTo(user, RelationType.ManyToOne, RelationConsts.Customer);
                mbInquery.AddRelationTo(user, RelationType.ManyToOne, RelationConsts.ProcessedBy);
                mbInquery.AddRelationTo(notification, RelationType.OneToMany, RelationConsts.Inquery);

                dm.Entities.Add(mbInquery.EntityModel);
                dm.Entities.Add(arguments);
                dm.Entities.Add(user);

                return new ModuleRequirements()
                {
                    RequredModules = new[] { 0, 1, 2, 3 },
                    Domain = dm,
                    Permissions = new string[] { Permissions.Use },
                    UIDefinitions = new UIDefinition[]{
                        new GridDefinition(){ Name = "AskTheLib_Customer_InqueryGrid", Entity="Inquery", Label = "Inqueries' Grid", Fields = new List<ViewField>(){

                        }},

                        new ViewDefinition(){ Name = "AskTheLib_Customer_InqueryDetails", Entity="Inquery", Label = "Inqueries' Details View", Fields = new List<ViewField>(){

                        }},
                        new FormDefinition(){ Name = "AskTheLib_Customer_InqueryForm", Entity="Inquery", Label = "Inqueries' Form", Fields = new List<EditField>(){

                        }},

                        new GridDefinition(){ Name = "AskTheLib_Librarian_InqueryGrid", Entity="Inquery", Label = "Inqueries' Grid", Fields = new List<ViewField>(){

                        }},
                        new FormDefinition(){ Name = "AskTheLib_Librarian_InqueryForm", Entity="Inquery", Label = "Inqueries' Form", Fields = new List<EditField>(){

                        }},

                        new FormDefinition(){ Name = "AskTheLib_Librarian_InqueryProcess", Entity="Inquery", Label = "Inqueries' Process Form", Fields = new List<EditField>(){

                        }},

                        new ViewDefinition(){ Name = "AskTheLib_Librarian_InqueryDetails", Entity="Inquery", Label = "Inqueries' Details View", Fields = new List<ViewField>(){

                        }}
                    },

                };
            }
        }
    }

    public class AskTheLibUIProvider : IUIProvider
    {
        public string GetClientTemplates(UserTypes type)
        {
            //if (type == UserTypes.Admin)
            //    return GetContent("Templates.AdminUI.html");
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Modules.AskTheLib.{0}", resourceFileName)))
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
            var scripts = new List<ClientScript>();
            if (type == UserTypes.Librarian || type == UserTypes.Customer)
            {
                scripts.Add(new ClientScript()
                {
                    Name = "askthelib",
                    Content = GetContent("Scripts.askthelib.js")
                });
                scripts.Add(new ClientScript()
                {
                    Name = "vm.inqueries",
                    Content = GetContent("Scripts.vm.inqueries.js")
                });
                scripts.Add(new ClientScript()
                {
                    Name = "vm.inquery",
                    Content = GetContent("Scripts.vm.inquery.js")
                });
            };
            return scripts;
        }
    }

    public class EntityOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        private IEntityRepository _repository;

        public EntityOperationInspector(ISecurityService securityService, IEntityRepository repository)
        {
            _securityService = securityService;
            _repository = repository;
        }

        public InspectionResult Inspect(EntityOperation operation)
        {
            if (operation.IsEntity("Inquery"))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, AskTheLibModule.Id, Permissions.Use))
                {
                    if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                        return InspectionResult.Allow;
                    else if (_securityService.CurrentUser.UserType == UserTypes.Customer)
                    {
                        if (operation is EntityUpdate && (operation as EntityUpdate).IsCreate())
                            return InspectionResult.Allow;
                        else if (operation is EntityUpdate)
                        {
                            var update = operation as EntityUpdate;
                            if (update.ContainsRelation(User.ENTITY, RelationConsts.Customer))
                                return InspectionResult.Deny;

                            var q = new EntityQuery2(Inquery.EntityType, update.Id.Value);
                            q.AddProperties("Status");
                            q.Include(User.ENTITY, RelationConsts.Customer);
                            var inquery = _repository.Read(q);

                            if (inquery.GetData<QueryStatus>("Status") != QueryStatus.New)
                                return InspectionResult.Deny;

                            if (update.ContainsProperty("Status")
                                && update.Get<QueryStatus>("Status") != QueryStatus.Canceled)
                                return InspectionResult.Deny;

                            var customer = inquery.GetSingleRelation(User.ENTITY, RelationConsts.Customer);
                            if (customer != null && customer.Entity.Id == _securityService.CurrentUser.Id)
                                return InspectionResult.Allow;
                        }
                    }
                }
            }
            else if (operation.IsEntity(Notification.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate()
                    && update.ContainsRelation(Inquery.EntityType, RelationConsts.Inquery)
                    && _securityService.HasModulePermission(_securityService.CurrentUser, AskTheLibModule.Id, Permissions.Use)
                    && _securityService.CurrentUser.UserType == UserTypes.Librarian)
                {
                    return InspectionResult.Allow;
                }
            }
            return InspectionResult.None;
        }
    }

    public class AskTheLibOperationLogic : IEntityOperationLogic
    {

        private ISecurityService _securityService;
        private IEntityRepository _repository;

        public AskTheLibOperationLogic(ISecurityService securityService, IEntityRepository repository)
        {
            _securityService = securityService;
            _repository = repository;

        }
        public void Before(EntityOperation operation, EntityOperationContext context)
        {
            if (!operation.IsEntity(Inquery.EntityType))
                return;

            if (operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (_securityService.CurrentUser.UserType == UserTypes.Customer && update.IsCreate())
                {
                    update.Attach(User.ENTITY, RelationConsts.Customer, _securityService.CurrentUser.Id);
                }
                else if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                {
                    bool attach = false;
                    if (update.IsCreate())
                        attach = true;
                    else
                    {
                        var q = new EntityQuery2(User.ENTITY);
                        q.WhereRelated(new RelationQuery(Inquery.EntityType, RelationConsts.ProcessedBy, update.Id.Value));
                        var user = _repository.Read(q);
                        if (user == null)
                            attach = true;
                        else if (user.Id != _securityService.CurrentUser.Id)
                        {
                            update.Detach(User.ENTITY, RelationConsts.ProcessedBy, user.Id);
                            attach = true;
                        }
                    }

                    if (attach)
                        update.Attach(User.ENTITY, RelationConsts.ProcessedBy, _securityService.CurrentUser.Id);
                }
            }

        }

        public void After(EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (!result.Success)
                return;


        }
    }



    //public class BusinessLogic : IBusinessLogic
    //{
    //    private ISecurityService _securityService;
    //    private IEntityService _entityService;

    //    public BusinessLogic(ISecurityService securityService, IEntityService entityService)
    //    {

    //        _securityService = securityService;
    //        _entityService = entityService;
    //    }

    //    public OperationResolution Resolve(EntityOperation operation)
    //    {
    //        if (operation.EntityName != "Inquery") return OperationResolution.None;
    //        else return OperationResolution.Regonized;

    //        //if (operation is EntityUpdate)
    //        //{
    //        //    var update = operation as EntityUpdate;
    //        //    if (!update.IsCreate && _securityService.CurrentUser.UserType == UserTypes.Librarian)
    //        //    {
    //        //        Inquery inquery = new Inquery();
    //        //        //inquery.LoadRawData(update.PropertyUpdates.ToDictionary(p => p.Name, p => (object)p.Value));

    //        //        EntityQuery query = new EntityQuery();
    //        //        query.EntityName = "User";
    //        //        var relCond = new RelationCondition();
    //        //        relCond.Role = RelationConsts.ProcessedBy;
    //        //        relCond.EntityName = inquery.EntityName;
    //        //        var id = Convert.ToInt32(update.PropertyUpdates.Find(p => p.Name == "Id").Value);
    //        //        relCond.PropertyRules.Add(new Condition("Id", Condition.Is, id));
    //        //        query.RelationRules.Add(relCond);

    //        //        var processedBy = _entityService.Search(query).SingleOrDefault();
    //        //        if (processedBy != null && processedBy.Id != _securityService.CurrentUser.Id)
    //        //            return OperationResolution.Forbidden;
    //        //    }
    //        //}
    //    }


    //    public void Prepare(EntityOperation operation, ref BLContext context)
    //    {
    //        if (operation is EntityUpdate)
    //        {
    //            var update = operation as EntityUpdate;

    //            if (update.IsCreate)
    //            {
    //                if (!update.PropertyUpdates.Contains("Status"))
    //                {
    //                    update.PropertyUpdates.Add(new PropertyUpdate("Status", QueryStatus.New));
    //                }

    //                var customerUpdate = update.RelationUpdates.Find(p => p.EntityName == "User" && p.Role == RelationConsts.Customer && p.Operation == RelationOperation.Attach);
    //                if (customerUpdate == null)
    //                {
    //                    update.RelationUpdates.Add(new RelationUpdate()
    //                    {
    //                        Id = _securityService.CurrentUser.Id,
    //                        EntityName = "User",
    //                        Role = RelationConsts.Customer,
    //                        Operation = RelationOperation.Attach
    //                    });
    //                }
    //                var processedByUpdate = update.RelationUpdates.Find(p => p.EntityName == "User" && p.Role == RelationConsts.ProcessedBy && p.Operation == RelationOperation.Attach);
    //                if (_securityService.CurrentUser.UserType == UserTypes.Librarian && processedByUpdate == null)
    //                {
    //                    update.RelationUpdates.Add(new RelationUpdate()
    //                    {
    //                        Id = _securityService.CurrentUser.Id,
    //                        EntityName = "User",
    //                        Role = RelationConsts.ProcessedBy,
    //                        Operation = RelationOperation.Attach
    //                    });
    //                }
    //            }
    //            else
    //            {
    //                if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
    //                {
    //                    if (update.RelationUpdates == null)
    //                        update.RelationUpdates = new List<RelationUpdate>();
    //                    update.RelationUpdates.Add(new RelationUpdate()
    //                    {
    //                        Id = _securityService.CurrentUser.Id,
    //                        EntityName = "User",
    //                        Role = RelationConsts.ProcessedBy,
    //                        Operation = RelationOperation.Attach
    //                    });
    //                }
    //            }
    //        }
    //    }

    //    public void Complete(EntityOperation operation, ref BLContext context, ref BLResponse response)
    //    {

    //    }
    //}
}
