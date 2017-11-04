using NbuLibrary.Core.AccountModule;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NbuLibrary.Web.Controllers
{
    public class TemplatesController : ApiController
    {
        private ITemplateService _templateService;
        private ISecurityService _securityService;
        public TemplatesController(ISecurityService securityService, ITemplateService templateService)
        {
            _securityService = securityService;
            _templateService = templateService;
        }

        public IEnumerable<HtmlTemplate> GetAllTemplates()
        {
            return _templateService.GetAllTemplatesInfo();
        }

        public HtmlTemplate GetTemplate(Guid id)
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
                throw new Exception("Only admins can edit UI texts.");

            return _templateService.Get(id);
        }

        public object Save(HtmlTemplate template)
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
                throw new Exception("Only admins can edit UI texts.");

            _templateService.Save(template);
            return new { success = true };
        }
    }
}
