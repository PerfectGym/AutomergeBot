using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PerfectGym.AutomergeBot.Models
{
    public class PullRequestReviewInfoModel : InfoModelBase
    {
        public string State { get; }
        public int PullRequestNumber { get; }
        public string BranchName { get; }
        public string UserLogin { get; }
        public List<String> Labels { get; }


        public PullRequestReviewInfoModel(string state, int pullRequestNumber, string branchName, string userLogin, List<String> labels)
        {
            State = state;
            PullRequestNumber = pullRequestNumber;
            BranchName = branchName;
            UserLogin = userLogin;
            Labels = labels;
        }

        public static PullRequestReviewInfoModel CreateFromPayload(JObject pushPayload)
        {
            return new PullRequestReviewInfoModel(
                SafeGet<string>(pushPayload, "review.state"),
                SafeGet<int>(pushPayload, "pull_request.number"),
                SafeGet<string>(pushPayload, "pull_request.head.ref"),
                SafeGet<string>(pushPayload, "review.user.login"),
                SafeGetList<String>(pushPayload, "pull_request.labels.name")
            );
        }

        public class Label
        {
            public string Name { get; set; }
        }

        public static class States
        {
            public const string Approved = "approved";
        }

    }
}
