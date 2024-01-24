using System.Collections.Generic;
using System.Linq;
using EPiServer.Forms.Core;
using EPiServer.Forms.Core.Internal.Autofill;
using EPiServer.Forms.Core.Internal.ExternalSystem;
using EPiServer.ServiceLocation;
using Microsoft.AspNetCore.Http;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public class MailChimpDataSystem : IExternalSystem, IAutofillProvider
    {
        private readonly MailChimpService _mailChimpService;

        public MailChimpDataSystem()
        {
            _mailChimpService = ServiceLocator.Current.GetInstance<MailChimpService>();
        }

        public virtual string Id => "MailChimpDataSystem";

        public virtual IEnumerable<IDatasource> Datasources
        {
            get
            {
                var items = this._mailChimpService.GetListsAsDictionary();

                var select = items
                     .Select(x => new Datasource()
                     {
                         Name = x.Value,
                         Id = x.Key,
                         OwnerSystem = this,
                         Columns = this._mailChimpService.GetFormFieldsAsDictionary(x.Key)
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