using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GPS.SimpleCache
{
    public sealed partial class SimpleCache<K, V>
    {
        public class DuplicateKeysException<TCacheItem> : ApplicationException where TCacheItem : ICacheItem<K, V>
        {
            public ConcurrentBag<DuplicateKeyException<TCacheItem>> CacheItems =
                new ConcurrentBag<DuplicateKeyException<TCacheItem>>();

            public DuplicateKeysException(ConcurrentBag<DuplicateKeyException<TCacheItem>> exceptions)
            {
                Parallel.ForEach(exceptions, CacheItems.Add);
            }
        }
    }

}
