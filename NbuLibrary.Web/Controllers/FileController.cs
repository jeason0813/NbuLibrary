using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace NbuLibrary.Web.Controllers
{
    public class FileController : Controller
    {
        private IFileService _fileService;
        private IEntityOperationService _entityService;

        public FileController(IFileService fileService, IEntityOperationService entityService, ISecurityService securityService)
        {
            _fileService = fileService;
            _entityService = entityService;
            _securityService = securityService;
        }

        //TODO: filecontroller index
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Upload()
        {
            var response = new FileUploadResponse();
            foreach (string filename in Request.Files)
            {
                var file = Request.Files[filename];
                var stat = _fileService.CanUpload(file.FileName, file.ContentLength);
                if (stat == CanUploadStatus.FileTypeNotAllowed)
                    throw new Exception("Files of this type are not allowed.");
                else if(stat == CanUploadStatus.DiskUsageLimitExceeded)
                    throw new Exception("Disk usage limit exceeded.");

                Guid id = _fileService.StoreFileContent(file.InputStream);
                var f = new File()
                {
                    FileName = System.IO.Path.GetFileNameWithoutExtension(file.FileName),
                    ContentType = file.ContentType,
                    ContentPath = id.ToString(),
                    Extension = System.IO.Path.GetExtension(file.FileName),
                    Size = file.ContentLength
                };

                EntityUpdate create = new EntityUpdate(f);
                var result = _entityService.Update(create);
                if (result.Success)
                {
                    response.files.Add(new FileUploadResponse.File()
                    {
                        id = create.Id.Value,
                        name = file.FileName,
                        size = file.ContentLength,
                        url = Url.Action("Download") + "?id=" + create.Id.Value
                    });
                }
            }

            //System.Threading.Thread.Sleep(500);

            return Json(response);
        }

        public ActionResult CheckAccess(int id)
        {
            return Json(new { hasAccess = _fileService.HasAccess(_securityService.CurrentUser, id) }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Download(int id)
        {
            try
            {
                var file = _fileService.GetFile(id);
                return File(_fileService.GetFileContent(id), file.ContentType, string.Format("{0}{1}", file.FileName, file.Extension));
            }
            catch (UnauthorizedAccessException)
            {
                return Redirect("~/#/noaccess/");
            }
        }

        public ActionResult OpenAttachment(int id)
        {
            string url = Url.Action("Download", new { id = id });
            return View((object)url);
        }

        public ISecurityService _securityService { get; set; }
    }

    public class FileUploadResponse
    {
        public class File
        {
            public int id { get; set; }
            public string name { get; set; }
            public int size { get; set; }
            public string url { get; set; }
            public string thumbnailUrl { get; set; }
            public string deleteUrl { get; set; }
            public string deleteType { get; set; }
        }

        public FileUploadResponse()
        {
            files = new List<File>();
        }

        public List<FileUploadResponse.File> files { get; set; }
    }
}
