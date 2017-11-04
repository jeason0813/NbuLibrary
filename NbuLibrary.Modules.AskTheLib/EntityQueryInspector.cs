using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Modules.AskTheLib
{
    public class EntityQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;

        public EntityQueryInspector(ISecurityService securityService)
        {
            _securityService = securityService;

        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (query.IsForEntity("Arguments") && _securityService.HasModulePermission(_securityService.CurrentUser, AskTheLibModule.Id, Permissions.Use))
                return InspectionResult.Allow;
            else if (query.IsForEntity(Inquery.EntityType))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, AskTheLibModule.Id, Permissions.Use))
                {
                    if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                        return InspectionResult.Allow;
                    else if (_securityService.CurrentUser.UserType == UserTypes.Customer)
                    {
                        var relTo = query.GetRelatedQuery(User.ENTITY, RelationConsts.Customer);
                        if (relTo != null && relTo.GetSingleId().HasValue && relTo.GetSingleId().Value == _securityService.CurrentUser.Id)
                            return InspectionResult.Allow;
                        else if (!query.HasInclude(User.ENTITY, RelationConsts.Customer))
                            query.Include(User.ENTITY, RelationConsts.Customer);

                    }
                }
            }
            else if (query.IsForEntity(User.ENTITY))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, AskTheLibModule.Id, Permissions.Use)
                    && _securityService.CurrentUser.UserType == UserTypes.Librarian)
                {
                    return InspectionResult.Allow;
                }
            }
            else if (query.IsForEntity(Notification.ENTITY)
                && _securityService.CurrentUser.UserType == UserTypes.Librarian
                && _securityService.HasModulePermission(_securityService.CurrentUser, AskTheLibModule.Id, Permissions.Use)
                && query.GetRelatedQuery(Inquery.EntityType, RelationConsts.Inquery) != null)
            {
                return InspectionResult.Allow;
            }
            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities)
        {
            if (query.IsForEntity(Inquery.EntityType)
                && _securityService.HasModulePermission(_securityService.CurrentUser, AskTheLibModule.Id, Permissions.Use))
            {
                if (_securityService.CurrentUser.UserType == UserTypes.Librarian)
                    return InspectionResult.Allow;
                else if (_securityService.CurrentUser.UserType == UserTypes.Customer)
                {
                    bool isMe = false;
                    foreach (var e in entities)
                    {
                        isMe = false;
                        var rel = e.GetSingleRelation(User.ENTITY, RelationConsts.Customer);
                        if (rel != null && rel.Entity.Id == _securityService.CurrentUser.Id)
                            isMe = true;
                        if (!isMe)
                            break;
                    }

                    if (isMe)
                        return InspectionResult.Allow;
                }
            }

            return InspectionResult.None;
        }
    }
}
