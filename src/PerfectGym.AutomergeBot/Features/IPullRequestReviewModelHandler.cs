using System;
using System.Collections.Generic;
using System.Text;
using PerfectGym.AutomergeBot.Models;

namespace PerfectGym.AutomergeBot.Features
{
    public interface IPullRequestReviewModelHandler
    {
        void Handle(PullRequestReviewInfoModel pullRequestReviewInfoModel);
    }
}
