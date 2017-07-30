using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GPS.SimpleThreading;

namespace GPS.SimpleCache
{
    public sealed class SimpleCache<K, V>
    {
        private CacheExpirationTypes _expirationType = CacheExpirationTypes.OnDemand;
        public CacheExpirationTypes ExpirationType => _expirationType;

        private TimeSpan _cacheExpirationDuration = TimeSpan.Zero;
        public TimeSpan CacheExpirationDuration => _cacheExpirationDuration;

        private readonly ThreadSafeDictionary<K, CacheItem<K, V>> _cacheItems = new ThreadSafeDictionary<K, CacheItem<K, V>>();

        private static readonly object PadLock = new object();
        private Timer _timer;

        public event EventHandler<CacheItem<K, V>> ItemExpired;

        public SimpleCache(
            TimeSpan expirationTimeSpan,
            CacheExpirationTypes expirationType,
            ThreadSafeList<CacheItem<K, V>> items = null)
        {
            _cacheExpirationDuration = expirationTimeSpan;
            _expirationType = expirationType;

            if (items != null)
            {
                Parallel.ForEach(items, AddItem);
            }

            if (ExpirationType == CacheExpirationTypes.Timed)
            {
                _timer = new Timer(s =>
                    {
                        ExpireCacheItems();
                    },
                    null,
                    TimeSpan.FromSeconds(1.0),
                    _cacheExpirationDuration);
            }
        }

        public void AddItem(CacheItem<K, V> item)
        {
            if (item != null && item.Key != null)
            {
                if (_cacheItems.ContainsKey(item.Key))
                {
                    throw new DuplicateKeyException<CacheItem<K, V>>(item);
                }

                _cacheItems.Add(item.Key, item);
            }
        }

        public void AddItems(IEnumerable<CacheItem<K, V>> items)
        {
            var exceptions = new ThreadSafeList<DuplicateKeyException<CacheItem<K, V>>>();

            Parallel.ForEach(items, i =>
            {
                try
                {
                    AddItem(i);
                }
                catch (DuplicateKeyException<CacheItem<K, V>> exception)
                {
                    exceptions.Add(exception);
                    throw;
                }
            });

            if (exceptions.Count > 0)
            {
                throw new DuplicateKeysException<CacheItem<K, V>>(exceptions);
            }
        }

        public bool RemoveItem(CacheItem<K, V> item)
        {
            return _cacheItems.Remove(item.Key);
        }

        public void InvalidateCache()
        {
            _cacheItems.Clear();

            _timer = new Timer(s =>
                {
                    ExpireCacheItems();
                },
                null,
                TimeSpan.FromSeconds(1.0),
                _cacheExpirationDuration);
        }

        public int Count()
        {
            return _cacheItems.Count;
        }

        public void ExpireCacheItems()
        {
            var now = DateTimeOffset.UtcNow;

            Parallel.ForEach(_cacheItems, item =>
            {
                lock (PadLock)
                {
                    if (now - item.Value.LastAccessed >= _cacheExpirationDuration)
                    {
                        _cacheItems.Remove(item);
                        ItemExpired?.Invoke(this, item.Value);
                    }
                }
            });
        }

        public CacheItem<K, V> this[K key]
        {
            get
            {
                if (_cacheItems.ContainsKey(key))
                {
                    var item = _cacheItems[key];

                    if (DateTimeOffset.UtcNow - item.LastAccessed < _cacheExpirationDuration)
                    {
                        return item;
                    }

                    throw new ItemExpiredException();
                }
                throw new ItemNotFoundException();
            }
        }

        #region Exceptions
        public class ItemNotFoundException : Exception
        {
        }

        public class ItemExpiredException : Exception
        {
        }

        public class DuplicateKeysException<TCacheItem> : ApplicationException where TCacheItem : ICacheItem<K, V>
        {
            public ThreadSafeList<DuplicateKeyException<TCacheItem>> CacheItems =
                new ThreadSafeList<DuplicateKeyException<TCacheItem>>();

            public DuplicateKeysException(ThreadSafeList<DuplicateKeyException<TCacheItem>> exceptions)
            {
                Parallel.ForEach(exceptions, CacheItems.Add);
            }
        }

        public class DuplicateKeyException<TCacheItem> : ApplicationException where TCacheItem : ICacheItem<K, V>
        {
            public TCacheItem Item { get; }

            public DuplicateKeyException(TCacheItem item)
            {
                Item = item;
            }
        }
        #endregion
    }

}
