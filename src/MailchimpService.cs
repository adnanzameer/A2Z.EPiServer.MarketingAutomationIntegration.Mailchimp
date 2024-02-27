using System;
using System.Collections.Generic;
using System.Linq;
using A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp.Caching;
using EPiServer.Logging;
using MailChimp.Net;
using MailChimp.Net.Interfaces;
using MailChimp.Net.Models;
using Microsoft.Extensions.Options;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public class MailchimpService : IMailchimpService
    {
        private readonly IMailChimpManager _mailChimpManager;
        private readonly IOptions<MarketingAutomationMailchimpOptions> _options;

        readonly ICacheService _cacheService;

        private const int CacheTimeout = 720; // in minutes

        public MailchimpService(ICacheService cacheService, IOptions<MarketingAutomationMailchimpOptions> options)
        {
            _cacheService = cacheService;

            if (string.IsNullOrWhiteSpace(options.Value.MailChimpApiKey))
            {
                throw new ArgumentException("Missing API Key for A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp");
            }

            _options = options;

            _mailChimpManager = new MailChimpManager(options.Value.MailChimpApiKey);
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
                var items = GetLists();
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
                var result = GetLists().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return result;
            });
        }

        public List GetListById(string id)
        {
            var cacheKey = string.Format(MailChimpConstants.ListId, id);
            return _cacheService.Get(cacheKey, CacheTimeout,
                () => _mailChimpManager.Lists.GetAsync(id).GetAwaiter().GetResult());
        }

        public List<MergeField> GetFormFields(string listId)
        {
            return _cacheService.Get(string.Format(MailChimpConstants.ListMergeFields, listId), CacheTimeout,
                () => _mailChimpManager.MergeFields.GetAllAsync(listId).GetAwaiter().GetResult().ToList());
        }

        public Dictionary<string, string> GetFormFieldsAsDictionary(string listId)
        {
            var dictionary = new Dictionary<string, string>() { { "EMAIL", "Email" } };
            GetFormFields(listId)
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

                var member = new Member
                {
                    EmailAddress = fields["EMAIL"],
                    StatusIfNew = Status.Pending
                };

                foreach (var externalField in externalFields.Where(externalField => fields.ContainsKey(externalField.Tag)))
                {
                    member.MergeFields.Add(externalField.Tag, fields[externalField.Tag]);
                }

                var sources = fields.Where(x => x.Key.StartsWith("CUSTOM-INTERESTS", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var source in sources)
                {
                    foreach (var option in _options.Value.OptionalInterests)
                    {
                        if (source.Value.Equals(option.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            member.Interests.Add(option.Value, true);
                        }
                    }
                }

                foreach (var interest in _options.Value.RequiredInterests)
                {
                    member.Interests.Add(interest, true);
                }

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