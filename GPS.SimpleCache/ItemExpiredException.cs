using System;

namespace GPS.SimpleCache
{
    public sealed partial class SimpleCache<K, V>
    {
        public class ItemExpiredException : Exception
        {
        }
    }

}
