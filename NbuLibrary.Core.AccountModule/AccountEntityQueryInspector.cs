using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.AccountModule
{
    public class AccountEntityQueryInspector : IEntityQueryInspector
    {
        private ISecurityService _securityService;
        private IDomainModelService _domainService;
        public AccountEntityQueryInspector(ISecurityService securityService, IDomainModelService domainService)
        {
            _securityService = securityService;
            _domainService = domainService;
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (query.IsForEntity(User.ENTITY))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserManagement))
                    return InspectionResult.Allow;
                else if (query.GetRuleByProperty("Id") != null && Convert.ToInt32(query.GetRuleByProperty("Id").Values[0]) == _securityService.CurrentUser.Id)
                    return InspectionResult.Allow;

                if (query.HasProperty("RecoveryCode"))
                    return InspectionResult.Deny; //Only users with UserManagement permission can access this property

                bool hasUserActivationPermission = _securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserActivation);
                var isActiveRule = query.Rules.Find(r => r.IsForProperty("IsActive"));
                if (isActiveRule != null
                    && isActiveRule.Values.Count() == 1
                    && Convert.ToBoolean(isActiveRule.Values.Single()) == false
                    && hasUserActivationPermission)
                    return InspectionResult.Allow;

                if (hasUserActivationPermission && !query.HasProperty("IsActive"))
                    query.AddProperty("IsActive");
            }
            else if (query.IsForEntity(UserGroup.ENTITY))
            {
                if (_securityService.CurrentUser.UserType == UserTypes.Librarian
                    || _securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserGroupManagement)
                    || _securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserActivation))
                    return InspectionResult.Allow;
                else if (query.GetRuleByProperty("UserType") != null
                    && (UserTypes)Convert.ToInt32(query.GetRuleByProperty("UserType").Values[0]) == _securityService.CurrentUser.UserType
                    && !query.HasInclude("User", "UserGroup"))
                    return InspectionResult.Allow;
            }

            return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entity)
        {
            if (query.IsForEntity(User.ENTITY))
            {
                foreach (var e in entity)
                {
                    if (e.Data.ContainsKey("Password"))
                    {
                        e.SetData<string>("Password", "******");
                    }
                }

                if (_securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserManagement))
                    return InspectionResult.Allow;
                else if (_securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserActivation))
                {
                    foreach (var e in entity)
                    {
                        if (e.GetData<bool>("IsActive") == true)
                            return InspectionResult.None;
                    }
                    return InspectionResult.Allow; //Only inactive users are returned and the user has permission to see inactive users
                }
            }
            else if (query.IsForEntity(UserGroup.ENTITY))
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, AccountModule.Id, Permissions.UserGroupManagement))
                    return InspectionResult.Allow;
            }

            foreach (var inc in query.Includes)
            {
                if (inc.Entity.Equals(User.ENTITY))
                {
                    var rel = _domainService.Domain.Entities[query.Entity].GetRelation(inc.Entity, inc.Role);
                    if (rel != null && (rel.TypeFor(query.Entity) == RelationType.ManyToMany || rel.TypeFor(query.Entity) == RelationType.OneToMany))
                    {
                        foreach (var e in entity)
                        {
                            var users = e.GetManyRelations(inc.Entity, inc.Role);
                            foreach (var user in users)
                            {
                                if (user.Entity.Data.ContainsKey("Password"))
                                {
                                    user.Entity.SetData<string>("Password", "******");
                                }
                            }
                        }
                    }
                    else if (rel != null)
                    {
                        foreach (var e in entity)
                        {
                            var user = e.GetSingleRelation(inc.Entity, inc.Role);
                            if (user != null && user.Data.ContainsKey("Password"))
                            {
                                user.SetData<string>("Password", "******");
                            }
                        }
                    }
                }
            }

            return InspectionResult.None;
        }
    }
}
