using System;
using System.Runtime.Caching;

namespace MemCaching
{
    /// <summary>
    /// MemoryCache with expire
    /// Immediate
    /// Sliding
    /// Never
    /// </summary>
    public class MemCache
    {
        /// <summary>
        /// stores the objects
        /// </summary>
        private readonly ObjectCache objectCache;

        /// <summary>
        /// when the item was created/set
        /// </summary>
        private readonly ObjectCache creationTicks;

        /// <summary>
        /// how long will the item be expired after created/set
        /// </summary>
        private readonly ObjectCache expireInSeconds;

        /// <summary>
        /// default
        /// </summary>
        private readonly CacheItemPolicy defaultPolicy;

        /// <summary>
        /// callback: update the value (pulling from source) 
        /// </summary>
        private readonly UpdateCacheCallback updateCallback;

        public MemCache(UpdateCacheCallback callback)
        {
            objectCache = MemoryCache.Default;
            updateCallback = callback;

            creationTicks = new MemoryCache("MemCache_CreationTick");
            expireInSeconds = new MemoryCache("MemCache_ExpireInSeconds");
            defaultPolicy = new CacheItemPolicy { SlidingExpiration = new TimeSpan(24, 0, 0) };            
        }

        /// <summary>
        /// Get value
        /// </summary>
        public object Get(string key)
        {
            var expired = (long)(creationTicks[key]) + (long)(expireInSeconds[key]);
            var now = DateTime.Now.Ticks;
            if (now >= expired)
            {
                // if it is expired, then pull the latest value from source
                updateCallback?.Invoke(key);
                creationTicks[key] = now;
            }

            return objectCache.Get(key);
        }

        /// <summary>
        /// Set value
        /// </summary>
        public void Set(string key, object value)
        {
            // update the creation time
            creationTicks[key] = DateTime.Now.Ticks;
            objectCache[key] = value;
        }

        /// <summary>
        /// add an item with expire
        /// </summary>
        /// <param name="expire">expire in seconds</param>
        /// <param name="isValueInitialized">true:the value is already initialized; false:need refresh</param>
        public void Add(string key, object value, int expire, bool isValueInitialized = false)
        {
            objectCache.Set(key, value, defaultPolicy);
            var ts = new TimeSpan(0, 0, expire);
            if (isValueInitialized)
            {
                creationTicks.Set(key, DateTime.Now.Ticks, defaultPolicy);
            }
            else
            {
                creationTicks.Set(key, DateTime.Now.Subtract(ts).Ticks, defaultPolicy);
            }
            expireInSeconds.Set(key, ts.Ticks, defaultPolicy);
        }

        /// <summary>
        /// add an item, which will be expired immediately 
        /// </summary>
        public void AddImmediate(string key, object value)
        {
            objectCache.Set(key, value, defaultPolicy);

            creationTicks.Set(key, DateTime.Now.Ticks, defaultPolicy);
            expireInSeconds.Set(key, (long)0, defaultPolicy);
        }

        /// <summary>
        /// add an item, which will never be expired
        /// <param name="isValueInitialized">true:the value is already initialized; false:need refresh</param>
        /// </summary>
        public void AddOnce(string key, object value, bool isValueInitialized = false)
        {
            objectCache.Set(key, value, defaultPolicy);

            var ts = new TimeSpan(24, 0, 0);
            if (isValueInitialized)
            {
                creationTicks.Set(key, DateTime.Now.Ticks, defaultPolicy);
            }
            else
            {
                creationTicks.Set(key, DateTime.Now.Subtract(ts).Ticks, defaultPolicy);
            }
            expireInSeconds.Set(key, ts.Ticks, defaultPolicy);
        }

        /// <summary>
        /// remove one item
        /// </summary>
        public void Remove(string key)
        {
            objectCache.Remove(key);
            creationTicks.Remove(key);
            expireInSeconds.Remove(key);
        }
    }
}
