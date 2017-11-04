using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ninject;
using NbuLibrary.Core.Kernel;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Domain;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace NbuLibrary.Web.Controllers
{
    public class HomeController : Controller
    {
        private IModule[] _modules;
        private ISecurityService _securityService;

        public HomeController(ISecurityService securityService, IModule[] modules)
        {
            _modules = modules;
            _securityService = securityService;
        }

        //
        // GET: /Home/

        public ActionResult Index()
        {
            var model = new InitialLoadModel();
            model.UITexts = getTexts();
            model.User = _securityService.CurrentUser;

            foreach (var module in _modules)
            {

                foreach (var script in module.UIProvider.GetClientScripts(_securityService.CurrentUser.UserType))
                {
                    model.Mods.Add(string.Format("mods/{0}", script.Name));
                }
                //model.Scripts.Add(module.UIProvider.GetClientScripts(_securityService.CurrentUser.UserType));
                //model.Templates.Add(module.UIProvider.GetClientTemplates(_securityService.CurrentUser.UserType));
            }

            foreach (var file in Directory.GetFiles(Server.MapPath("~/Templates")))
            {
                model.Templates.Add(new InitialLoadModel.Template()
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Content = System.IO.File.ReadAllText(file)
                });
            }

            return View(model);
        }

        public ActionResult Test()
        {
            return View();
        }

        public JsonResult LoadPermissions()
        {
            throw new NotImplementedException();
            //if (_securityService.CurrentUser.UserType == UserTypes.Admin)
            //{
            //    List<ModulePermission> availPerms = new List<ModulePermission>();
            //    foreach (var module in _modules)
            //    {
            //        availPerms.Add(new ModulePermission() { ModuleID = module.Id, ModuleName = module.Name, Permission = Permissions.RW });
            //    }

            //    return Json(availPerms, JsonRequestBehavior.AllowGet);
            //}
            //var currentGroup = _securityService.CurrentUser.UserGroup_UserGroup;
            //if (currentGroup != null)
            //    return Json(_securityService.GetModulePermissions(currentGroup), JsonRequestBehavior.AllowGet);
            //else
            //    return Json(new string[] { }, JsonRequestBehavior.AllowGet);

        }

        public JsonResult GetCurrentUser()
        {
            return Json(_securityService.CurrentUser, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult Install()
        {
            if (_securityService.CurrentUser.UserType != UserTypes.Admin)
                throw new UnauthorizedAccessException();

            return View();
        }

        public ActionResult Scripts()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var module in _modules)
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, module.Id))
                {
                    //sb.AppendLine(module.UIProvider.GetClientScripts(_securityService.CurrentUser.UserType));
                    //model.Templates.Add(module.UIProvider.GetClientTemplates(_securityService.CurrentUser.UserType));
                }
            }

            return Content(sb.ToString(), "text/javascript");
        }

        public string Templates()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var module in _modules)
            {
                if (_securityService.HasModulePermission(_securityService.CurrentUser, module.Id))
                {
                    sb.AppendLine(module.UIProvider.GetClientTemplates(_securityService.CurrentUser.UserType));
                    //model.Templates.Add(module.UIProvider.GetClientTemplates(_securityService.CurrentUser.UserType));
                }
            }

            return sb.ToString();
        }

        private IEnumerable<UIText> getTexts()
        {
            var file = System.Web.HttpContext.Current.Server.MapPath("~/textresources.xml");

            if (System.IO.File.Exists(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<UIText>));
                using (var fs = new FileStream(file, FileMode.Open))
                {
                    return ser.Deserialize(fs) as List<UIText>;
                }
            }
            else
                return new List<UIText>();
        }
    }


    /// <summary>
    /// Must include: the modules to load, the resources
    /// </summary>
    public class InitialLoadModel
    {
        public class Template
        {
            public string Name { get; set; }
            public string Content { get; set; }
        }

        //public List<string> Scripts { get; set; }
        public List<Template> Templates { get; set; }
        public IEnumerable<UIText> UITexts { get; set; }
        public List<string> Mods { get; set; }
        public User User { get; set; }


        public InitialLoadModel()
        {
            //Scripts = new List<string>();
            Templates = new List<Template>();
            Mods = new List<string>();
        }
    }
}
