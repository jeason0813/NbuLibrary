using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Infrastructure
{
    internal class SystemSecurityContext : IDisposable
    {
        [ThreadStatic]
        private static User _user;

        public static User SystemUser
        {
            get
            {
                return _user;
            }
        }

        public static bool IsActive
        {
            get
            {
                return _user != null;
            }
        }

        public SystemSecurityContext(User user)
        {
            if (_user == null)
                _user = user;
        }

        public void Dispose()
        {
            _user = null;
        }
    }

    public class SecurityService : ISecurityService, IEntityQueryInspector, IEntityOperationInspector
    {
        private const string KEY_REQUEST_CACHE = "__PER_REQUEST_CACHE_KEY_CURRENT_USER__";

        private User _currentUser;
        private IEntityRepository _repository;
        private string _systemUserEmail;

        public SecurityService(IEntityRepository repository)
        {
            _repository = repository;
            _systemUserEmail = System.Configuration.ConfigurationManager.AppSettings["admin"];
        }

        public Domain.User CurrentUser
        {
            get
            {
                if (SystemSecurityContext.IsActive)
                    return SystemSecurityContext.SystemUser;
                else if (_currentUser != null)
                    return _currentUser;
                else if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Items != null)
                {
                    var cachedUser = System.Web.HttpContext.Current.Items[KEY_REQUEST_CACHE] as User;
                    if (cachedUser != null)
                    {
                        _currentUser = cachedUser;
                        return cachedUser;
                    }
                }

                //TODO: CurrentUser
                var authCookie = System.Web.HttpContext.Current.Request.Cookies["NbuLib"];
                if (authCookie != null)
                {
                    var ticket = System.Web.Security.FormsAuthentication.Decrypt(authCookie.Value);

                    _currentUser = GetCurrentUser(ticket.Name);
                    System.Web.HttpContext.Current.Items[KEY_REQUEST_CACHE] = _currentUser;
                    return _currentUser;
                }
                else
                    return null;


            }
        }

        public string SystemUserEmail
        {
            get
            {
                return _systemUserEmail;
            }
        }

        public void UpdateCurrentUserEmail(string email)
        {
            var authCookie = System.Web.HttpContext.Current.Request.Cookies["NbuLib"];
            if (authCookie != null)
            {
                var ticket = System.Web.Security.FormsAuthentication.Decrypt(authCookie.Value);
                bool isPersistent = ticket.IsPersistent;
                string newEmail = email;
                System.Web.Security.FormsAuthentication.SignOut();
                System.Web.Security.FormsAuthentication.SetAuthCookie(newEmail, isPersistent);
                _currentUser = GetCurrentUser(newEmail);
                System.Web.HttpContext.Current.Items[KEY_REQUEST_CACHE] = _currentUser;
            }
        }

        public LoginResult Login(string username, string password, bool persistent)
        {
            SHA1 sha1 = SHA1.Create();
            var pwdBytes = Encoding.UTF8.GetBytes(password);
            var hash = Convert.ToBase64String(sha1.ComputeHash(pwdBytes));

            EntityQuery2 query = new EntityQuery2(User.ENTITY);
            query.AllProperties = true;
            query.Include(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE);
            query.WhereIs("email", username);
            //query.WhereIs("password", hash);
            var e = _repository.Read(query);
            if (e == null)
                return LoginResult.InvalidCredentials;
            User user = new User(e);

            if (user.FailedLoginsCount.HasValue && user.FailedLoginsCount.Value > 3 && user.LastFailedLogin.HasValue && user.LastFailedLogin.Value.Add(TimeSpan.FromHours(4)) > DateTime.Now)
            {
                return LoginResult.UserLocked;
            }

            if (!user.Password.Equals(hash, StringComparison.InvariantCultureIgnoreCase))
            {
                user.LastFailedLogin = DateTime.Now;
                if (user.FailedLoginsCount.HasValue)
                    user.FailedLoginsCount = user.FailedLoginsCount.Value + 1;
                else
                    user.FailedLoginsCount = 1;

                var upd = new User(user.Id);
                upd.FailedLoginsCount = user.FailedLoginsCount;
                upd.LastFailedLogin = user.LastFailedLogin;
                _repository.Update(upd);
                return LoginResult.InvalidCredentials;
            }

            if (!user.IsActive)
                return LoginResult.UserInactive;


            System.Web.Security.FormsAuthentication.SetAuthCookie(user.Email, persistent);
            return LoginResult.Success;

        }

        public void Logout()
        {
            System.Web.Security.FormsAuthentication.SignOut();
        }

        public bool HasModulePermission(Domain.User user, int moduleId, string permission)
        {
            if (user.UserType == UserTypes.Admin)
                return true;
            else if (user.UserGroup == null)
                return false;
            else
            {
                var activePerms = user.UserGroup.ModulePermissions;
                if (activePerms != null)
                    return activePerms.FirstOrDefault(mp => mp.ModuleID == moduleId && mp.Granted.Contains(permission, StringComparer.InvariantCultureIgnoreCase)) != null;
                else
                    return false;

            }
        }

        public bool HasModulePermission(Domain.User user, int moduleId)
        {
            if (user.UserType == UserTypes.Admin)
                return true;
            else if (user.UserGroup == null)
                return false;
            else
            {
                var activePerms = user.UserGroup.ModulePermissions;
                if (activePerms != null)
                    return activePerms.FirstOrDefault(mp => mp.ModuleID == moduleId) != null;
                else
                    return false;

            }
        }

        public void Grant(Domain.UserGroup group, int moduleId, string perm)
        {
            throw new NotImplementedException();
        }

        public void Revoke(Domain.UserGroup group, int moduleId, string perm)
        {
            throw new NotImplementedException();
        }

        public void RevokeAll(Domain.UserGroup group, int moduleId, string perm)
        {
            throw new NotImplementedException();
        }

        public void Grant(Domain.UserGroup group, Domain.EntityPermission perm)
        {
            throw new NotImplementedException();
        }

        public void Revoke(Domain.UserGroup group, Domain.EntityPermission perm)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Domain.ModulePermission> GetAvailableModulePermissions(Domain.UserGroup group)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Domain.EntityPermission> GetEntityPermissions(Domain.UserGroup group)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Domain.ModulePermission> GetAvailableEntityPermissions(Domain.UserGroup group)
        {
            throw new NotImplementedException();
        }

        InspectionResult IEntityOperationInspector.Inspect(Services.tmp.EntityOperation operation)
        {
            if (CurrentUser == null)
                return InspectionResult.Deny;
            else if (CurrentUser.UserType == UserTypes.Admin)
                return InspectionResult.Allow;
            else
                return InspectionResult.None;
        }


        public IDisposable BeginSystemContext()
        {
            EntityQuery2 query = new EntityQuery2(User.ENTITY);
            query.AllProperties = true;
            query.Include(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE);
            query.WhereIs("email", _systemUserEmail);
            var e = _repository.Read(query);
            return new SystemSecurityContext(new User(e));
        }

        public InspectionResult InspectQuery(EntityQuery2 query)
        {
            if (CurrentUser == null)
                return InspectionResult.Deny;
            else if (CurrentUser.UserType == UserTypes.Admin)
                return InspectionResult.Allow;
            else
                return InspectionResult.None;
        }

        public InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entity)
        {
            if (CurrentUser == null)
                return InspectionResult.Deny;
            else if (CurrentUser.UserType == UserTypes.Admin)
                return InspectionResult.Allow;
            else
                return InspectionResult.None;
        }

        private User GetCurrentUser(string email)
        {
            EntityQuery2 query = new EntityQuery2(User.ENTITY);
            query.AllProperties = true;
            query.Include(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE);
            query.WhereIs("email", email);
            var e = _repository.Read(query);
            if (e == null)
                return null;
            var user = new User(e);
            if (user.UserGroup != null)
            {
                var q2 = new EntityQuery2(UserGroup.ENTITY, user.UserGroup.Id);
                q2.AllProperties = true;
                q2.Include(ModulePermission.ENTITY, ModulePermission.DEFAULT_ROLE);
                user.UserGroup = new UserGroup(_repository.Read(q2));
            }

            return user;
        }
    }
}
