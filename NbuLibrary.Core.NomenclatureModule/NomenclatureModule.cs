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

namespace NbuLibrary.Core.AccountModule
{
    public class NomenclatureNinjectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IModule>().To<NomenclatureModule>();
            Bind<IEntityOperationInspector>().To<NomenclaturesEntityOperationInspector>();
            Bind<IEntityQueryInspector>().To<NomenclaturesEntityQueryInspector>();
        }
    }


    public class Permissions
    {
        public const string Manage = "Manage";
    }

    public class NomenclatureModule : IModule
    {
        private IUIProvider _uiProvider;

        public const int Id = 2;

        public NomenclatureModule()
        {
        }

        int IModule.Id
        {
            get { return Id; }
        }

        public decimal Version
        {
            get { return 1.4m; }
        }

        public string Name
        {
            get { return "Nomenclatures"; }
        }

        public IUIProvider UIProvider
        {
            get
            {
                if (_uiProvider == null)
                    _uiProvider = new NomenclatureUIProvider();

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
                return new ModuleRequirements()
                {
                    RequredModules = new[] { 1 },
                    Permissions = new string[] { Permissions.Manage },
                    //UIDefinitions = new List<UIDefinition>() {
                    //    new GridDefinition(){
                    //        Name="Nomenclatures_Admin_Grid", Label="Nomenclatures Grid", Entity = "ArgumentsNom", Fields = new List<ViewField>(){
                    //        new Textfield(){ Label = "Name", Order = 1, Property = "Name" },
                    //        new Numberfield(){Label = "Order", Order = 1, Property = "Order" }
                    //    }
                    //    },
                    //    new FormDefinition(){
                    //        Name = "Nomenclatures_Admin_Form", Label = "Nomenclatures Form", Entity = "ArgumentsNom", Fields = new List<EditField>(){
                    //            new Textbox(){ Property = "Value", Label="Value", Required = true, MaxLength = 256},
                    //            new Numberbox(){ Property = "DisplayOrder", Label="DisplayOrder", Required = true, Integer = true},
                    //        }
                    //    }
                    //}
                };
            }
        }
    }

    public class NomenclatureUIProvider : IUIProvider
    {
        public string GetClientScripts(Domain.UserTypes type)
        {
            return GetContent("Scripts.nomenclatures.js");
        }

        public string GetClientTemplates(Domain.UserTypes type)
        {
            if (type == UserTypes.Admin)
                return GetContent("Templates.AdminUI.html");
            return string.Empty;
        }

        private string GetContent(string resourceFileName)
        {
            string resourceContent = null;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("NbuLibrary.Core.NomenclatureModule.{0}", resourceFileName)))
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
            return new List<ClientScript>() { new ClientScript(){
                Name = "nomenclatures",
                Content = GetContent("Scripts.nomenclatures.js")
            } };
        }
    }

    public class NomenclaturesEntityOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        private IDomainModelService _domainService;
        public NomenclaturesEntityOperationInspector(ISecurityService securityService, IDomainModelService domainService)
        {
            _securityService = securityService;
            _domainService = domainService;
        }

        public InspectionResult Inspect(EntityOperation operation)
        {
            if (_securityService.HasModulePermission(_securityService.CurrentUser, NomenclatureModule.Id, Permissions.Manage))
            {
                var em = _domainService.Domain.Entities[operation.Entity];
                if (em.IsNomenclature)
                    return InspectionResult.Allow;
            }

            return InspectionResult.None;
        }
    }

    public class NomenclaturesEntityQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        private IDomainModelService _domainService;
        public NomenclaturesEntityQueryInspector(ISecurityService securityService, IDomainModelService domainService)
        {
            _securityService = securityService;
            _domainService = domainService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (_securityService.HasModulePermission(_securityService.CurrentUser, NomenclatureModule.Id, Permissions.Manage))
            {
                var em = _domainService.Domain.Entities[query.Entity];
                if (em.IsNomenclature)
                    return InspectionResult.Allow;
            }

            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entity)
        {
            return InspectionResult.None;
        }
    }

}
