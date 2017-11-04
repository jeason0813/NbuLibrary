using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Services;
using NbuLibrary.Web.Filters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace NbuLibrary.Web.Controllers
{

    //TODO: Use Filter!
    //TODO: Move system logic to separate service!
    [AllowAnonymous]
    public class SystemController : Controller
    {
        public const string AUTH_TOKEN = "INSTALLER_IS_AUTHENTICATED";
        private const string DASHBOARD_PENDING = "DASHBOARD_PENDING_MODULES";

        private IEnumerable<IModule> _modules;
        private IDomainModelService _domainService;
        private IEntityRepository _repository;
        private IUIDefinitionService _uiService;
        private IDatabaseService _dbService;
        private ILoggingService _logService;
        private ITemplateService _templateService;

        public SystemController(IDomainModelService domainService, IEntityRepository repository, IDatabaseService dbService, IUIDefinitionService uiService, ILoggingService logService, IEnumerable<IModule> modules, ITemplateService templateService)
        {
            _modules = modules;
            _domainService = domainService;
            _repository = repository;
            _dbService = dbService;
            _uiService = uiService;
            _logService = logService;
            _templateService = templateService;
        }

        public List<DashboardModel.Module> Pending
        {
            get
            {
                List<DashboardModel.Module> pending = Session[DASHBOARD_PENDING] as List<DashboardModel.Module>;
                if (pending == null)
                {
                    pending = new List<DashboardModel.Module>();
                }

                return pending;
            }
            set
            {
                Session[DASHBOARD_PENDING] = value;
            }
        }

        public ActionResult Index()
        {
            if (Session[AUTH_TOKEN] != null)
                return RedirectToAction("Dashboard");
            else
                return View();
        }

        [HttpPost]
        public ActionResult Index(string username, string password)
        {
            string installer = ConfigurationManager.AppSettings["Installer"];
            if (string.IsNullOrEmpty(installer))
            {
                ModelState.AddModelError("", "Installer is disabled on the application.");
                return View();
            }
            else
            {
                string[] parts = installer.Split(new string[] { "::" }, StringSplitOptions.None);
                if (parts[0] != username || parts[1] != password)
                {
                    ModelState.AddModelError("", "Invalid credentials. Access to the system console is denied.");
                    return View();
                }
                else
                {
                    Session[AUTH_TOKEN] = true;
                    return RedirectToAction("Dashboard");
                }
            }
        }

        [InstallerAuthFilter]
        public ActionResult Dashboard()
        {
            DashboardModel model = new DashboardModel()
            {
                Modules = GetDashboardModules()
            };

            return View("Dashboard", model);
        }

        private List<DashboardModel.Module> GetDashboardModules()
        {
            var filepath = HttpContext.Server.MapPath("~/modules.config");
            XmlDocument installed = new XmlDocument();
            if (System.IO.File.Exists(filepath))
            {
                installed.Load(filepath);
            }
            else
            {
                installed.AppendChild(installed.CreateXmlDeclaration("1.0", "UTF-8", "yes"));
                installed.AppendChild(installed.CreateElement("InstalledModules"));
            }

            var modules = new List<DashboardModel.Module>();
            var pending = Pending;
            foreach (var im in _modules)
            {
                var pm = Pending.Find(m => m.ID == im.Id);
                if (pm != null && pm.Version != im.Version)
                {
                    pending.Remove(pm);
                    pm = null;
                }

                var node = installed.SelectSingleNode(string.Format("/InstalledModules/Module[@id={0}]", im.Id));
                decimal ver = node != null ? decimal.Parse(node.Attributes["ver"].Value, CultureInfo.InvariantCulture) : 0.0m;
                if (pm != null)
                    modules.Add(pm);
                else if (node == null)
                    modules.Add(new DashboardModel.Module(im.Id, im.Version, im.Name, DashboardModel.State.NotInstalled, im.Requirements.RequredModules));
                else if (node != null && ver < im.Version)
                    modules.Add(new DashboardModel.Module(im.Id, im.Version, im.Name, DashboardModel.State.NewVersion, im.Requirements.RequredModules));
                else
                    modules.Add(new DashboardModel.Module(im.Id, im.Version, im.Name, DashboardModel.State.Installed, im.Requirements.RequredModules));
            }
            Pending = pending;
            return modules;
        }

        [InstallerAuthFilter]
        public ActionResult Upgrade()
        {
            List<UpgradeLogMessage> log = new List<UpgradeLogMessage>();

            //TODO: use ILoggingService

            PreparePendingForInstall();

            while (Pending.Count > 0)
            {
                var p = Pending.First();
                var im = _modules.Single(m => m.Id == p.ID);
                try
                {
                    if (!checkRequirements(im))
                        throw new Exception(string.Format("Upgrading module {0} canceled because required modules were not installed successfully.", im.Name));

                    _logService.WriteEvent(LogEventType.Info, string.Format("Upgrading module {0}", im.Name), 0);
                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.info, im.Name, "Merging domain requirements..."));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    _domainService.Merge(im.Requirements.Domain);
                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.success, im.Name, "Domain requirements merged successfully."));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.info, im.Name, "Initializing..."));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    im.Initialize();
                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.success, im.Name, "Initialized successfully."));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);

                    if (im.Requirements.Permissions != null && im.Requirements.Permissions.Count() > 0)
                    {
                        log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.info, im.Name, "Installing permissions..."));
                        _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                        var infoPerms = InstallModulesPermissions(im);
                        log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.success, im.Name, "Permissions installed successfully.", infoPerms));
                        _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    }

                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.info, im.Name, "Installing UI scripts..."));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    var instScriptsInfo = InstallModuleScripts(im);
                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.success, im.Name, "UI scripts installed successfully.", instScriptsInfo));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    
                    if (im.Requirements.UIDefinitions != null && im.Requirements.UIDefinitions.Count() > 0)
                    {
                        log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.info, im.Name, "Installing user interface..."));
                        _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                        var infoUI = InstallModuleUI(im);
                        log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.success, im.Name, "User interface installed successfully.", infoUI));
                        _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    }
                    if (im.Requirements.Templates != null && im.Requirements.Templates.Count() > 0)
                    {
                        log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.info, im.Name, "Installing notification templates..."));
                        _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                        var infoTemplates = InstallNotificationTemplates(im);
                        log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.success, im.Name, "Notification templates installed successfully.", infoTemplates));
                        _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    }

                    SetModuleInstalled(im);
                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.success, im.Name, "Installation completed."));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                }
                catch (Exception ex)
                {
                    log.Add(new UpgradeLogMessage(UpgradeLogMessage.MessageType.error, im.Name, ex));
                    _logService.WriteEvent(log.Last().EventType, log.Last().ToLog(), 1);
                    var pending = Pending;
                    pending.Remove(p);
                    Pending = pending;
                }
            }

            return Json(log);
        }

        private void PreparePendingForInstall()
        {
            var pending = Pending;
            //TODO-testing:remove!
            pending.Sort(delegate(DashboardModel.Module a, DashboardModel.Module b) { return a.Name.CompareTo(b.Name); });
            var ordered = new List<DashboardModel.Module>();
            foreach (var m in pending)
                getRequiredModules(m, ordered, pending);
            Pending = ordered;
        }

        private void getRequiredModules(DashboardModel.Module dmm, List<DashboardModel.Module> ordered, List<DashboardModel.Module> all)
        {
            foreach (var id in dmm.Required)
            {
                var rm = all.Find(m => m.ID == id);
                if (rm != null)
                {
                    getRequiredModules(rm, ordered, all);
                }
            }
            if (!ordered.Contains(dmm))
                ordered.Add(dmm);
        }

        [InstallerAuthFilter]
        public ActionResult Review(int id)
        {
            //var ids = GetDashboardModules().Where(m => m.State == DashboardModel.State.Installed || m.State == DashboardModel.State.PendingUpgrade || m.State == DashboardModel.State.PendingInstall).Select(m => m.ID);
            var reviewedModule = _modules.Single(m => m.Id == id);
            var dm = _domainService.Union(new DomainModel[] { _domainService.Domain, reviewedModule.Requirements.Domain });
            DomainModelChanges changes = _domainService.CompareWithExisting(dm);
            var model = new ReviewModel();
            model.DomainChanges = changes;

            return View(model);
        }

        [InstallerAuthFilter]
        [HttpPost]
        public JsonResult MarkModule(int id, DashboardModel.State state)
        {
            var im = _modules.Single(x => x.Id == id);
            var pending = Pending;

            if (!checkRequirements(im))
            {
                return Json(new { success = false, error = "Not all required modules are installed or pending." });
            }

            var ex = pending.Find(m => m.ID == id);
            if (ex != null)
                pending.Remove(ex);
            pending.Add(new DashboardModel.Module(im.Id, im.Version, im.Name, state, im.Requirements.RequredModules));
            return Json(new { success = true });
        }

        private bool checkRequirements(IModule im)
        {
            var all = GetDashboardModules();
            foreach (var id in im.Requirements.RequredModules)
            {
                if (all.Find(m => m.ID == id && (m.State == DashboardModel.State.Installed || m.State == DashboardModel.State.PendingInstall || m.State == DashboardModel.State.PendingUpgrade)) == null)
                    return false;
            }

            return true;
        }

        [InstallerAuthFilter]
        [HttpPost]
        public JsonResult ClearMarked()
        {
            Pending = null;
            return Json(new { success = true });
        }


        private string InstallModulesPermissions(IModule module)
        {
            StringBuilder info = new StringBuilder();
            info.Append("<ul>");
            ModulePermission mp = new ModulePermission()
            {
                Available = module.Requirements.Permissions != null ? module.Requirements.Permissions.ToArray() : new string[0],
                ModuleID = module.Id,
                ModuleName = module.Name
            };
            var q = new EntityQuery2(ModulePermission.ENTITY);
            q.AddProperty("Available");
            q.WhereIs("moduleId", module.Id);
            using (var dbContext = _dbService.GetDatabaseContext(true))
            {
                var ex = _repository.Read(q);
                if (ex == null)
                {
                    _repository.Create(mp.Entity);
                    foreach (var p in mp.Available)
                    {
                        info.AppendFormat("<li>{0} - added.</li>", p);
                    }
                }
                else if (ex.GetData<string>("Available") != mp.Entity.GetData<string>("Available"))
                {
                    var oldRaw = ex.GetData<string>("Available");
                    string[] old = null;
                    if (string.IsNullOrEmpty(oldRaw))
                        old = new string[0];
                    else
                        old = oldRaw.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    ex.SetData<string>("Available", mp.Entity.GetData<string>("Available"));
                    _repository.Update(ex);
                    foreach (var p in mp.Available)
                    {
                        if (!old.Contains(p))
                            info.AppendFormat("<li>{0} - added.</li>", p);
                    }
                    foreach (var p in old)
                    {
                        if (!mp.Available.Contains(p))
                            info.AppendFormat("<li>{0} - removed.</li>", p);
                    }
                }
                dbContext.Complete();
            }
            info.Append("</ul>");
            return info.ToString();
        }
        
        private string InstallModuleScripts(IModule module)
        {
            var rootpath = System.Web.HttpContext.Current.Server.MapPath("~/Scripts/mods/");
            var rootDir = new DirectoryInfo(rootpath);
            if (!rootDir.Exists)
                rootDir.Create();

            StringBuilder info = new StringBuilder();

            var moduleScripts = new List<ClientScript>();
            moduleScripts.AddRange(module.UIProvider.GetClientScripts(UserTypes.Admin));
            moduleScripts.AddRange(module.UIProvider.GetClientScripts(UserTypes.Customer));
            moduleScripts.AddRange(module.UIProvider.GetClientScripts(UserTypes.Librarian));
            info.AppendFormat("<h4>Scripts</h4>");
            info.Append("<ul>");
            SquishIt.Framework.Minifiers.JavaScript.JsMinMinifier min = new SquishIt.Framework.Minifiers.JavaScript.JsMinMinifier();
            foreach (var script in moduleScripts.Distinct(new ClientScriptComperer()))
            {
                var scriptpath = Path.Combine(rootpath, script.Name + ".js");
                if (System.IO.File.Exists(scriptpath))
                    System.IO.File.Delete(scriptpath);

                using (var writer = System.IO.File.CreateText(scriptpath))
                {
                    writer.Write(min.Minify(script.Content)); //TODO: Compress
                    writer.Flush();
                }
                info.AppendFormat("<li>{0}</li>", script.Name);
            }
            info.Append("</ul>");
            return info.ToString();
        }
        private string InstallModuleUI(IModule module)
        {
            StringBuilder info = new StringBuilder();
            info.AppendFormat("<h4>UI Definitions</h4>");
            info.Append("<ul>");
            foreach (var uidef in module.Requirements.UIDefinitions)
            {
                info.AppendFormat("<li>{0}", uidef.Name);
                if (_uiService.GetByName(uidef.Name) == null)
                {
                    _uiService.Add(uidef);
                    info.Append(" (Added)");
                }
                info.Append("</li>");
            }
            info.Append("</ul>");
            return info.ToString();
        }
        private string InstallNotificationTemplates(IModule module)
        {
            var info = new StringBuilder();
            info.Append("<ul>");

            foreach (var template in module.Requirements.Templates)
            {
                var existing = _templateService.Get(template.Id);
                if (existing != null)
                    info.AppendFormat("<li>{0} - skipped.</li>", template.Name);
                else
                {
                    _templateService.Save(template);
                    info.AppendFormat("<li>{0} - added.</li>", template.Name);
                }
            }

            info.Append("<ul>");
            return info.ToString();
        }

        //TODO: SetModuleInstalled
        private void SetModuleInstalled(IModule module)
        {
            var pending = Pending;
            var p = pending.Find(m => m.ID == module.Id);
            if (p != null)
                pending.Remove(p);
            Pending = pending;

            var filepath = HttpContext.Server.MapPath("~/modules.config");
            XmlDocument installed = new XmlDocument();
            if (System.IO.File.Exists(filepath))
            {
                installed.Load(filepath);
            }
            else
            {
                installed.AppendChild(installed.CreateXmlDeclaration("1.0", "UTF-8", "yes"));
                installed.AppendChild(installed.CreateElement("InstalledModules"));
            }

            var node = installed.SelectSingleNode(string.Format("/InstalledModules/Module[@id={0}]", module.Id));
            if (node == null)
            {
                node = installed.CreateElement("Module");
                var id = installed.CreateAttribute("id");
                id.Value = module.Id.ToString();
                var ver = installed.CreateAttribute("ver");
                node.Attributes.Append(id);
                node.Attributes.Append(ver);
                installed.SelectSingleNode("/InstalledModules").AppendChild(node);
            }

            node.Attributes["ver"].Value = module.Version.ToString(CultureInfo.InvariantCulture);

            installed.Save(filepath);
        }
    }

    public class UpgradeLogMessage
    {
        public enum MessageType
        {
            warning,
            info,
            error,
            success
        }

        public UpgradeLogMessage(MessageType type, string module, string msg, string fullText = null)
        {
            Type = type.ToString();
            Module = module;
            Message = msg;
            FullText = fullText;
        }

        public UpgradeLogMessage(MessageType type, string module, Exception ex)
        {
            Type = type.ToString();
            Module = module;
            Message = ex.Message;
            StringBuilder sb = new StringBuilder();
            writeErr(sb, ex);
            FullText = sb.ToString();
        }

        public string Type { get; set; }
        public string Module { get; set; }
        public string Message { get; set; }
        public string FullText { get; set; }

        public LogEventType EventType
        {
            get
            {
                switch (Type)
                {
                    case "error":
                        return LogEventType.Error;
                    case "warning":
                        return LogEventType.Warning;
                    default:
                        return LogEventType.Info;
                }
            }
        }

        public string ToLog()
        {
            return string.Format("{0}\n{1}", Message, FullText);
        }

        private void writeErr(StringBuilder sb, Exception ex)
        {
            sb.AppendFormat("{0}:{1}<br/>At:{2}<br/>", ex.GetType().Name, ex.Message, ex.StackTrace);
            if (ex.InnerException != null)
            {
                sb.Append("========Inner exception========<br/>");
                writeErr(sb, ex.InnerException);
            }
        }
    }

    public class ReviewModel
    {
        public ReviewModel()
        {
            Other = new List<string>();
        }
        public DomainModelChanges DomainChanges { get; set; }
        public List<string> Other { get; set; }
    }

    public class DashboardModel
    {
        public enum State
        {
            NotInstalled,
            NewVersion,
            PendingInstall,
            PendingUpgrade,
            Installed
        }
        public class Module
        {
            public Module(int id, decimal ver, string name, State state, IEnumerable<int> required)
            {
                ID = id;
                Version = ver;
                Name = name;
                State = state;
                Required = required.ToArray();

            }
            public int ID { get; set; }
            public decimal Version { get; set; }
            public string Name { get; set; }
            public State State { get; set; }
            public int[] Required { get; set; }
        }

        public DashboardModel()
        {
            Modules = new List<Module>();
        }

        public List<Module> Modules { get; set; }
    }
}
