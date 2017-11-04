using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NbuLibrary.Core.Services
{
    public delegate T RetrieveData<T>(object param);

    public class CacheKey
    {
        public string Region { get; private set; }
        public string ItemKey { get; private set; }

        public CacheKey(string itemKey, string region = null)
        {
            if (itemKey.StartsWith("__"))
                throw new NotSupportedException("ItemKey must not start with \"__\" - it is reserved.");

            this.ItemKey = itemKey;
            this.Region = region;
        }

        public override string ToString()
        {
            return string.Format("__{0}__{1}", Region, ItemKey);
        }
    }

    public interface ICacheService
    {
        /// <summary>
        /// Retrieves item using cache with sliding expiration - if the item is found in the cache, its expiration countdown will be reset. Otherwise, it will be retrieved from the data source using the delegate.
        /// </summary>
        /// <typeparam name="T">Type of the item to retrieve.</typeparam>
        /// <param name="key">CacheKey of the item.</param>
        /// <param name="retrieve">Delegate to use in case the item is not found in the cache.</param>
        /// <param name="ttl">Time-To-Live - the interval for the sliding expiration.</param>
        /// <returns>The requested item.</returns>
        T Get<T>(CacheKey key, RetrieveData<T> retrieve, TimeSpan ttl);

        /// <summary>
        /// Retrieves item using cache with absolute expiration. If the item is found, it is returned from the cache. Otherwise, it will be retrieved from the data source using the delegate.
        /// </summary>
        /// <typeparam name="T">Type of te item to retrieve</typeparam>
        /// <param name="key">CacheKey for the item.</param>
        /// <param name="retrieve">Delegate to use when the item is not found in the cache.</param>
        /// <param name="expires">The DateTime when the cache entry expires.</param>
        /// <returns>The requested item.</returns>
        T Get<T>(CacheKey key, RetrieveData<T> retrieve, DateTime expires);

        /// <summary>
        /// Clears all items currently cached (e.g. to free memory).
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Clears all items cached in the specific region.
        /// </summary>
        /// <param name="region">Name of the region to be cleared.</param>
        void ClearRegion(string region);
    }
}
