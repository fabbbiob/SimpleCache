using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GPS.SimpleCache
{
    public sealed partial class SimpleCache<K, V>
    {
        private CacheExpirationTypes _expirationType = CacheExpirationTypes.OnDemand;
        public CacheExpirationTypes ExpirationType => _expirationType;

        private TimeSpan _cacheExpirationDuration = TimeSpan.Zero;
        public TimeSpan CacheExpirationDuration => _cacheExpirationDuration;

        private readonly ConcurrentDictionary<K, CacheItem<K, V>> _cacheItems =
            new ConcurrentDictionary<K, CacheItem<K, V>>();

        private static readonly object PadLock = new object();
        private Timer _timer;

        public event EventHandler<CacheItem<K, V>> ItemExpired;

        protected void Initialize(TimeSpan expirationTimeSpan,
            CacheExpirationTypes expirationType
        )
        {
            _cacheExpirationDuration = expirationTimeSpan;
            _expirationType = expirationType;

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

        public SimpleCache(
            TimeSpan expirationTimeSpan,
            CacheExpirationTypes expirationType,
            IEnumerable<CacheItem<K, V>> items = null)
        {
            if (items != null)
            {
                Parallel.ForEach(items, AddItem);
            }

            Initialize(expirationTimeSpan, expirationType);

        }

        public SimpleCache(
            TimeSpan expirationTimeSpan,
            CacheExpirationTypes expirationType,
            ICollection<CacheItem<K, V>> items)
        {
            if (items != null)
            {
                Parallel.ForEach(items, AddItem);
            }

            Initialize(expirationTimeSpan, expirationType);
        }

        public SimpleCache(
            TimeSpan expirationTimeSpan,
            CacheExpirationTypes expirationType,
            IProducerConsumerCollection<CacheItem<K, V>> items)
        {
            if (items != null)
            {
                Parallel.ForEach(items, AddItem);
            }

            Initialize(expirationTimeSpan, expirationType);
        }

        public void AddItem(CacheItem<K, V> item)
        {
            if (item != null && item.Key != null)
            {
                if (_cacheItems.ContainsKey(item.Key))
                {
                    throw new DuplicateKeyException<CacheItem<K, V>>(item);
                }

                _cacheItems.AddOrUpdate(item.Key, item, (k, cacheItem) => item);
            }
        }

        public void AddItem(K key, V value, ExpirationStrategies strategy = ExpirationStrategies.Default)
        {
            AddItem(new CacheItem<K, V>() { Key = key, Value = value});
        }

        public void AddItems(IEnumerable<CacheItem<K, V>> items)
        {
            var exceptions = new ConcurrentBag<DuplicateKeyException<CacheItem<K, V>>>();

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
        public void AddItems(ICollection<CacheItem<K, V>> items)
        {
            var exceptions = new ConcurrentBag<DuplicateKeyException<CacheItem<K, V>>>();

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
        public void AddItems(IProducerConsumerCollection<CacheItem<K, V>> items)
        {
            var exceptions = new ConcurrentBag<DuplicateKeyException<CacheItem<K, V>>>();

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
            CacheItem<K, V> itemToRemove;
            return _cacheItems.TryRemove(item.Key, out itemToRemove);
        }

        public bool RemoveItem(K key)
        {
            CacheItem<K, V> itemToRemove;
            return _cacheItems.TryRemove(key, out itemToRemove);
        }

        public void InvalidateCache()
        {
            Parallel.ForEach(_cacheItems, item => { item.Value.Invalidate(); });

            ExpireCacheItems();

            _timer = new Timer(s =>
                {
                    ExpireCacheItems();
                },
                null,
                TimeSpan.FromMilliseconds(1),
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
                if (now - item.Value.LastAccessed >= _cacheExpirationDuration)
                {
                    CacheItem<K, V> itemToRemove;
                    _cacheItems.TryRemove(item.Key, out itemToRemove);
                    ItemExpired?.Invoke(this, itemToRemove);
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
                        item.SetLastAccessed();

                        return item;
                    }

                    throw new ItemExpiredException();
                }
                throw new ItemNotFoundException();
            }
        }
    }
}
