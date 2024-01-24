using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace A2Z.EPiServer.MarketingAutomationIntegration.Mailchimp
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMarketingAutomationMailchimp(this IServiceCollection services)
        {
            return AddMarketingAutomationMailchimp(services, _ => { });
        }

        public static IServiceCollection AddMarketingAutomationMailchimp(this IServiceCollection services, Action<MarketingAutomationMailchimpOptions> setupAction)
        {
            services.AddTransient<IMailchimpService, MailchimpService>();

            services.AddOptions<MarketingAutomationMailchimpOptions>().Configure<IConfiguration>((options, configuration) =>
            {
                setupAction(options);
                configuration.GetSection("A2Z:MarketingAutomationMailchimp").Bind(options);
            });

            return services;
        }
    }
}