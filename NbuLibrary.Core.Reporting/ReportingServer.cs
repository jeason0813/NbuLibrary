using NbuLibrary.Core.Reporting.SSRS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Reporting
{
    public class ReportingServer : IDisposable
    {
        private ReportingService2005SoapClient client;
        public ReportingServer()
        {
            client = new ReportingService2005SoapClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
        }

        public IEnumerable<Folder> GetFolders(string path = null)
        {
            CatalogItem[] items;

            client.ListChildren(path ?? "/", false, out items);
            //client.FindItems(path ?? "/", 
            //    BooleanOperatorEnum.And, 
            //    new SearchCondition[] { 
            //    new SearchCondition() { Condition = ConditionEnum.Equals, ConditionSpecified = true, Name = "Type", Value = ItemTypeEnum.Folder.ToString() } }, 
            //    out items);
            return items.Where(ci => ci.Type == ItemTypeEnum.Folder).Select(i => new Folder() { Name = i.Name, Path = i.Path });
        }

        public IEnumerable<Report> GetReports(string path)
        {
            CatalogItem[] items;
            try
            {
                client.FindItems(path, BooleanOperatorEnum.And, new SearchCondition[] { new SearchCondition() { Condition = ConditionEnum.Equals, ConditionSpecified = true, Name = "Type", Value = ItemTypeEnum.Report.ToString() } }, out items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                return new Report[0];
            }
            return items.Select(i => new Report() { Name = i.Name, Path = i.Path, Description = i.Description });
        }

        public bool CreateFolder(string name, string path = null)
        {
            string batchId = null;
            client.CreateBatch(out batchId);

            var result = client.CreateFolder(
                new BatchHeader()
                {
                    BatchID = batchId
                },
                name,
                path ?? "/",
                new Property[0]);

            client.ExecuteBatch(new BatchHeader() { BatchID = batchId });
            return true;
        }

        public bool CreateReport(string name, byte[] definition, string path = null)
        {
            string batchId = null;
            client.CreateBatch(out batchId);
            Warning[] warnings = null;
            var result = client.CreateReport(
                new BatchHeader()
                {
                    BatchID = batchId
                },
                name,
                path ?? "/",
                true,
                definition,
                new Property[0],
                out warnings);

            string reportPath = string.Format("/{0}/{1}", path.Trim('/'), name);

            DataSourceReference reference = new DataSourceReference();
            reference.Reference = "/libservices/DS";
            DataSource[] dataSources = new DataSource[1];
            DataSource ds = new DataSource();
            ds.Item = (DataSourceDefinitionOrReference)reference;
            ds.Name = "DS";
            dataSources[0] = ds;
            client.SetItemDataSources(new BatchHeader() { BatchID = batchId }, reportPath, dataSources);

            client.ExecuteBatch(new BatchHeader() { BatchID = batchId });
            if (warnings != null && warnings.Length > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var w in warnings)
                {
                    sb.AppendLine(w.Severity);
                    sb.AppendLine("---");
                    sb.AppendLine(w.ObjectName);
                    sb.AppendLine(w.ObjectType);
                    sb.AppendLine(w.Message);
                    sb.AppendLine();
                }

                throw new Exception(sb.ToString());
            }
            return true;
        }

        public bool DeleteReport(string name, string path = null)
        {
            var report = GetReports(path ?? "/").SingleOrDefault(r => r.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (report == null)
                return false;

            string batchId = null;
            client.CreateBatch(out batchId);
            client.DeleteItem(new BatchHeader() { BatchID = batchId }, report.Path);
            client.ExecuteBatch(new BatchHeader() { BatchID = batchId });
            return true;

        }

        public void Dispose()
        {
            if (client != null)
            {
                ((IDisposable)client).Dispose();
                client = null;
            }
        }
    }
}
