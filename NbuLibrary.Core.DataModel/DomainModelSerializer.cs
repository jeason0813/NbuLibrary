using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NbuLibrary.Core.DataModel
{
    public class DomainModelSerializer
    {
        private XmlDocument xml;

        public DomainModelSerializer()
        {
        }

        public XmlDocument Serialize(DomainModel dm)
        {
            xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", "yes"));
            var root = xml.CreateElement("Domain");
            xml.AppendChild(root);

            var entities = xml.CreateElement("Entities");
            root.AppendChild(entities);
            foreach (var em in dm.Entities)
            {
                entities.AppendChild(createEntity(em));
            }

            var relations = xml.CreateElement("Relations");
            root.AppendChild(relations);
            foreach (var rm in dm.Relations)
            {
                relations.AppendChild(createRelation(rm));
            }

            return xml;
        }

        private XmlNode createRelation(RelationModel rm)
        {
            var el = createEntity(rm);
            var relation = xml.CreateElement("Relation");
            relation.Attributes.Append(attr("type", rm.Type));
            relation.Attributes.Append(attr("left", rm.Left.Name));
            relation.Attributes.Append(attr("right", rm.Right.Name));
            relation.Attributes.Append(attr("role", rm.Role));
            while (el.HasChildNodes)
                relation.AppendChild(el.FirstChild);

            relation.Attributes.Append(attr("name", rm.Name));
            relation.Attributes.Append(attr("isNomenclature", rm.IsNomenclature));
            return relation;
        }

        private XmlNode createEntity(EntityModel em)
        {
            var entity = xml.CreateElement("Entity");
            entity.Attributes.Append(attr("name", em.Name));
            entity.Attributes.Append(attr("isNomenclature", em.IsNomenclature));

            var properties = xml.CreateElement("Properties");
            entity.AppendChild(properties);
            foreach (var pm in em.Properties)
            {
                properties.AppendChild(createProperty(pm));
            }

            var rules = xml.CreateElement("Rules");
            entity.AppendChild(rules);
            foreach (var rule in em.Rules)
            {
                rules.AppendChild(createRule(rule));
            }

            return entity;
        }

        private XmlNode createRule(EntityRuleModel rule)
        {
            var e = xml.CreateElement("Rule");
            if (rule is RequiredRuleModel)
            {
                e.Attributes.Append(attr("type", "RequiredRuleModel"));
                e.Attributes.Append(attr("property", (rule as RequiredRuleModel).Property.Name));
                return e;
            }
            else if (rule is UniqueRuleModel)
            {
                var ur = rule as UniqueRuleModel;
                e.Attributes.Append(attr("type", "UniqueRuleModel"));
                foreach (var p in ur.Properties)
                {
                    var pe = xml.CreateElement("Property");
                    pe.Attributes.Append(attr("name", p.Name));
                    e.AppendChild(pe);
                }
                return e;
            }
            else if (rule is FutureOrPastDateRuleModel)
            {
                var dr = rule as FutureOrPastDateRuleModel;
                e.Attributes.Append(attr("type", "FutureOrPastDateRuleModel"));
                e.Attributes.Append(attr("property", dr.Property.Name));
                e.Attributes.Append(attr("offset", dr.Offset));
                e.Attributes.Append(attr("future", dr.Future));
                return e;
            }
            else
                throw new NotImplementedException();
        }

        private XmlNode createProperty(PropertyModel pm)
        {
            var p = xml.CreateElement("Property");
            p.Attributes.Append(attr("name", pm.Name));
            p.Attributes.Append(attr("type", pm.Type));
            p.Attributes.Append(attr("default", pm.DefaultValue));
            switch (pm.Type)
            {
                case PropertyType.Boolean:
                    break;
                case PropertyType.Computed:
                    p.Attributes.Append(attr("formula", (pm as ComputedPropertyModel).Formula));
                    break;
                case PropertyType.Datetime:
                    break;
                case PropertyType.Enum:
                    p.Attributes.Append(attr("enumType", (pm as EnumPropertyModel).EnumType.AssemblyQualifiedName));
                    break;
                case PropertyType.Number:
                    p.Attributes.Append(attr("isInteger", (pm as NumberPropertyModel).IsInteger));
                    break;
                case PropertyType.Sequence:
                    var sp = pm as SequencePropertyModel;
                    p.Attributes.Append(attr("sequenceType", sp.SequenceType));
                    p.Attributes.Append(attr("sequenceId", sp.SequenceId));
                    break;
                case PropertyType.String:
                    p.Attributes.Append(attr("length", (pm as StringPropertyModel).Length));
                    break;
                default:
                    throw new NotImplementedException(string.Format("Domain model serialization for property type {0} is not implemented.", pm.Type));
            }

            return p;
        }

        public DomainModel Deserialize(XmlDocument doc)
        {
            xml = doc;
            var entityNodes = xml.SelectNodes("Domain/Entities/Entity");
            DomainModel dm = new DomainModel();
            foreach (XmlNode en in entityNodes)
            {
                if (en is XmlElement)
                    dm.Entities.Add(readEntity(en as XmlElement));
            }

            foreach (XmlNode rn in xml.SelectNodes("Domain/Relations/Relation"))
            {
                if (rn is XmlElement)
                {
                    var rm = readRelation(rn as XmlElement, dm.Entities);
                    dm.Relations.Add(rm);
                    dm.Entities[rm.Left.Name].Relations.Add(rm);
                    dm.Entities[rm.Right.Name].Relations.Add(rm);
                }
            }


            return dm;
        }

        private RelationModel readRelation(XmlElement el, EntityModelsCollection entities)
        {
            var relEntity = readEntity(el);
            var left = entities[el.Attributes["left"].Value];
            var right = entities[el.Attributes["right"].Value];
            var type = (RelationType)Enum.Parse(typeof(RelationType), el.Attributes["type"].Value);
            var role = el.Attributes["role"].Value;
            var rm = new RelationModel(left, right, type, role);
            rm.Properties = relEntity.Properties;
            rm.Rules = relEntity.Rules;

            return rm;
        }

        private EntityModel readEntity(XmlElement el)
        {
            var entity = new EntityModel(el.Attributes["name"].Value);
            entity.IsNomenclature = bool.Parse(el.Attributes["isNomenclature"].Value);
            var properties = el.SelectNodes("Properties/Property");
            foreach (var pe in properties)
            {
                if (pe is XmlElement)
                    entity.Properties.Add(readProperty(pe as XmlElement));
            }

            foreach (var re in el.SelectNodes("Rules/Rule"))
            {
                if (re is XmlElement)
                    entity.Rules.Add(readRule(re as XmlElement, entity));
            }

            return entity;
        }

        private EntityRuleModel readRule(XmlElement el, EntityModel em)
        {
            var type = el.Attributes["type"].Value;
            switch (type)
            {
                case "RequiredRuleModel":
                    return new RequiredRuleModel(em.Properties[el.Attributes["property"].Value]);
                case "UniqueRuleModel":
                    var propNodes = el.SelectNodes("Property");
                    PropertyModel[] pms = new PropertyModel[propNodes.Count];
                    int i = 0;
                    foreach (XmlElement pn in propNodes)
                    {
                        pms[i] = em.Properties[pn.Attributes["name"].Value];
                    }
                    return new UniqueRuleModel(pms);
                case "FutureOrPastDateRuleModel":
                    return new FutureOrPastDateRuleModel(em.Properties[el.Attributes["property"].Value] as DateTimePropertyModel,
                        TimeSpan.Parse(el.Attributes["offset"].Value),
                        bool.Parse(el.Attributes["future"].Value));
                default:
                    throw new NotImplementedException(string.Format("Deserialization of EntityRule of type {0} is not implemented.", type));
            }
        }

        private PropertyModel readProperty(XmlElement el)
        {
            string name = el.Attributes["name"].Value;
            PropertyType type = (PropertyType)Enum.Parse(typeof(PropertyType), el.Attributes["type"].Value);
            switch (type)
            {
                case PropertyType.Boolean:
                    bool bdef = false;
                    if (bool.TryParse(el.Attributes["default"].Value, out bdef))
                        return new BooleanPropertyModel() { Name = name, DefaultValue = bdef };
                    else
                        return new BooleanPropertyModel() { Name = name };
                case PropertyType.Computed:
                    return new ComputedPropertyModel()
                    {
                        Name = name,
                        Formula = el.Attributes["formula"].Value
                    };
                case PropertyType.Datetime:
                    return new DateTimePropertyModel() { Name = name };
                case PropertyType.Enum:
                    return new EnumPropertyModel() { Name = name, EnumType = Type.GetType(el.Attributes["enumType"].Value) };
                    break;
                case PropertyType.Number:
                    bool isInteger = bool.Parse(el.Attributes["isInteger"].Value);
                    return new NumberPropertyModel() { Name = name, IsInteger = isInteger };
                case PropertyType.Sequence:
                    SequenceType sequenceType = (SequenceType)Enum.Parse(typeof(SequenceType), el.Attributes["sequenceType"].Value);
                    string sequenceId = el.Attributes["sequenceId"] != null ? el.Attributes["sequenceId"].Value : null;
                    return new SequencePropertyModel() { Name = name, SequenceType = sequenceType, SequenceId = sequenceId };
                case PropertyType.String:
                    var length = int.Parse(el.Attributes["length"].Value);
                    return new StringPropertyModel() { Name = name, Length = length };
                default:
                    throw new NotImplementedException(string.Format("Domain model deserialization for property type {0} is not implemented.", type));
            }
        }

        #region Helpers

        private XmlAttribute attr(string name, object value)
        {
            var a = xml.CreateAttribute(name);
            a.Value = string.Format("{0}", value);
            return a;
        }

        #endregion
    }
}
