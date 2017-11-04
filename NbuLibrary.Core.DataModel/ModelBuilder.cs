using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.DataModel
{
    public class ModelBuilder
    {
        private EntityModel _model;

        public ModelBuilder(EntityModel model)
        {
            _model = model;
            Rules = new RulesBuilder(_model);
        }

        public ModelBuilder(string modelName)
            : this(new EntityModel(modelName))
        {
        }

        public RulesBuilder Rules { get; set; }

        public EntityModel EntityModel
        {
            get { return _model; }
        }

        public void AddIdentity(string name)
        {
            EntityModel.Properties.Add(new SequencePropertyModel() { Name = name, SequenceType = SequenceType.Identity, SequenceId = EntityModel.Name });
        }

        public void AddString(string name, int length, string defaultValue = null)
        {
            EntityModel.Properties.Add(new StringPropertyModel()
            {
                Name = name,
                Length = length,
                DefaultValue = defaultValue
            });
        }

        public void AddStringMax(string name, string defaultValue = null)
        {
            EntityModel.Properties.Add(new StringPropertyModel()
            {
                Name = name,
                Length = 4000,
                DefaultValue = defaultValue
            });
        }

        public void AddInteger(string name, int? defaultValue = null)
        {
            EntityModel.Properties.Add(new NumberPropertyModel()
            {
                Name = name,
                IsInteger = true,
                DefaultValue = defaultValue
            });
        }

        public void AddDecimal(string name, decimal? defaultValue = null)
        {
            EntityModel.Properties.Add(new NumberPropertyModel()
            {
                Name = name,
                IsInteger = false,
                DefaultValue = defaultValue
            });
        }

        public void AddBoolean(string name, bool? defaultValue = null)
        {
            EntityModel.Properties.Add(new BooleanPropertyModel()
            {
                Name = name,
                DefaultValue = defaultValue
            });
        }

        public void AddEnum<T>(string name)
        {
            var p = new EnumPropertyModel() { Name = name, EnumType = typeof(T) };
            _model.Properties.Add(p);
        }

        public void AddEnum<T>(string name, T defaultValue)
        {
            var p = new EnumPropertyModel() { Name = name, EnumType = typeof(T), DefaultValue = defaultValue };
            _model.Properties.Add(p);
        }

        public void AddDateTime(string name)
        {
            EntityModel.Properties.Add(new DateTimePropertyModel()
            {
                Name = name
            });
        }

        public void AddComputed(string name, string formula)
        {
            EntityModel.Properties.Add(new ComputedPropertyModel()
            {
                Name = name,
                Formula = formula
            });
        }

        public void AddUri(string name, string sequence)
        {
            EntityModel.Properties.Add(new SequencePropertyModel()
            {
                Name = name,
                SequenceType = SequenceType.Uri,
                SequenceId = sequence
            });
            Rules.AddUnique(name);
        }

        public void AddGuid(string name)
        {
            EntityModel.Properties.Add(new SequencePropertyModel() { Name = name, SequenceType = SequenceType.Guid, SequenceId = SequenceType.Guid.ToString() });
        }

        public RelationModel AddRelationTo(EntityModel em, RelationType type, string role)
        {
            var rel = new RelationModel(_model, em, type, role);
            _model.Relations.Add(rel);
            em.Relations.Add(rel);
            return rel;
        }
    }

    public class RulesBuilder
    {
        public RulesBuilder(EntityModel model)
        {
            EntityModel = model;
        }

        public EntityModel EntityModel { get; private set; }

        public void AddUnique(params string[] properties)
        {
            var rule = new UniqueRuleModel(properties.Select(p => EntityModel.Properties[p]).ToArray());
            if (!EntityModel.Rules.Contains(rule))
                EntityModel.Rules.Add(rule);
        }

        public void AddRequired(string property)
        {
            var rule = new RequiredRuleModel(EntityModel.Properties[property]);
            if (!EntityModel.Rules.Contains(rule))
                EntityModel.Rules.Add(rule);
        }

        public void AddFutureDate(string property, TimeSpan offset)
        {
            var prop = EntityModel.Properties[property] as DateTimePropertyModel;
            if(prop == null)
                throw new ArgumentException("FutureDate entity rule can be applied only on DateTimePropertyModels");
            var rule = new FutureOrPastDateRuleModel(prop, offset, true);
            if (!EntityModel.Rules.Contains(rule))
                EntityModel.Rules.Add(rule);
        }

        public void AddPastDate(string property, TimeSpan offset)
        {
            var prop = EntityModel.Properties[property] as DateTimePropertyModel;
            if (prop == null)
                throw new ArgumentException("PastDate entity rule can be applied only on DateTimePropertyModels");
            var rule = new FutureOrPastDateRuleModel(prop, offset, false);
            if (!EntityModel.Rules.Contains(rule))
                EntityModel.Rules.Add(rule);
        }
    }
}
