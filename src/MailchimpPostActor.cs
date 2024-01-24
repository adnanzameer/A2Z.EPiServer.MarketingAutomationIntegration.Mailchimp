using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Forms.Core.PostSubmissionActor;
using EPiServer.ServiceLocation;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public class MailChimpPostActor : PostSubmissionActorBase
    {
        readonly MailChimpService _mailChimpService;

        public MailChimpPostActor()
        {
            _mailChimpService = ServiceLocator.Current.GetInstance<MailChimpService>();
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

            var interests = SubmissionData.Data.FirstOrDefault(x =>
                x.Value is string value && (value.Contains("Feller News", StringComparison.OrdinalIgnoreCase) ||
                                            value.Contains("KNX News", StringComparison.OrdinalIgnoreCase))).Value;

            var mappings = base.ActiveExternalFieldMappingTable;
            if (mappings != null)
            {
                var formDataAttributes = new Dictionary<string, string>();
                if (interests is string formInterests)
                    formDataAttributes.Add("CUSTOM-INTERESTS", formInterests);

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
                    var member = _mailChimpService.Send(listId, formDataAttributes);
                }
            }

            return submissionResult;
        }

    }
}