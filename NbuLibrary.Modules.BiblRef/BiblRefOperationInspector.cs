using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Modules.BiblRef
{
    public class BiblRefOperationInspector : IEntityOperationInspector
    {
        private ISecurityService _securityService;
        private IEntityRepository _repository;
        public BiblRefOperationInspector(ISecurityService securityService, IEntityRepository repository)
        {
            _securityService = securityService;
            _repository = repository;
        }

        public InspectionResult Inspect(Core.Services.tmp.EntityOperation operation)
        {
            if ((operation.IsEntity(EntityConsts.BibliographicQuery)
                || operation.IsEntity(EntityConsts.Bibliography))
                && _securityService.HasModulePermission(_securityService.CurrentUser, BiblRefModule.Id, Permissions.Use))
            {
                if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                    return InspectionResult.Allow;
                else if (operation is EntityUpdate)
                {
                    var update = operation as EntityUpdate;
                    if (update.IsCreate())
                        return InspectionResult.Allow;
                    else if (update.IsEntity(EntityConsts.BibliographicQuery))
                    {
                        var q = new EntityQuery2(User.ENTITY, _securityService.CurrentUser.Id);
                        q.WhereRelated(new RelationQuery(EntityConsts.BibliographicQuery, Roles.Customer, update.Id.Value));
                        if (_repository.Read(q) != null)
                            return InspectionResult.Allow;
                    }
                    else if(update.IsEntity(EntityConsts.Bibliography))
                    {
                        var q = new EntityQuery2(EntityConsts.BibliographicQuery);
                        q.WhereIs("ForNew", true);
                        q.WhereRelated(new RelationQuery(EntityConsts.Bibliography, Roles.Query, update.Id.Value));
                        q.WhereRelated(new RelationQuery(User.ENTITY, Roles.Customer, _securityService.CurrentUser.Id));
                        q.Include(EntityConsts.Bibliography, Roles.Query);
                        
                        if (_repository.Read(q) != null)
                            return InspectionResult.Allow;
                        
                    }
                }
            }

            return InspectionResult.None;
        }
    }
}
