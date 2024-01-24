using System;
using EPiServer.Framework.Cache;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp.Caching
{
    public abstract class AbstractCacheService : ICacheService
    {
        public abstract TValue Get<TValue>(string cacheKey) where TValue : class;

        protected abstract TValue Get<TValue>(string cacheKey, CacheEvictionPolicy cacheEvictionPolicy, Func<TValue> getItemCallback) where TValue : class;

        public TValue Get<TValue>(string cacheKey, int durationInMinutes, Func<TValue> getItemCallback) where TValue : class
            => Get(cacheKey, new CacheEvictionPolicy(TimeSpan.FromMinutes(durationInMinutes), CacheTimeoutType.Absolute), getItemCallback);

        public abstract TValue Get<TValue, TId>(string cacheKeyFormat, TId id, int durationInMinutes, Func<TId, TValue> getItemCallback) where TValue : class;

        public abstract void Remove(string cacheKey);
    }
}