using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using NbuLibrary.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace NbuLibrary.Web.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private ISecurityService _securityService;
        private IEntityOperationService _entityService;

        public LoginController(ISecurityService securityService, IEntityOperationService entityService)
        {
            _securityService = securityService;
            _entityService = entityService;
        }
        //
        // GET: /Login/

        public ActionResult Index(string ReturnUrl)
        {
            return View(new LoginViewModel() { ReturnUrl = ReturnUrl });
        }

        [HttpPost]
        public ActionResult Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = _securityService.Login(model.Email, model.Password, model.RememberMe);
                if (result == LoginResult.Success)
                {
                    if (string.IsNullOrEmpty(model.ReturnUrl))
                        return Redirect("/");
                    else
                        return Redirect(model.ReturnUrl);
                }
                else
                {
                    string error = null;
                    if (result == LoginResult.InvalidCredentials)
                        error = "Потребителското име или паролата са невалидни.";
                    else if (result == LoginResult.UserLocked)
                    {
                        error = "Поради големия брой пъти грешно въведена парола, акаунтът Ви беше временно деактивиран. Използвайте \"Забравена парола?\", за да го активирате отново и да възстановите достъпа си до системата. За помощ: тел. 02 8110296.";
                    }
                    else
                        error = "Вашата регистрация още не е обработена. Свържете се с библиотекар, за да проверите нейния статус (тел. 02 8110296).";

                    ModelState.AddModelError(string.Empty, error);
                    return View(model);
                }
            }
            else
                return View(model);
        }

        [HttpGet]
        public ActionResult ForgottenPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgottenPassword(string email)
        {
            using (_securityService.BeginSystemContext())
            {
                var q = new EntityQuery2("User");
                q.WhereIs("Email", email);
                q.WhereIs("IsActive", true);
                Entity user = _entityService.Query(q).SingleOrDefault();
                if (user == null)
                {
                    ModelState.AddModelError("email", string.Format("В системата няма активен потребител с имейл \"{0}\". За помощ: тел. 02 8110296.", email));
                    return View();
                }
                else
                {
                    var recoveryCode = Guid.NewGuid().ToString();

                    var update = new EntityUpdate(user.Name, user.Id);
                    update.Set("RecoveryCode", recoveryCode);
                    var result = _entityService.Update(update);
                    if(result.Success)
                        return View("ForgottenPassword_Success", (object)email);    
                    else
                    {
                        ModelState.AddModelError("email", "Възникна грешка при стартиране на процеса по възстановяване на забравена парола. За помощ: тел. 02 8110296.");
                        return View();
                    }
                }

            }
        }

        [HttpGet]
        public ActionResult RecoverPassword(string email, string rc)
        {
            return View(new PasswordRecoveryModel() { RecoveryCode = rc, Email = email });
        }

        [HttpPost]
        public ActionResult RecoverPassword(PasswordRecoveryModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (_securityService.BeginSystemContext())
            {
                var q = new EntityQuery2("User");
                q.WhereIs("Email", model.Email);
                q.WhereIs("RecoveryCode", model.RecoveryCode);
                q.WhereIs("IsActive", true);
                Entity user = _entityService.Query(q).SingleOrDefault();
                if (user == null)
                {
                    ModelState.AddModelError("", "Грешен имейл или код за възстановяване. Започнете процеса по възстановяване (през забравена парола) отново или позвънете на тел. 02 8110296.");
                    return View(model);
                }
                else
                {
                    var update = new EntityUpdate(user.Name, user.Id);
                    update.Set("Password", model.Password);
                    var result = _entityService.Update(update);
                    if(result.Success)
                    {
                        return View("RecoverPassword_Success");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Възникна грешка при смяна на паролата. Започнете процеса по възстановяване (през забравена парола) отново или позвънете на тел. 02 8110296.");
                        return View(model);
                    }
                }
            }
        }

        [HttpGet]
        public ActionResult SignOut()
        {
            _securityService.Logout();
            return View();
        }

        [HttpGet]
        public ActionResult Register()
        {
            var model = new RegisterGroupModel();
            var query = new EntityQuery2(UserGroup.ENTITY);
            query.AllProperties = true;
            query.WhereIs("UserType", UserTypes.Customer);
            using (_securityService.BeginSystemContext())
            {
                model.AvailableGroups = _entityService.Query(query).Select(e => new UserGroup(e));
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult Register(RegisterGroupModel model)
        {
            using (_securityService.BeginSystemContext())
            {
                var query = new EntityQuery2(UserGroup.ENTITY);
                query.AllProperties = true;
                query.WhereIs("UserType", UserTypes.Customer);
                model.AvailableGroups = _entityService.Query(query).Select(e => new UserGroup(e));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }
            else
            {
                var selectedGroup = model.AvailableGroups.Single(g => g.Id == model.UserGroup);
                if (selectedGroup.Name.Equals("Студент", StringComparison.InvariantCultureIgnoreCase)
                    || selectedGroup.Name.Equals("Преподавател", StringComparison.InvariantCultureIgnoreCase))
                {
                    return View("RegisterStudent", new RegisterStudentViewModel() { UserGroup = model.UserGroup, UserGroupName = selectedGroup.Name });
                }
                else if (selectedGroup.Name.Equals("Външен (с читателска карта)", StringComparison.InvariantCultureIgnoreCase))
                    return View("RegisterExternal", new RegisterExternalViewModel() { UserGroup = model.UserGroup, UserGroupName = selectedGroup.Name });
                else
                    return View("RegisterOther", new RegisterViewModel() { UserGroup = model.UserGroup, UserGroupName = selectedGroup.Name });
            }
        }

        [HttpPost]
        public ActionResult RegisterStudent(RegisterStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            else
                return FinishResigration(new RegisterViewModel(model));
        }

        [HttpPost]
        public ActionResult RegisterExternal(RegisterExternalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            else
                return FinishResigration(new RegisterViewModel(model));
        }

        [HttpPost]
        public ActionResult RegisterOther(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            else
                return FinishResigration(model);
        }

        [HttpPost]
        public ActionResult FinishResigration(RegisterViewModel model)
        {
            User user = new User()
            {
                Email = model.Email,
                Password = model.Password,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                FacultyNumber = model.FacultyNumber,
                CardNumber = model.CardNumber,
                PhoneNumber = model.PhoneNumber,
                UserType = UserTypes.Customer
            };
            var update = new EntityUpdate(user);
            update.Attach(UserGroup.ENTITY, UserGroup.DEFAULT_ROLE, model.UserGroup);
            EntityOperationResult result = null;
            using (_securityService.BeginSystemContext())
            {
                result = _entityService.Update(update);
            }

            if (result.Success)
                return View("RegisterComplete");
            else
            {
                IEnumerable<UserGroup> availableGroups = null;
                using (_securityService.BeginSystemContext())
                {
                    var query = new EntityQuery2(UserGroup.ENTITY);
                    query.AllProperties = true;
                    query.WhereIs("UserType", UserTypes.Customer);
                    availableGroups = _entityService.Query(query).Select(e => new UserGroup(e));
                }

                if (result.Errors == null || result.Errors.Count == 0)
                    ModelState.AddModelError("", "Unexpected error occured. Please, try again. If there is still a problem, contact the administrator.");
                else
                {
                    foreach (var err in result.Errors)
                    {
                        ModelState.AddModelError("", err.Message);
                    }
                }

                var selectedGroup = availableGroups.Single(g => g.Id == model.UserGroup);
                if (selectedGroup.Name.Equals("Студенти", StringComparison.InvariantCultureIgnoreCase)
                    || selectedGroup.Name.Equals("Преподаватели", StringComparison.InvariantCultureIgnoreCase))
                {
                    return View("RegisterStudent", new RegisterStudentViewModel() { UserGroup = model.UserGroup, UserGroupName = selectedGroup.Name });
                }
                else if (selectedGroup.Name.Equals("Външни", StringComparison.InvariantCultureIgnoreCase))
                    return View("RegisterExternal", new RegisterExternalViewModel() { UserGroup = model.UserGroup, UserGroupName = selectedGroup.Name });
                else
                    return View("RegisterOther", new RegisterViewModel() { UserGroup = model.UserGroup, UserGroupName = selectedGroup.Name });
            }
        }
    }
}
