using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GPS.SimpleCache.Tests
{
    [TestClass]
    public class UnitTest1
    {
        readonly TimeSpan _expirationSpan = TimeSpan.FromSeconds(2.5);

        [TestMethod]
        public void CreateCache()
        {
            var cache = new SimpleCache<Guid, string>(_expirationSpan, CacheExpirationTypes.Timed);

            Assert.IsNotNull(cache);
        }

        [TestMethod]
        public void AddAndExpireItem()
        {
            var are = new AutoResetEvent(false);
            var cache = new SimpleCache<Guid, string>(_expirationSpan, CacheExpirationTypes.Timed);
            cache.ItemExpired += (sender, item) =>
            {
                are.Set();
            };
            

            cache.AddItem(new CacheItem<Guid, string> { Key = Guid.NewGuid(), Value = "test"});

            Thread.Sleep(1000);

            Thread.Sleep(_expirationSpan);

            var signaled = are.WaitOne(10);

            Assert.IsTrue(signaled);
        }

        [TestMethod]
        public void AddAndRetrieveItem()
        {
            var key = Guid.NewGuid();
            var cache = new SimpleCache<Guid, string>(_expirationSpan, CacheExpirationTypes.Timed);
            cache.AddItem(new CacheItem<Guid, string> { Key = key, Value = "test" });

            var item = cache[key];

            Assert.IsNotNull(item);
            Assert.AreEqual("test", item.Value);
        }

        [TestMethod]
        public void InvalidateCache()
        {
            var cache = new SimpleCache<Guid, string>(_expirationSpan, CacheExpirationTypes.Timed);
            cache.AddItem(new CacheItem<Guid, string> { Key = Guid.Empty, Value = string.Empty});

            var cachedCount = cache.Count();

            cache.InvalidateCache();

            Assert.AreNotEqual(cachedCount, cache.Count());
        }

        [TestMethod]
        public void ItemPropertyChanged()
        {
            var item = new CacheItem<Guid, string> { Key = Guid.Empty, Value = "1"};
            var changed = false;
            item.PropertyChanged += (sender, args) =>
            {
                changed = true;
                Assert.AreEqual("2", item.Value);
                Assert.AreEqual(nameof(CacheItem<Guid, string>.Value), args.PropertyName);
            };

            item.Value = "2";

            Assert.IsTrue(changed);

        }
    }
}
