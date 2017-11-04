using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using NbuLibrary.Core.DataModel;

namespace NbuLibrary.Core.AccountModule
{
    public class NomenclatureController : ApiController
    {
        private IEntityOperationService _entityService;
        private ISecurityService _securityService;
        private IDomainModelService _domainService;

        public NomenclatureController(IDomainModelService domainService, IEntityOperationService entityService, ISecurityService securityService)
        {
            _domainService = domainService;
            _entityService = entityService;
            _securityService = securityService;
        }

        [HttpGet]
        public IEnumerable<string> GetNomenclatures()
        {

            if (!_securityService.HasModulePermission(_securityService.CurrentUser, NomenclatureModule.Id, Permissions.Manage))
                return null;

            return _domainService.Domain.Entities.Where(e => e.IsNomenclature).Select(e => e.Name);
        }
    }
}
