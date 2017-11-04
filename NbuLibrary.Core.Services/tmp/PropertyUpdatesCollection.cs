using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.Services.tmp
{
    public class PropertyUpdatesCollection : ICollection<PropertyUpdate>
    {
        private Dictionary<string, PropertyUpdate> _updates;

        public PropertyUpdatesCollection()
        {
            _updates = new Dictionary<string, PropertyUpdate>();
        }

        public PropertyUpdate this[string name]
        {
            get
            {
                PropertyUpdate update = null;
                _updates.TryGetValue(name, out update);
                return update;

            }
            set
            {
                _updates[value.Name] = value;
            }
        }

        public void Add(PropertyUpdate item)
        {
            _updates.Add(item.Name, item);
        }

        public void Clear()
        {
            _updates.Clear();
        }

        public bool Contains(PropertyUpdate item)
        {
            return _updates.ContainsKey(item.Name);
        }
        public bool Contains(string name)
        {
            return _updates.ContainsKey(name);
        }

        public void CopyTo(PropertyUpdate[] array, int arrayIndex)
        {
            _updates.Values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return _updates.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(PropertyUpdate item)
        {
            return _updates.Remove(item.Name);
        }
        public bool Remove(string propName)
        {
            return _updates.Remove(propName);
        }

        public IEnumerator<PropertyUpdate> GetEnumerator()
        {
            return _updates.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _updates.Values.GetEnumerator();
        }
    }
}
