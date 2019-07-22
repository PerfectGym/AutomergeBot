using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PerfectGym.AutomergeBot.Features.TempBranchesRemoving;
using PerfectGym.AutomergeBot.Models;
using PerfectGym.AutomergeBot.RepositoryConnection;

namespace PerfectGym.AutomergeBot.Features.MergingBranches
{
 
    public class PullRequestReviewModelHandler : IPullRequestReviewModelHandler
    {
        private readonly IRepositoryConnectionProvider _repositoryConnectionProvider;
        private readonly ILogger<TempBranchesRemoverPullRequestHandlerPullRequestHandler> _logger;
        private readonly IOptionsMonitor<AutomergeBotConfiguration> _cfg;
        private readonly IMergePerformer _mergePerformer;


        public PullRequestReviewModelHandler(
            IRepositoryConnectionProvider repositoryConnectionProvider,
            ILogger<TempBranchesRemoverPullRequestHandlerPullRequestHandler> logger,
            IOptionsMonitor<AutomergeBotConfiguration> cfg,
            IMergePerformer mergePerformer
            )
        {
            _repositoryConnectionProvider = repositoryConnectionProvider;
            _logger = logger;
            _cfg = cfg;
            _mergePerformer = mergePerformer;
        }

        public void Handle(PullRequestReviewInfoModel pullRequestReviewInfoModel)
        {
            
            var prReviewModel = pullRequestReviewInfoModel;

            if (prReviewModel.State == PullRequestReviewInfoModel.States.Approved
                && IsAutomergeBotPullRequest(prReviewModel))
            {

                using (var repoContext = _repositoryConnectionProvider.GetRepositoryConnection())
                {
                    var pullRequest = repoContext.GetPullRequest(prReviewModel.PullRequestNumber);
                    if (pullRequest.Mergeable == null)
                    {
                        _logger.LogInformation("Pull request {pullRequestNumber} has Mergeable == null. Checking once again. ", prReviewModel.PullRequestNumber);
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
                        pullRequest = repoContext.GetPullRequest(prReviewModel.PullRequestNumber);
                    }

                    if (pullRequest.Mergeable??false)
                    {
                        _logger.LogInformation("Pull request {pullRequestNumber} has been approved. Trying merge it", prReviewModel.PullRequestNumber);
                        _mergePerformer.TryMergeExistingPullRequest(pullRequest, repoContext);
                    }
                    else
                    {
                        _logger.LogInformation("Pull request {pullRequestNumber} is not mergeable. pullRequest.Mergeable={mergeable}", prReviewModel.PullRequestNumber, pullRequest.Mergeable==null?"null":"false");
                    }
                }
            }
            
        }


        private bool IsAutomergeBotPullRequest(PullRequestReviewInfoModel prReviewModel)
        {
            return prReviewModel.BranchName.StartsWith(_cfg.CurrentValue.CreatedBranchesPrefix);
        }

     
    }
}
