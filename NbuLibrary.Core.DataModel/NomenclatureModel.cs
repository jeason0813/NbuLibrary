using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NbuLibrary.Core.DataModel
{
    public class NomenclatureModel : EntityModel
    {
        private NomenclatureModel()
            : base()
        {
            this.Properties.Add(new SequencePropertyModel() { Name = "Id", SequenceType = SequenceType.Identity });
            this.Properties.Add(new StringPropertyModel() { Name = "Value", Length = 512 });
            this.Properties.Add(new NumberPropertyModel() { Name = "DisplayOrder", IsInteger = true });
        }

        public NomenclatureModel(string name)
            : this()
        {
            Name = name;
            IsNomenclature = true;
        }
        public PropertyModel Id
        {
            get
            {
                return Properties["Id"];
            }
        }
        public PropertyModel Value
        {
            get
            {
                return Properties["Value"];
            }
        }
        public PropertyModel DisplayOrder
        {
            get
            {
                return Properties["DisplayOrder"];
            }
        }
    }
}
