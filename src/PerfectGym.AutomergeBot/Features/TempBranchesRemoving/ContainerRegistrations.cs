using Microsoft.Extensions.DependencyInjection;

namespace PerfectGym.AutomergeBot.Features.TempBranchesRemoving
{
    public class ContainerRegistrations : IContainerRegistrations
    {
        public void DoRegistrations(IServiceCollection services)
        {
            services.AddTransient<ITempBranchesRemoverPullRequestHandler, TempBranchesRemoverPullRequestHandlerPullRequestHandler>();
        }
    }
}
