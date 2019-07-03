﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services.Configure<AdditionalCodeReviewConfiguration>(_configuration);
        }
    }
}
