using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PerfectGym.AutomergeBot.Features.CodeReviewInspector
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
            services.AddTransient<CodeReviewInspector>();
            services.Configure<CodeReviewInspectorConfiguration>(_configuration);
            // services.AddTransient<IPullRequestReviewModelHandler, PullRequestReviewModelHandler>();
            //services.Configure<CodeReviewInspectorConfiguration>(_configuration);
        }
    }
}
