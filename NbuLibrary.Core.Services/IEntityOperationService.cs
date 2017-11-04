using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public enum InspectionResult
    {
        None,
        Allow,
        Deny
    }
    public interface IEntityOperationInspector
    {
        InspectionResult Inspect(EntityOperation operation);
    }

    public interface IEntityQueryInspector
    {
        InspectionResult InspectQuery(EntityQuery2 query);
        InspectionResult InspectResult(EntityQuery2 query, IEnumerable<Entity> entities);
    }

    public class EntityOperationContext
    {
        private Dictionary<string, object> _data;

        public EntityOperationContext()
        {
            _data = new Dictionary<string, object>();
        }

        public void Set<T>(string key, T data)
        {
            _data[key] = data;
        }

        public T Get<T>(string key)
        {
            object o = null;
            if (_data.TryGetValue(key, out o))
                return (T)o;
            else
                return default(T);

        }
    }

    public interface IEntityOperationLogic
    {
        void Before(EntityOperation operation, EntityOperationContext context);
        void After(EntityOperation operation, EntityOperationContext context, EntityOperationResult result);
    }

    public interface IEntityOperationService
    {
        EntityOperationResult Update(EntityUpdate update);
        EntityOperationResult Delete(EntityDelete delete);
        IEnumerable<Entity> Query(EntityQuery2 query);
        int Count(EntityQuery2 query);
    }

    public interface IEntityRepository
    {
        int Create(Entity entity);
        Entity Read(EntityQuery2 query);
        void Update(Entity entity);
        void UpdateRelation(Entity entity, Relation relation);
        void Delete(Entity entity, bool recursive = false);
        IEnumerable<Entity> Search(EntityQuery2 query);

        int Count(EntityQuery2 query);

        void Attach(Entity entity, Relation relation);
        void Detach(Entity entity, Relation relation);
    }

    public class EntityRuleViolationException : Exception
    {
        public EntityRuleViolationException()
        {

        }

        public EntityRuleViolationException(string message)
            : base(message)
        {

        }
    }

    public class RelationExistsException : Exception
    {
        public RelationExistsException()
            : base()
        {

        }

        public RelationExistsException(string message)
            : base(message)
        {

        }

        public RelationExistsException(Exception inner)
            : base("Relation with this item exists.", inner)
        {

        }
    }

    public class UniqueRuleViolationException : EntityRuleViolationException
    {
        public UniqueRuleViolationException()
        {

        }

        public UniqueRuleViolationException(UniqueRuleModel rule)
            : base(string.Format("Unique rule violation for properties: {0}", string.Join(", ", rule.Properties.Select(p => p.Name).ToArray())))
        {
            Rule = rule;
        }

        public UniqueRuleModel Rule { get; private set; }
    }

    public class RequiredRuleViolationException : EntityRuleViolationException
    {

    }

    public class EntityOperationResult
    {
        private EntityOperationResult(bool success, params EntityOperationError[] errors)
        {
            Success = success;
            Errors = new List<EntityOperationError>(errors);
            Data = new Dictionary<string, object>();
        }

        public static EntityOperationResult SuccessResult()
        {
            return new EntityOperationResult(true);
        }

        public static EntityOperationResult FailResult(params EntityOperationError[] errors)
        {
            return new EntityOperationResult(false, errors);
        }

        public bool Success { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public List<EntityOperationError> Errors { get; set; }
    }

    public class EntityOperationError
    {
        public const string RELATION_EXISTS = "RelationExists";
        public const string ACCESS_VIOLATION = "AccessViolation";
        public const string UNIQUE_RULE_VIOLATION = "UniqueRuleViolation";
        public const string UNKNOWN = "AccessViolation";

        public EntityOperationError(string message, string type = UNKNOWN)
        {
            Message = message;
            ErrorType = type;
        }

        public string Message { get; set; }
        public string ErrorType { get; set; }
    }


    public class EntityQueryInclude
    {

        public EntityQueryInclude()
        {

        }

        public EntityQueryInclude(string role, string entity)
        {
            Role = role;
            Entity = entity;
        }
        public string Role { get; set; }
        public string Entity { get; set; }
        public Paging Paging { get; set; }
        public Sorting SortBy { get; set; }
    }
    public class EntityQuery2
    {
        public EntityQuery2()
        {
            Properties = new List<string>();
            Rules = new List<Condition>();
            Includes = new List<EntityQueryInclude>();
            RelatedTo = new List<RelationQuery>();
        }

        public EntityQuery2(string entity)
            : this()
        {
            Entity = entity;
        }

        public EntityQuery2(string entity, int id)
            : this(entity)
        {
            Rules.Add(new Condition("Id", Condition.Is, id));
        }

        public string Entity { get; set; }
        public List<string> Properties { get; set; }
        public bool AllProperties { get; set; }

        public Paging Paging { get; set; }
        public Sorting SortBy { get; set; }

        public List<Condition> Rules { get; set; }
        public List<EntityQueryInclude> Includes { get; set; }
        public List<RelationQuery> RelatedTo { get; set; }


        public void AddProperty(string property)
        {
            var p = property.ToLower();
            if (!Properties.Contains(p))
                Properties.Add(p);
        }
        public void AddProperties(params string[] properties)
        {
            foreach (var p in properties)
                AddProperty(p);
        }
        public void Include(string entity, string role)
        {
            var ex = Includes.Find(i => i.Entity.Equals(entity, StringComparison.InvariantCultureIgnoreCase) && i.Role.Equals(role, StringComparison.InvariantCultureIgnoreCase));
            if (ex == null)
                Includes.Add(new EntityQueryInclude(role, entity));
        }

        public void WhereIs(string property, object value)
        {
            Rules.Add(new Condition(property, Condition.Is, value));
        }
        public void WhereLessThenOrEqual(string property, object value)
        {
            Rules.Add(new Condition(property, Condition.LessThenOrEqual, value));
        }
        public void WhereLessThen(string property, object value)
        {
            Rules.Add(new Condition(property, Condition.LessThen, value));
        }
        public void WhereGreaterThen(string property, object value)
        {
            Rules.Add(new Condition(property, Condition.GreaterThen, value));
        }
        public void WhereGreaterThenOrEqual(string property, object value)
        {
            Rules.Add(new Condition(property, Condition.GreaterThenOrEqual, value));
        }
        public void WhereStartsWith(string property, object value)
        {
            Rules.Add(new Condition(property, Condition.StartsWith, value));
        }
        public void WhereEndsWith(string property, object value)
        {
            Rules.Add(new Condition(property, Condition.EndsWith, value));
        }
        public void WhereAnyOf(string property, object[] values)
        {
            Rules.Add(new Condition(property, Condition.AnyOf, values));
        }
        public void WhereBetween(string property, object from, object to)
        {
            Rules.Add(new Condition(property, Condition.Between, new object[] { from, to }));
        }
        public void WhereRelated(RelationQuery relatedQuery) { RelatedTo.Add(relatedQuery); }

        public bool IsForEntity(string entity)
        {
            return this.Entity.Equals(entity, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool HasProperty(string property)
        {
            return AllProperties || Properties.Contains(property, StringComparer.InvariantCultureIgnoreCase);
        }

        public bool HasInclude(string entity, string role)
        {
            return Includes.Find(i => i.Entity.Equals(entity, StringComparison.InvariantCultureIgnoreCase) && i.Role.Equals(role, StringComparison.InvariantCultureIgnoreCase)) != null;
        }

        public RelationQuery GetRelatedQuery(string withEntity, string role)
        {
            return RelatedTo.Find(rq => rq.IsForEntity(withEntity) && rq.Role.Equals(role, StringComparison.InvariantCultureIgnoreCase));
        }

        public Condition GetRuleByProperty(string property)
        {
            return Rules.Find(r => r.IsForProperty(property));
        }
    }

    public class RelationQuery : EntityQuery2
    {
        public RelationQuery()
        {
            RelationRules = new List<Condition>();
        }

        public RelationQuery(string entity, string role)
            : base(entity)
        {
            Role = role;
            RelationRules = new List<Condition>();
        }

        public RelationQuery(string entity, string role, int id)
            : base(entity, id)
        {
            Role = role;
            RelationRules = new List<Condition>();
        }

        public string Role { get; set; }
        public List<Condition> RelationRules { get; set; }


        public int? GetSingleId()
        {
            var rule = Rules.Find(r => r.IsForProperty("id"));
            if (rule != null && rule.Operator == Condition.Is)
                return Convert.ToInt32(rule.Values.Single());
            else
                return null;
        }
    }

    public class EntityUpdate : EntityOperation
    {
        public EntityUpdate()
        {
            PropertyUpdates = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            RelationUpdates = new List<RelationUpdate>();
        }

        public EntityUpdate(string entity)
            : this()
        {
            Entity = entity;
        }

        public EntityUpdate(string entity, int id)
            : this()
        {
            Entity = entity;
            Id = id;
        }

        public EntityUpdate(Entity e)
            : this(e.Name)
        {
            if (e.Id > 0) Id = e.Id;
            foreach (var d in e.Data)
                this.Set(d.Key, d.Value);
        }

        public Dictionary<string, object> PropertyUpdates { get; set; }
        public List<RelationUpdate> RelationUpdates { get; set; }

        public void Set(string property, object value)
        {
            PropertyUpdates[property] = value;
        }

        public T Get<T>(string property)
        {
            object o = null;
            if (!PropertyUpdates.TryGetValue(property, out o))
                return default(T);
            else
                return ConvertValue<T>(o);
        }

        public RelationUpdate Attach(string entity, string role, int id)
        {
            var ru = new RelationUpdate(entity, role, RelationOperation.Attach, id);
            RelationUpdates.Add(ru);
            return ru;
        }
        public void Detach(string entity, string role, int id)
        {
            RelationUpdates.Add(new RelationUpdate(entity, role, RelationOperation.Detach, id));
        }

        public bool ContainsProperty(string property) { return PropertyUpdates.ContainsKey(property); }
        public bool ContainsRelation(string withEntity, string role) { return RelationUpdates.Find(r => r.Entity.Equals(withEntity, StringComparison.InvariantCultureIgnoreCase) && r.Role.Equals(role, StringComparison.InvariantCultureIgnoreCase)) != null; }
        public RelationUpdate GetRelationUpdate(string withEntity, string role) { return RelationUpdates.Find(r => r.Entity.Equals(withEntity, StringComparison.InvariantCultureIgnoreCase) && r.Role.Equals(role, StringComparison.InvariantCultureIgnoreCase)); }
        public IEnumerable<RelationUpdate> GetMultipleRelationUpdates(string withEntity, string role) { return RelationUpdates.Where(r => r.Entity.Equals(withEntity, StringComparison.InvariantCultureIgnoreCase) && r.Role.Equals(role, StringComparison.InvariantCultureIgnoreCase)); }

        public bool IsCreate() { return !Id.HasValue; }

        public Entity ToEntity()
        {
            return new Entity(Entity, Id.HasValue ? Id.Value : 0)
            {
                Data = PropertyUpdates
            };
        }

        private T ConvertValue<T>(object v)
        {
            if (typeof(T) == typeof(bool))
                return (T)(object)Convert.ToBoolean(v);
            else if (typeof(T) == typeof(decimal))
                return (T)(object)Convert.ToDecimal(v);
            else if (typeof(T) == typeof(int))
                return (T)(object)Convert.ToInt32(v);
            else if (typeof(T) == typeof(DateTime))
                return (T)(object)Convert.ToDateTime(v);
            else if (typeof(T).IsEnum)
            {
                if (v is string)
                {
                    string s = v as string;
                    int integer = 0;
                    if (int.TryParse(s, out integer))
                        return (T)(object)integer;
                    else
                        return (T)Enum.Parse(typeof(T), s);
                }
                else
                    return (T)v;

            }
            else
                return (T)v;
        }
    }

    public class RelationUpdate : EntityUpdate
    {
        public RelationUpdate(string entity, string role, RelationOperation operation, int? id = null)
            : base(entity)
        {
            Id = id;
            Role = role;
            Operation = operation;
        }

        public RelationOperation Operation { get; set; }
        public string Role { get; set; }

        public Relation ToRelation()
        {
            var rel = new Relation(Role, new Entity(Entity));
            if (Id.HasValue)
                rel.Entity.Id = Id.Value;

            foreach (var pu in PropertyUpdates)
                rel.SetData<object>(pu.Key, pu.Value);
            return rel;
        }
    }
}
