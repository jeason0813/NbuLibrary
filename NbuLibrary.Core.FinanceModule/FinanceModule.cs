using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.Services;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.FinanceModule
{
    public class Permissions
    {
        public const string Approve = "Approve";
    }

    public class FinanceNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<FinanceModule>();
            Bind<IEntityOperationInspector>().To<FinanceOperationInspector>();
            Bind<IEntityQueryInspector>().To<FinanceQueryInspector>();
            Bind<IEntityOperationLogic>().To<FinanceOperationLogic>();
        }
    }


    public class FinanceModule : IModule
    {
        public const int Id = 5;

        int IModule.Id
        {
            get { return Id; }
        }

        public decimal Version
        {
            get { return 0.8m; }
        }

        public string Name
        {
            get { return "Finance"; }
        }

        public IUIProvider UIProvider
        {
            get { return new FinanceUIProvider(); }
        }

        public ModuleRequirements Requirements
        {
            get
            {
                var user = new EntityModel(User.ENTITY);
                var mbPayment = new ModelBuilder(Payment.ENTITY);
                mbPayment.AddDecimal("Amount");
                mbPayment.AddEnum<PaymentStatus>("Status", PaymentStatus.Pending);
                mbPayment.AddEnum<PaymentMethod>("Method");

                mbPayment.Rules.AddRequired("Amount");
                mbPayment.Rules.AddRequired("Status");
                mbPayment.Rules.AddRequired("Method");

                mbPayment.AddRelationTo(user, RelationType.ManyToOne, Payment.ROLE_CUSTOMER);

                var dm = new DomainModel();
                dm.Entities.Add(mbPayment.EntityModel);
                dm.Entities.Add(user);

                return new ModuleRequirements()
                {
                    RequredModules = new[] { 1 },
                    Domain = dm,
                    Permissions = new string[] { Permissions.Approve },
                    UIDefinitions = new UIDefinition[] {
                        new GridDefinition(){ Entity = Payment.ENTITY, Name = "Finance_Customer_WaitingPayments", Label = "Waiting payments (Customer)"},
                        new GridDefinition(){ Entity = Payment.ENTITY, Name = "Finance_Librarian_WaitingPayments", Label = "Waiting payments (Librarian)"},
                        new FormDefinition(){ Entity= Payment.ENTITY, Name = "Finance_Librarian_PaymentForm", Label = "Payment form (Librarian)"},
                        new ViewDefinition(){ Entity= Payment.ENTITY, Name = "Finance_Librarian_PaymentDetails", Label = "Payment details (Librarian)"},
                        new ViewDefinition(){ Entity= Payment.ENTITY, Name = "Finance_Customer_PaymentDetails", Label = "Payment details (Customer)"}
                    }
                };
            }
        }

        public void Initialize()
        {
        }
    }

    public class FinanceUIProvider : IUIProvider
    {
        public string GetClientTemplates(Domain.UserTypes type)
        {
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (System.IO.Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Core.FinanceModule.{0}", resourceFileName)))
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
                new ClientScript(){ Name = "finance", Content = GetContent("Scripts.finance.js") },
                new ClientScript(){ Name = "vm.payments", Content = GetContent("Scripts.vm.payments.js") }
            };
        }
    }

    public class FinanceQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        public FinanceQueryInspector(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (query.IsForEntity(Payment.ENTITY))
            {
                var cust = query.GetRelatedQuery(User.ENTITY, Payment.ROLE_CUSTOMER);
                if (cust != null && cust.GetSingleId().HasValue && cust.GetSingleId().Value == _securityService.CurrentUser.Id)
                    return InspectionResult.Allow;
                else if (!query.HasInclude(User.ENTITY, Payment.ROLE_CUSTOMER))
                    query.Include(User.ENTITY, Payment.ROLE_CUSTOMER);
            }

            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities)
        {
            if (query.IsForEntity(Payment.ENTITY))
            {
                bool mine = true;
                foreach (var e in entities)
                {
                    var rel = e.GetSingleRelation(User.ENTITY, Payment.ROLE_CUSTOMER);
                    if (rel == null || rel.Entity.Id != _securityService.CurrentUser.Id)
                    {
                        mine = false;
                        break;
                    }
                }
                if (mine)
                    return InspectionResult.Allow;
            }

            return InspectionResult.None;
        }
    }


    public class FinanceOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        public FinanceOperationInspector(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public InspectionResult Inspect(Services.tmp.EntityOperation operation)
        {
            if (operation.IsEntity(Payment.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate() && !update.ContainsProperty("Status"))
                    return InspectionResult.Allow; //TODO-Finance: Everyone is allowed to create payments
                else if (_securityService.HasModulePermission(_securityService.CurrentUser, FinanceModule.Id, Permissions.Approve))
                    return InspectionResult.Allow;

            }
            return InspectionResult.None;
        }
    }

    public class FinanceOperationLogic : IEntityOperationLogic
    {
        private IDomainModelService _domainService;
        private IEntityRepository _repository;
        public FinanceOperationLogic(IDomainModelService domainService, IEntityRepository repository)
        {
            _domainService = domainService;
            _repository = repository;
        }

        public void Before(Services.tmp.EntityOperation operation, EntityOperationContext context)
        {
            if (operation.IsEntity(Payment.ENTITY) && operation is EntityUpdate)
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate())
                {
                    List<int> usersToAttach = new List<int>();
                    foreach (var attach in update.RelationUpdates.Where(ru => ru.Operation == Services.tmp.RelationOperation.Attach))
                    {
                        var em = _domainService.Domain.Entities[attach.Entity];
                        var customerRel = em.Relations[em.Name, User.ENTITY, Payment.ROLE_CUSTOMER];
                        if (customerRel != null && customerRel.TypeFor(User.ENTITY) == RelationType.OneToMany)
                        {
                            var q = new EntityQuery2(User.ENTITY);
                            q.WhereRelated(new RelationQuery(em.Name, customerRel.Role, attach.Id.Value));
                            var cust = _repository.Read(q);
                            if (cust != null)
                                usersToAttach.Add(cust.Id);
                        }
                    }

                    foreach (var id in usersToAttach)
                    {
                        update.Attach(User.ENTITY, Payment.ROLE_CUSTOMER, id);
                    }
                }
            }
        }

        public void After(Services.tmp.EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
        }
    }


}
