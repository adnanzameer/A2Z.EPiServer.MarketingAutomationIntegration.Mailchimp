using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Forms.Core.PostSubmissionActor;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Options;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public class MailChimpPostActor : PostSubmissionActorBase
    {
        readonly IMailchimpService _mailchimpService;
        private readonly IOptions<MarketingAutomationMailchimpOptions> _options;

        public MailChimpPostActor(IMailchimpService mailchimpService, IOptions<MarketingAutomationMailchimpOptions> options)
        {
            _mailchimpService = mailchimpService;
            _options = options;
        }

        public override object Run(object input)
        {
            var submissionResult = string.Empty;

            if (SubmissionData == null)
                return submissionResult;

            var postedFormDataDictionary = new Dictionary<string, string>();

            foreach (var pair in SubmissionData.Data)
            {
                if (!pair.Key.ToLower().StartsWith("systemcolumn") && pair.Value != null)
                {
                    postedFormDataDictionary.Add(pair.Key, pair.Value.ToString());
                }
            }

            var interests = new List<string>();
            foreach (var valuePair in SubmissionData.Data.Where(x => x.Value is string))
            {
                foreach (var option in _options.Value.OptionalInterests)
                {
                    if (!string.IsNullOrWhiteSpace(option.Value) && valuePair.Value is string value && value.Equals(option.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        interests.Add(value);
                    }
                }
            }

            var mappings = base.ActiveExternalFieldMappingTable;
            if (mappings != null)
            {
                var formDataAttributes = new Dictionary<string, string>();

                var count = 1;
                foreach (var interest in interests)
                {
                    formDataAttributes.Add("CUSTOM-INTERESTS" + count, interest);
                    count += 1;
                }

                var listId = string.Empty;
                foreach (var item in mappings)
                {
                    if (item.Value != null)
                    {
                        var fieldName = item.Key;
                        var remoteFieldName = item.Value.ColumnId;

                        if (postedFormDataDictionary.ContainsKey(fieldName))
                        {
                            formDataAttributes.Add(remoteFieldName, postedFormDataDictionary[fieldName]);
                            if (string.IsNullOrWhiteSpace(listId))
                                listId = item.Value.DatasourceId;
                        }
                    }
                }

                if (formDataAttributes.Count > 0)
                {
                    var member = _mailchimpService.Send(listId, formDataAttributes);
                }
            }

            return submissionResult;
        }
    }
}