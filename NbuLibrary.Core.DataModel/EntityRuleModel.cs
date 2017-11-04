using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{
    public enum EntityRuleType
    {
        Required,
        Unique,
        FutureOrPastDate,
    }

    public abstract class EntityRuleModel
    {
        public abstract string Identifier { get; }
        public abstract EntityRuleType Type { get; }
        public abstract bool AppliesFor(PropertyModel property);
    }

    public class UniqueRuleModel : EntityRuleModel
    {
        private string _identifier;

        public UniqueRuleModel(params PropertyModel[] properties)
        {
            Properties = properties;
            Array.Sort(properties);
            StringBuilder id = new StringBuilder();
            id.AppendFormat("unique::");
            foreach (var p in properties)
                id.AppendFormat("{0}&|&", p.Name.ToLower());
            _identifier = id.ToString();
        }
        public IEnumerable<PropertyModel> Properties { get; private set; }
        public override EntityRuleType Type
        {
            get { return EntityRuleType.Unique; }
        }

        public override string Identifier
        {
            get { return _identifier; }
        }

        public override bool AppliesFor(PropertyModel property)
        {
            return Properties.First(p => p.Name.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)) != null;
        }
    }

    public class RequiredRuleModel : EntityRuleModel
    {
        public RequiredRuleModel(PropertyModel property)
        {
            Property = property;
        }

        public PropertyModel Property { get; set; }
        public override EntityRuleType Type
        {
            get { return EntityRuleType.Required; }
        }

        public override string Identifier
        {
            get { return string.Format("required::{0}", Property.Name.ToLower()); }
        }

        public override bool AppliesFor(PropertyModel property)
        {
            return property.Name.Equals(Property.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    /// <summary>
    /// EntityRule for date that enforces either a future or past date based on the Future flag. The value of the property for that rule must be an Offset away from the current date (after or before).
    /// </summary>
    public class FutureOrPastDateRuleModel : EntityRuleModel
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="property">The property for which the rule will be enforced.</param>
        /// <param name="offset">The time offset from current date.</param>
        /// <param name="future">Flag that switches between the two modes of the rule: future dates if true, past dates if false.</param>
        public FutureOrPastDateRuleModel(DateTimePropertyModel property, TimeSpan offset, bool future = false)
        {
            Property = property;
            Offset = offset;
            Future = future;
        }

        public DateTimePropertyModel Property { get; set; }
        public TimeSpan Offset { get; set; }
        public bool Future { get; set; }

        public override EntityRuleType Type
        {
            get { return EntityRuleType.FutureOrPastDate; }
        }

        public override string Identifier
        {
            get { return string.Format("futureorpast::{0}", Property.Name.ToLower()); }
        }

        public override bool AppliesFor(PropertyModel property)
        {
            return property.Name.Equals(Property.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    //public class DateRangeRuleModel : EntityRuleModel
    //{
    //    public DateRangeRuleModel(DateTimePropertyModel property, DateTime from)
    //    {

    //    }


    //    public DateRangeRuleModel(DateTimePropertyModel property, DateTime to)
    //    {

    //    }


    //    public DateRangeRuleModel(DateTimePropertyModel property, DateTime from, DateTime to)
    //    {

    //    }

    //}


}
