using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public interface ITemplateService
    {
        void Save(HtmlTemplate template);
        HtmlTemplate Get(Guid id);
        IEnumerable<HtmlTemplate> GetAllTemplatesInfo();
        void Render(HtmlTemplate htmlTemplate, Dictionary<string, Entity> context, out string subject, out string body);
    }
}
