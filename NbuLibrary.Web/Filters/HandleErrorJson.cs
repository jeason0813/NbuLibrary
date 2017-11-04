using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NbuLibrary.Web.Filters
{
    public class HandleErrorJsonAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.HttpContext.Request.AcceptTypes.Contains("application/json"))
            {
                var result = new JsonResult();
                result.Data = new { success = false, error = filterContext.Exception.Message, errorType = filterContext.Exception.GetType().Name };
                filterContext.ExceptionHandled = true;
                filterContext.Result = result;
            }
            else
                base.OnException(filterContext);
        }
    }
}