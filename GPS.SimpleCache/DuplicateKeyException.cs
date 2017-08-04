using System;

namespace GPS.SimpleCache
{
    public sealed partial class SimpleCache<K, V>
    {
        public class DuplicateKeyException<TCacheItem> : ApplicationException where TCacheItem : ICacheItem<K, V>
        {
            public TCacheItem Item { get; }

            public DuplicateKeyException(TCacheItem item)
            {
                Item = item;
            }
        }
    }

}
