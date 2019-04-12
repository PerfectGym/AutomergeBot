using Microsoft.Extensions.DependencyInjection;

namespace PerfectGym.AutomergeBot.Features.MergingBranches
{
    public class ContainerRegistrations : IContainerRegistrations
    {
        public void DoRegistrations(IServiceCollection services)
        {
            services.AddTransient<MergingBranchesPushHandler>();
            services.AddTransient<IProcessPushPredicate, ProcessPushPredicate>();
            services.AddTransient<IMergePerformer, MergePerformer>();
            services.AddTransient<IPullRequestMergeRetryier, PullRequestMergeRetryier>();

            var mergeDirectionsProviderInstance = new MergeDirectionsProvider();
            services.AddSingleton<IMergeDirectionsProviderConfigurator>(mergeDirectionsProviderInstance);
            services.AddSingleton<IMergeDirectionsProvider>(mergeDirectionsProviderInstance);
        }
    }
}
