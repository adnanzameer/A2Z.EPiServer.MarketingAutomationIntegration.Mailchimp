using MailChimp.Net.Models;
using System.Collections.Generic;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public interface IMailchimpService
    {
        List<List> GetLists();
        Dictionary<string, string> GetListsAsDictionary();
        List GetListByName(string name);
        List GetListById(string id);
        List<MergeField> GetFormFields(string listId);
        Dictionary<string, string> GetFormFieldsAsDictionary(string listId);
        Member Send(string listId, Dictionary<string, string> fields);
        Member AddOrUpdateMember(string listId, Member member);
    }
}
