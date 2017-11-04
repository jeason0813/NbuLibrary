//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using NbuLibrary.Core.Kernel;
//using NbuLibrary.Core.Services;
//using Ninject;
//using NbuLibrary.Core.Services.tmp;
//using NbuLibrary.Core.Domain;

//namespace NbuLibrary.Core.EntityLogic
//{
//    public class EntityServiceImpl : IEntityService
//    {
//        internal class EntityAdapter : IEntity
//        {
//            private EntityDefinition _definition;

//            private Dictionary<string, object> _data;

//            public EntityAdapter(EntityDefinition definiton)
//            {
//                _definition = definiton;
//            }

//            public int Id
//            {
//                get;
//                set;
//            }

//            public string EntityName
//            {
//                get { return _definition.Name; }
//            }

//            public IDictionary<string, object> GetRawData()
//            {
//                _data["Id"] = Id;
//                return _data;
//            }

//            public void LoadRawData(IDictionary<string, object> raw)
//            {
//                _data = new Dictionary<string, object>(raw);
//                if (_data.ContainsKey("Id"))
//                    Id = Convert.ToInt32(_data["Id"]);
//            }
//        }

//        private IEntityDefinitionService _definitionService;

//        public EntityServiceImpl(IEntityDefinitionService definitionService)
//        {
//            _definitionService = definitionService;
//        }

//        #region Generic

//        public void Create<TEntityType>(TEntityType entity) where TEntityType : Domain.IEntity
//        {
//            var definition = _definitionService.GetDefinitionByName(entity.EntityName);

//            var data = entity.GetRawData();


//            SqlCommand cmd = new SqlCommand(string.Format("{0}_Create", definition.Name));
//            cmd.CommandType = System.Data.CommandType.StoredProcedure;

//            definition.Properties.ForEach(prop =>
//            {
//                if (prop.Type == PropertyTypes.Computed)
//                    return;

//                var param = cmd.CreateParameter();
//                param.ParameterName = prop.Name;
//                param.SqlDbType = GetSqlType(prop.Type);
//                if (data.ContainsKey(prop.Name) && data[prop.Name] != null)
//                    param.Value = data[prop.Name];
//                else
//                    param.Value = DBNull.Value;

//                cmd.Parameters.Add(param);
//            });

//            var idParam = cmd.CreateParameter();
//            idParam.ParameterName = "Id";
//            idParam.SqlDbType = System.Data.SqlDbType.Int;
//            idParam.Direction = System.Data.ParameterDirection.Output;
//            cmd.Parameters.Add(idParam);

//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                cmd.Connection = conn;
//                cmd.ExecuteNonQuery();

//                entity.Id = (int)cmd.Parameters[idParam.ParameterName].Value;
//            }
//        }

//        public TEntityType Read<TEntityType>(int id) where TEntityType : Domain.IEntity
//        {
//            var inst = Activator.CreateInstance<TEntityType>();
//            inst.Id = id;
//            return Read<TEntityType>(inst);
//        }
//        public TEntityType Read<TEntityType>(TEntityType entity) where TEntityType : Domain.IEntity
//        {
//            var definition = _definitionService.GetDefinitionByName(entity.EntityName);

//            SqlCommand cmd = new SqlCommand(string.Format("{0}_Read", definition.Name));
//            cmd.CommandType = CommandType.StoredProcedure;

//            var idParam = cmd.CreateParameter();
//            idParam.ParameterName = "Id";
//            idParam.SqlDbType = SqlDbType.Int;
//            idParam.Value = entity.Id;
//            cmd.Parameters.Add(idParam);

//            Dictionary<string, object> raw = new Dictionary<string, object>();
//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                cmd.Connection = conn;

//                using (var reader = cmd.ExecuteReader())
//                {
//                    if (reader.Read())
//                    {
//                        entity.Id = (int)reader["Id"];
//                        definition.Properties.ForEach(prop =>
//                        {
//                            raw[prop.Name] = reader[prop.Name];
//                        });
//                    }
//                    else
//                        entity = default(TEntityType);
//                }
//            }
//            if (entity != null)
//                entity.LoadRawData(raw);

//            return entity;
//        }
//        public void Update<TEntityType>(TEntityType entity) where TEntityType : Domain.IEntity
//        {
//            var definition = _definitionService.GetDefinitionByName(entity.EntityName);

//            var data = entity.GetRawData();

//            SqlCommand cmd = new SqlCommand(string.Format("{0}_Update", definition.Name));
//            cmd.CommandType = System.Data.CommandType.StoredProcedure;

//            definition.Properties.ForEach(prop =>
//            {
//                var param = cmd.CreateParameter();
//                param.ParameterName = prop.Name;
//                param.SqlDbType = GetSqlType(prop.Type);
//                param.Value = data[prop.Name] ?? DBNull.Value;


//                cmd.Parameters.Add(param);
//            });

//            var idParam = cmd.CreateParameter();
//            idParam.ParameterName = "Id";
//            idParam.SqlDbType = System.Data.SqlDbType.Int;
//            idParam.Value = entity.Id;
//            cmd.Parameters.Add(idParam);

//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                cmd.Connection = conn;
//                cmd.ExecuteNonQuery();
//            }
//        }

//        public void Delete<TEntityType>(int id) where TEntityType : Domain.IEntity
//        {
//            var inst = Activator.CreateInstance<TEntityType>();
//            inst.Id = id;
//            Delete<TEntityType>(inst);
//        }
//        public void Delete<TEntityType>(TEntityType entity) where TEntityType : Domain.IEntity
//        {
//            var definition = _definitionService.GetDefinitionByName(entity.EntityName);

//            SqlCommand cmd = new SqlCommand(string.Format("{0}_Delete", definition.Name));
//            cmd.CommandType = CommandType.StoredProcedure;

//            var idParam = cmd.CreateParameter();
//            idParam.ParameterName = "Id";
//            idParam.SqlDbType = SqlDbType.Int;
//            idParam.Value = entity.Id;
//            cmd.Parameters.Add(idParam);

//            try
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//                {
//                    conn.Open();
//                    cmd.Connection = conn;
//                    cmd.ExecuteNonQuery();
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                System.Diagnostics.Trace.WriteLine(sqlEx);
//                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("REFERENCE");
//                if (regex.IsMatch(sqlEx.Message))
//                    throw new RelationExistsException(sqlEx);
//            }
//        }

//        //public void Attach<TEntityType, TEntityType2>(TEntityType entity, TEntityType2 relateTo, string role)
//        //    where TEntityType : Domain.IEntity
//        //    where TEntityType2 : Domain.IEntity
//        //{
//        //    var relation = _definitionService.GetRelation(entity.EntityName, role);
//        //    if (relateTo == null)
//        //        throw new InvalidOperationException("The two entities do not have a relation!");

//        //    string relTable = string.Format("[{0}_{1}_{2}]", relation.LeftEntity, relation.RightEntity, relation.Role);
//        //    var sql = string.Format("INSERT INTO {0} (LID, RID) VALUES(@LID, @RID);", relTable);

//        //    bool isLeft = entity.EntityName == relation.LeftEntity;

//        //    SqlCommand cmd = new SqlCommand(sql);
//        //    var lidParam = cmd.CreateParameter();
//        //    lidParam.ParameterName = "LID";
//        //    lidParam.SqlDbType = SqlDbType.Int;
//        //    lidParam.Value = isLeft ? entity.Id : relateTo.Id;
//        //    cmd.Parameters.Add(lidParam);

//        //    var ridParam = cmd.CreateParameter();
//        //    ridParam.ParameterName = "RID";
//        //    ridParam.SqlDbType = SqlDbType.Int;
//        //    ridParam.Value = isLeft ? relateTo.Id : entity.Id;
//        //    cmd.Parameters.Add(ridParam);

//        //    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//        //    {
//        //        conn.Open();
//        //        cmd.Connection = conn;

//        //        cmd.ExecuteNonQuery();
//        //    }
//        //}

//        //public void Detach<TEntityType, TEntityType2>(TEntityType entity, TEntityType2 relatedEntity, string role)
//        //    where TEntityType : Domain.IEntity
//        //    where TEntityType2 : Domain.IEntity
//        //{
//        //    var relation = _definitionService.GetRelation(entity.EntityName, role);
//        //    if (relatedEntity == null)
//        //        throw new InvalidOperationException("The two entities do not have a relation!");

//        //    string relTable = string.Format("[{0}_{1}_{2}]", relation.LeftEntity, relation.RightEntity, relation.Role);
//        //    var sql = string.Format("DELETE FROM {0} WHERE LID = @LID AND RID = @RID;", relTable);

//        //    bool isLeft = entity.EntityName == relation.LeftEntity;

//        //    SqlCommand cmd = new SqlCommand(sql);
//        //    var lidParam = cmd.CreateParameter();
//        //    lidParam.ParameterName = "LID";
//        //    lidParam.SqlDbType = SqlDbType.Int;
//        //    lidParam.Value = isLeft ? entity.Id : relatedEntity.Id;
//        //    cmd.Parameters.Add(lidParam);

//        //    var ridParam = cmd.CreateParameter();
//        //    ridParam.ParameterName = "RID";
//        //    ridParam.SqlDbType = SqlDbType.Int;
//        //    ridParam.Value = isLeft ? relatedEntity.Id : entity.Id;
//        //    cmd.Parameters.Add(ridParam);

//        //    using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//        //    {
//        //        conn.Open();
//        //        cmd.Connection = conn;

//        //        cmd.ExecuteNonQuery();
//        //    }
//        //}

//        public IEnumerable<TEntityType> Search<TEntityType>(EntityQuery query, IEnumerable<Domain.Sorting> sortings = null, Domain.Paging paging = null) where TEntityType : Domain.IEntity
//        {
//            var entityName = Activator.CreateInstance<TEntityType>().EntityName;
//            return Search<TEntityType>(entityName, query, sortings, paging);
//        }

//        public IEnumerable<TEntityType> Search<TEntityType>(string entityName, EntityQuery query, IEnumerable<Domain.Sorting> sortings = null, Domain.Paging paging = null) where TEntityType : Domain.IEntity
//        {
//            var def = _definitionService.GetDefinitionByName(entityName);

//            StringBuilder sql = new StringBuilder();
//            sql.AppendFormat("SELECT [{0}].* \nFROM [{0}]", def.Name);


//            Dictionary<string, int> relTablesSufixes = new Dictionary<string, int>();

//            int sufix = 0;
//            if (query != null)
//            {
//                foreach (var rel in query.RelationRules)
//                {
//                    sufix++;
//                    relTablesSufixes.Add(rel.Role, sufix);
//                    GenerateJoin(def, rel, sql, 0, ref sufix);
//                }

//                for (int i = 0; i < query.PropertyRules.Count; i++)
//                {
//                    if (i == 0)
//                        sql.Append("\nWHERE ");
//                    var cond = query.PropertyRules[i];
//                    if (cond.Property == "Id")
//                        sql.AppendFormat("[{0}].[{1}] = {2}", def.Name, cond.Property, cond.Values.Single().ToString());
//                    else
//                    {
//                        sql.Append(DatabaseBuilder.BuildCondition(query.PropertyRules[i], def, string.Format("[{0}]", def.Name)));
//                    }
//                    if (i + 1 < query.PropertyRules.Count)
//                        sql.Append(" AND ");
//                }
//            }

//            if (sortings != null)
//            {
//                int idx = 0;
//                foreach (var sort in sortings.Where(s => s.IsRel))
//                {
//                    var rel = _definitionService.GetRelation(entityName, sort.Role);
//                    if (rel.GetTypeFor(entityName) == Services.tmp.RelationTypes.ManyToMany
//                        || rel.GetTypeFor(entityName) == Services.tmp.RelationTypes.OneToMany)
//                        throw new ArgumentException("Cannot sort based on property of one-to-many or many-to-many relation!");
//                    if (!relTablesSufixes.ContainsKey(rel.Role))
//                    {
//                        var relTable = DatabaseBuilder.GetTableFor(rel);
//                        bool isLeft = rel.LeftEntity == entityName;
//                        var masterKeyCol = isLeft ? "LID" : "RID";
//                        var slaveKeyCol = isLeft ? "RID" : "LID";
//                        var slaveAlias = string.Format("slave_{0}", idx);
//                        var slaveTable = isLeft ? rel.RightEntity : rel.LeftEntity;
//                        sql.AppendFormat(" \n LEFT JOIN [{0}] ON [{0}].[{1}] = [{2}].Id \n", relTable, masterKeyCol, def.Name);
//                        sql.AppendFormat(" \n LEFT JOIN [{0}] as {3} ON [{1}].[{2}] = [{3}].Id \n", slaveTable, relTable, slaveKeyCol, slaveAlias);
//                        sort.Property = string.Format("[{0}].[{1}]", slaveAlias, sort.Property);
//                    }
//                    else
//                        sort.Property = string.Format("{1}_{0}.[{2}]", relTablesSufixes[rel.Role], rel.LeftEntity == def.Name ? rel.RightEntity : rel.LeftEntity, sort.Property);
//                }
//            }
//            //sql.AppendFormat("\n{0}\n", GetWhereClause(def, conditions));

//            sql.AppendFormat("\n{0}\n", GetOrderByClause(sortings));

//            List<TEntityType> results = new List<TEntityType>();
//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                SqlCommand cmd = new SqlCommand(sql.ToString(), conn);
//                using (var reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        var item = Activator.CreateInstance<TEntityType>();
//                        Dictionary<string, object> raw = new Dictionary<string, object>(def.Properties.Count);
//                        def.Properties.ForEach(prop =>
//                        {
//                            raw[prop.Name] = reader[prop.Name];
//                        });
//                        item.Id = (int)reader["Id"];
//                        item.LoadRawData(raw);
//                        results.Add(item);
//                    }
//                }
//            }

//            return results;
//        }

//        #endregion

//        #region Helpers

//        private void GenerateJoin(EntityDefinition masterDef, RelationCondition relCondition, StringBuilder sql, int masterSufix, ref int slaveSufix)
//        {
//            var rel = _definitionService.GetRelation(masterDef.Name, relCondition.Role);
//            bool isLeft = masterDef.Name == rel.LeftEntity;
//            var slaveDef = _definitionService.GetDefinitionByName(relCondition.EntityName);
//            var masterTbl = masterSufix > 0 ? string.Format("[{0}_{1}]", masterSufix, masterDef.Name) : string.Format("[{0}]", masterDef.Name);
//            var relTbl = string.Format("[{0}_{1}_{2}]", rel.LeftEntity, rel.RightEntity, rel.Role);
//            var alias = string.Format("{1}_{0}", slaveSufix, slaveDef.Name);
//            sql.AppendLine();


//            var masterKeyColumn = isLeft ? "LID" : "RID";
//            var slaveKeyColumn = isLeft ? "RID" : "LID";
//            sql.AppendFormat("INNER JOIN {0} ON {0}.{2} = {1}.Id\n", relTbl, masterTbl, masterKeyColumn);
//            sql.AppendFormat("INNER JOIN [{0}] as {2} ON {2}.Id = {1}.{3}", slaveDef.Name, relTbl, alias, slaveKeyColumn);
//            foreach (var propCond in relCondition.PropertyRules)
//            {
//                var sqlCond = DatabaseBuilder.BuildCondition(propCond, slaveDef, string.Format("{0}", alias));
//                sql.AppendFormat(" AND {0}", sqlCond);
//            }

//            sql.AppendLine();

//            foreach (var subRelCond in relCondition.RelationRules)
//            {
//                var def = _definitionService.GetDefinitionByName(subRelCond.EntityName);
//                masterSufix = slaveSufix;
//                slaveSufix++;
//                GenerateJoin(slaveDef, subRelCond, sql, masterSufix, ref slaveSufix);
//            }
//        }

//        private System.Data.SqlDbType GetSqlType(PropertyTypes dataType)
//        {
//            switch (dataType)
//            {
//                case PropertyTypes.Integer:
//                    return SqlDbType.Int;
//                case PropertyTypes.Number:
//                    return SqlDbType.Decimal;
//                case PropertyTypes.String:
//                    return SqlDbType.NVarChar;
//                case PropertyTypes.EnumValue:
//                    return SqlDbType.TinyInt;
//                case PropertyTypes.Date:
//                    return SqlDbType.DateTime;
//                case PropertyTypes.Boolean:
//                    return SqlDbType.Bit;
//                default:
//                    throw new NotImplementedException(string.Format("GetSqlType for type {0} is not yet implemented.", dataType));
//            }
//        }

//        private string GetOrderByClause(IEnumerable<Domain.Sorting> sortings)
//        {
//            StringBuilder sb = new StringBuilder();
//            if (sortings != null && sortings.Count() > 0)
//            {
//                sb.Append("\n ORDER BY \n");
//                foreach (var sort in sortings)
//                {
//                    string format = null;
//                    if (sort.Property.Contains("["))
//                        format = " {0} {1},";
//                    else
//                        format = " [{0}] {1},";
//                    sb.AppendFormat(format, sort.Property, sort.Descending ? "desc" : "asc");
//                }
//                sb.Remove(sb.Length - 1, 1);
//            }

//            return sb.ToString();
//        }

//        //TODO: Remove
//        //private object GetWhereClause(Services.tmp.EntityDefinition definiton, IEnumerable<Domain.Condition> conditions, IDictionary<string, string> prefixMapping = null)
//        //{
//        //    StringBuilder sb = new StringBuilder();
//        //    if (conditions != null && conditions.Count() > 0)
//        //    {
//        //        sb.Append("\n WHERE \n");
//        //        foreach (var cond in conditions)
//        //        {
//        //            string sqlCondition = null;
//        //            if (cond.IsRel)
//        //                throw new NotImplementedException();

//        //            var property = definiton.Properties.Find(p => p.Name == cond.Property);

//        //            string value = null;
//        //            if (property.ValueType == typeof(decimal))
//        //                value = ((decimal)(object)cond.Values.Single()).ToString(System.Globalization.CultureInfo.InvariantCulture);
//        //            else if (property.ValueType == typeof(byte))
//        //                value = ((byte)cond.Values.Single()).ToString();
//        //            else
//        //                value = cond.Values.Single().ToString();

//        //            string column = null;
//        //            if (prefixMapping != null)
//        //            {
//        //                string prefix = null;
//        //                if (prefixMapping.TryGetValue(cond.Property, out prefix))
//        //                    column = string.Format("{0}.[{1}]", prefix, cond.Property);
//        //                else
//        //                    column = string.Format("[{0}]", cond.Property);
//        //            }
//        //            else
//        //                column = string.Format("[{0}]", cond.Property);

//        //            if (cond.Operator == Condition.Is)
//        //            {
//        //                if (property.ValueType == typeof(string))
//        //                    sqlCondition = string.Format("{0} = '{1}'", column, value);
//        //                else
//        //                    sqlCondition = string.Format("{0} = {1}", column, value);
//        //            }
//        //            else if (cond.Operator == "not")
//        //            {
//        //                if (property.ValueType == typeof(string))
//        //                    sqlCondition = string.Format("{0} <> '{1}'", column, value);
//        //                else
//        //                    sqlCondition = string.Format("{0} <> {1}", column, value);
//        //            }
//        //            else if (cond.Operator == "lt")
//        //                sqlCondition = string.Format("{0} < {1}", column, value);
//        //            else if (cond.Operator == "lte")
//        //                sqlCondition = string.Format("{0} <= {1}", column, value);
//        //            else if (cond.Operator == "gt")
//        //                sqlCondition = string.Format("{0} > {1}", column, value);
//        //            else if (cond.Operator == "gte")
//        //                sqlCondition = string.Format("{0} >= {1}", column, value);
//        //            else if (cond.Operator == Condition.StartsWith && property.ValueType == typeof(string))
//        //                sqlCondition = string.Format("{0} LIKE '{1}%'", column, value);
//        //            else
//        //                throw new NotImplementedException("Unrecognized operator!");


//        //            sb.AppendFormat(" {0} AND", sqlCondition);
//        //        }
//        //        sb.Remove(sb.Length - 4, 4);
//        //    }

//        //    return sb.ToString();
//        //}

//        #endregion


//        public void Create(Domain.IEntity entity)
//        {
//            throw new NotImplementedException();
//        }

//        public Domain.IEntity Read(EntityKey key)
//        {
//            var definition = _definitionService.GetDefinitionByName(key.EntityName);

//            SqlCommand cmd = new SqlCommand(string.Format("{0}_Read", definition.Name));
//            cmd.CommandType = CommandType.StoredProcedure;

//            var idParam = cmd.CreateParameter();
//            idParam.ParameterName = "Id";
//            idParam.SqlDbType = SqlDbType.Int;
//            idParam.Value = key.Id;
//            cmd.Parameters.Add(idParam);

//            Dictionary<string, object> raw = new Dictionary<string, object>();

//            IEntity entity = new EntityAdapter(definition);
//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                cmd.Connection = conn;

//                using (var reader = cmd.ExecuteReader())
//                {
//                    if (reader.Read())
//                    {
//                        entity.Id = (int)reader["Id"];
//                        definition.Properties.ForEach(prop =>
//                        {
//                            raw[prop.Name] = reader[prop.Name];
//                        });
//                    }
//                }
//            }
//            if (entity != null)
//                entity.LoadRawData(raw);

//            return entity;
//        }

//        public void ProcessUpdate(Domain.IEntity entity)
//        {
//            throw new NotImplementedException();
//        }

//        public void Delete(EntityDelete delete)
//        {
//            var definition = _definitionService.GetDefinitionByName(delete.EntityName);
//            var entity = new EntityAdapter(definition);
//            entity.Id = delete.Id;

//            if (delete.Recursive)
//            {
//                foreach (var rel in definition.Relations)
//                {
//                    bool isLeft = rel.LeftEntity == definition.Name;
//                    var query = new EntityQuery();
//                    query.EntityName = isLeft ? rel.RightEntity : rel.LeftEntity;
//                    query.RelationRules.Add(new RelationCondition()
//                    {
//                        EntityName = definition.Name,
//                        Role = rel.Role,
//                        PropertyRules = new List<Condition>() {
//                            new Condition("Id", Condition.Is, delete.Id)
//                        }
//                    });

//                    foreach (var attached in Search(query))
//                    {
//                        Detach(entity, attached, rel.Role);
//                    }
//                }
//            }

//            SqlCommand cmd = new SqlCommand(string.Format("{0}_Delete", definition.Name));
//            cmd.CommandType = CommandType.StoredProcedure;

//            var idParam = cmd.CreateParameter();
//            idParam.ParameterName = "Id";
//            idParam.SqlDbType = SqlDbType.Int;
//            idParam.Value = delete.Id;
//            cmd.Parameters.Add(idParam);

//            try
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//                {
//                    conn.Open();
//                    cmd.Connection = conn;
//                    cmd.ExecuteNonQuery();
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                System.Diagnostics.Trace.WriteLine(sqlEx);
//                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("REFERENCE");
//                if (regex.IsMatch(sqlEx.Message))
//                    throw new RelationExistsException(sqlEx);
//            }
//        }

//        public void Attach(Domain.IEntity entity, Domain.IEntity relateTo, string role)
//        {
//            var relation = _definitionService.GetRelation(entity.EntityName, role);
//            if (relateTo == null)
//                throw new InvalidOperationException("The two entities do not have a relation!");

//            if (relation.GetTypeFor(entity.EntityName) == RelationTypes.OneToOne || relation.GetTypeFor(entity.EntityName) == RelationTypes.ManyToOne)
//            {
//                EntityQuery query = new EntityQuery();
//                query.EntityName = relateTo.EntityName;
//                var relCond = new RelationCondition();
//                relCond.Role = role;
//                relCond.EntityName = entity.EntityName;
//                relCond.PropertyRules.Add(new Condition("Id", Condition.Is, entity.Id));
//                query.RelationRules.Add(relCond);

//                var currentRelated = Search(query).SingleOrDefault();
//                if (currentRelated != null)
//                    Detach(entity, currentRelated, relation.Role);
//            }

//            string relTable = string.Format("[{0}_{1}_{2}]", relation.LeftEntity, relation.RightEntity, relation.Role);
//            var sql = string.Format("INSERT INTO {0} (LID, RID) VALUES(@LID, @RID);", relTable);

//            bool isLeft = entity.EntityName == relation.LeftEntity;

//            SqlCommand cmd = new SqlCommand(sql);
//            var lidParam = cmd.CreateParameter();
//            lidParam.ParameterName = "LID";
//            lidParam.SqlDbType = SqlDbType.Int;
//            lidParam.Value = isLeft ? entity.Id : relateTo.Id;
//            cmd.Parameters.Add(lidParam);

//            var ridParam = cmd.CreateParameter();
//            ridParam.ParameterName = "RID";
//            ridParam.SqlDbType = SqlDbType.Int;
//            ridParam.Value = isLeft ? relateTo.Id : entity.Id;
//            cmd.Parameters.Add(ridParam);

//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                cmd.Connection = conn;

//                cmd.ExecuteNonQuery();
//            }
//        }

//        public void Detach(Domain.IEntity entity, Domain.IEntity relatedEntity, string role)
//        {
//            var relation = _definitionService.GetRelation(entity.EntityName, role);
//            if (relatedEntity == null)
//                throw new InvalidOperationException("The two entities do not have a relation!");

//            string relTable = string.Format("[{0}_{1}_{2}]", relation.LeftEntity, relation.RightEntity, relation.Role);
//            var sql = string.Format("DELETE FROM {0} WHERE LID = @LID AND RID = @RID;", relTable);

//            bool isLeft = entity.EntityName == relation.LeftEntity;

//            SqlCommand cmd = new SqlCommand(sql);
//            var lidParam = cmd.CreateParameter();
//            lidParam.ParameterName = "LID";
//            lidParam.SqlDbType = SqlDbType.Int;
//            lidParam.Value = isLeft ? entity.Id : relatedEntity.Id;
//            cmd.Parameters.Add(lidParam);

//            var ridParam = cmd.CreateParameter();
//            ridParam.ParameterName = "RID";
//            ridParam.SqlDbType = SqlDbType.Int;
//            ridParam.Value = isLeft ? relatedEntity.Id : entity.Id;
//            cmd.Parameters.Add(ridParam);

//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                cmd.Connection = conn;

//                cmd.ExecuteNonQuery();
//            }
//        }

//        public IEnumerable<Domain.IEntity> Search(EntityQuery query, IEnumerable<Domain.Sorting> sortings = null, Domain.Paging paging = null)
//        {
//            var def = _definitionService.GetDefinitionByName(query.EntityName);

//            StringBuilder sql = new StringBuilder();
//            sql.AppendFormat("SELECT [{0}].* \nFROM [{0}]", def.Name);


//            Dictionary<string, int> relTablesSufixes = new Dictionary<string, int>();


//            var whereClause = new StringBuilder();
//            int sufix = 0;
//            if (query != null)
//            {
//                foreach (var rel in query.RelationRules)
//                {
//                    sufix++;
//                    relTablesSufixes.Add(rel.Role, sufix);
//                    GenerateJoin(def, rel, whereClause, 0, ref sufix);
//                }

//                for (int i = 0; i < query.PropertyRules.Count; i++)
//                {
//                    if (i == 0)
//                        whereClause.Append("\nWHERE ");
//                    var cond = query.PropertyRules[i];
//                    if (cond.Property == "Id")
//                        whereClause.AppendFormat("[{0}].[{1}] = {2}", def.Name, cond.Property, cond.Values.Single().ToString());
//                    else
//                    {
//                        whereClause.Append(DatabaseBuilder.BuildCondition(query.PropertyRules[i], def, string.Format("[{0}]", def.Name)));
//                    }
//                    if (i + 1 < query.PropertyRules.Count)
//                        whereClause.Append(" AND ");
//                }
//            }

//            if (sortings != null)
//            {
//                int idx = 0;
//                foreach (var sort in sortings.Where(s => s.IsRel))
//                {
//                    var rel = _definitionService.GetRelation(query.EntityName, sort.Role);
//                    if (rel.GetTypeFor(query.EntityName) == Services.tmp.RelationTypes.ManyToMany
//                        || rel.GetTypeFor(query.EntityName) == Services.tmp.RelationTypes.OneToMany)
//                        throw new ArgumentException("Cannot sort based on property of one-to-many or many-to-many relation!");
//                    if (!relTablesSufixes.ContainsKey(rel.Role))
//                    {
//                        var relTable = DatabaseBuilder.GetTableFor(rel);
//                        bool isLeft = rel.LeftEntity == query.EntityName;
//                        var masterKeyCol = isLeft ? "LID" : "RID";
//                        var slaveKeyCol = isLeft ? "RID" : "LID";
//                        var slaveAlias = string.Format("slave_{0}", idx);
//                        var slaveTable = isLeft ? rel.RightEntity : rel.LeftEntity;
//                        sql.AppendFormat(" \n LEFT JOIN [{0}] ON [{0}].[{1}] = [{2}].Id \n", relTable, masterKeyCol, def.Name);
//                        sql.AppendFormat(" \n LEFT JOIN [{0}] as {3} ON [{1}].[{2}] = [{3}].Id \n", slaveTable, relTable, slaveKeyCol, slaveAlias);
//                        sort.Property = string.Format("[{0}].[{1}]", slaveAlias, sort.Property);
//                    }
//                    else
//                        sort.Property = string.Format("{1}_{0}.[{2}]", relTablesSufixes[rel.Role], rel.LeftEntity == def.Name ? rel.RightEntity : rel.LeftEntity, sort.Property);
//                }
//            }
//            //sql.AppendFormat("\n{0}\n", GetWhereClause(def, conditions));

//            sql.AppendFormat("\n{0}\n", whereClause.ToString());

//            sql.AppendFormat("\n{0}\n", GetOrderByClause(sortings));

//            List<EntityAdapter> results = new List<EntityAdapter>();
//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                SqlCommand cmd = new SqlCommand(sql.ToString(), conn);
//                using (var reader = cmd.ExecuteReader())
//                {
//                    while (reader.Read())
//                    {
//                        var item = new EntityAdapter(def);
//                        Dictionary<string, object> raw = new Dictionary<string, object>(def.Properties.Count);
//                        def.Properties.ForEach(prop =>
//                        {
//                            raw[prop.Name] = reader[prop.Name];
//                        });
//                        item.Id = (int)reader["Id"];
//                        item.LoadRawData(raw);
//                        results.Add(item);
//                    }
//                }
//            }

//            return results;
//        }

//        public void ProcessUpdate(EntityUpdate update)
//        {
//            var definition = _definitionService.GetDefinitionByName(update.EntityName);
//            var adapter = new EntityAdapter(definition);
//            adapter.LoadRawData(update.PropertyUpdates.ToDictionary(e => e.Name, e => (object)e.Value));

//            //TODO: Do not update when no property updates
//            if (adapter.Id > 0)
//            {
//                var entity = Read(new EntityKey() { EntityName = definition.Name, Id = adapter.Id });
//                foreach (var kvp in adapter.GetRawData())
//                {
//                    entity.GetRawData()[kvp.Key] = kvp.Value;
//                }

//                Update(entity);
//            }
//            else
//                Create(adapter);

//            if (update.RelationUpdates != null)
//                foreach (var rel in update.RelationUpdates)
//                {
//                    updateRelation(rel, adapter);
//                }
//        }

//        private void updateRelation(RelationUpdate update, IEntity master)
//        {
//            ProcessUpdate(update);
//            var definition = _definitionService.GetDefinitionByName(update.EntityName);
//            EntityAdapter adapter = new EntityAdapter(definition);
//            adapter.LoadRawData(update.PropertyUpdates.ToDictionary(e => e.Name, e => (object)e.Value));

//            if (update.Operation == RelationOperation.Attach)
//                Attach(master, adapter, update.Role);
//            else if (update.Operation == RelationOperation.Detach)
//                Detach(master, adapter, update.Role);
//        }


//        public void Update(IEntity entity)
//        {
//            var definition = _definitionService.GetDefinitionByName(entity.EntityName);

//            var data = entity.GetRawData();

//            SqlCommand cmd = new SqlCommand(string.Format("{0}_Update", definition.Name));
//            cmd.CommandType = System.Data.CommandType.StoredProcedure;

//            definition.Properties.ForEach(prop =>
//            {
//                //skip computed - they are updated automatically
//                if (prop.Type == PropertyTypes.Computed)
//                    return;

//                var param = cmd.CreateParameter();
//                param.ParameterName = prop.Name;
//                param.SqlDbType = GetSqlType(prop.Type);
//                param.Value = data[prop.Name] ?? DBNull.Value;


//                cmd.Parameters.Add(param);
//            });

//            var idParam = cmd.CreateParameter();
//            idParam.ParameterName = "Id";
//            idParam.SqlDbType = System.Data.SqlDbType.Int;
//            idParam.Value = entity.Id;
//            cmd.Parameters.Add(idParam);

//            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["EntityDB"].ConnectionString))
//            {
//                conn.Open();
//                cmd.Connection = conn;
//                cmd.ExecuteNonQuery();
//            }
//        }
//    }
//}
