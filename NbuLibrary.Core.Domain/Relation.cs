using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class Relation
    {
        public Relation()
        {
            Data = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        }

        public Relation(string role, Entity entity = null)
            : this()
        {
            Role = role;
            Entity = entity;
        }

        public int Id
        {
            get
            {
                return GetData<int>("id");
            }
        }
        public Dictionary<string, object> Data { get; set; }
        public Entity Entity { get; set; }
        public string Role { get; set; }

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
