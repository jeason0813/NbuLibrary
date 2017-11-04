using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services.tmp
{
    public enum OperationResolution
    {
        None,
        Regonized,
        Allowed,
        Forbidden
    }

    public class BLContext
    {
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        public void Set<T>(string key, T ctx)
        {
            _data[key] = ctx;
        }

        public T Get<T>(string key)
        {
            if (_data.ContainsKey(key))
                return (T)_data[key];
            else
                return
                    default(T);
        }
    }

    public class BLResponse
    {

    }

    public interface IBusinessLogic
    {
        OperationResolution Resolve(EntityOperation operation);
        void Prepare(EntityOperation operation, ref BLContext context);
        void Complete(EntityOperation operation, ref BLContext context, ref BLResponse response);
    }
}
