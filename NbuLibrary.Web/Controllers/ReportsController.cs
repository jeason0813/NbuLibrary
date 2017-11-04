using NbuLibrary.Core.Reporting;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NbuLibrary.Web.Controllers
{
    public class ReportsController : Controller
    {
        private IReportingService _reportingService;
        private ISecurityService _securityService;
        public ReportsController(IReportingService reportingService, ISecurityService securityService)
        {
            _reportingService = reportingService;
            _securityService = securityService;
        }

        public JsonResult Index(int moduleId)
        {
            if (_reportingService.CanAccess(moduleId))
                return Json(new { service = _reportingService.GetService(moduleId), reports = _reportingService.GetReports(moduleId) }, JsonRequestBehavior.AllowGet);
            else
                return Json(new string[0], JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ActionName("Folder")]
        public JsonResult CreateFolder(string service)
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
            {
                return Json(new { ok = false, message = "No access." });
            }

            return Json(new { ok = _reportingService.CreateServiceFolder(service) });
        }

        [HttpPost]
        public ActionResult Upload()
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
            {
                return Json(new { ok = false, message = "No access." });
            }

            string name = Request["report_name"];
            string service = Request["service"];
            var response = new FileUploadResponse();
            var file = Request.Files[0];

            byte[] bytes = new byte[file.ContentLength];
            file.InputStream.Read(bytes, 0, bytes.Length);

            bool ok = _reportingService.UploadReport(service, name, bytes);

            if (ok)
            {
                response.files.Add(new FileUploadResponse.File()
                {
                    name = file.FileName,
                    size = file.ContentLength
                });


                return Json(response);
            }
            else
            {
                Response.StatusCode = 500;
                return Json(new { ok = false, message = "Upload failed" });
            }
        }

        [HttpPost]
        public JsonResult Delete(string service, string report)
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
            {
                return Json(new { ok = false, message = "No access." });
            }
            bool ok = _reportingService.DeleteReport(service, report);
            if (ok)
                return Json(new { ok = true });
            else
                return Json(new { ok = false });
        }

        public JsonResult All()
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
            {
                return Json(new { ok = false, message = "No access." });
            }


            return Json(_reportingService.GetServiceFolders(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult View(string service, string report)
        {
            return Redirect(string.Format("~/Report.aspx?service={0}&report={1}", service, report));
        }
    }
}
