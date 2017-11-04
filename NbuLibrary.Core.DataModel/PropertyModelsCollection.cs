using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{
    public class PropertyModelsCollection : ICollection<PropertyModel>
    {
        private Dictionary<string, PropertyModel> _data;

        public PropertyModelsCollection()
        {
            _data = new Dictionary<string, PropertyModel>(StringComparer.InvariantCultureIgnoreCase);
        }

        public PropertyModel this[string name]
        {
            get
            {
                PropertyModel v = null;
                if (_data.TryGetValue(name, out v))
                    return v;
                else
                    return null;
            }
            set { _data[name] = value; }
        }

        public void Add(PropertyModel item)
        {
            _data.Add(item.Name, item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(PropertyModel item)
        {
            return _data.ContainsValue(item);
        }

        public bool Contains(string propertyName)
        {
            return _data.ContainsKey(propertyName);
        }

        public void CopyTo(PropertyModel[] array, int arrayIndex)
        {
            _data.Values.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(PropertyModel item)
        {
            return _data.Remove(item.Name);
        }

        public IEnumerator<PropertyModel> GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }
    }
}
