using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using Ninject;
using NbuLibrary.Core.Sql;
using System.Data;

namespace NbuLibrary.Core.EntityLogic
{
    public class EntityValueMapper
    {
        public static SqlDbType GetDbType(PropertyTypes type)
        {
            switch (type)
            {
                case PropertyTypes.Boolean:
                    return SqlDbType.Bit;
                case PropertyTypes.Date:
                    return SqlDbType.DateTime;
                case PropertyTypes.EnumValue:
                    return SqlDbType.TinyInt;
                case PropertyTypes.Integer:
                    return SqlDbType.Int;
                case PropertyTypes.Number:
                    return SqlDbType.Decimal;
                case PropertyTypes.String:
                    return SqlDbType.NVarChar;
                case PropertyTypes.Computed:
                    return SqlDbType.NVarChar;
                default:
                    throw new NotImplementedException(string.Format("Type {0} is not reconginzed!", type));
            }
        }
    }

    public class EntityDefinitionServiceImpl : IEntityDefinitionService
    {
        private DefinitionRegistry _registry;

        protected DefinitionRegistry Registry
        {
            get
            {
                if (_registry == null)
                    LoadRegistry();
                return _registry;
            }
        }

        public void Ensure(IEnumerable<EntityDefinition> definitions, IEnumerable<EntityRelation> relations)
        {
            foreach (var def in definitions)
            {
                EntityDefinition existing = Registry.GetDefintionByName(def.Name);
                if (existing != null)
                {
                    //Upgrade
                    foreach (var prop in def.Properties)
                    {
                        var existingProp = existing.Properties.Find(p => p.Name == prop.Name);
                        if (existingProp == null) //added
                            existing.Properties.Add(prop);
                        else //change
                        {
                            existing.Properties.Remove(existingProp);
                            existing.Properties.Add(prop);
                        }
                    }
                }
                else
                {
                    //Create
                    //TODO: Think about adding the id prop to definitions
                    //var idProp = def.Properties.Find(prop => prop.Name.Equals("Id", StringComparison.InvariantCultureIgnoreCase));
                    //if (idProp == null)
                    //{
                    //    idProp = new PropertyDefinition("Id", PropertyTypes.Integer, nullable: false);
                    //}
                    Registry.AddDefinition(def);
                }
            }

            if (relations != null)
            {
                foreach (var relation in relations)
                {
                    var existing = Registry.GetRelation(relation.LeftEntity, relation.Role);
                    if (existing != null && relation.Type != existing.Type)
                        throw new NotImplementedException("Upgrade of relations is not implemented yet!");
                    else if (existing == null) //new relation
                        Registry.AddRelation(relation);
                }
            }

            List<StoredProcedure> procs = new List<StoredProcedure>();
            var tables = new List<Table>();
            foreach (var def in Registry.GetAllDefinitions())
            {
                var table = new Table(def.Name);
                foreach (var prop in def.Properties)
                {
                    var length = 0;
                    string computedDefinition = null;
                    if (prop is ComputedProperty)
                        computedDefinition = ((ComputedProperty)prop).Format;

                    if (prop is StringProperty)
                        length = ((StringProperty)prop).Length;

                    table.Columns.Add(new Column(prop.Name, EntityValueMapper.GetDbType(prop.Type), length, prop.Nullable, computed: computedDefinition));
                    if (prop.Unique)
                        table.Constraints.Add(new Sql.Constraint(string.Format("UK_{0}_{1}", def.Name, prop.Name), Sql.Constraint.UNIQUE, prop.Name));

                    //if(prop.HasDefault)
                    //{
                    //    string defValue = null;
                    //    if (prop.Type == PropertyTypes.Boolean)
                    //    {
                    //        defValue = bool.Parse(prop.DefaultValue) ? "1" : "0";
                    //    }
                    //    else
                    //        throw new NotImplementedException(); //TODO: Default constraint!

                    //    //table.Constraints.Add(new Sql.DefaultConstraint(string.Format("DFLT_{0}_{1}", def.Name, prop.Name), prop.Name, defValue));
                    //}
                }

                var idCol = new Column("Id", EntityValueMapper.GetDbType(PropertyTypes.Integer), nullable: false, identity: true);
                table.Columns.Add(idCol);
                table.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", def.Name), Sql.Constraint.PRIMARY_KEY, idCol.Name));
                tables.Add(table);
                procs.AddRange(BuildProcs(def));
            }

            foreach (var relation in Registry.GetAllRelations())
            {
                var table = new Table(string.Format("{0}_{1}_{2}", relation.LeftEntity, relation.RightEntity, relation.Role));
                table.Columns.Add(new Column("Id", SqlDbType.Int, nullable: false, identity: true));
                table.Columns.Add(new Column("LID", SqlDbType.Int, nullable: false));
                table.Columns.Add(new Column("RID", SqlDbType.Int, nullable: false));

                switch (relation.Type)
                {
                    case RelationTypes.OneToOne:
                        table.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", table.Name), Sql.Constraint.PRIMARY_KEY, "LID", "RID"));
                        break;
                    case RelationTypes.OneToMany:
                        table.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", table.Name), Sql.Constraint.PRIMARY_KEY, "RID"));
                        break;
                    case RelationTypes.ManyToOne:
                        table.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", table.Name), Sql.Constraint.PRIMARY_KEY, "LID"));
                        break;
                    case RelationTypes.ManyToMany:
                        table.Constraints.Add(new Sql.Constraint(string.Format("PK_{0}", table.Name), Sql.Constraint.PRIMARY_KEY, "Id"));
                        break;
                    default:
                        throw new NotImplementedException("Unknown RelationType.");
                }

                table.Constraints.Add(new Sql.ForeignKeyConstraint(string.Format("FK_{0}_{1}", table.Name, relation.LeftEntity), new string[] { "LID" }, relation.LeftEntity, new string[] { "Id" }));
                table.Constraints.Add(new Sql.ForeignKeyConstraint(string.Format("FK_{0}_{1}", table.Name, relation.RightEntity), new string[] { "RID" }, relation.RightEntity, new string[] { "Id" }));

                tables.Add(table);
            }


            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
            {
                conn.Open();
                //var tran = conn.BeginTransaction();
                DatabaseManager du = new DatabaseManager(conn);
                du.Merge(tables, procs);

                //tran.Commit();
            }
            SaveRegistry();
        }

        public EntityDefinition GetDefinitionByName(string entityName)
        {
            return Registry.GetDefintionByName(entityName);
        }

        public EntityRelation GetRelation(string entity, string role)
        {
            return Registry.GetRelation(entity, role);
        }

        public IEnumerable<EntityDefinition> GetAll()
        {
            return Registry.GetAllDefinitions();
        }

        #region Helpers

        private IEnumerable<StoredProcedure> BuildProcs(EntityDefinition def)
        {
            #region Build Create Proc

            StringBuilder create = new StringBuilder();
            create.AppendFormat("CREATE PROC [{0}_Create] \n", def.Name);
            create.Append("@Id INT OUTPUT ");

            def.Properties.ForEach(prop =>
            {
                if (prop.Type == PropertyTypes.Computed)
                    return;

                var size = prop is StringProperty ? ((StringProperty)prop).Length : -1;
                if (prop is StringProperty)
                {
                    create.AppendFormat("\n,@{0} {1}({2}) ", prop.Name, SqlDbType.NVarChar, ((StringProperty)prop).Length);
                }
                else
                    create.AppendFormat("\n,@{0} {1} ", prop.Name, EntityValueMapper.GetDbType(prop.Type));
                //parameters[idx++] = new ParameterInfo(prop.Name, prop.Type, size);
            });

            create.Append("\nAS\n");

            create.AppendFormat("INSERT INTO [{0}] \n(", def.Name);
            for (int i = 0; i < def.Properties.Count; i++)
            {
                if (def.Properties[i].Type != PropertyTypes.Computed)
                {
                    create.AppendFormat("[{0}]", def.Properties[i].Name);
                }

                if (i < def.Properties.Count - 1 && def.Properties[i + 1].Type != PropertyTypes.Computed)
                    create.Append(",\n");
            }
            create.Append(") VALUES \n (");

            for (int i = 0; i < def.Properties.Count; i++)
            {
                //Skip computed since they are not inserted
                if (def.Properties[i].Type != PropertyTypes.Computed)
                {
                    create.AppendFormat("@{0}", def.Properties[i].Name);
                }

                if (i < def.Properties.Count - 1 && def.Properties[i+1].Type != PropertyTypes.Computed)
                    create.Append(",\n");
            }
            create.Append(")\n");

            create.AppendFormat("\n SET @Id = SCOPE_IDENTITY();");

            #endregion

            #region Build Update Proc

            StringBuilder update = new StringBuilder();
            update.AppendFormat("CREATE PROC [{0}_Update] \n", def.Name);
            update.Append("@Id INT ");

            def.Properties.ForEach(prop =>
            {
                if (prop.Type == PropertyTypes.Computed)
                    return;

                var size = prop is StringProperty ? ((StringProperty)prop).Length : -1;
                if (prop is StringProperty)
                {
                    update.AppendFormat("\n,@{0} {1}({2}) ", prop.Name, SqlDbType.NVarChar, ((StringProperty)prop).Length);
                }
                else
                    update.AppendFormat("\n,@{0} {1} ", prop.Name, EntityValueMapper.GetDbType(prop.Type));
            });

            update.Append("\n AS \n");

            update.AppendFormat("UPDATE [{0}] \nSET \n", def.Name);
            for (int i = 0; i < def.Properties.Count; i++)
            {
                if (def.Properties[i].Type != PropertyTypes.Computed)
                {
                    update.AppendFormat("[{0}] = @{0}", def.Properties[i].Name);
                }

                //TODO: ugly code
                if (i < def.Properties.Count - 1 && def.Properties[i+1].Type != PropertyTypes.Computed)
                    update.Append(",\n");
            }
            update.AppendFormat("\nWHERE [Id] = @Id \n");

            #endregion

            #region Build Read Proc

            StringBuilder read = new StringBuilder();
            read.AppendFormat("CREATE PROC [{0}_Read] \n", def.Name);
            read.Append("@Id INT ");
            read.Append("\n AS \n");
            read.AppendFormat("SELECT * FROM [{0}] WHERE [Id] = @Id", def.Name);

            #endregion

            #region Build Delete Proc

            StringBuilder delete = new StringBuilder();
            delete.AppendFormat("CREATE PROC [{0}_Delete] \n", def.Name);
            delete.Append("@Id INT ");
            delete.Append("\n AS \n");
            delete.AppendFormat("DELETE FROM [{0}] WHERE Id = @Id", def.Name);

            #endregion

            return new StoredProcedure[]{ 
                new StoredProcedure(string.Format("{0}_Create", def.Name), create.ToString()),
                new StoredProcedure(string.Format("{0}_Read", def.Name), read.ToString()),
                new StoredProcedure(string.Format("{0}_Update", def.Name), update.ToString()),
                new StoredProcedure(string.Format("{0}_Delete", def.Name), delete.ToString())
            };
        }

        private void LoadRegistry()
        {
            var filepath = GetFilePath();
            _registry = new DefinitionRegistry();

            if (File.Exists(filepath))
            {
                XmlSerializer ser = new XmlSerializer(typeof(DefinitionsRoot));
                DefinitionsRoot root;
                using (var fs = File.OpenRead(filepath))
                    root = (DefinitionsRoot)ser.Deserialize(fs);

                root.Definitions.ForEach(def => _registry.AddDefinition(def));
                root.Relations.ForEach(rel => _registry.AddRelation(rel));
            }
        }

        private void SaveRegistry()
        {
            XmlSerializer ser = new XmlSerializer(typeof(DefinitionsRoot));
            var root = new DefinitionsRoot()
            {
                Definitions = _registry.GetAllDefinitions().ToList(),
                Relations = _registry.GetAllRelations().ToList()
            };

            var filepath = GetFilePath();
            using (var fs = File.Open(filepath, File.Exists(filepath) ? FileMode.Truncate : FileMode.Create, FileAccess.Write))
                ser.Serialize(fs, root);
        }

        private string GetFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "definition_registry.config");
        }

        #endregion
    }

    public class DefinitionRegistry
    {
        protected Dictionary<string, EntityDefinition> Definitions { get; set; }
        protected Dictionary<string, EntityRelation> Relations { get; set; }

        protected Dictionary<string, string> RelationsKeyHash { get; set; }

        public DefinitionRegistry()
        {
            Definitions = new Dictionary<string, EntityDefinition>();
            Relations = new Dictionary<string, EntityRelation>();
            RelationsKeyHash = new Dictionary<string, string>();
        }

        public void AddDefinition(EntityDefinition definition)
        {
            Definitions.Add(definition.Name, definition);
        }

        public void RemoveDefinition(EntityDefinition definition)
        {
            Definitions.Remove(definition.Name);
        }

        public EntityDefinition GetDefintionByName(string name)
        {
            EntityDefinition def = null;
            if (Definitions.TryGetValue(name, out def))
                return def;
            else
                return null;
        }

        public IEnumerable<EntityDefinition> GetAllDefinitions()
        {
            return Definitions.Values;
        }

        public void AddRelation(EntityRelation relation)
        {
            var key = GetRelationKey(relation.LeftEntity, relation.RightEntity, relation.Role);
            Relations.Add(key, relation);
            Definitions[relation.LeftEntity].Relations.Add(relation);
            Definitions[relation.RightEntity].Relations.Add(relation);

            RelationsKeyHash.Add(string.Format("{0}_{1}", relation.LeftEntity, relation.Role), key);
            RelationsKeyHash.Add(string.Format("{0}_{1}", relation.RightEntity, relation.Role), key);
        }

        public void RemoveRelation(EntityRelation relation)
        {
            var key = GetRelationKey(relation.LeftEntity, relation.RightEntity, relation.Role);
            Relations.Remove(key);
            Definitions[relation.LeftEntity].Relations.Remove(relation);
            Definitions[relation.RightEntity].Relations.Remove(relation);
            RelationsKeyHash.Remove(string.Format("{0}_{1}", relation.LeftEntity, relation.Role));
            RelationsKeyHash.Remove(string.Format("{0}_{1}", relation.RightEntity, relation.Role));
        }

        public EntityRelation GetRelation(string entity, string role)
        {
            string relKey = null;
            if (!RelationsKeyHash.TryGetValue(string.Format("{0}_{1}", entity, role), out relKey))
                return null;

            EntityRelation relation = null;
            if (Relations.TryGetValue(relKey, out relation))
                return relation;
            else
                return null;
        }

        public IEnumerable<EntityRelation> GetRelationsFor(string entityName)
        {
            return Relations.Values.Where(rel => rel.LeftEntity == entityName || rel.RightEntity == entityName);
        }

        public IEnumerable<EntityRelation> GetAllRelations()
        {
            return Relations.Values;
        }

        public static string GetRelationKey(string entity1, string entity2, string role)
        {
            if (entity1.CompareTo(entity2) > 0)
            {
                var tmp = entity1;
                entity1 = entity2;
                entity2 = tmp;
            }

            return string.Format("{0}_{1}_{2}", entity1, entity2, role);
        }
    }
}
