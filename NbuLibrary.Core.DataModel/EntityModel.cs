using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.DataModel
{
    public class EntityModel
    {
        public EntityModel()
        {
            Properties = new PropertyModelsCollection();
            Relations = new RelationModelsCollection();
            Rules = new EntityRulesCollection();
        }

        public EntityModel(string name)
            : this()
        {
            Name = name;
        }

        public EntityModel(EntityModel em) : this(em.Name)
        {
            IsNomenclature = em.IsNomenclature;
            foreach (var pm in em.Properties)
                Properties.Add(pm);
            foreach (var rel in em.Relations)
                Relations.Add(rel);
            foreach (var rule in em.Rules)
                Rules.Add(rule);
        }

        public string Name { get; protected set; }
        public bool IsNomenclature { get; set; }

        public PropertyModelsCollection Properties { get; set; }
        public RelationModelsCollection Relations { get; set; }
        public EntityRulesCollection Rules { get; set; }

        public RelationModel GetRelation(string withEntity, string role)
        {
            return Relations[this.Name, withEntity, role];
        }
    }
}
