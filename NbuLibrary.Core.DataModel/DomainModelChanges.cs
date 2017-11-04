using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.DataModel
{
    public class DomainModelChanges
    {
        public IEnumerable<EntityModelChange> EntityChanges { get; set; }
        public IEnumerable<RelationModelChange> RelationChanges { get; set; }
    }

    public enum ChangeType
    {
        None,
        Created,
        Deleted,
        Modified
    }

    public class ModelChange<T>
    {
        public ModelChange()
        {

        }

        public ModelChange(ChangeType change, T oldModel, T newModel)
        {
            Change = change;
            Old = oldModel;
            New = newModel;
        }

        public ChangeType Change { get; set; }
        public T Old { get; set; }
        public T New { get; set; }
    }

    public class EntityModelChange : ModelChange<EntityModel>
    {
        public EntityModelChange()
        {

        }

        public EntityModelChange(ChangeType change, EntityModel oldModel, EntityModel newModel)
            : base(change, oldModel, newModel)
        {

        }

        public IEnumerable<ModelChange<PropertyModel>> PropertyChanges { get; set; }
        public IEnumerable<ModelChange<EntityRuleModel>> RuleChanges { get; set; }

    }

    public class RelationModelChange : ModelChange<RelationModel>
    {
        public RelationModelChange(ChangeType change, RelationModel oldModel, RelationModel newModel)
            : base(change, oldModel, newModel)
        {

        }
        public IEnumerable<ModelChange<PropertyModel>> PropertyChanges { get; set; }
        public IEnumerable<ModelChange<EntityRuleModel>> RuleChanges { get; set; }

    }
}
