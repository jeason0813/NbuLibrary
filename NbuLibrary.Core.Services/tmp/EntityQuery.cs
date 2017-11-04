using NbuLibrary.Core.Domain;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services.tmp
{
    public class EntityDelete : EntityOperation
    {
        public bool Recursive { get; set; }
    }

    public class PropertyUpdate
    {
        public PropertyUpdate()
        {
        }

        public PropertyUpdate(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public object Value { get; set; }
    }

    public enum RelationOperation
    {
        None = 0,
        Attach = 1,
        Detach = 2
    }
}
