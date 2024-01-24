using System;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp.Caching
{
    public interface ICacheService
    {
        TValue Get<TValue>(string cacheKey, int durationInMinutes, Func<TValue> getItemCallback) where TValue : class;
        void Remove(string cacheKey);
    }
}