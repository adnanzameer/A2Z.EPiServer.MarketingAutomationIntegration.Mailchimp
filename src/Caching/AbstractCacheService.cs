using System;
using EPiServer.Framework.Cache;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp.Caching
{
    public abstract class AbstractCacheService : ICacheService
    {
        private static readonly TimeSpan CacheDuration;

        private static readonly CacheEvictionPolicy CacheEvictionPolicy;

        protected virtual TimeSpan DefaultCacheDuration => CacheDuration;

        protected virtual CacheEvictionPolicy DefaultCacheEvictionPolicy => CacheEvictionPolicy;

        static AbstractCacheService()
        {
            CacheDuration = TimeSpan.FromHours(24);
            CacheEvictionPolicy = new CacheEvictionPolicy(CacheDuration, CacheTimeoutType.Absolute);
        }

        public abstract TValue Get<TValue>(string cacheKey) where TValue : class;

        public abstract TValue Get<TValue>(string cacheKey, CacheEvictionPolicy cacheEvictionPolicy, Func<TValue> getItemCallback) where TValue : class;

        public TValue Get<TValue>(string cacheKey, Func<TValue> getItemCallback) where TValue : class
            => Get(cacheKey, DefaultCacheEvictionPolicy, getItemCallback);

        public TValue Get<TValue>(string cacheKey, int durationInMinutes, Func<TValue> getItemCallback) where TValue : class
            => Get(cacheKey, new CacheEvictionPolicy(TimeSpan.FromMinutes(durationInMinutes), CacheTimeoutType.Absolute), getItemCallback);

        public abstract TValue Get<TValue, TId>(string cacheKeyFormat, TId id, int durationInMinutes, Func<TId, TValue> getItemCallback) where TValue : class;

        public TValue Get<TValue, TId>(string cacheKeyFormat, TId id, Func<TId, TValue> getItemCallback) where TValue : class
            => Get(cacheKeyFormat, id, (int)DefaultCacheDuration.TotalMinutes, getItemCallback);

        protected virtual string FormatKey<TId>(string cacheKeyFormat, TId id) => string.Format(cacheKeyFormat, id);

        public abstract void Remove(string cacheKey);

        public void Remove<TId>(string cacheKey, TId id) => Remove(FormatKey(cacheKey, id));
    }
}