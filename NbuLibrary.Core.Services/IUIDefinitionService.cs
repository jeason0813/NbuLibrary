using NbuLibrary.Core.Service.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public interface IUIDefinitionService
    {
        UIDefinition GetByName(string name);
        T GetByName<T>(string name) where T : UIDefinition;

        void Add(UIDefinition definition);
        void Remove(UIDefinition definition);
        void Update(UIDefinition definition);

        IEnumerable<UIDefinition> GetAll();
    }
}
