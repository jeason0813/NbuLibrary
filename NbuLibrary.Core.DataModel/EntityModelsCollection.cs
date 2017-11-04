using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{
    public class EntityModelsCollection : ICollection<EntityModel>
    {
        private Dictionary<string, EntityModel> _data;

        public EntityModelsCollection()
        {
            _data = new Dictionary<string, EntityModel>(StringComparer.InvariantCultureIgnoreCase);
        }

        public EntityModel this[string name]
        {
            get
            {
                EntityModel v = null;
                if (_data.TryGetValue(name, out v))
                    return v;
                else
                    return null;
            }
            set { _data[name] = value; }
        }

        public void Add(EntityModel item)
        {
            _data.Add(item.Name, item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(EntityModel item)
        {
            return _data.ContainsValue(item);
        }

        public bool Contains(string entityName)
        {
            return _data.ContainsKey(entityName);
        }

        public void CopyTo(EntityModel[] array, int arrayIndex)
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

        public bool Remove(EntityModel item)
        {
            return _data.Remove(item.Name);
        }

        public IEnumerator<EntityModel> GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }
    }
}
