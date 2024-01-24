using System.Collections.Generic;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public class MarketingAutomationMailchimpOptions
    {
        public string MailChimpApiKey { get; set; }
        public List<OptionalInterest> OptionalInterests { get; set; }
        public List<string> RequiredInterests { get; set; }
    }
    
    public class OptionalInterest
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}