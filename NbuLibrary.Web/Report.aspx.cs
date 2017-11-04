using Microsoft.Reporting.WebForms;
using NbuLibrary.Core.Infrastructure;
using NbuLibrary.Core.ModuleEngine;
using NbuLibrary.Core.Reporting;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace NbuLibrary.Web
{
    public partial class Report : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ReportViewer1.ProcessingMode = ProcessingMode.Remote;

                var serviceName = Request.QueryString["service"];
                var reportName = Request.QueryString["report"];

                var application = ((MvcApplication)HttpContext.Current.ApplicationInstance);
                IReportingService reportingService = (IReportingService)application.Kernel.GetService(typeof(IReportingService));
                if (!reportingService.CanAccess(serviceName))
                {
                    Response.Clear();
                    Response.Write("<h2>No access.</h2>");
                    return;
                }


                ReportViewer1.ServerReport.ReportServerUrl = new Uri(GetReportingServiceUrl());
                ReportViewer1.ServerReport.ReportPath = reportingService.GetReportPath(serviceName, reportName);

                //ReportParameter[] param = new ReportParameter[1]; 
                //param[0] = new ReportParameter("CustomerID", txtparam.Text);
                //ReportViewer1.ServerReport.SetParameters(param);

                ReportViewer1.ServerReport.Refresh();
            }
        }

        private string GetReportingServiceUrl()
        {
            string appSettingEntry = System.Configuration.ConfigurationManager.AppSettings["ReportingServiceUrl"];
            if (string.IsNullOrEmpty(appSettingEntry))
                return "http://localhost/reportserver";
            else
                return appSettingEntry;
        }
    }
}