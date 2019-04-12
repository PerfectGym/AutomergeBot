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

namespace PerfectGym.AutomergeBot.Features.AdditionalCodeReview
{
    public interface IPullRequestReviewModelHandler
    {
        void Handle(PullRequestReviewInfoModel pullRequestReviewInfoModel);
    }

    public class PullRequestReviewModelHandler : IPullRequestReviewModelHandler
    {
        private readonly IRepositoryConnectionProvider _repositoryConnectionProvider;
        private readonly ILogger<TempBranchesRemoverPullRequestHandlerPullRequestHandler> _logger;
        private readonly IOptionsMonitor<AutomergeBotConfiguration> _cfg;


        public PullRequestReviewModelHandler(
            IRepositoryConnectionProvider repositoryConnectionProvider,
            ILogger<TempBranchesRemoverPullRequestHandlerPullRequestHandler> logger,
            IOptionsMonitor<AutomergeBotConfiguration> cfg
            )
        {
            _repositoryConnectionProvider = repositoryConnectionProvider;
            _logger = logger;
            _cfg = cfg;
        }

        public void Handle(PullRequestReviewInfoModel pullRequestReviewInfoModel)
        {
            if (_cfg.CurrentValue.AdditionalCodeReviewConfiguration == null)
            {
                return;
            }

            var prReviewModel = pullRequestReviewInfoModel;

            if (IsAnyAdditionalReviewLabel(prReviewModel))
            {
                return;
            }

            using (var repoContext = _repositoryConnectionProvider.GetRepositoryConnection())
            {
                string reviewer = null;

                if (prReviewModel.State == PullRequestReviewInfoModel.States.Approved
                    && IsAutomergeBotPullRequest(prReviewModel) == false)
                {
                    reviewer = GetRandomReviewerOrNull(prReviewModel.UserLogin);

                    if (reviewer != null)
                    {
                        CreateReviewRequestAndAddLabel(repoContext, prReviewModel, reviewer);
                    }
                }

                if (reviewer == null)
                {
                    AddNoNeedAdditionalReviewLabel(repoContext, prReviewModel);
                }
            }
        }

        private bool IsAnyAdditionalReviewLabel(PullRequestReviewInfoModel pullRequestReviewInfoModel)
        {
            var labels = pullRequestReviewInfoModel.Labels;
            var config = _cfg.CurrentValue.AdditionalCodeReviewConfiguration;

            return labels.Contains(config.NeedAdditionalReviewLabel)
                   || labels.Contains(config.NoNeedAdditionalReviewLabel);
        }

        private void CreateReviewRequestAndAddLabel(IRepositoryConnectionContext repoContext, PullRequestReviewInfoModel pullRequestReviewInfoModel, string reviewer)
        {
            var conf = _cfg.CurrentValue.AdditionalCodeReviewConfiguration;
            var prReviewModel = pullRequestReviewInfoModel;

            _logger.LogDebug("Creating new code review for {pullRequestNumber} by {reviewer} and set label '{label}' ",
                prReviewModel.PullRequestNumber, reviewer, conf.NeedAdditionalReviewLabel);
            try
            {
                repoContext.CreateReviewRequest(prReviewModel.PullRequestNumber, reviewer);
                repoContext.AddLabelToIssue(prReviewModel.PullRequestNumber, conf.NeedAdditionalReviewLabel);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    "Error when creating new code review for {pullRequestNumber} by {reviewer} and set label '{label}'",
                    prReviewModel.PullRequestNumber, reviewer,
                    conf.NeedAdditionalReviewLabel);
            }
        }

        private void AddNoNeedAdditionalReviewLabel(IRepositoryConnectionContext repoContext, PullRequestReviewInfoModel pullRequestReviewInfoModel)
        {

            try
            {
                repoContext.AddLabelToIssue(pullRequestReviewInfoModel.PullRequestNumber,
                    _cfg.CurrentValue.AdditionalCodeReviewConfiguration.NoNeedAdditionalReviewLabel);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Error when adding label '{label}' to issue {issueNumber} ",
                    _cfg.CurrentValue.AdditionalCodeReviewConfiguration.NoNeedAdditionalReviewLabel,
                    pullRequestReviewInfoModel.PullRequestNumber);
            }
        }

        private bool IsAutomergeBotPullRequest(PullRequestReviewInfoModel prReviewModel)
        {
            return prReviewModel.BranchName.StartsWith(_cfg.CurrentValue.CreatedBranchesPrefix);
        }

        private string GetRandomReviewerOrNull(string excludeUserLogin)
        {
            var reviewers = _cfg.CurrentValue.AdditionalCodeReviewConfiguration.Reviewers ?? new List<string>();

            reviewers = reviewers.Select(t => t.ToLower()).ToList();
            reviewers.Remove(excludeUserLogin.ToLower());

            if (reviewers.Count == 0)
            {
                return null;
            }

            string result = null;
            Random random = new Random();
            var number = random.Next(1, 100);

            if (number <= _cfg.CurrentValue.AdditionalCodeReviewConfiguration.AdditionalReviewProbability)
            {
                var index = random.Next(0, reviewers.Count - 1);
                result = reviewers[index];
            }

            return result;
        }
    }
}
