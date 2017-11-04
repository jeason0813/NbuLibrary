using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.AccountModule
{
    public class AccountEntityOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        private IEntityRepository _repository;
        public AccountEntityOperationInspector(ISecurityService securityService, IEntityRepository repository)
        {
            _securityService = securityService;
            _repository = repository;
        }

        public InspectionResult Inspect(EntityOperation operation)
        {
            if (operation.IsEntity(User.ENTITY))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserManagement))
                    return InspectionResult.Allow;

                if(operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    if (update.ContainsProperty("RecoveryCode"))
                        return InspectionResult.Deny; //Only users with UserManagement permission can access this property
                }


                if (operation.Id.HasValue && _securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserActivation))
                {
                    EntityQuery2 query = new EntityQuery2("User", operation.Id.Value);
                    query.AddProperty("isActive");
                    var e = _repository.Read(query);
                    if (e.GetData<bool>("IsActive") == false)
                        return InspectionResult.Allow;
                }
                else if (operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    if (update.Id.HasValue
                        && update.Id.Value == _securityService.CurrentUser.Id
                        && !update.ContainsProperty("IsActive"))//TODO: allowing the user to edit his data and relations
                        return InspectionResult.Allow;
                }
            }

            return InspectionResult.None;
        }
    }
}
