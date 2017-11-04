using NbuLibrary.Core.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public interface IDomainModelService
    {
        DomainModel Domain { get; }

        DomainModel Union(IEnumerable<DomainModel> domains);
        void Save(DomainModel dm);
        void Merge(DomainModel dm);
        DomainModelChanges CompareWithExisting(DomainModel dm);
    }
}
