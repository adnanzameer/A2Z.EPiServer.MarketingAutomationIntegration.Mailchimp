using A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp.Caching;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    [ScheduledPlugIn(DisplayName = "Mail Chimp Cache Manager", IntervalType = global::EPiServer.DataAbstraction.ScheduledIntervalType.Hours, Restartable = true, IntervalLength = 6)]
    public class MailChimpCacheScheduledJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        private readonly MailChimpService _mailChimpService;

        private readonly ICacheService _cacheService;

        public MailChimpCacheScheduledJob()
        {
            IsStoppable = true;
            _mailChimpService = ServiceLocator.Current.GetInstance<MailChimpService>();
            _cacheService = ServiceLocator.Current.GetInstance<ICacheService>();
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
            OnStatusChanged($"Starting execution of {this.GetType()}");

            // Clear Lists
            this._cacheService.Remove(MailChimpConstants.ListIds);
            var lists = this._mailChimpService.GetLists();
            var cacheItemsUpdated = 0;
            foreach (var list in lists)
            {
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                this._cacheService.Remove(string.Format(MailChimpConstants.ListId, list.Id));
                this._cacheService.Get(string.Format(MailChimpConstants.ListId, list.Id), 720, () =>
                {
                    return list;
                });

                this._cacheService.Remove(string.Format(MailChimpConstants.ListMergeFields, list.Id));
                this._mailChimpService.GetFormFields(list.Id);

                cacheItemsUpdated++;
                this.OnStatusChanged($"Cache Items Updated: {cacheItemsUpdated}");
            }

            // Clear the dictionary
            this._cacheService.Remove(MailChimpConstants.ListDictionaryItems);
            this._mailChimpService.GetListsAsDictionary();

            //For long running jobs periodically check if stop is signaled and if so stop execution

            return $"Cache Items Updated: {cacheItemsUpdated}";
        }
    }
}