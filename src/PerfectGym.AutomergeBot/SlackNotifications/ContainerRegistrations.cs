using Microsoft.Extensions.DependencyInjection;
using PerfectGym.AutomergeBot.SlackClient;

namespace PerfectGym.AutomergeBot.SlackNotifications
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