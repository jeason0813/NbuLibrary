using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Service.tmp;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.ModuleEngine
{
    public interface IModule
    {
        int Id { get; }
        decimal Version { get; }
        string Name { get; }

        IUIProvider UIProvider { get; }

        ModuleRequirements Requirements { get; }

        void Initialize();
    }

    public class ModuleRequirements
    {
        public ModuleRequirements()
        {
            Domain = new DomainModel();
            UIDefinitions = new UIDefinition[0];
            RequredModules = new int[0];
            Templates = new HtmlTemplate[0];
        }

        public DomainModel Domain { get; set; }
        public IEnumerable<UIDefinition> UIDefinitions { get; set; }
        public IEnumerable<string> Permissions { get; set; }
        public IEnumerable<int> RequredModules { get; set; }
        public IEnumerable<HtmlTemplate> Templates { get; set; }
    }
}
