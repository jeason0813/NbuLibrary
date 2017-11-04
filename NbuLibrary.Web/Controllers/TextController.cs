using NbuLibrary.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Serialization;

namespace NbuLibrary.Web.Controllers
{
    public class UIText
    {
        [XmlAttribute(AttributeName="k")]
        public string Key { get; set; }

        [XmlAttribute(AttributeName = "v")]
        public string Value { get; set; }
    }

    public class TextController : ApiController
    {
        private ISecurityService _securityService;
        public TextController(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        public IEnumerable<UIText> GetTexts()
        {
            var file = System.Web.HttpContext.Current.Server.MapPath("~/textresources.xml");

            if (File.Exists(file))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<UIText>));
                using (var fs = new FileStream(file, FileMode.Open))
                {
                    return ser.Deserialize(fs) as List<UIText>;
                }
            }
            else
                return new List<UIText>();
        }

        public object SetText(UIText entry)
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
                throw new Exception("Only admins can edit UI texts.");

            var dict = GetTexts() as List<UIText>;

            var existing = dict.FindIndex(e => e.Key == entry.Key);
            if (existing >= 0)
                dict.RemoveAt(existing);
            
            dict.Add(entry);

            var file = System.Web.HttpContext.Current.Server.MapPath("~/textresources.xml");
            XmlSerializer ser = new XmlSerializer(typeof(List<UIText>));
            using (var fs = new FileStream(file, File.Exists(file) ? FileMode.Truncate : FileMode.Create))
            {
                ser.Serialize(fs, dict);
                fs.Flush();
            }

            return new { success = true };
        }

        public object UnsetText(UIText entry)
        {
            if (_securityService.CurrentUser.UserType != Core.Domain.UserTypes.Admin)
                throw new Exception("Only admins can edit UI texts.");

            var dict = GetTexts() as List<UIText>;

            var existing = dict.FindIndex(e => e.Key == entry.Key);
            if (existing >= 0)
                dict.RemoveAt(existing);

            var file = System.Web.HttpContext.Current.Server.MapPath("~/textresources.xml");
            XmlSerializer ser = new XmlSerializer(typeof(List<UIText>));
            using (var fs = new FileStream(file, File.Exists(file) ? FileMode.Truncate : FileMode.Create))
            {
                ser.Serialize(fs, dict);
                fs.Flush();
            }

            return new { success = true };
        }
    }
}
