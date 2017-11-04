using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Infrastructure
{
    public class EntityRepository : IEntityRepository
    {
        private IDomainModelService _domainService;
        private IDatabaseService _dbService;
        private ISequenceProvider _sequenceProvider;
        //private List<SqlCommand> _commands;

        public EntityRepository(IDomainModelService domainService, IDatabaseService dbService, ISequenceProvider sequenceProvider)
        {
            _domainService = domainService;
            _dbService = dbService;
            _sequenceProvider = sequenceProvider;
        }

        public int Create(Entity entity)
        {
            var em = _domainService.Domain.Entities[entity.Name];

            CheckEntityRules(entity, em);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO [{0}] (", entity.Name);

            foreach (var pm in em.Properties.Where(p => p.Type == PropertyType.Sequence))
            {
                var sqpm = (SequencePropertyModel)pm;
                if (sqpm.SequenceType != SequenceType.Identity)
                {
                    var value = _sequenceProvider.GetNext(sqpm);
                    entity.Data[sqpm.Name] = value;
                }
            }

            string[] columns = new string[entity.Data.Count];
            string[] parameters = new string[entity.Data.Count];
            int idx = 0;
            SqlCommand cmd = new SqlCommand();
            foreach (var data in entity.Data)
            {
                var pm = em.Properties[data.Key];
                columns[idx] = string.Format("[{0}]", pm.Name);
                parameters[idx] = string.Format("@{0}", pm.Name);
                cmd.Parameters.AddWithValue(pm.Name, data.Value != null ? data.Value : DBNull.Value);

                idx++;
            }

            using (var ctx = _dbService.GetDatabaseContext(true))
            {
                var sql = string.Format("INSERT INTO [{0}] ({1}) OUTPUT INSERTED.ID VALUES ({2})", em.Name, string.Join(",", columns), string.Join(",", parameters));
                cmd.CommandText = sql;
                cmd.Connection = ctx.Connection;
                try
                {
                    int id = (Int32)cmd.ExecuteScalar();
                    entity.Id = id;
                    ctx.Complete();
                    return id;
                }
                catch (SqlException sex)
                {
                    throw WrapSqlException(sex, em);
                }
            }
        }

        private UniqueRuleModel GetUniqueRuleFromConstraintName(string cnst, EntityModel em)
        {
            var parts = cnst.Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var rule = em.Rules.SingleOrDefault(r =>
            {
                if (r.Type != EntityRuleType.Unique)
                    return false;
                var ur = r as UniqueRuleModel;
                bool ok = true;
                for (int i = 0; i < parts.Length; i++)
                {
                    var pm = em.Properties[parts[i]];
                    if (!ur.Properties.Contains(pm))
                    {
                        ok = false;
                        break;
                    }
                }

                return ok;
            });
            return rule as UniqueRuleModel;
        }

        public Entity Read(EntityQuery2 query)
        {
            return Search(query).SingleOrDefault();
        }

        public void Update(Entity entity)
        {
            var em = _domainService.Domain.Entities[entity.Name];

            CheckEntityRules(entity, em);

            SqlCommand cmd = new SqlCommand();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("UPDATE [{0}] SET ", em.Name);
            var it = entity.Data.GetEnumerator();
            bool hasNext = it.MoveNext();
            while (hasNext)
            {
                var pm = em.Properties[it.Current.Key];
                sb.AppendFormat("\t[{0}] = @{0}", pm.Name);
                cmd.Parameters.AddWithValue(pm.Name, it.Current.Value);
                hasNext = it.MoveNext();
                if (hasNext)
                    sb.Append(",\n");
            }


            sb.Append("\nWHERE [Id] = @Id;");

            using (var ctx = _dbService.GetDatabaseContext(true))
            {
                cmd.Parameters.AddWithValue("Id", entity.Id);
                cmd.CommandText = sb.ToString();
                cmd.Connection = ctx.Connection;
                try
                {
                    cmd.ExecuteNonQuery();
                    ctx.Complete();
                }
                catch (SqlException ex)
                {
                    throw WrapSqlException(ex, em);
                }
            }
        }

        public void UpdateRelation(Entity entity, Relation relation)
        {
            var em = _domainService.Domain.Entities[entity.Name];
            var rm = em.GetRelation(relation.Entity.Name, relation.Role);

            //CheckEntityRules(relation, rm); //TODO: check entity rules for relation

            SqlCommand cmd = new SqlCommand();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("UPDATE [{0}] SET ", rm.Name);
            var it = relation.Data.GetEnumerator();
            bool hasNext = it.MoveNext();
            while (hasNext)
            {
                var pm = rm.Properties[it.Current.Key];
                sb.AppendFormat("\t[{0}] = @{0}", pm.Name);
                cmd.Parameters.AddWithValue(pm.Name, it.Current.Value);
                hasNext = it.MoveNext();
                if (hasNext)
                    sb.Append(",\n");
            }


            sb.Append("\nWHERE [rid] = @rid AND [lid] = @lid;");

            using (var ctx = _dbService.GetDatabaseContext(true))
            {
                bool isLeft = em.Name == rm.Left.Name;
                cmd.Parameters.AddWithValue("lid", isLeft ? entity.Id : relation.Entity.Id);
                cmd.Parameters.AddWithValue("rid", isLeft ? relation.Entity.Id : entity.Id);
                cmd.CommandText = sb.ToString();
                cmd.Connection = ctx.Connection;
                try
                {
                    cmd.ExecuteNonQuery();
                    ctx.Complete();
                }
                catch (SqlException sex)
                {
                    throw WrapSqlException(sex, rm);
                }
            }

        }

        public void Delete(Entity entity, bool recursive = false)
        {
            var em = _domainService.Domain.Entities[entity.Name];

            if (entity.Id <= 0)
                throw new ArgumentException("entity.Id must be positive integer");


            if (recursive)
            {
                EntityQuery2 getAllRels = new EntityQuery2(em.Name, entity.Id);
                foreach (var rel in em.Relations)
                    getAllRels.Include(rel.GetOther(em.Name).Name, rel.Role);
                var e = Read(getAllRels);
                foreach (var rel in em.Relations)
                {
                    var relType = rel.TypeFor(em.Name);
                    var other = rel.GetOther(em.Name);
                    if (relType == RelationType.OneToOne || relType == RelationType.ManyToOne)
                    {
                        var item = e.GetSingleRelation(other.Name, rel.Role);
                        if (item != null)
                            Detach(e, item);
                    }
                    else
                    {
                        var items = e.GetManyRelations(other.Name, rel.Role);
                        foreach (var item in items)
                            Detach(e, item);
                    }
                }
            }
            using (var ctx = _dbService.GetDatabaseContext(true))
            {
                SqlCommand cmd = new SqlCommand(string.Format("DELETE [{0}] WHERE ID = @Id", em.Name), ctx.Connection);
                cmd.Parameters.AddWithValue("Id", entity.Id);
                try
                {
                    cmd.ExecuteNonQuery();
                    ctx.Complete();
                }
                catch (SqlException sex)
                {
                    throw WrapSqlException(sex, em);
                }
            }
        }

        public void Attach(Entity entity, Relation relation)
        {
            var rm = _domainService.Domain.Relations[entity.Name, relation.Entity.Name, relation.Role];
            var em = _domainService.Domain.Entities[entity.Name];

            bool isLeft = em.Name == rm.Left.Name;
            if (!relation.Data.ContainsKey("lid"))
            {
                relation.SetData<int>("lid", isLeft ? entity.Id : relation.Entity.Id);
            }
            if (!relation.Data.ContainsKey("rid"))
            {
                relation.SetData<int>("rid", isLeft ? relation.Entity.Id : entity.Id);
            }

            foreach (var pm in rm.Properties.Where(p => p.Type == PropertyType.Sequence))
            {
                var sqpm = (SequencePropertyModel)pm;
                if (sqpm.SequenceType != SequenceType.Identity)
                {
                    var value = _sequenceProvider.GetNext(sqpm);
                    relation.Data[sqpm.Name] = value;
                }
            }

            string[] columns = new string[relation.Data.Count];
            string[] parameters = new string[relation.Data.Count];
            int idx = 0;
            SqlCommand cmd = new SqlCommand();
            foreach (var data in relation.Data)
            {
                var pm = rm.Properties[data.Key];
                columns[idx] = string.Format("[{0}]", pm.Name);
                parameters[idx] = string.Format("@{0}", pm.Name);
                cmd.Parameters.AddWithValue(pm.Name, data.Value);
                idx++;
            }

            using (var ctx = _dbService.GetDatabaseContext(true))
            {

                var sql = string.Format("INSERT INTO [{0}] ({1}) OUTPUT INSERTED.ID VALUES ({2})", rm.Name, string.Join(",", columns), string.Join(",", parameters));
                cmd.CommandText = sql;
                cmd.Connection = ctx.Connection;
                try
                {
                    relation.SetData<int>("id", (Int32)cmd.ExecuteScalar());
                    ctx.Complete();
                }
                catch (SqlException ex)
                {
                    throw WrapSqlException(ex, rm);
                }
            }
        }

        public void Detach(Entity entity, Relation relation)
        {
            var rm = _domainService.Domain.Relations[entity.Name, relation.Entity.Name, relation.Role];
            var em = _domainService.Domain.Entities[entity.Name];

            bool isLeft = em.Name == rm.Left.Name;
            int lid = isLeft ? entity.Id : relation.Entity.Id;
            int rid = isLeft ? relation.Entity.Id : entity.Id;
            var sql = string.Format("DELETE FROM [{0}] WHERE lid = {1} AND rid = {2}", rm.Name, lid, rid);

            using (var ctx = _dbService.GetDatabaseContext(true))
            {
                SqlCommand cmd = new SqlCommand(sql, ctx.Connection);
                try
                {
                    cmd.ExecuteNonQuery();
                    ctx.Complete();
                }
                catch (SqlException ex)
                {
                    throw WrapSqlException(ex, rm);
                }
            }
        }

        public IEnumerable<Entity> Search(EntityQuery2 query)
        {
            var em = _domainService.Domain.Entities[query.Entity];

            SqlTable main = new SqlTable(em.Name);
            var select = new SqlSelect(main, new SqlColumn("iD", main));
            if (query.AllProperties)
                query.Properties = em.Properties.Select(p => p.Name).ToList();

            if (query.RelatedTo.Count > 0)
                select.Distinct = true;

            select.AddColumns(query.Properties.Where(p => !p.Equals("id", StringComparison.InvariantCultureIgnoreCase)).Select(p => new SqlColumn(em.Properties[p].Name, main)).ToArray());
            if (query.SortBy == null)
                query.SortBy = new Sorting("ID");

            if (!query.SortBy.IsRel)
            {
                var sortByColumn = new SqlColumn(query.SortBy.Property, main);
                select.OrderBy = new SqlOrderBy(sortByColumn, query.SortBy.Descending);
            }
            if (query.Paging != null)
            {
                var start = (query.Paging.Page - 1) * query.Paging.PageSize;
                select.Paging = new SqlPaging(start + 1, start + query.Paging.PageSize);
            }
            int id = 1;

            Dictionary<string, SqlSelect> additSearches = new Dictionary<string, SqlSelect>();
            foreach (var inc in query.Includes)
            {
                var rel = em.GetRelation(inc.Entity, inc.Role);
                var relType = rel.TypeFor(em.Name);
                bool isLeft = rel.Left.Name == em.Name;
                var other = isLeft ? rel.Right : rel.Left;
                if (relType == RelationType.OneToOne || relType == RelationType.ManyToOne)
                {
                    var relTable = new SqlTable(rel.Name, string.Format("tbl{0}", id++));
                    var entTable = new SqlTable(other.Name, string.Format("tbl{0}", id++));
                    if (isLeft)
                    {
                        select.AddJoin(relTable, new SqlColumn("LID", relTable), new SqlColumn("ID", main));
                        select.AddJoin(entTable, new SqlColumn("RID", relTable), new SqlColumn("ID", entTable));
                    }
                    else
                    {
                        select.AddJoin(relTable, new SqlColumn("RID", relTable), new SqlColumn("ID", main));
                        select.AddJoin(entTable, new SqlColumn("LID", relTable), new SqlColumn("ID", entTable));
                    }

                    select.AddColumns(rel.Properties.Select(p => new SqlColumn(p.Name, relTable)).ToArray());
                    select.AddColumns(other.Properties.Select(p => new SqlColumn(p.Name, entTable)).ToArray());
                    if (query.SortBy != null
                        && query.SortBy.IsRel
                        && query.SortBy.Role.Equals(rel.Role, StringComparison.InvariantCultureIgnoreCase)
                        && query.SortBy.Entity.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //TODO: sort by rel properties
                        select.OrderBy = new SqlOrderBy(new SqlColumn(query.SortBy.Property, entTable), query.SortBy.Descending);
                    }
                }
                else
                {
                    var relTbl = new SqlTable(rel.Name, rel.Name);
                    var s = new SqlSelect(relTbl, rel.Properties.Select(pm => new SqlColumn(pm.Name, relTbl)).ToArray());
                    var entTbl = new SqlTable(other.Name, other.Name);
                    s.AddJoin(entTbl, new SqlColumn(isLeft ? "rid" : "lid", relTbl), new SqlColumn("id", entTbl), SqlJoinType.Inner);
                    s.AddColumns(other.Properties.Select(pm => new SqlColumn(pm.Name, entTbl)).ToArray());

                    if (inc.SortBy == null)
                        inc.SortBy = new Sorting("ID");

                    var incSortCol = s.Columns.Find(c => c.Name.Equals(inc.SortBy.Property, StringComparison.InvariantCultureIgnoreCase));
                    if (incSortCol != null)
                        s.OrderBy = new SqlOrderBy(incSortCol, inc.SortBy.Descending);
                    if (inc.Paging != null)
                    {
                        var start = (inc.Paging.Page - 1) * inc.Paging.PageSize;
                        s.Paging = new SqlPaging(start + 1, start + inc.Paging.PageSize);
                    }

                    additSearches.Add(rel.Name, s);
                }
            }

            //TODO: group?
            foreach (var rule in query.Rules)
            {
                AddRuleToSelect(em, select, main, rule);
            }

            foreach (var relto in query.RelatedTo)
            {
                if (relto.RelatedTo.Count > 0)
                    throw new NotImplementedException("Only one level of nested query (related) is allowed.");

                var rel = em.GetRelation(relto.Entity, relto.Role);
                var other = rel.GetOther(em.Name);

                var relTable = new SqlTable(rel.Name, string.Format("tbl{0}", id++));
                var otherTable = new SqlTable(other.Name, string.Format("tbl{0}", id++));
                //TODO: code repetition with the includes logic!
                if (rel.Left.Name == em.Name)
                {
                    select.AddJoin(relTable, new SqlColumn("LID", relTable), new SqlColumn("ID", main));
                    select.AddJoin(otherTable, new SqlColumn("RID", relTable), new SqlColumn("ID", otherTable));
                }
                else
                {
                    select.AddJoin(relTable, new SqlColumn("RID", relTable), new SqlColumn("ID", main));
                    select.AddJoin(otherTable, new SqlColumn("LID", relTable), new SqlColumn("ID", otherTable));
                }

                foreach (var rule in relto.Rules)
                {
                    AddRuleToSelect(other, select, otherTable, rule);
                }
                foreach (var rule in relto.RelationRules)
                {
                    AddRuleToSelect(rel, select, relTable, rule);
                }
            }

            using (var ctx = _dbService.GetDatabaseContext(false))
            {

                List<Entity> result = new List<Entity>();
                SqlCommand cmd = select.ToSqlCommand(ctx.Connection);
                using (var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        var e = new Entity(em.Name, reader.GetInt32(0));
                        var idx = 1;
                        foreach (var prop in query.Properties)
                        {
                            var pm = em.Properties[prop];

                            if (pm.Is("id"))
                                continue;

                            e.Data[pm.Name] = GetValueFromReader(reader, pm, idx);
                            idx++;
                        }

                        foreach (var inc in query.Includes)
                        {
                            var rel = em.GetRelation(inc.Entity, inc.Role);
                            var relType = rel.TypeFor(em.Name);
                            bool isLeft = rel.Left.Name == em.Name;
                            var other = isLeft ? rel.Right : rel.Left;
                            if (relType == RelationType.OneToOne || relType == RelationType.ManyToOne)
                            {
                                var relData = new Relation(rel.Role, new Entity(other.Name));

                                foreach (var prop in rel.Properties)
                                {
                                    relData.Data[prop.Name] = GetValueFromReader(reader, prop, idx++);
                                }
                                foreach (var prop in other.Properties)
                                {
                                    relData.Entity.Data[prop.Name] = GetValueFromReader(reader, prop, idx++);
                                }
                                if (!relData.Entity.Data.ContainsKey("id") || relData.Entity.Data["id"] == null)
                                    continue;

                                relData.Entity.Id = relData.Entity.GetData<int>("id");
                                e.SetSingleRelation(relData);
                            }
                        }

                        result.Add(e);
                    }
                }
                if (result.Count > 0)
                    foreach (var kvp in additSearches)
                    {
                        var rel = em.Relations[kvp.Key];
                        var sel = kvp.Value;
                        bool isLeft = em.Name == rel.Left.Name;
                        var other = isLeft ? rel.Right : rel.Left;
                        sel.Where.In(new SqlColumn(isLeft ? "lid" : "rid", sel.From), result.Select(e => e.Id));
                        SqlCommand getRelsCmd = sel.ToSqlCommand(ctx.Connection);
                        using (var reader = getRelsCmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
                        {
                            while (reader.Read())
                            {
                                var idx = 0;
                                Relation relationData = new Relation(rel.Role, new Entity(other.Name));
                                foreach (var prop in rel.Properties)
                                {
                                    relationData.Data[prop.Name] = GetValueFromReader(reader, prop, idx++);
                                }
                                foreach (var prop in other.Properties)
                                {
                                    relationData.Entity.Data[prop.Name] = GetValueFromReader(reader, prop, idx++);
                                }

                                relationData.Entity.Id = relationData.Entity.GetData<int>("id");

                                var mainId = isLeft ? relationData.GetData<int>("lid") : relationData.GetData<int>("rid");
                                result.Find(e => e.Id == mainId).AddManyRelation(relationData);
                            }
                        }
                    }
                ctx.Complete();
                return result;
            }
        }

        public int Count(EntityQuery2 query)
        {
            var em = _domainService.Domain.Entities[query.Entity];

            SqlTable main = new SqlTable(em.Name);
            var select = new SqlSelect(main, new SqlColumn("ID", main));
            select.Count = true;
            if (query.AllProperties)
                query.Properties = em.Properties.Select(p => p.Name).ToList();

            if (query.RelatedTo.Count > 0)
                select.Distinct = true;

            select.AddColumns(query.Properties.Where(p => !p.Equals("id", StringComparison.InvariantCultureIgnoreCase)).Select(p => new SqlColumn(em.Properties[p].Name, main)).ToArray());
            if (query.SortBy == null)
                query.SortBy = new Sorting("ID");

            if (!query.SortBy.IsRel)
            {
                var sortByColumn = new SqlColumn(query.SortBy.Property, main);
                select.OrderBy = new SqlOrderBy(sortByColumn, query.SortBy.Descending);
            }
            
            int id = 1;

            foreach (var rule in query.Rules)
            {
                AddRuleToSelect(em, select, main, rule);
            }

            foreach (var relto in query.RelatedTo)
            {
                if (relto.RelatedTo.Count > 0)
                    throw new NotImplementedException("Only one level of nested query (related) is allowed.");

                var rel = em.GetRelation(relto.Entity, relto.Role);
                var other = rel.GetOther(em.Name);

                var relTable = new SqlTable(rel.Name, string.Format("tbl{0}", id++));
                var otherTable = new SqlTable(other.Name, string.Format("tbl{0}", id++));
                //TODO: code repetition with the includes logic!
                if (rel.Left.Name == em.Name)
                {
                    select.AddJoin(relTable, new SqlColumn("LID", relTable), new SqlColumn("ID", main));
                    select.AddJoin(otherTable, new SqlColumn("RID", relTable), new SqlColumn("ID", otherTable));
                }
                else
                {
                    select.AddJoin(relTable, new SqlColumn("RID", relTable), new SqlColumn("ID", main));
                    select.AddJoin(otherTable, new SqlColumn("LID", relTable), new SqlColumn("ID", otherTable));
                }

                foreach (var rule in relto.Rules)
                {
                    AddRuleToSelect(other, select, otherTable, rule);
                }
                foreach (var rule in relto.RelationRules)
                {
                    AddRuleToSelect(rel, select, relTable, rule);
                }
            }

            using (var ctx = _dbService.GetDatabaseContext(false))
            {
                SqlCommand cmd = select.ToSqlCommand(ctx.Connection);
                int result = (int)cmd.ExecuteScalar();
                return result;
            }
        }

        private object ConvertValue(PropertyModel pm, object p)
        {
            //TODO: EntityRepo - convert values for search
            switch (pm.Type)
            {
                case PropertyType.Boolean:
                    return Convert.ToBoolean(p);
                case PropertyType.Datetime:
                    return Convert.ToDateTime(p);
                default:
                    return p;
            }
        }

        private object GetValueFromReader(SqlDataReader reader, PropertyModel pm, int idx)
        {
            if (reader.IsDBNull(idx))
                return null;

            switch (pm.Type)
            {
                case PropertyType.Boolean:
                    return reader.GetBoolean(idx);
                case PropertyType.Datetime:
                    return reader.GetDateTime(idx);
                case PropertyType.Enum:
                    var epm = pm as EnumPropertyModel;
                    return Enum.Parse(epm.EnumType, Enum.GetName(epm.EnumType, reader.GetValue(idx)));
                case PropertyType.Number:
                    var npm = pm as NumberPropertyModel;
                    if (npm.IsInteger)
                        return reader.GetInt32(idx);
                    else
                        return reader.GetDecimal(idx);
                case PropertyType.String:
                    return reader.GetString(idx);
                case PropertyType.Sequence:
                    var sqpm = pm as SequencePropertyModel;
                    if (sqpm.SequenceType == SequenceType.Identity)
                        return reader.GetInt32(idx);
                    else if (sqpm.SequenceType == SequenceType.Guid)
                        return reader.GetGuid(idx);
                    else if (sqpm.SequenceType == SequenceType.Uri)
                        return reader.GetString(idx);
                    else
                        throw new NotImplementedException();
                case PropertyType.Computed:
                    return reader.GetString(idx);//TODO: GetValueFromReader - Computed
                default:
                    throw new NotImplementedException();
            }

        }

        private T CastEnum<T>(object v)
        {
            return (T)v;
        }
        private void SkipValuesFromReader(SqlDataReader reader, int idx, int skipCount)
        {
            object o;
            for (int i = 0; i < skipCount; i++)
                o = reader.GetValue(idx + i);
        }

        private void CheckEntityRules(Entity entity, EntityModel em)
        {
            foreach (var rule in em.Rules)
            {
                if (rule is FutureOrPastDateRuleModel)
                {
                    var r = rule as FutureOrPastDateRuleModel;
                    if (!entity.Data.ContainsKey(r.Property.Name))
                        continue;

                    var value = entity.GetData<DateTime>(r.Property.Name);
                    if (r.Future)
                    {
                        if (DateTime.Now.Add(r.Offset) > value)
                            throw new Exception(string.Format("EntityRuleViolation for property '{0}'", r.Property.Name)); //TODO: EntityRuleValidationException
                    }
                    else
                    {
                        if (value.Add(r.Offset) > DateTime.Now)
                            throw new Exception(string.Format("EntityRuleViolation for property '{0}'", r.Property.Name)); //TODO: EntityRuleValidationException
                    }

                }
            }
        }

        private Exception WrapSqlException(SqlException sex, EntityModel em)
        {//TODO: handle other errors as well: already has a relation, relations exists so cannot delete an item
            if (sex.Number == 2627)
            {
                var term = "constraint 'UK_";
                var start = sex.Message.IndexOf(term) + term.Length;
                var end = sex.Message.IndexOf("'"[0], start);
                var constraintName = sex.Message.Substring(start, end - start);
                UniqueRuleModel rule = GetUniqueRuleFromConstraintName(constraintName, em);
                return new UniqueRuleViolationException(rule);
            }
            else if (sex.Number == 2601)
            {
                string term = "with unique index 'UK_";
                var start = sex.Message.IndexOf(term) + term.Length;
                var end = sex.Message.IndexOf("'"[0], start);
                var cnstName = sex.Message.Substring(start, end - start);
                var rule = GetUniqueRuleFromConstraintName(cnstName, em);
                return new UniqueRuleViolationException(rule);
            }
            else if (sex.Number == 547)
            {
                throw new RelationExistsException(sex);
            }
            else
                return sex;
        }

        private void AddRuleToSelect(EntityModel em, SqlSelect select, SqlTable table, Condition rule)
        {
            var pm = em.Properties[rule.Property];
            var col = new SqlColumn(pm.Name, table);
            switch (rule.Operator)
            {
                case Condition.Is:
                    select.Where.Is(col, ConvertValue(pm, rule.Values.Single()));
                    break;
                case Condition.GreaterThen:
                    select.Where.GreaterThen(col, GetValueForComparison(pm, rule.Values.Single()));
                    break;
                case Condition.GreaterThenOrEqual:
                    select.Where.GreaterThenOrEqual(col, GetValueForComparison(pm, rule.Values.Single()));
                    break;
                case Condition.LessThen:
                    select.Where.LessThen(col, GetValueForComparison(pm, rule.Values.Single()));
                    break;
                case Condition.LessThenOrEqual:
                    select.Where.LessThenOrEqual(col, GetValueForComparison(pm, rule.Values.Single()));
                    break;
                case Condition.StartsWith:
                    select.Where.StartsWith(col, rule.Values.Single() as string);
                    break;
                case Condition.EndsWith:
                    select.Where.EndsWith(col, rule.Values.Single() as string);
                    break;
                case Condition.ContainsPhrase:
                    select.Where.Like(col, rule.Values.Single() as string);
                    break;
                case Condition.AnyOf:
                    select.Where.In(col, rule.Values);
                    break;
                case Condition.Between:
                    select.Where.Between(col, rule.Values[0], rule.Values[1]);
                    break;
                case Condition.Not:
                    select.Where.IsNot(col, rule.Values.Single());
                    break;
                default:
                    throw new NotImplementedException(string.Format("Rule with operator {0} is not implemented by the EntityRepository.", rule.Operator));
            }
        }

        private object GetValueForComparison(PropertyModel pm, object value)
        {
            if (pm.Type == PropertyType.Datetime && value is DateTime == false)
            {
                return Convert.ToDateTime(value);
            }
            else
                return value;
        }
    }


    public class SqlSelect
    {
        public SqlSelect(SqlTable from, params SqlColumn[] columns)
        {
            Columns = new List<SqlColumn>();
            Joins = new List<SqlJoin>();
            AddColumns(columns);
            From = from;
            Where = new SqlWhere();
        }

        public bool Distinct { get; set; }
        public bool Count { get; set; }
        public List<SqlColumn> Columns { get; set; }
        public SqlTable From { get; set; }
        public List<SqlJoin> Joins { get; set; }
        public SqlWhere Where { get; set; }

        public SqlOrderBy OrderBy { get; set; }
        public SqlPaging Paging { get; set; }

        public void AddColumns(params SqlColumn[] columns)
        {
            foreach (var col in columns)
            {
                if (Columns.Find(c => c.Name.Equals(col.Name, StringComparison.InvariantCultureIgnoreCase) && c.Table == col.Table) == null)
                    Columns.Add(col);
            }
        }

        public void AddJoin(SqlTable tbl, SqlColumn col1, SqlColumn col2, SqlJoinType type = SqlJoinType.Left)
        {
            Joins.Add(new SqlJoin(From, tbl, type, col1, col2));
        }

        public SqlCommand ToSqlCommand(SqlConnection connection = null, SqlTransaction transaction = null)
        {
            StringBuilder sql = new StringBuilder();
            if (Distinct)
                sql.AppendFormat("SELECT DISTINCT ");
            else
                sql.AppendFormat("SELECT ");
            if (Paging != null && !Count)
            {
                sql.AppendFormat("ROW_NUMBER() OVER (ORDER BY [{0}].[{1}] {2}) as [__ROWNUMBER__],", OrderBy.Column.Table.Alias, OrderBy.Column.Alias, OrderBy.Desc ? "desc" : "");
            }
            if(Count)
            {
                sql.Append(" COUNT(*) ");
            }

            if (!Count)
            {
                for (int i = 0; i < Columns.Count; i++)
                {
                    var col = Columns[i];
                    sql.AppendFormat("[{0}].[{1}] as [{0}_{1}]", col.Table.Alias, col.Name);

                    if (i < Columns.Count - 1)
                        sql.Append(", ");
                }
            }

            sql.AppendFormat("\nFROM [{0}] as [{1}]\n", From.Name, From.Alias);
            foreach (var join in Joins)
            {
                sql.AppendFormat("\n{0} JOIN [{1}] as [{2}] ON [{3}].[{4}] = [{5}].[{6}]", join.Type, join.Right.Name, join.Right.Alias, join.Column1.Table.Alias, join.Column1.Name, join.Column2.Table.Alias, join.Column2.Name);
            }

            if (!Where.IsEmpty)
                Where.WriteSql(sql);

            if (Count)
            {
                var cmd = new SqlCommand(sql.ToString(), connection, transaction);
                Where.WriteParameters(cmd);
                return cmd;
            } 
            else if (Paging != null)
            {
                StringBuilder wrap = new StringBuilder();
                wrap.Append("SELECT ");
                for (int i = 0; i < Columns.Count; i++)
                {
                    var col = Columns[i];
                    wrap.AppendFormat("[{0}_{1}]", col.Table.Alias, col.Name);

                    if (i < Columns.Count - 1)
                        wrap.Append(", ");
                }
                wrap.AppendFormat("FROM ({0}) T WHERE [__ROWNUMBER__] between {1} and {2}", sql.ToString(), Paging.StartIndex, Paging.EndIndex);
                var cmd = new SqlCommand(wrap.ToString(), connection, transaction);
                Where.WriteParameters(cmd);
                return cmd;
            }
            else
            {
                if (OrderBy != null)
                    sql.AppendFormat("\nORDER BY [{0}].[{1}] {2}", OrderBy.Column.Table.Alias, OrderBy.Column.Alias, OrderBy.Desc ? "desc" : "");
                var cmd = new SqlCommand(sql.ToString(), connection, transaction);
                Where.WriteParameters(cmd);
                return cmd;
            }
        }
    }

    public class SqlOrderBy
    {
        public SqlOrderBy(SqlColumn column, bool desc = false)
        {
            Column = column;
            Desc = desc;
        }

        public SqlColumn Column { get; set; }
        public bool Desc { get; set; }
    }

    public class SqlPaging
    {
        public SqlPaging(int start, int end)
        {
            StartIndex = start;
            EndIndex = end;
        }

        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }

    public class SqlWhere
    {
        //TODO: use sql parameters
        private List<string> _conditions = new List<string>();
        private List<KeyValuePair<string, object>> _params = new List<KeyValuePair<string, object>>();
        private int gid = 1;

        public void Is(SqlColumn col, object value)
        {
            if (value is string)
                _conditions.Add(string.Format("[{0}].[{1}] LIKE @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
            else if (value is DateTime)
                _conditions.Add(string.Format("Convert(Date, [{0}].[{1}]) = @{2}", col.Table.Alias, col.Alias, AddNewParam(((DateTime)value).Date)));
            else
                _conditions.Add(string.Format("[{0}].[{1}] = @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void GreaterThen(SqlColumn col, object value)
        {
            if (value is string)
                throw new NotImplementedException();
            else if (value is DateTime)
                _conditions.Add(string.Format("Convert(Date, [{0}].[{1}]) > @{2}", col.Table.Alias, col.Alias, AddNewParam(((DateTime)value).Date)));
            else
                _conditions.Add(string.Format("[{0}].[{1}] > @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void GreaterThenOrEqual(SqlColumn col, object value)
        {
            if (value is string)
                throw new NotImplementedException();
            else if (value is DateTime)
                _conditions.Add(string.Format("Convert(Date, [{0}].[{1}]) >= @{2}", col.Table.Alias, col.Alias, AddNewParam(((DateTime)value).Date)));
            else
                _conditions.Add(string.Format("[{0}].[{1}] >= @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void LessThen(SqlColumn col, object value)
        {
            if (value is string)
                throw new NotImplementedException();
            else if (value is DateTime)
                _conditions.Add(string.Format("Convert(Date, [{0}].[{1}]) < @{2}", col.Table.Alias, col.Alias, AddNewParam(((DateTime)value).Date)));
            else
                _conditions.Add(string.Format("[{0}].[{1}] < @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void LessThenOrEqual(SqlColumn col, object value)
        {
            if (value is string)
                throw new NotImplementedException();
            else if (value is DateTime)
                _conditions.Add(string.Format("Convert(Date, [{0}].[{1}]) <= @{2}", col.Table.Alias, col.Alias, AddNewParam(((DateTime)value).Date)));
            else
                _conditions.Add(string.Format("[{0}].[{1}] <= @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void Between(SqlColumn col, object from, object to)
        {
            _conditions.Add(string.Format("[{0}].[{1}] BETWEEN @{2} AND @{3}", col.Table.Alias, col.Alias, AddNewParam(from), AddNewParam(to)));
        }

        public void In(SqlColumn col, IEnumerable values)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0}].[{1}] IN (", col.Table.Alias, col.Alias);

            foreach (var v in values)
            {
                sb.AppendFormat("@{0},", AddNewParam(v));
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(")");
            _conditions.Add(sb.ToString());

        }

        public void StartsWith(SqlColumn col, string value)
        {
            _conditions.Add(string.Format("[{0}].[{1}] LIKE @{2} + N'%'", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void EndsWith(SqlColumn col, string value)
        {
            _conditions.Add(string.Format("[{0}].[{1}] LIKE N'%'+ @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void Like(SqlColumn col, string value)
        {

            _conditions.Add(string.Format("[{0}].[{1}] LIKE N'%' + @{2} + N'%'", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public void IsNot(SqlColumn col, object value)
        {
            if (value is string)
                _conditions.Add(string.Format("[{0}].[{1}] NOT LIKE @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
            else
                _conditions.Add(string.Format("[{0}].[{1}] <> @{2}", col.Table.Alias, col.Alias, AddNewParam(value)));
        }

        public bool IsEmpty
        {
            get
            {
                return _conditions.Count == 0;
            }
        }

        public void WriteSql(StringBuilder sql)
        {
            sql.AppendFormat("\nWHERE {0}", string.Join(" AND ", _conditions));
        }

        public void WriteParameters(SqlCommand cmd)
        {
            foreach (var p in _params)
            {
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            }
        }

        private string AddNewParam(object value)
        {
            var pname = string.Format("_whereparam{0}", gid++);
            _params.Add(new KeyValuePair<string, object>(pname, value));
            return pname;
        }

        //private string ValueToString(object value)
        //{
        //    if (value is string)
        //        return string.Format("N'{0}'", value);
        //    else if (value is decimal)
        //        return ((decimal)value).ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
        //    else if (value is bool)
        //        return (bool)value ? "1" : "0";
        //    else
        //        return value.ToString();
        //}

    }

    public class SqlJoin
    {
        public SqlJoin(SqlTable left, SqlTable right, SqlJoinType type, SqlColumn col1, SqlColumn col2)
        {
            Left = left;
            Right = right;
            Type = type;
            Column1 = col1;
            Column2 = col2;
        }

        public SqlTable Left { get; set; }
        public SqlTable Right { get; set; }
        public SqlJoinType Type { get; set; }
        public SqlColumn Column1 { get; set; }
        public SqlColumn Column2 { get; set; }
    }

    public enum SqlJoinType
    {
        Left,
        Right,
        Inner
    }

    public class SqlColumn
    {

        public SqlColumn(string name, SqlTable table)
        {
            Name = name;
            Alias = name;
            Table = table;
        }

        public string Name { get; set; }
        public string Alias { get; set; }

        public SqlTable Table { get; set; }
    }

    public class SqlTable
    {

        public SqlTable(string name)
        {
            Name = name;
            Alias = name;
        }

        public SqlTable(string name, string alias)
        {
            Name = name;
            Alias = alias;
        }

        public string Name { get; set; }
        public string Alias { get; set; }
    }
}
