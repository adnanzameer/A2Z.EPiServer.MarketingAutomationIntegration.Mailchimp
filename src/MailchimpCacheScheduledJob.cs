using A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp.Caching;
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    [ScheduledPlugIn(DisplayName = "[A2Z] Mailchimp Cache Manager", IntervalType = global::EPiServer.DataAbstraction.ScheduledIntervalType.Hours, Restartable = true, IntervalLength = 6)]
    public class MailChimpCacheScheduledJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        private readonly IMailchimpService _mailchimpService;

        private readonly ICacheService _cacheService;

        public MailChimpCacheScheduledJob(IMailchimpService mailchimpService, ICacheService cacheService)
        {
            _mailchimpService = mailchimpService;
            _cacheService = cacheService;
            IsStoppable = true;
        }

        /// <summary>
        /// Called when a user clicks on Stop for a manually started job, or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }

        /// <summary>
        /// Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log and visible from admin mode</returns>
        public override string Execute()
        {
            //Call OnStatusChanged to periodically notify progress of job for manually started jobs
            OnStatusChanged($"Starting execution of {GetType()}");

            // Clear Lists
            _cacheService.Remove(MailChimpConstants.ListIds);
            var lists = _mailchimpService.GetLists();
            var cacheItemsUpdated = 0;
            foreach (var list in lists)
            {
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                _cacheService.Remove(string.Format(MailChimpConstants.ListId, list.Id));
                _cacheService.Get(string.Format(MailChimpConstants.ListId, list.Id), 720, () => list);

                _cacheService.Remove(string.Format(MailChimpConstants.ListMergeFields, list.Id));
                _mailchimpService.GetFormFields(list.Id);

                cacheItemsUpdated++;
                OnStatusChanged($"Cache Items Updated: {cacheItemsUpdated}");
            }

            // Clear the dictionary
            _cacheService.Remove(MailChimpConstants.ListDictionaryItems);
            _mailchimpService.GetListsAsDictionary();

            //For long running jobs periodically check if stop is signaled and if so stop execution

            return $"Cache Items Updated: {cacheItemsUpdated}";
        }
    }
}