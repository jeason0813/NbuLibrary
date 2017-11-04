using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Infrastructure;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NbuLibrary.Web.Controllers
{
    public class EntityRequest
    {
        public int? Id { get; set; }
        public string Entity { get; set; }
    }

    public class PropertyUpdateRequest
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class EntityUpdateRequest : EntityRequest
    {
        public List<PropertyUpdateRequest> PropertyUpdates { get; set; }
        public List<RelationUpdateRequest> RelationUpdates { get; set; }

        public EntityUpdate ToEntityUpdate()
        {
            var update = new EntityUpdate(this.Entity);
            update.Id = this.Id;
            if (PropertyUpdates != null)
                foreach (var pu in PropertyUpdates)
                    update.Set(pu.Name, pu.Value);
            if (RelationUpdates != null)
                foreach (var ru in RelationUpdates)
                {
                    var r = new RelationUpdate(ru.Entity, ru.Role, ru.Operation, ru.Id);
                    if (ru.PropertyUpdates != null)
                        foreach (var pu in ru.PropertyUpdates)
                            r.Set(pu.Name, pu.Value);
                    update.RelationUpdates.Add(r);
                }

            return update;
        }
    }

    public class RelationUpdateRequest : EntityUpdateRequest
    {
        public RelationOperation Operation { get; set; }
        public string Role { get; set; }
    }

    public class EntityModelJson
    {
        public EntityModelJson()
        {

        }

        public EntityModelJson(EntityModel em)
        {
            Name = em.Name;
            IsNomenclature = em.IsNomenclature;
            Properties = em.Properties.ToList();
            Relations = em.Relations.Select(rm => new RelationModelJson(rm)).ToList();
            Rules = em.Rules.ToList();
        }

        public string Name { get; set; }
        public bool IsNomenclature { get; set; }
        public List<PropertyModel> Properties { get; set; }
        public List<RelationModelJson> Relations { get; set; }
        public List<EntityRuleModel> Rules { get; set; }
    }

    public class RelationModelJson : EntityModelJson
    {
        public RelationModelJson()
        {

        }

        public RelationModelJson(RelationModel rm)
            : base(rm)
        {
            Role = rm.Role;
            Type = rm.Type;
            LeftEntity = rm.Left.Name;
            RightEntity = rm.Right.Name;
        }

        public string Role { get; set; }
        public RelationType Type { get; set; }
        public string LeftEntity { get; set; }
        public string RightEntity { get; set; }
    }

    public class EntityController : ApiController
    {
        private IEntityOperationService _entityService;
        private IDomainModelService _domainService;

        public EntityController(IEntityOperationService entityService, IDomainModelService domainService)
        {
            _entityService = entityService;
            _domainService = domainService;
        }

        public EntityOperationResult Update(EntityUpdateRequest updateRequest)
        {
            return _entityService.Update(updateRequest.ToEntityUpdate());
        }

        [HttpPost]
        public EntityOperationResult Delete(EntityDelete delete)
        {
            return _entityService.Delete(delete);
        }

        public IEnumerable<Entity> Search(EntityQuery2 query)
        {
            return _entityService.Query(query);
        }

        public int Count(EntityQuery2 query)
        {
            return _entityService.Count(query);
        }

        [HttpGet]
        public IDictionary<int, string> GetEnum(string enumClass)
        {
            var res = new Dictionary<int, string>();
            var enumType = Type.GetType(enumClass);
            foreach (var name in Enum.GetNames(enumType))
            {
                int val = (int)Enum.Parse(enumType, name);
                res.Add(val, name);
            }

            return res;
        }

        [HttpPost]
        public EntityModelJson GetEntityModel(string name)
        {
            return new EntityModelJson(_domainService.Domain.Entities[name]);
        }
    }

    //public class EntityController : ApiController
    //{
    //    private IEntityService _entityService;
    //    private IEntityDefinitionService _entityDefinitionService;
    //    private IBusinessLogic[] _businessLogic;
    //    public EntityController(IEntityService entityService, IBusinessLogic[] businessLogic, IEntityDefinitionService entityDefinitionService)
    //    {
    //        _entityService = entityService;
    //        _businessLogic = businessLogic;
    //        _entityDefinitionService = entityDefinitionService;
    //    }

    //    [HttpPost]
    //    public IDictionary<string, object> Get(EntityRequest request)
    //    {
    //        bool allowed = false;
    //        var logic = applyBusinessLogic(request.Query, out allowed);

    //        if (!allowed)
    //            throw new Exception("Operation is not allowed.");

    //        var ctx = new BLContext();

    //        logic.ForEach(bl => bl.Prepare(request.Query, ref ctx));

    //        var idCond = request.Query.PropertyRules.First(p => p.Property.Equals("Id", StringComparison.InvariantCultureIgnoreCase));
    //        if (idCond != null)
    //        {
    //            var result = Search(request).SingleOrDefault();
    //            var response = new BLResponse();
    //            logic.ForEach(bl => bl.Complete(request.Query, ref ctx, ref response));
    //            return result;
    //            //var entity = _entityService.Read(new EntityKey() { Id = Convert.ToInt32(idCond.Values.Single()), EntityName = request.Query.EntityName }).GetRawData();
    //            //return entity;
    //        }
    //        else
    //            throw new NotImplementedException();
    //    }

    //    [HttpGet]
    //    public IDictionary<byte, string> GetEnum(string enumClass)
    //    {
    //        var res = new Dictionary<byte, string>();
    //        var enumType = Type.GetType(enumClass);
    //        foreach (var name in Enum.GetNames(enumType))
    //        {
    //            byte val = (byte)Enum.Parse(enumType, name);
    //            res.Add(val, name);
    //        }

    //        return res;
    //    }

    //    [HttpPost]
    //    public IEnumerable<IDictionary<string, object>> Search(EntityRequest request)
    //    {
    //        bool allowed = false;
    //        var logic = applyBusinessLogic(request.Query, out allowed);
    //        if (!allowed)
    //            throw new Exception("Operation is not allowed.");
    //        var ctx = new BLContext();
    //        logic.ForEach(bl => bl.Prepare(request.Query, ref ctx));

    //        var res = _entityService.Search(request.Query, request.SortBy, null).Select(e => e.GetRawData());
    //        if (request.Includes != null)
    //            foreach (var include in request.Includes)
    //            {
    //                if (include.RelationRules == null)
    //                    include.RelationRules = new List<RelationCondition>();

    //                var relCond = new RelationCondition();
    //                include.RelationRules.Add(relCond);
    //                int idx = include.RelationRules.IndexOf(relCond);
    //                foreach (var e in res)
    //                {
    //                    include.RelationRules[idx] = new RelationCondition();
    //                    include.RelationRules[idx].EntityName = request.Query.EntityName;
    //                    include.RelationRules[idx].Role = include.Role;
    //                    include.RelationRules[idx].PropertyRules.Add(new Condition("Id", Condition.Is, e["Id"]));
    //                    e.Add(string.Format("{0}_{1}", include.EntityName, include.Role), _entityService.Search(include, null, null).Select(en => en.GetRawData()));
    //                }
    //            }

    //        var response = new BLResponse();
    //        logic.ForEach(bl => bl.Complete(request.Query, ref ctx, ref response));

    //        return res;
    //    }

    //    [HttpPost]
    //    public object Update(EntityUpdate update)
    //    { 
    //        bool allowed = false;
    //        var logic = applyBusinessLogic(update, out allowed);
    //        if (!allowed)
    //            throw new Exception("Operation is not allowed.");


    //        var ctx = new BLContext();
    //        logic.ForEach(bl => bl.Prepare(update, ref ctx));
    //        _entityService.ProcessUpdate(update);

    //        var response = new BLResponse();
    //        logic.ForEach(bl => bl.Complete(update, ref ctx, ref response));

    //        return new { success = true };
    //    }

    //    [HttpPost]
    //    public object Delete(EntityDelete delete)
    //    {
    //        bool allowed = false;
    //        var logic = applyBusinessLogic(delete, out allowed);
    //        if (!allowed)
    //            throw new Exception("Operation is not allowed.");

    //        var ctx = new BLContext();
    //        logic.ForEach(bl => bl.Prepare(delete, ref ctx));

    //        try
    //        {
    //            _entityService.Delete(delete);
    //        }
    //        catch (RelationExistsException ex)
    //        {
    //            System.Diagnostics.Trace.WriteLine(ex);
    //            return new { success = false, error = ex.Message, errorType = "RelationExistsException" };
    //        }

    //        var response = new BLResponse();
    //        logic.ForEach(bl => bl.Complete(delete, ref ctx, ref response));

    //        return new { success = true };
    //    }

    //    [HttpPost]
    //    public object GetDefinition(string entity)
    //    {
    //        return _entityDefinitionService.GetDefinitionByName(entity);
    //    }

    //    private List<IBusinessLogic> applyBusinessLogic(EntityOperation operation, out bool allowed)
    //    {
    //        List<IBusinessLogic> logic = new List<IBusinessLogic>();
    //        allowed = false;
    //        foreach (var bl in _businessLogic)
    //        {
    //            var resol = bl.Resolve(operation);
    //            if (resol == OperationResolution.Allowed)
    //            {
    //                allowed = true;
    //                logic.Add(bl);
    //            }
    //            else if (resol == OperationResolution.Regonized)
    //                logic.Add(bl);
    //            else if (resol == OperationResolution.Forbidden) //Forbidden is more powerful then allowed
    //            {
    //                allowed = false;
    //                break;
    //            }
    //        }

    //        return logic;
    //    }
    //}
}
