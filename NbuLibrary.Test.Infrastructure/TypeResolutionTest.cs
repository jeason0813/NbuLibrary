using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NbuLibrary.Core.Domain;
using NbuLibrary.Core.Infrastructure;
using NbuLibrary.Core.Kernel;
using NbuLibrary.Core.Services;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using System.Text;

namespace NbuLibrary.Test.Infrastructure
{
    [TestClass]
    public class TypeResolutionTest
    {
        [TestMethod]
        public void Test_One()
        {
            var entSvc1 = ApplicationKernel.Current.Get<IEntityService>();
            Assert.IsNotNull(entSvc1);
        }

        [TestMethod]
        public void Test_NotificationService_SendEmail()
        {
            var body = new StringBuilder();
            body.AppendFormat("Hello, {0}\n", "Kiril");
            body.AppendLine("This is a test email to see if the notification sending functionallity is working properly. \nPlease, do not respond to this email.\n\nRegards,\n The System");

            var notSvc = new NotificationServiceImpl();
            notSvc.SendEmail("kirilvuchkov@gmail.com", "Test Email NbuLib", body.ToString(), null);
        }
    }

    public class TestModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind<IEntityService>().To<EntitySvcGen>();
        }
    }
    
    public class EntitySvcGen : IEntityService
    {
        public void Create<TEntityType>(TEntityType entity) where TEntityType : IEntity
        {
            Trace.WriteLine("Generic");
        }

        public TEntityType Read<TEntityType>(int id) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public void Update<TEntityType>(TEntityType entity) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public void Delete<TEntityType>(int id) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public void Attach<TEntityType, TEntityType2>(TEntityType entity, TEntityType2 relateTo)
            where TEntityType : IEntity
            where TEntityType2 : IEntity
        {
            throw new NotImplementedException();
        }

        public void Detach<TEntityType, TEntityType2>(TEntityType entity, TEntityType2 relatedEntity)
            where TEntityType : IEntity
            where TEntityType2 : IEntity
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<TEntityType> Search<TEntityType>(System.Collections.Generic.IEnumerable<ICondition> conditions, System.Collections.Generic.IEnumerable<Sorting> sortings = null, System.Collections.Generic.IEnumerable<string> groupings = null, Paging paging = null) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<TEntityType2> SearchRelated<TEntityType, TEntityType2>(TEntityType master, System.Collections.Generic.IEnumerable<ICondition> conditions, System.Collections.Generic.IEnumerable<Sorting> sortings = null, System.Collections.Generic.IEnumerable<string> groupings = null, Paging paging = null)
            where TEntityType : IEntity
            where TEntityType2 : IEntity
        {
            throw new NotImplementedException();
        }


        public void Attach<TEntityType, TEntityType2>(TEntityType entity, TEntityType2 relateTo, string role)
            where TEntityType : IEntity
            where TEntityType2 : IEntity
        {
            throw new NotImplementedException();
        }

        public void Detach<TEntityType, TEntityType2>(TEntityType entity, TEntityType2 relatedEntity, string role)
            where TEntityType : IEntity
            where TEntityType2 : IEntity
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<TEntityType2> SearchRelated<TEntityType, TEntityType2>(TEntityType master, string role, System.Collections.Generic.IEnumerable<ICondition> conditions = null, System.Collections.Generic.IEnumerable<Sorting> sortings = null, System.Collections.Generic.IEnumerable<string> groupings = null, Paging paging = null)
            where TEntityType : IEntity
            where TEntityType2 : IEntity
        {
            throw new NotImplementedException();
        }


        public System.Collections.Generic.IEnumerable<TEntityType> Search<TEntityType>(System.Collections.Generic.IEnumerable<Condition> conditions, System.Collections.Generic.IEnumerable<Sorting> sortings = null, System.Collections.Generic.IEnumerable<string> groupings = null, Paging paging = null) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<TEntityType2> SearchRelated<TEntityType, TEntityType2>(TEntityType master, string role, System.Collections.Generic.IEnumerable<Condition> conditions = null, System.Collections.Generic.IEnumerable<Sorting> sortings = null, System.Collections.Generic.IEnumerable<string> groupings = null, Paging paging = null)
            where TEntityType : IEntity
            where TEntityType2 : IEntity
        {
            throw new NotImplementedException();
        }


        public System.Collections.Generic.IEnumerable<TEntityType> Search<TEntityType>(string entityName, System.Collections.Generic.IEnumerable<Condition> conditions, System.Collections.Generic.IEnumerable<Sorting> sortings = null, System.Collections.Generic.IEnumerable<string> groupings = null, Paging paging = null) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }


        public TEntityType Read<TEntityType>(string entityName, int id) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public void Delete<TEntityType>(string entityName, int id) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }


        public TEntityType Read<TEntityType>(TEntityType entity) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public void Delete<TEntityType>(TEntityType entity) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }


        public System.Collections.Generic.IEnumerable<TEntityType> Search<TEntityType>(Core.Services.tmp.EntityQuery query, System.Collections.Generic.IEnumerable<Sorting> sortings = null, Paging paging = null) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<TEntityType> Search<TEntityType>(string entityName, Core.Services.tmp.EntityQuery query, System.Collections.Generic.IEnumerable<Sorting> sortings = null, Paging paging = null) where TEntityType : IEntity
        {
            throw new NotImplementedException();
        }

        public void Create(IEntity entity)
        {
            throw new NotImplementedException();
        }

        public void Read(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(IEntity entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public void Attach(IEntity entity, IEntity relateTo, string role)
        {
            throw new NotImplementedException();
        }

        public void Detach(IEntity entity, IEntity relatedEntity, string role)
        {
            throw new NotImplementedException();
        }

        public System.Collections.Generic.IEnumerable<IEntity> Search(Core.Services.tmp.EntityQuery query, System.Collections.Generic.IEnumerable<Sorting> sortings = null, Paging paging = null)
        {
            throw new NotImplementedException();
        }


        public IEntity Read(EntityKey key)
        {
            throw new NotImplementedException();
        }

        public void Delete(EntityKey key)
        {
            throw new NotImplementedException();
        }

        public void ProcessUpdate(Core.Services.tmp.EntityUpdate update)
        {
            throw new NotImplementedException();
        }


        public void Delete(Core.Services.tmp.EntityDelete key)
        {
            throw new NotImplementedException();
        }
    }
}
