using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Sql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace NbuLibrary.Core.Infrastructure
{
    public class DomainModelService : IDomainModelService
    {
        private DomainModel _domain;
        private IDatabaseService _dbService;
        private IEnumerable<IDomainChangeListener> _changeListeners;

        public DomainModelService(IDatabaseService dbService, IEnumerable<IDomainChangeListener> changeListeners = null)
        {
            _dbService = dbService;
            _changeListeners = changeListeners ?? new IDomainChangeListener[0];
        }

        public DomainModel Domain
        {
            get
            {
                if (_domain == null)
                    _domain = LoadDomain();
                return _domain;
            }
        }

        public void Save(DomainModel dm)
        {
            //ensure relations
            foreach (var em in dm.Entities)
            {
                foreach (var rel in em.Relations)
                {
                    if (!dm.Relations.Contains(rel.Name))
                        dm.Relations.Add(rel);
                }
            }

            List<Table> tables = new List<Table>();
            foreach (var entity in dm.Entities)
            {
                foreach (var listener in _changeListeners)
                    listener.BeforeSave(entity);

                Table tbl = new Table(entity.Name);
                foreach (var prop in entity.Properties)
                {
                    if (prop is StringPropertyModel)
                        tbl.Columns.Add(CreateStringColumn(prop as StringPropertyModel));
                    else if (prop is NumberPropertyModel)
                        tbl.Columns.Add(CreateNumberPropertyColumn(prop as NumberPropertyModel));
                    else if (prop is BooleanPropertyModel)
                        tbl.Columns.Add(CreateBooleanPropertyModel(prop as BooleanPropertyModel));
                    else if (prop is EnumPropertyModel)
                        tbl.Columns.Add(CreateEnumPropertyColumn(prop as EnumPropertyModel));
                    else if (prop is SequencePropertyModel)
                        tbl.Columns.Add(CreateSequencePropertyColumn(prop as SequencePropertyModel));
                    else if (prop is ComputedPropertyModel)
                        tbl.Columns.Add(CreateComputedPropertyColumn(prop as ComputedPropertyModel));
                    else if (prop is DateTimePropertyModel)
                        tbl.Columns.Add(CreateDateTimePropertyColumn(prop as DateTimePropertyModel));

                    if (prop.DefaultValue != null)
                    {
                        tbl.Constraints.Add(new DefaultConstraint(entity.Name, prop.Name, GetDefaultValueForProperty(prop)));
                    }
                }

                foreach (var rule in entity.Rules)
                {
                    if (rule is RequiredRuleModel)
                    {
                        var rr = rule as RequiredRuleModel;
                        var col = tbl.Columns.Find(c => c.Name.Equals(rr.Property.Name, StringComparison.InvariantCultureIgnoreCase));
                        col.IsNullable = false;
                    }
                    else if (rule is UniqueRuleModel)
                    {
                        var ur = rule as UniqueRuleModel;
                        tbl.Constraints.Add(new UniqueConstraint(tbl.Name, ur.Properties.Select(p => p.Name).ToArray()));
                    }
                    else if (rule is FutureOrPastDateRuleModel)
                    {
                        continue;
                    }
                    else
                        throw new NotImplementedException();
                }

                tbl.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", tbl.Name), Sql.Constraint.PRIMARY_KEY, "Id"));
                tables.Add(tbl);
            }

            foreach (var rel in dm.Relations)
            {
                Table tbl = new Table(rel.Name);
                //TODO
                ModelBuilder mb = new ModelBuilder(rel);
                if (!rel.Properties.Contains("id"))
                {
                    mb.AddIdentity("id");
                }
                if (!rel.Properties.Contains("lid"))
                {
                    mb.AddInteger("lid");
                    mb.Rules.AddRequired("lid");
                }
                if (!rel.Properties.Contains("rid"))
                {
                    mb.AddInteger("rid");
                    mb.Rules.AddRequired("rid");
                }

                if (rel.Type == RelationType.OneToOne)
                {
                    mb.Rules.AddUnique("lid");
                    mb.Rules.AddUnique("rid");
                }

                foreach (var prop in rel.Properties)
                {
                    if (prop is StringPropertyModel)
                        tbl.Columns.Add(CreateStringColumn(prop as StringPropertyModel));
                    else if (prop is NumberPropertyModel)
                        tbl.Columns.Add(CreateNumberPropertyColumn(prop as NumberPropertyModel));
                    else if (prop is BooleanPropertyModel)
                        tbl.Columns.Add(CreateBooleanPropertyModel(prop as BooleanPropertyModel));
                    else if (prop is EnumPropertyModel)
                        tbl.Columns.Add(CreateEnumPropertyColumn(prop as EnumPropertyModel));
                    else if (prop is SequencePropertyModel)
                        tbl.Columns.Add(CreateSequencePropertyColumn(prop as SequencePropertyModel));
                    else if (prop is ComputedPropertyModel)
                        tbl.Columns.Add(CreateComputedPropertyColumn(prop as ComputedPropertyModel));
                    else if (prop is DateTimePropertyModel)
                        tbl.Columns.Add(CreateDateTimePropertyColumn(prop as DateTimePropertyModel));

                    if (prop.DefaultValue != null)
                    {
                        tbl.Constraints.Add(new DefaultConstraint(rel.Name, prop.Name, GetDefaultValueForProperty(prop)));
                    }
                }

                foreach (var rule in rel.Rules)
                {
                    if (rule is RequiredRuleModel)
                    {
                        var rr = rule as RequiredRuleModel;
                        var col = tbl.Columns.Find(c => c.Name.Equals(rr.Property.Name, StringComparison.InvariantCultureIgnoreCase));
                        col.IsNullable = false;
                    }
                    else if (rule is UniqueRuleModel)
                    {
                        var ur = rule as UniqueRuleModel;
                        tbl.Constraints.Add(new UniqueConstraint(rel.Name, ur.Properties.Select(p => p.Name).ToArray()));
                    }
                    else
                        throw new NotImplementedException();
                }

                switch (rel.Type)
                {
                    case RelationType.OneToOne:
                        tbl.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", tbl.Name), Sql.Constraint.PRIMARY_KEY, "LID", "RID"));
                        break;
                    case RelationType.OneToMany:
                        tbl.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", tbl.Name), Sql.Constraint.PRIMARY_KEY, "RID"));
                        break;
                    case RelationType.ManyToOne:
                        tbl.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", tbl.Name), Sql.Constraint.PRIMARY_KEY, "LID"));
                        break;
                    case RelationType.ManyToMany:
                        tbl.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", tbl.Name), Sql.Constraint.PRIMARY_KEY, "LID", "RID"));
                        break;
                    default:
                        throw new NotImplementedException("Unknown RelationType.");
                }

                tbl.Constraints.Add(new Sql.ForeignKeyConstraint(string.Format("FK_{0}_{1}", tbl.Name, rel.Left.Name), new string[] { "LID" }, rel.Left.Name, new string[] { "Id" }));
                tbl.Constraints.Add(new Sql.ForeignKeyConstraint(string.Format("FK_{0}_{1}", tbl.Name, rel.Right.Name), new string[] { "RID" }, rel.Right.Name, new string[] { "Id" }));

                tables.Add(tbl);
            }

            using (SqlConnection conn = _dbService.GetSqlConnection())
            {
                conn.Open();
                //TODO: transaction
                //var tran = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
                DatabaseManager mgr = new DatabaseManager(conn);
                mgr.Merge(tables, null);
                //tran.Commit();
            }

            DomainModelSerializer ser = new DomainModelSerializer();
            ser.Serialize(dm).Save(GetXmlPath());
            _domain = null;
        }

        public DomainModelChanges CompareWithExisting(DomainModel dm)
        {
            //ensure relations
            foreach (var em in dm.Entities)
            {
                foreach (var rel in em.Relations)
                {
                    if (!dm.Relations.Contains(rel.Name))
                        dm.Relations.Add(rel);
                }
            }

            DomainModelChanges result = new DomainModelChanges();
            var entityChanges = new List<EntityModelChange>();
            foreach (var ex in Domain.Entities)
            {
                if (!dm.Entities.Contains(ex.Name))
                    entityChanges.Add(new EntityModelChange(ChangeType.Deleted, ex, null));
                else
                {
                    EntityModelChange change = null;
                    var em = dm.Entities[ex.Name];
                    foreach (var listener in _changeListeners)
                        listener.BeforeSave(em);
                    if (TryGetChanges<EntityModelChange>(ex, em, out change))
                        entityChanges.Add(change);
                }
            }

            foreach (var en in dm.Entities)
            {
                if (!Domain.Entities.Contains(en.Name))
                {
                    foreach (var listener in _changeListeners)
                        listener.BeforeSave(en);

                    var ec = new EntityModelChange(ChangeType.Created, null, en);
                    var propChanges = new List<ModelChange<PropertyModel>>();
                    foreach (var pm in en.Properties)
                        propChanges.Add(new ModelChange<PropertyModel>(ChangeType.Created, null, pm));
                    ec.PropertyChanges = propChanges;
                    entityChanges.Add(ec);
                }
            }
            result.EntityChanges = entityChanges;

            List<RelationModelChange> relChanges = new List<RelationModelChange>();
            foreach (var ex in Domain.Relations)
            {
                if (!dm.Relations.Contains(ex.Name))
                    relChanges.Add(new RelationModelChange(ChangeType.Deleted, ex, null));
                else
                {
                    //TODO: listeners
                    RelationModelChange change = null;
                    if (TryGetChanges<RelationModelChange>(ex, dm.Relations[ex.Name], out change))
                        relChanges.Add(change);
                }
            }

            foreach (var rm in dm.Relations)
            {
                if (!Domain.Relations.Contains(rm.Name))
                {
                    var rc = new RelationModelChange(ChangeType.Created, null, rm);
                    var propChanges = new List<ModelChange<PropertyModel>>();
                    foreach (var pm in rm.Properties)
                        propChanges.Add(new ModelChange<PropertyModel>(ChangeType.Created, null, pm));
                    rc.PropertyChanges = propChanges;
                    relChanges.Add(rc);
                }
            }

            result.RelationChanges = relChanges;

            return result;
        }

        public DomainModel Union(IEnumerable<DomainModel> domains)
        {
            //ensure relations
            foreach (var dm in domains)
            {
                foreach (var em in dm.Entities)
                {
                    foreach (var rel in em.Relations)
                    {
                        if (!dm.Relations.Contains(rel.Name))
                            dm.Relations.Add(rel);
                    }
                }
            }

            DomainModel u = new DomainModel();
            foreach (var dm in domains)
            {
                foreach (var em in dm.Entities)
                {
                    if (u.Entities.Contains(em.Name)) continue;
                    EntityModel ne = new EntityModel(em);
                    foreach (var dm2 in domains)
                    {
                        if (dm2 == dm)
                            continue;

                        var em2 = dm2.Entities[em.Name];
                        if (em2 == null)
                            continue;

                        foreach (var pm in em2.Properties)
                        {
                            var np = ne.Properties[pm.Name];
                            if (np != null) continue;//TODO: properties clash
                            //throw new NotImplementedException("Properties clash!");
                            else
                                ne.Properties.Add(pm);
                        }

                        foreach (var rm in em2.Rules)
                        {
                            if (!ne.Rules.Contains(rm))
                                ne.Rules.Add(rm);
                        }
                    }
                    u.Entities.Add(ne);
                }

                foreach (var relm in dm.Relations)
                {
                    if (u.Relations.Contains(relm.Name)) continue;
                    RelationModel ne = new RelationModel(relm);
                    foreach (var dm2 in domains)
                    {
                        if (dm2 == dm)
                            continue;

                        var relm2 = dm2.Relations[relm.Name];
                        if (relm2 == null)
                            continue;

                        foreach (var pm in relm2.Properties)
                        {
                            var np = ne.Properties[pm.Name];
                            if (np != null)
                                //throw new NotImplementedException("Properties clash!");
                                continue;
                            else
                                ne.Properties.Add(pm);
                        }

                        foreach (var rm in relm2.Rules)
                        {
                            if (!ne.Rules.Contains(rm))
                                ne.Rules.Add(rm);
                        }
                    }
                    u.Relations.Add(ne);
                }
            }

            return u;
        }

        public void Merge(DomainModel dm)
        {
            //ensure relations
            foreach (var em in dm.Entities)
            {
                foreach (var rel in em.Relations)
                {
                    if (!dm.Relations.Contains(rel.Name))
                        dm.Relations.Add(rel);
                }
            }

            dm = Union(new DomainModel[] { Domain, dm });
            var toSave = LoadDomain();
            var dc = CompareWithExisting(dm);
            foreach (var ec in dc.EntityChanges)
            {
                switch (ec.Change)
                {
                    case ChangeType.Created:
                        toSave.Entities.Add(ec.New);
                        break;
                    case ChangeType.Deleted:
                        toSave.Entities.Remove(ec.Old);
                        break;
                    case ChangeType.Modified:
                        MergeEntityModels(toSave, ec);
                        break;
                }
            }
            foreach (var rc in dc.RelationChanges)
            {
                switch (rc.Change)
                {
                    case ChangeType.Created:
                        toSave.Relations.Add(rc.New);
                        break;
                    case ChangeType.Deleted:
                        toSave.Relations.Remove(rc.Old);
                        break;
                    case ChangeType.Modified:
                        MergeRelationModels(toSave, rc);
                        break;
                }
            }

            Save(toSave);
        }

        private void MergeRelationModels(DomainModel dm, RelationModelChange rc)
        {
            var toSave = dm.Relations[rc.Old.Name];
            foreach (var pc in rc.PropertyChanges)
            {
                switch (pc.Change)
                {
                    case ChangeType.Created:
                        toSave.Properties.Add(pc.New);
                        break;
                    case ChangeType.Deleted:
                        toSave.Properties.Remove(pc.Old);
                        break;
                    case ChangeType.Modified:
                        toSave.Properties[pc.New.Name] = pc.New;
                        break;
                }
            }

            foreach (var ruleChange in rc.RuleChanges)
            {
                switch (ruleChange.Change)
                {
                    case ChangeType.Created:
                        toSave.Rules.Add(ruleChange.New);
                        break;
                    case ChangeType.Deleted:
                        toSave.Rules.Remove(ruleChange.Old);
                        break;
                }
            }
        }

        private void MergeEntityModels(DomainModel dm, EntityModelChange ec)
        {
            var toSave = dm.Entities[ec.Old.Name];
            foreach (var pc in ec.PropertyChanges)
            {
                switch (pc.Change)
                {
                    case ChangeType.Created:
                        toSave.Properties.Add(pc.New);
                        break;
                    case ChangeType.Deleted:
                        toSave.Properties.Remove(pc.Old);
                        break;
                    case ChangeType.Modified:
                        toSave.Properties[pc.New.Name] = pc.New;
                        break;
                }
            }

            foreach (var ruleChange in ec.RuleChanges)
            {
                switch (ruleChange.Change)
                {
                    case ChangeType.Created:
                        toSave.Rules.Add(ruleChange.New);
                        break;
                    case ChangeType.Deleted:
                        toSave.Rules.Remove(ruleChange.Old);
                        break;
                }
            }
        }

        //TODO: duplicate code!
        private bool TryGetChanges<T>(EntityModel ex, EntityModel cu, out T change)
        {
            var propChanges = new List<ModelChange<PropertyModel>>();
            foreach (var pm in ex.Properties)
            {
                if (!cu.Properties.Contains(pm.Name))
                    propChanges.Add(new ModelChange<PropertyModel>(ChangeType.Deleted, pm, null));
                else
                {
                    var cp = cu.Properties[pm.Name];
                    if (pm.Type != cp.Type)
                        throw new NotImplementedException("Changing property types it not implemented by the DomainModelService.");

                    bool hasChanges = false;
                    if (string.Format("{0}", pm.DefaultValue) != string.Format("{0}", cp.DefaultValue))
                        hasChanges = true;
                    else if (pm.Type == PropertyType.String && (pm as StringPropertyModel).Length != (cp as StringPropertyModel).Length)
                        hasChanges = true;

                    if (hasChanges)
                        propChanges.Add(new ModelChange<PropertyModel>(ChangeType.Modified, pm, cp));
                }
            }

            foreach (var pm in cu.Properties)
                if (!ex.Properties.Contains(pm.Name))
                    propChanges.Add(new ModelChange<PropertyModel>(ChangeType.Created, null, pm));

            var ruleChanges = new List<ModelChange<EntityRuleModel>>();
            foreach (var rule in ex.Rules)
            {
                if (!cu.Rules.Contains(rule))
                    ruleChanges.Add(new ModelChange<EntityRuleModel>(ChangeType.Deleted, rule, null));
            }

            foreach (var rule in cu.Rules)
            {
                if (!ex.Rules.Contains(rule))
                    ruleChanges.Add(new ModelChange<EntityRuleModel>(ChangeType.Created, null, rule));
            }

            if (propChanges.Count > 0 || ruleChanges.Count > 0)
            {
                if (typeof(T) == typeof(EntityModelChange))
                    change = (T)(object)new EntityModelChange(ChangeType.Modified, ex, cu) { PropertyChanges = propChanges, RuleChanges = ruleChanges };
                else if (typeof(T) == typeof(RelationModelChange))
                    change = (T)(object)new RelationModelChange(ChangeType.Modified, (RelationModel)ex, (RelationModel)cu) { PropertyChanges = propChanges, RuleChanges = ruleChanges };
                else
                    throw new NotImplementedException();
                return true;
            }
            else
            {
                change = default(T);
                return false;
            }
        }

        private DomainModel LoadDomain()
        {
            var path = GetXmlPath();
            if (File.Exists(path))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(path);
                var ser = new DomainModelSerializer();
                return ser.Deserialize(xml);
            }
            else
                return new DomainModel();
        }

        private string GetXmlPath()
        {
            return Path.Combine(_dbService.GetRootPath(), "dm.xml");
        }

        #region Column Helpers

        private Column CreateComputedPropertyColumn(ComputedPropertyModel pm)
        {
            return new Column(pm.Name, System.Data.SqlDbType.NVarChar, computed: pm.Formula);
        }

        private Column CreateDateTimePropertyColumn(DateTimePropertyModel dtPropertyModel)
        {
            return new Column(dtPropertyModel.Name, System.Data.SqlDbType.DateTime);
        }

        private Column CreateSequencePropertyColumn(SequencePropertyModel propModel)
        {
            switch (propModel.SequenceType)
            {
                case SequenceType.Identity:
                    return new Column(propModel.Name, System.Data.SqlDbType.Int, identity: true, nullable: false);
                case SequenceType.Guid:
                    return new Column(propModel.Name, System.Data.SqlDbType.UniqueIdentifier);
                case SequenceType.Uri:
                    return new Column(propModel.Name, System.Data.SqlDbType.NVarChar, 128);
                default:
                    throw new NotImplementedException(string.Format("SequencePropertyModel of type {0} not implemented.", propModel.SequenceType));
            }
        }

        private Column CreateBooleanPropertyModel(BooleanPropertyModel booleanPropertyModel)
        {
            return new Column(booleanPropertyModel.Name, System.Data.SqlDbType.Bit);
        }

        private Column CreateEnumPropertyColumn(EnumPropertyModel enumPropertyModel)
        {
            return new Column(enumPropertyModel.Name, System.Data.SqlDbType.SmallInt);
        }

        private Column CreateNumberPropertyColumn(NumberPropertyModel numberPropertyModel)
        {
            return new Column(numberPropertyModel.Name, numberPropertyModel.IsInteger ? System.Data.SqlDbType.Int : System.Data.SqlDbType.Decimal);
        }

        private Column CreateStringColumn(StringPropertyModel stringPropertyModel)
        {
            return new Column(stringPropertyModel.Name, System.Data.SqlDbType.NVarChar, stringPropertyModel.Length);
        }

        private string GetDefaultValueForProperty(PropertyModel prop)
        {
            switch (prop.Type)
            {
                case PropertyType.Boolean:
                    return (bool)prop.DefaultValue ? "1" : "0";
                case PropertyType.Computed:
                case PropertyType.Sequence:
                    throw new NotSupportedException("Computed properties cannot have default values.");
                case PropertyType.Enum:
                    return ((int)prop.DefaultValue).ToString();
                case PropertyType.Number:
                    var np = prop as NumberPropertyModel;
                    if (np.IsInteger)
                        return prop.DefaultValue.ToString();
                    else
                        return ((decimal)prop.DefaultValue).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                case PropertyType.String:
                    return string.Format("N'{0}'", prop.DefaultValue);
                default:
                    return prop.DefaultValue.ToString();
            }
        }

        #endregion
    }
}
