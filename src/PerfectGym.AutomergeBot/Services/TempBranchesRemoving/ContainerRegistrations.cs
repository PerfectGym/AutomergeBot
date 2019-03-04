using Microsoft.Extensions.DependencyInjection;

namespace PerfectGym.AutomergeBot.Services.TempBranchesRemoving
{
    public class ContainerRegistrations : IContainerRegistrations
    {
        public void DoRegistrations(IServiceCollection services)
        {
            services.AddTransient<ITempBranchesRemoverPullRequestHandler, TempBranchesRemoverPullRequestHandlerPullRequestHandler>();
        }
    }
}
