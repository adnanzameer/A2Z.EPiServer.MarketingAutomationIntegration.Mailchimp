using System;
using System.Collections.Generic;
using System.Linq;
using A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp.Caching;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using MailChimp.Net;
using MailChimp.Net.Interfaces;
using MailChimp.Net.Models;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    [ServiceConfiguration(ServiceType = typeof(MailChimpService), Lifecycle = ServiceInstanceScope.Transient)]
    public class MailChimpService
    {
        private readonly IMailChimpManager _mailChimpManager;

        readonly ICacheService _cacheService;

        private const int CacheTimeout = 720; // in minutes

        public MailChimpService(ICacheService cacheService)
        {
            _cacheService = cacheService;

            var mailChimpApiKey = "";
            //if (PageHelper.SiteSettingsPage != null && !string.IsNullOrEmpty(PageHelper.SiteSettingsPage.MailChimpApiKey))
            //{
            //    mailChimpApiKey = PageHelper.SiteSettingsPage.MailChimpApiKey;
            //}

            _mailChimpManager = new MailChimpManager(mailChimpApiKey);
        }

        public List<List> GetLists()
        {
            return _cacheService.Get(MailChimpConstants.ListIds, CacheTimeout, () =>
            {
                return
                    _mailChimpManager.Lists
                        .GetAllAsync().GetAwaiter().GetResult()
                        .OrderBy(x => x.Name)
                        .ToList();
            });
        }

        public Dictionary<string, string> GetListsAsDictionary()
        {
            return _cacheService.Get(MailChimpConstants.ListDictionaryItems, CacheTimeout, () =>
            {
                var list = new Dictionary<string, string>();
                var items = this.GetLists();
                foreach (var item in items)
                {
                    if (!list.ContainsKey(item.Id))
                    {
                        list.Add(item.Id, item.Name);
                    }
                }
                return list;
            });
        }

        public List GetListByName(string name)
        {
            var cacheKey = string.Format(MailChimpConstants.ListId, name);
            return _cacheService.Get(cacheKey, CacheTimeout, () =>
            {
                var result = this.GetLists().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return result;
            });
        }

        public List GetListById(string id)
        {
            var cacheKey = string.Format(MailChimpConstants.ListId, id);
            return this._cacheService.Get(cacheKey, CacheTimeout,
                () => { return _mailChimpManager.Lists.GetAsync(id).GetAwaiter().GetResult(); });
        }

        public List<MergeField> GetFormFields(string listId)
        {
            return _cacheService.Get(string.Format(MailChimpConstants.ListMergeFields, listId), CacheTimeout,
                () =>
                {
                    return _mailChimpManager.MergeFields.GetAllAsync(listId).GetAwaiter().GetResult().ToList();
                });
        }

        public Dictionary<string, string> GetFormFieldsAsDictionary(string listId)
        {
            var dictionary = new Dictionary<string, string>() { { "EMAIL", "Email" } };
            this.GetFormFields(listId)
                  .Select(x => new KeyValuePair<string, string>(x.Tag, x.Name))
                  .ToList()
                  .ForEach(x => dictionary.Add(x.Key, x.Value));

            return dictionary;
        }

        public Member Send(string listId, Dictionary<string, string> fields)
        {
            try
            {
                var externalFields = GetFormFields(listId);

                var member = new Member()
                {
                    EmailAddress = fields["EMAIL"],
                    StatusIfNew = Status.Pending
                };

                var sources = fields.FirstOrDefault(x => x.Key.Equals("CUSTOM-INTERESTS", StringComparison.OrdinalIgnoreCase)).Value.Split(',').ToList();

                foreach (var externalField in externalFields.Where(externalField => fields.ContainsKey(externalField.Tag)))
                {
                    member.MergeFields.Add(externalField.Tag, fields[externalField.Tag]);
                }

                foreach (var source in sources.Where(source => !string.IsNullOrEmpty(source)))
                {
                    if (source.Trim().Equals("Feller News", StringComparison.OrdinalIgnoreCase))
                    {
                        member.Interests.Add("d24e1f759c", true);
                    }
                    else if (source.Trim().Equals("KNX News", StringComparison.OrdinalIgnoreCase))
                    {
                        member.Interests.Add("1c558ccf5f", true);
                    }
                }

                member.Interests.Add("a5623146b8", true); //Datenschutz

                return _mailChimpManager.Members.AddOrUpdateAsync(listId, member).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error(ex.Message, ex);
            }

            return null;
        }

        public Member AddOrUpdateMember(string listId, Member member)
        {
            try
            {
                return _mailChimpManager.Members.AddOrUpdateAsync(listId, member).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogManager.GetLogger().Error(ex.Message, ex);
            }

            return null;
        }
    }
}