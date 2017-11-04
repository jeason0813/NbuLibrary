using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{
    public class RelationModelsCollection : ICollection<RelationModel>
    {
        private Dictionary<string, RelationModel> _data;

        public RelationModelsCollection()
        {
            _data = new Dictionary<string, RelationModel>(StringComparer.InvariantCultureIgnoreCase);
        }


        public RelationModel this[string entity1, string entity2, string role]
        {
            get
            {
                var isLeft = entity1.ToLower().CompareTo(entity2.ToLower()) < 0;
                var left = isLeft ? entity1 : entity2;
                var right = isLeft ? entity2 : entity1;
                var key = string.Format("{0}_{1}_{2}", left, right, role);
                return this[key];
            }
            set { _data[value.Name] = value; }
        }
        public RelationModel this[string name]
        {
            get
            {
                RelationModel v = null;
                if (_data.TryGetValue(name, out v))
                    return v;
                else
                    return null;
            }
            set { _data[name] = value; }
        }
        public void Add(RelationModel item)
        {
            _data.Add(item.Name, item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(RelationModel item)
        {
            return _data.ContainsValue(item);
        }

        public bool Contains(string relationName)
        {
            return _data.ContainsKey(relationName);
        }

        public bool Contains(string entity1, string entity2, string role)
        {
            return this[entity1, entity2, role] != null;
        }

        public void CopyTo(RelationModel[] array, int arrayIndex)
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

        public bool Remove(RelationModel item)
        {
            return _data.Remove(item.Name);
        }

        public IEnumerator<RelationModel> GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }
    }
}
