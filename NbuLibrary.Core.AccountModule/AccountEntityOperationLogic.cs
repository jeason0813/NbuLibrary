using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.AccountModule
{
    public class AccountEntityOperationLogic : IEntityOperationLogic
    {
        private const string CTXKEY_PASSWORD_UPDATE = "AccountLogic:PasswordUpdate";
        private const string CTXKEY_USER_CREATION = "AccountLogic:UserCreation";
        private const string CTXKEY_UPDATE_PROFILE = "AccountLogic:UserUpdateProfile";
        private const string CTXKEY_USER_PASSWORDRECOVERY = "AccountLogic:UserPasswordRecovery";
        private INotificationService _notificationService;

        public AccountEntityOperationLogic(INotificationService notificationService, ITemplateService templateService, IEntityRepository repository, ISecurityService securityService)
        {
            _notificationService = notificationService;
            _templateService = templateService;
            _repository = repository;
            _securityService = securityService;
        }

        public void Before(EntityOperation operation, EntityOperationContext context)
        {
            if (operation is EntityUpdate && operation.IsEntity(User.ENTITY))
            {
                var update = operation as EntityUpdate;
                if (update.IsCreate() && !update.ContainsProperty("Password")) //generate random password
                {
                    update.Set("Password", GenerateRandomPassword());
                }

                if (update.ContainsProperty("password"))
                {
                    var newPassword = update.PropertyUpdates["password"] as string;
                    context.Set<string>(CTXKEY_PASSWORD_UPDATE, newPassword);

                    string hash = null;
                    using (SHA1 sha1 = SHA1.Create())
                    {
                        hash = Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(newPassword)));
                    }

                    update.Set("password", hash);
                    update.Set("FailedLoginsCount", 0);
                }

                if (update.ContainsProperty("RecoveryCode"))
                {
                    context.Set<bool>(CTXKEY_USER_PASSWORDRECOVERY, true);
                }

                if (update.IsCreate())
                    context.Set<bool>(CTXKEY_USER_CREATION, true);
                else if (update.Id.Value == _securityService.CurrentUser.Id)
                    context.Set<int>(CTXKEY_UPDATE_PROFILE, _securityService.CurrentUser.Id);
            }
        }

        public void After(EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            if (!result.Success)
                return;

            if (!operation.IsEntity(User.ENTITY))
                return;

            var update = operation as EntityUpdate;
            if (update == null)
                return;

            if (context.Get<bool>(CTXKEY_USER_PASSWORDRECOVERY))
            {
                var user = new User(_repository.Read(new EntityQuery2(update.Entity, update.Id.Value) { AllProperties = true }));
                var template = _templateService.Get(new Guid(NotificationTemplates.USER_PASSWORDRECOVERY));

                string subject = null, body = null;
                Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
                templateContext.Add("User", user);

                _templateService.Render(template, templateContext, out subject, out body);

                //TODO: async execution
                _notificationService.SendEmail(user.Email, subject, body, null);

            }

            if (context.Get<bool>(CTXKEY_USER_CREATION))
            {
                var user = new User(_repository.Read(new EntityQuery2(update.Entity, update.Id.Value) { AllProperties = true }));

                var pwd = context.Get<string>(CTXKEY_PASSWORD_UPDATE);
                user.SetData<String>("Password", pwd);

                var template = _templateService.Get(new Guid(NotificationTemplates.USER_CREATED));

                string subject = null, body = null;
                Dictionary<string, Entity> templateContext = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
                templateContext.Add("User", user);

                _templateService.Render(template, templateContext, out subject, out body);

                //TODO: async execution
                _notificationService.SendEmail(user.Email, subject, body, null);
                if (update.ContainsProperty("IsActive") && update.Get<bool>("IsActive"))
                    SendUserActivationEmail(user);
            }
            else if (update.ContainsProperty("IsActive") && update.Get<bool>("IsActive"))
            {
                var user = new User(_repository.Read(new EntityQuery2(update.Entity, update.Id.Value) { AllProperties = true }));
                SendUserActivationEmail(user);
            }
            else if (context.Get<int>(CTXKEY_UPDATE_PROFILE) > 0)
            {
                if (update.ContainsRelation("UserGroup", "UserGroup")
                    || update.ContainsProperty("FacultyNumber")
                    || update.ContainsProperty("CardNumber"))
                {
                    var user = new User(context.Get<int>(CTXKEY_UPDATE_PROFILE));
                    user.IsActive = false;
                    _repository.Update(user);
                    _securityService.Logout();
                    result.Data.Add("account_warning_logged_out", true);
                }
                if (update.ContainsProperty("Email"))
                {
                    _securityService.UpdateCurrentUserEmail(update.Get<string>("Email"));
                    result.Data.Add("account_event_email_changed", update.Get<string>("Email"));
                }
            }

        }

        private static string RandomPasswordSymbols = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        private ITemplateService _templateService;
        private IEntityRepository _repository;
        private ISecurityService _securityService;
        private string GenerateRandomPassword()
        {
            Random r = new Random();
            var symbolsCnt = RandomPasswordSymbols.Length;
            var len = r.Next(6, 8);
            var pwd = new char[len];
            for (int i = 0; i < len; i++)
            {
                pwd[i] = RandomPasswordSymbols[r.Next(0, symbolsCnt)];
            }
            return new string(pwd);
        }

        private void SendUserActivationEmail(User user)
        {
            var templ = _templateService.Get(new Guid(NotificationTemplates.USER_ACTIVATED));
            string subject = null, body = null;
            Dictionary<string, Entity> ctx = new Dictionary<string, Entity>(StringComparer.InvariantCultureIgnoreCase);
            ctx.Add("User", user);
            _templateService.Render(templ, ctx, out subject, out body);
            _notificationService.SendEmail(user.Email, subject, body, null);
        }
    }
}
