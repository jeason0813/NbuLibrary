using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class Entity
    {
        public Entity()
        {
            Data = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            RelationsData = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        }

        public Entity(string name)
            : this()
        {
            Name = name;
        }

        public Entity(string name, int id)
            : this()
        {
            Name = name;
            Id = id;
        }

        public Entity(Entity e)
        {
            Id = e.Id;
            Name = e.Name;
            Data = new Dictionary<string, object>(e.Data, StringComparer.InvariantCultureIgnoreCase);
            RelationsData = new Dictionary<string, object>(e.RelationsData, StringComparer.InvariantCultureIgnoreCase);
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public Dictionary<string, object> Data { get; set; }
        public Dictionary<string, object> RelationsData { get; set; }

        public T GetData<T>(string property)
        {
            object v = null;
            if (!Data.TryGetValue(property, out v) || v == null)
                return default(T);
            else
                return ConvertValue<T>(v);
        }
        public void SetData<T>(string property, T value)
        {
            Data[property] = value;
        }

        public Relation GetSingleRelation(string entity, string role)
        {
            object rel = null;
            var key = string.Format("{0}_{1}", entity, role);
            if (RelationsData.TryGetValue(key, out rel))
                return rel as Relation;
            else
                return null;
        }
        public IEnumerable<Relation> GetManyRelations(string entity, string role)
        {
            object rels = null;
            var key = string.Format("{0}_{1}", entity, role);
            if (RelationsData.TryGetValue(key, out rels))
                return rels as IEnumerable<Relation>;
            else
                return new Relation[0];
        }

        public void SetSingleRelation(Relation relation)
        {
            var key = string.Format("{0}_{1}", relation.Entity.Name, relation.Role);
            RelationsData[key] = relation;
        }

        public void AddManyRelation(Relation relation)
        {
            var key = string.Format("{0}_{1}", relation.Entity.Name, relation.Role);
            object data = null;
            List<Relation> rels = null;
            if (RelationsData.TryGetValue(key, out data))
                rels = data as List<Relation>;
            else
            {
                rels = new List<Relation>();
                RelationsData[key] = rels;
            }

            rels.Add(relation);
        }

        //TODO: code repetition in EntityUpdate
        private T ConvertValue<T>(object v)
        {
            if (typeof(T) == typeof(bool))
                return (T)(object)Convert.ToBoolean(v);
            else if (typeof(T) == typeof(decimal))
                return (T)(object)Convert.ToDecimal(v);
            else if (typeof(T) == typeof(int))
                return (T)(object)Convert.ToInt32(v);
            else if (typeof(T) == typeof(DateTime))
                return (T)(object)Convert.ToDateTime(v);
            else if (typeof(T).IsEnum)
            {
                if (v is string)
                {
                    string s = v as string;
                    int integer = 0;
                    if (int.TryParse(s, out integer))
                        return (T)(object)integer;
                    else
                        return (T)Enum.Parse(typeof(T), s);
                }
                else
                    return (T)v;

            }
            else
                return (T)v;
        }
    }
}
