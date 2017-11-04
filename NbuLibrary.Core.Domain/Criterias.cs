using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Domain
{
    public class Sorting
    {
        public string Role { get; set; }
        public string Entity { get; set; }

        public string Property { get; set; }
        
        public bool Descending { get; set; }
        public bool IsRel
        {
            get
            {
                return !string.IsNullOrEmpty(Role);
            }
        }

        public Sorting() {}

        public Sorting(string property, bool desc = false)
        {
            Property = property;
            Descending = desc;
        }
        public Sorting(string property, string role, string entity, bool desc = false)
        {
            Property = property;
            Descending = desc;
            Role = role;
            Entity = entity;
        }
    }

    public class Condition
    {
        #region consts

        public const string Is = "is";
        public const string LessThen = "lt";
        public const string LessThenOrEqual = "lte";
        public const string GreaterThen = "gt";
        public const string GreaterThenOrEqual = "gte";
        public const string StartsWith = "sw";
        public const string EndsWith = "ew";
        public const string ContainsPhrase = "cntp";
        public const string Not = "not";
        public const string Between = "between";
        public const string AnyOf = "anyof";


        #endregion


        public string Property { get; set; }
        public string Operator { get; set; }
        public object[] Values { get; set; }
        public string Role { get; set; }
        public string Entity { get; set; }

        public bool IsRel
        {
            get
            {
                return !string.IsNullOrEmpty(Role);
            }
        }

        public Condition()
        {

        }

        public Condition(string property, string op, object[] values)
        {
            Property = property;
            Operator = op;
            Values = values;
        }
        public Condition(string property, string op, object value)
        {
            Property = property;
            Operator = op;
            Values = new object[] { value };
        }

        public bool IsForProperty(string property)
        {
            return Property.Equals(property, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class Paging
    {
        public int Page { get; set; }
        public int PageSize { get; set; }

        public Paging()
        {

        }
        public Paging(int page = 1, int pageSize = 10)
        {
            Page = page;
            PageSize = pageSize;
        }
    }

    public interface ICondition
    {
        string Column
        { get; set; }

        string ToSql();
    }

    public class CompareCondition<T> : ICondition
    {
        public enum Comparison
        {
            GreaterThen,
            GreaterThenOrEqual,
            LessThen,
            LessThenOrEqual,
            Equal,
            NotEqual
        }

        private string _property;
        private Comparison _comparison;
        private T _value;

        public CompareCondition(string property, Comparison comparison, T value)
        {
            _property = property;
            _comparison = comparison;
            _value = value;
        }

        public string ToSql()
        {
            string op = null;
            switch (_comparison)
            {
                case Comparison.Equal:
                    op = "=";
                    break;
                case Comparison.GreaterThen:
                    op = ">";
                    break;
                case Comparison.GreaterThenOrEqual:
                    op = ">=";
                    break;
                case Comparison.LessThen:
                    op = "<";
                    break;
                case Comparison.LessThenOrEqual:
                    op = "<=";
                    break;
                case Comparison.NotEqual:
                    op = "<>";
                    break;
                default:
                    throw new NotImplementedException();
            }

            string val = _value.ToString();
            if (typeof(T) == typeof(decimal))
                val = ((decimal)(object)_value).ToString(System.Globalization.CultureInfo.InvariantCulture);

            return string.Format("{0} {1} {2}", _property, op, val);
        }

        public string Column
        {
            get
            {
                return _property;
            }
            set
            {
                _property = value;
            }
        }
    }

    public class StringCondition : ICondition
    {
        public enum StringOp
        {
            Contains,
            StartsWith,
            EndsWith,
            Is
        }

        private string _property;
        private StringOp _op;
        private string _value;

        public StringCondition(string property, StringOp op, string value)
        {
            _property = property;
            _op = op;
            _value = value;
        }


        public string ToSql()
        {
            switch (_op)
            {
                case StringOp.StartsWith:
                    return string.Format("{0} LIKE '%{1}'", _property, _value);
                case StringOp.EndsWith:
                    return string.Format("{0} LIKE '{1}%'", _property, _value);
                case StringOp.Contains:
                    return string.Format("{0} LIKE '%{1}%'", _property, _value);
                case StringOp.Is:
                    return string.Format("{0} = '{1}'", _property, _value);
                default:
                    throw new NotImplementedException();
            }
        }

        public string Column
        {
            get
            {
                return _property;
            }
            set
            {
                _property = value;
            }
        }
    }
}
