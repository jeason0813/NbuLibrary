using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Serialization;
using NbuLibrary.Web.Models;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.Services;

namespace NbuLibrary.Web.Controllers
{
    public class UIDefinitionUpdate
    {
        public string Raw { get; set; }
    }
    public class ViewDefController : ApiController
    {
        private IUIDefinitionService _uiService;
        public ViewDefController(IUIDefinitionService uiService)
        {
            _uiService = uiService;
        }

        [HttpGet]
        public UIDefinition Get(string viewName)
        {
            return _uiService.GetByName(viewName);
        }

        [HttpPost]
        public object Save(UIDefinitionUpdate update)
        {
            var def = Newtonsoft.Json.JsonConvert.DeserializeObject<UIDefinition>(update.Raw);

            var existing = _uiService.GetByName(def.Name);
            if (existing == null)
                throw new NotImplementedException("Creating UI Definitions from the ui is not supported yet");

            _uiService.Update(def);


            return new { success = true };
        }

        [HttpGet]
        public IEnumerable<UIDefinition> All()
        {
            return _uiService.GetAll();
        }
    }
}
