using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PerfectGym.AutomergeBot.Features.TempBranchesRemoving;
using PerfectGym.AutomergeBot.Models;
using PerfectGym.AutomergeBot.RepositoryConnection;

namespace PerfectGym.AutomergeBot.Features.MergingBranches
{
    public interface ICheckRunModelHandler
    {
        void Handle(CheckRunInfoModel model);
    }

    public class CheckRunModelHandler : ICheckRunModelHandler
    {
        private readonly IRepositoryConnectionProvider _repositoryConnectionProvider;
        private readonly IMergePerformer _mergePerformer;


        public CheckRunModelHandler(
            IRepositoryConnectionProvider repositoryConnectionProvider,
            IMergePerformer mergePerformer
            )
        {
            _repositoryConnectionProvider = repositoryConnectionProvider;
            _mergePerformer = mergePerformer;
        }

        public void Handle(CheckRunInfoModel model)
        {
            if (
                model.Status == CheckRunInfoModel.StatusValues.Completed &&
                model.Conclusion == CheckRunInfoModel.ConclusionValues.Success
            )
            {
                using (var repoContext = _repositoryConnectionProvider.GetRepositoryConnection())
                {
                     var pullRequest=repoContext.GetPullRequest(model.PullRequestNumber);

                    _mergePerformer.TryMergeExistingPullRequest(pullRequest,repoContext);

                }
            }
        }
    
    }
}
