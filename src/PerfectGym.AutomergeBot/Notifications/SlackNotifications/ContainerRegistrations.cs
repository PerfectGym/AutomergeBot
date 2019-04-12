using Microsoft.Extensions.DependencyInjection;
using PerfectGym.AutomergeBot.Notifications.SlackClient;

namespace PerfectGym.AutomergeBot.Notifications.SlackNotifications
{
    public class ContainerRegistrations : IContainerRegistrations
    {
        public void DoRegistrations(IServiceCollection services)
        {
            services.AddTransient<INow, DateTimeNow>();
            services.AddTransient<ISlackMessageProvider, SlackMessageProvider>();
            services.AddTransient<ISlackClientProvider, SlackClientProvider>();
        }
    }
}