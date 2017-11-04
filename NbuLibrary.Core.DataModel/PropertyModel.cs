using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{

    public enum PropertyType
    {
        String,
        Number,
        Boolean,
        Enum,
        Sequence,
        Computed,
        Datetime
    }

    public abstract class PropertyModel
    {
        public PropertyModel()
        {

        }

        public PropertyModel(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public object DefaultValue { get; set; }

        public abstract PropertyType Type { get; }

        public bool Is(string name) {
            return Name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        }
        //TODO: is nullable
    }

    public class StringPropertyModel : PropertyModel
    {
        public int Length { get; set; }
        public override PropertyType Type
        {
            get
            {
                return PropertyType.String;
            }
        }
    }

    public class NumberPropertyModel : PropertyModel
    {
        public bool IsInteger { get; set; }
        public override PropertyType Type
        {
            get
            {
                return PropertyType.Number;
            }
        }
    }

    public class BooleanPropertyModel : PropertyModel
    {
        public override PropertyType Type
        {
            get
            {
                return PropertyType.Boolean;
            }
        }
    }

    public class EnumPropertyModel : PropertyModel
    {
        public Type EnumType { get; set; }

        public override PropertyType Type
        {
            get
            {
                return PropertyType.Enum;
            }
        }

    }

    public class SequencePropertyModel : PropertyModel
    {
        public string SequenceId { get; set; }
        public SequenceType SequenceType { get; set; }
        public override PropertyType Type
        {
            get
            {
                return PropertyType.Sequence;
            }
        }
    }

    public class ComputedPropertyModel : PropertyModel
    {

        public string Formula { get; set; }
        public override PropertyType Type
        {
            get
            {
                return PropertyType.Computed;
            }
        }
    }

    public class DateTimePropertyModel : PropertyModel
    {
        public override PropertyType Type
        {
            get
            {
                return PropertyType.Datetime;
            }
        }
    }

    public enum SequenceType
    {
        Identity,
        Guid,
        Uri
    }
}
