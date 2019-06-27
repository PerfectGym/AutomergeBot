using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerfectGym.AutomergeBot.Features.MergingBranches;
using PerfectGym.AutomergeBot.Notifications.SlackClient;

namespace PerfectGym.AutomergeBot.Features.AdditionalCodeReview
{
    public class ContainerRegistrations : IContainerRegistrations
    {

        private readonly IConfiguration _configuration;

        public ContainerRegistrations(IConfiguration configuration)
        {
            _configuration = configuration;
        }

    

        public void DoRegistrations(IServiceCollection services)
        {
            services.AddTransient<IPullRequestReviewModelHandler, PullRequestReviewModelHandler>();
            services.AddTransient<ICheckRunModelHandler, CheckRunModelHandler>();

            services.Configure<AdditionalCodeReviewConfiguration>(_configuration);
        }
    }
}
