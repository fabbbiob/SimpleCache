using System;

namespace GPS.SimpleCache
{
    public sealed partial class SimpleCache<K, V>
    {
        #region Exceptions
        public class ItemNotFoundException : Exception
        {
        }
        #endregion
    }

}
