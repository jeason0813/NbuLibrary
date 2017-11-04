using NbuLibrary.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NbuLibrary.Web.Filters
{
    public class InstallerAuthFilter : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            if (httpContext.Session[SystemController.AUTH_TOKEN] != null && (bool)httpContext.Session[SystemController.AUTH_TOKEN])
                base.OnAuthorization(filterContext);
            else
            {
                var url = new UrlHelper(filterContext.RequestContext);
                filterContext.Result = new RedirectResult(url.Action("Index", "System"));
            }
        }
    }
}