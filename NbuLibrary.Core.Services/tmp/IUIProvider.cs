using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NbuLibrary.Core.Domain;

namespace NbuLibrary.Core.ModuleEngine
{
    public class ClientScript
    {
        public string Name { get; set; }
        //todo: string or stream
        public string Content { get; set; }
    }

    public class ClientScriptComperer : IEqualityComparer<ClientScript>
    {
        public bool Equals(ClientScript x, ClientScript y)
        {
            return x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(ClientScript obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    //TODO: UIProvider - is it required for all modules?
    public interface IUIProvider
    {
        IEnumerable<ClientScript> GetClientScripts(UserTypes type);
        string GetClientTemplates(UserTypes type);
    }
}
