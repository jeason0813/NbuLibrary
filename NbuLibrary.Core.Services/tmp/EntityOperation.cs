using NbuLibrary.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.Services.tmp
{
    public class EntityOperation
    {
        public string Entity { get; set; }
        public int? Id { get; set; }

        public bool IsEntity(string entity)
        {
            return Entity.Equals(entity, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
