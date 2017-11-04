using NbuLibrary.Core.DataModel;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Services;
using NbuLibrary.Core.Services.tmp;
using NbuLibrary.Core.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Infrastructure
{
    public class EntityOperationService : IEntityOperationService
    {
        private IEntityRepository _repository;
        private IEnumerable<IEntityOperationInspector> _inspectors;
        private IEnumerable<IEntityQueryInspector> _queryInspectors;
        private IEnumerable<IEntityOperationLogic> _logics;
        private IDatabaseService _dbService;

        public EntityOperationService(IEntityRepository repository, IDatabaseService dbService, IEnumerable<IEntityOperationInspector> inspectors, IEnumerable<IEntityQueryInspector> queryInspectors, IEnumerable<IEntityOperationLogic> logics)
        {
            _repository = repository;
            _dbService = dbService;
            _inspectors = inspectors ?? new IEntityOperationInspector[0];
            _queryInspectors = queryInspectors ?? new IEntityQueryInspector[0];
            _logics = logics ?? new IEntityOperationLogic[0];
        }

        public EntityOperationResult Update(EntityUpdate update)
        {
            using (var dbContext = _dbService.GetDatabaseContext(true))
            {
                var result = UpdateInternal(update);
                if (result.Success)
                    dbContext.Complete();

                return result;
            }
        }

        public EntityOperationResult Delete(Services.tmp.EntityDelete delete)
        {
            using (var dbContext = _dbService.GetDatabaseContext(true))
            {
                if (!RunInspection(delete))
                {
                    return EntityOperationResult.FailResult(new EntityOperationError("Insufficient rights to perform this operation.", EntityOperationError.ACCESS_VIOLATION));
                }

                var ctx = new EntityOperationContext();
                this.AppyLogicBefore(delete, ctx);

                EntityOperationResult result = null;
                try
                {
                    _repository.Delete(new Entity(delete.Entity, delete.Id.Value), delete.Recursive);
                    result = EntityOperationResult.SuccessResult();
                }
                catch (RelationExistsException ree)
                {
                    result = EntityOperationResult.FailResult(new EntityOperationError(ree.Message, EntityOperationError.RELATION_EXISTS));
                }
                catch (Exception ex)
                {
                    result = EntityOperationResult.FailResult(new EntityOperationError(ex.Message));
                }

                this.AppyLogicAfter(delete, ctx, result);

                if (result.Success)
                    dbContext.Complete();

                return result;
            }
        }

        public IEnumerable<Entity> Query(EntityQuery2 query)
        {
            using (var dbContext = _dbService.GetDatabaseContext(false))
            {
                int allow = 0;
                foreach (var inspector in _queryInspectors)
                {
                    var result = inspector.InspectQuery(query);
                    if (result == InspectionResult.Allow)
                        allow++;
                    else if (result == InspectionResult.Deny)
                        return new Entity[0];
                }

                var queryResult = _repository.Search(query);
                //if (allow == 0)
                //{
                foreach (var inspector in _queryInspectors)
                {
                    var result = inspector.InspectResult(query, queryResult);
                    if (result == InspectionResult.Allow)
                        allow++;
                    else if (result == InspectionResult.Deny)
                        return new Entity[0];
                }
                //}

                if (allow > 0)
                    return queryResult;
                else
                    return new Entity[0];
            }
        }

        public int Count(EntityQuery2 query)
        {
            using (var dbContext = _dbService.GetDatabaseContext(false))
            {
                int allow = 0;
                foreach (var inspector in _queryInspectors)
                {
                    var result = inspector.InspectQuery(query);
                    if (result == InspectionResult.Allow)
                        allow++;
                    else if (result == InspectionResult.Deny)
                        return 0;
                }

                return _repository.Count(query);
            }
        }

        private EntityOperationResult UpdateInternal(EntityUpdate update)
        {
            if (!RunInspection(update))
                return EntityOperationResult.FailResult(new EntityOperationError("Insufficient rights to perform this operation.", EntityOperationError.ACCESS_VIOLATION));

            EntityOperationContext ctx = new EntityOperationContext();

            this.AppyLogicBefore(update, ctx);

            var entity = update.ToEntity();
            EntityOperationResult result = null;
            bool created = false;
            try
            {
                if (update.Id.HasValue)
                {
                    if (update.PropertyUpdates.Count > 0)
                        _repository.Update(entity);
                }
                else
                {
                    update.Id = _repository.Create(entity);
                    created = true;
                }

                //Order by operation - process detach first
                foreach (var relUpdate in update.RelationUpdates.OrderByDescending(ru => ru.Operation))
                {
                    if (relUpdate.Operation == RelationOperation.Attach)
                        _repository.Attach(entity, relUpdate.ToRelation());
                    else if (relUpdate.Operation == RelationOperation.Detach)
                        _repository.Detach(entity, relUpdate.ToRelation());
                    else if (relUpdate.PropertyUpdates.Count > 0)
                    {
                        _repository.UpdateRelation(entity, relUpdate.ToRelation());
                    }
                }

                result = EntityOperationResult.SuccessResult();
                if (created)
                    result.Data.Add("Created", update.Id.Value);
            }
            catch (UniqueRuleViolationException rex)
            {
                result = EntityOperationResult.FailResult(new EntityOperationError(rex.Message, EntityOperationError.UNIQUE_RULE_VIOLATION));
            }
            this.AppyLogicAfter(update, ctx, result);
            return result;
        }

        private bool RunInspection(EntityOperation operation)
        {
            int allow = 0;
            foreach (var inspector in _inspectors)
            {
                var result = inspector.Inspect(operation);
                if (result == InspectionResult.Allow)
                    allow++;
                else if (result == InspectionResult.Deny)
                    return false;
            }

            return allow > 0;
        }

        private void AppyLogicBefore(EntityOperation operation, EntityOperationContext context)
        {
            foreach (var logic in _logics)
                logic.Before(operation, context);

        }
        private void AppyLogicAfter(EntityOperation operation, EntityOperationContext context, EntityOperationResult result)
        {
            foreach (var logic in _logics)
                logic.After(operation, context, result);
        }
    }

    public class EntityRepositoryDomainListener : IDomainChangeListener
    {
        void IDomainChangeListener.BeforeSave(EntityModel entityModel)
        {
            if (!entityModel.Properties.Contains("Id"))
            {
                ModelBuilder mb = new ModelBuilder(entityModel);
                mb.AddIdentity("Id");
            }
        }

        void IDomainChangeListener.AfterSave(EntityModel entityModel)
        {
        }
    }
}
