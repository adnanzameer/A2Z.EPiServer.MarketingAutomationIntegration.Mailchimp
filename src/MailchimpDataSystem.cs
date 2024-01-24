using System.Collections.Generic;
using System.Linq;
using EPiServer.Forms.Core;
using EPiServer.Forms.Core.Internal.Autofill;
using EPiServer.Forms.Core.Internal.ExternalSystem;
using Microsoft.AspNetCore.Http;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public class MailChimpDataSystem : IExternalSystem, IAutofillProvider
    {
        private readonly IMailchimpService _mailchimpService;

        public MailChimpDataSystem(IMailchimpService mailchimpService)
        {
            _mailchimpService = mailchimpService;
        }
        
        public virtual string Id => "MailChimpDataSystem";

        public virtual IEnumerable<IDatasource> Datasources
        {
            get
            {
                var items = _mailchimpService.GetListsAsDictionary();

                var select = items
                     .Select(x => new Datasource()
                     {
                         Name = x.Value,
                         Id = x.Key,
                         OwnerSystem = this,
                         Columns = _mailchimpService.GetFormFieldsAsDictionary(x.Key)
                     });

                return select;
            }
        }

        public virtual IEnumerable<string> GetSuggestedValues(IDatasource selectedDatasource, IEnumerable<RemoteFieldInfo> remoteFieldInfos,
            ElementBlockBase content, IFormContainerBlock formContainerBlock, HttpContext context)
        {

            if (selectedDatasource == null || remoteFieldInfos == null)
                return Enumerable.Empty<string>();

            if (Datasources.Any(ds => selectedDatasource != null && ds.Id == selectedDatasource.Id) && remoteFieldInfos.Any(mi => selectedDatasource != null && mi.DatasourceId == selectedDatasource.Id))
            {
                var fieldInfos = remoteFieldInfos.ToList();
                var activeRemoteFieldInfo = fieldInfos.FirstOrDefault(mi => mi.DatasourceId == selectedDatasource.Id);

                if (activeRemoteFieldInfo != null)
                {
                    switch (activeRemoteFieldInfo.ColumnId.ToLower())
                    {
                        case "email":
                            return new List<string>
                            {
                                context.Request.Query["email"]
                            };
                        default:
                            return Enumerable.Empty<string>();
                    }
                }
            }

            return Enumerable.Empty<string>();
        }
    }
}