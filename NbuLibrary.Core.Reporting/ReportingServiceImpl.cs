using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Reporting
{
    class ReportingServiceImpl : IReportingService
    {
        private IEnumerable<IModule> _modules;
        private ISecurityService _securityService;
        private ILoggingService _loggingService;
        private Dictionary<string, IModule> _modulesMap;


        public ReportingServiceImpl(ISecurityService securityService, IEnumerable<IModule> modules, ILoggingService loggingService)
        {
            _securityService = securityService;
            _modules = modules;
            _loggingService = loggingService;
            _modulesMap = new Dictionary<string, IModule>();
            foreach (var m in modules)
                _modulesMap.Add(m.Name, m);

        }

        public bool CanAccess(string service)
        {
            if (!_modulesMap.ContainsKey(service))
                return false;
            else
                return CanAccess(_modulesMap[service].Id);
        }

        public string GetReportPath(string service, string report)
        {
            if (!CanAccess(service))
                return null;
            else
            {
                var rs = new ReportingServer();
                var reportItem = rs.GetReports(String.Format("/libservices/{0}", service)).Where(r => r.Name.Equals(report, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
                if (reportItem == null)
                    return null;

                return reportItem.Path;
            }
        }


        public IEnumerable<string> GetReports(string service)
        {
            using (var rs = new ReportingServer())
            {
                return rs.GetReports(String.Format("/libservices/{0}", service)).Select(r => r.Name);
            }
        }


        public IEnumerable<string> GetServices()
        {
            return _modules.Select(m => m.Name);
        }


        public bool CanAccess(int moduleId)
        {
            return _securityService.HasModulePermission(_securityService.CurrentUser, moduleId)
                && (_securityService.CurrentUser.UserType == Domain.UserTypes.Librarian || _securityService.CurrentUser.UserType == Domain.UserTypes.Admin);
        }

        public IEnumerable<string> GetReports(int moduleId)
        {
            return GetReports(GetService(moduleId));
        }

        public string GetService(int moduleId)
        {
            var module = _modules.SingleOrDefault(m => m.Id == moduleId);
            if (module != null)
                return module.Name;
            else
                return null;
        }

        public IEnumerable<ServiceFolder> GetServiceFolders()
        {

            using (var rs = new ReportingServer())
            {
                var folders = rs.GetFolders("/libservices");
                Dictionary<string, Folder> map = new Dictionary<string, Folder>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var folder in folders)
                    map.Add(folder.Name, folder);
                foreach (var module in _modules)
                {
                    var serviceFolder = new ServiceFolder()
                    {
                        Exists = map.ContainsKey(module.Name),
                        Name = module.Name
                    };
                    if (serviceFolder.Exists)
                    {
                        serviceFolder.Reports = rs.GetReports(map[module.Name].Path).Select(r => new Core.Services.Report()
                        {
                            Name = r.Name,
                            Description = r.Description
                        });
                    }

                    yield return serviceFolder;
                }
            }
        }


        public bool UploadReport(string service, string report, byte[] definition)
        {
            if (_securityService.CurrentUser.UserType != Domain.UserTypes.Admin)
                return false;
            else if (!_modulesMap.ContainsKey(service))
                return false;
            try
            {
                using (var rs = new ReportingServer())
                {
                    return rs.CreateReport(report, definition, string.Format("/libservices/{0}", service));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                _loggingService.WriteEvent(LogEventType.Error, string.Format("({0}) {1}", ex.GetType().Name, ex.Message));
                return false;
            }
        }


        public bool CreateServiceFolder(string service)
        {
            if (_securityService.CurrentUser.UserType != Domain.UserTypes.Admin)
                return false;
            else if (!_modulesMap.ContainsKey(service))
                return false;
            try
            {
                using (var rs = new ReportingServer())
                {
                    return rs.CreateFolder(service, "/libservices");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                _loggingService.WriteEvent(LogEventType.Error, string.Format("({0}) {1}", ex.GetType().Name, ex.Message));
                return false;
            }
        }


        public bool DeleteReport(string service, string report)
        {
            if (_securityService.CurrentUser.UserType != Domain.UserTypes.Admin)
                return false;
            else if (!_modulesMap.ContainsKey(service))
                return false;
            try
            {
                using (var rs = new ReportingServer())
                {
                    return rs.DeleteReport(report, String.Format("/libservices/{0}", service));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                _loggingService.WriteEvent(LogEventType.Error, string.Format("({0}) {1}", ex.GetType().Name, ex.Message));
                return false;
            }
        }
    }
}
