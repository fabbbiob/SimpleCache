# Introduction
SimpleCache is the powerfull cache that's easy to use, yet fast and thread-safe!  You can host SimpleCache
in a Singleton in your app, or as a service in your n-Tier architecture.  SimpleCache is scoped to 
CacheItem definitions based on the Key and Value types, allowing for high-speed data recovery.  

# How does it work?
SimpleCache uses a ThreadSafeDictionary to hold it's data.  This dictionary allows for quick and easy, yet
thread safe, access to the cache's data.  You can find the ThreadSafeDictionary in the 
[GPS.SimpleThreading](https://preview.nuget.org/packages/GPS.SimpleThreading/)
package on http://nuget.org.

# Examples
## Instantiation
    var cache = new SimpleCache<Guid, string>(
        TimeSpan.FromMinutes(5.0), CacheExpirationTypes.Timed);

The first parameter on the constructor accepts the TimeSpan that defines how long a cache item can go 
_unused_ before expiring. The second parameter defines the type of caching to use. 

Timed caching starts a timer that fires every second.  Any items that have not been accessed from the
cache in the defined time period will be expunged from the cache and the ItemExpired event of the cache
will be invoked for each expired item.

OnDemand caching does not start a timer, but instead the application is repsonsible for calling the 
ExpireCacheItems method of the cache.  This method will check the current cache state for the expiration
of any CacheItems.

## The CacheItem
All data stored in the cache must be wrapped in a CacheItem.  This item will be used as a container
that stores pertinent caching data such as the DateTimeOffset of the last access to the CacheItem 
through the cache itself only. Working with the CacheItem in your application does not update the
last access, unless you change the data, which will trigger the NotifyPropertyChanged event that
will tell the cache you have changed the data and will update the last access property.

    var item = new CacheItem<int, record> 
    {
        Key = 0,
        Value = myRec;
    };

## Adding to the cache and Expiration
    var cache = new SimpleCache<Guid, string>(
        TimeSpan.FromMinutes(2.5), CacheExpirationTypes.Timed);

    cache.ItemExpired += (sender, item) =>
    {
        // You expiration logic.  This is not required, it should
        // only be used if you need to ensure you always have fresh
        // data in the cache for your application.
    };
    
    cache.AddItem(new CacheItem<Guid, string> 
        { Key = Guid.Empty, Value = "example"});

## Retrieving a CacheItem
    var item = cache[key];
    var value = item.Value;

When you access the item through the cache, it updates the last accessed 
property of the CacheItem, meaning that the sliding window for the expiration
of the CacheItem is reset.

When you attempt to access a CacheItem, if it is expired you will receive an
ItemExpiredException. This allows you to watch for the exception and reload the
data if necessary.

    CacheItem<long, Tuple<string, string>> item;

    try { item = cache[key];}
    catch(ItemExpiredException iee)
    {
        // Your data access here.
        cache.AddItem(new CacheItem<long, Tuple<string, string> 
            { Key = key, Value = data);
    }

## Invalidating the Cache
    cache.InvalidateCache();

Invalidating the cache will remove all items from the cache.  Attempts to
access any removed cache item will result in the ItemExpiredException.

# Where to Get SimpleCache
SimpleCache may be retrieved directly into your application through Nuget
in Visual Studio.  SimpleCache depends on GPS.SimpleThreading which will
be pulled in as a dependency through Nuget.

The source code can be found on 
[GitHub](https://github.com/gatewayprogrammingschool/SimpleCache).