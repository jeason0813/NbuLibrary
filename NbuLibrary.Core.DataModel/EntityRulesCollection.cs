using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{
    public class EntityRulesCollection : ICollection<EntityRuleModel>
    {
        private Dictionary<string, EntityRuleModel> _data;

        public EntityRulesCollection()
        {
            _data = new Dictionary<string, EntityRuleModel>();
        }

        public IEnumerable<EntityRuleModel> GetRulesFor(PropertyModel property)
        {
            return _data.Values.Where(r => r.AppliesFor(property));
        }


        public void Add(EntityRuleModel item)
        {
            _data.Add(item.Identifier, item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(EntityRuleModel item)
        {
            return _data.ContainsKey(item.Identifier);
        }

        public void CopyTo(EntityRuleModel[] array, int arrayIndex)
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

        public bool Remove(EntityRuleModel item)
        {
            return _data.Remove(item.Identifier);
        }

        public IEnumerator<EntityRuleModel> GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.Values.GetEnumerator();
        }
    }
}
