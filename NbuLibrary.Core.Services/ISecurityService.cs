using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.Domain;

namespace NbuLibrary.Core.Services
{
    public enum LoginResult
    {
        InvalidCredentials,
        UserInactive,
        Success,
        UserLocked
    }

    public interface ISecurityService
    {
        /// <summary>
        /// Returns the currently logged user.
        /// </summary>
        User CurrentUser
        {
            get;
        }

        string SystemUserEmail { get; }

        /// <summary>
        /// Starts a new security context, i.e. all further operations will be executed on behalf the system user.
        /// </summary>
        /// <returns>The security context. Use with caution and surrounded by a using(){} block!</returns>
        IDisposable BeginSystemContext();

        /// <summary>
        /// Attempts login with the specified credentials. 
        /// </summary>
        /// <param name="username">The email address of the user.</param>
        /// <param name="password">The password (not the password cache) of the user.</param>
        /// <returns>True, if the login succeeds. False - otherwise.</returns>
        LoginResult Login(string username, string password, bool persistent);

        /// <summary>
        /// Updates the email addess of the currently logged in user
        /// </summary>
        /// <param name="email">The new email address of the user</param>
        void UpdateCurrentUserEmail(string email);

        void Logout();

        /// <summary>
        /// Checks if the group has a specific permition (e.g. ReadWrite on Module with Id 3).
        /// </summary>
        /// <param name="group">Group for which we are checking.</param>
        /// <param name="perm">The permission we are checking.</param>
        /// <returns></returns>
        bool HasModulePermission(User user, int moduleId, string permission);
        bool HasModulePermission(User user, int moduleId);

        void Grant(UserGroup group, int moduleId, string perm);
        void Revoke(UserGroup group, int moduleId, string perm);
        void RevokeAll(UserGroup group, int moduleId, string perm);

        void Grant(UserGroup group, EntityPermission perm);
        void Revoke(UserGroup group, EntityPermission perm);

        IEnumerable<ModulePermission> GetAvailableModulePermissions(UserGroup group);

        IEnumerable<EntityPermission> GetEntityPermissions(UserGroup group);
        IEnumerable<ModulePermission> GetAvailableEntityPermissions(UserGroup group);
    }
}
