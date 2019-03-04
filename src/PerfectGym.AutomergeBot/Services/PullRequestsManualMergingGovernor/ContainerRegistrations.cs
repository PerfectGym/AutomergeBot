using Microsoft.Extensions.DependencyInjection;

namespace PerfectGym.AutomergeBot.Services.PullRequestsManualMergingGovernor
{
    public class ContainerRegistrations : IContainerRegistrations
    {
        public void DoRegistrations(IServiceCollection services)
        {
            services.AddTransient<PullRequestsGovernor>();
        }
    }
}
