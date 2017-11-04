using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public class ServiceFolder
    {
        public bool Exists { get; set; }
        public string Name { get; set; }
        public IEnumerable<Report> Reports { get; set; }
    }

    public class Report
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public interface IReportingService
    {
        bool CanAccess(string service);

        bool CanAccess(int moduleId);

        string GetReportPath(string service, string report);

        IEnumerable<string> GetReports(string service);

        IEnumerable<string> GetReports(int moduleId);

        string GetService(int moduleId);

        IEnumerable<ServiceFolder> GetServiceFolders();

        bool CreateServiceFolder(string service);

        bool UploadReport(string service, string report, byte[] definition);

        bool DeleteReport(string service, string report);
    }
}
