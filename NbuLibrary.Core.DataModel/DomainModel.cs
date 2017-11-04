using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.DataModel
{
    public class DomainModel
    {
        public DomainModel()
        {
            Entities = new EntityModelsCollection();
            Relations = new RelationModelsCollection();
        }

        public EntityModelsCollection Entities { get; set; }
        public RelationModelsCollection Relations { get; set; }
    }
}
