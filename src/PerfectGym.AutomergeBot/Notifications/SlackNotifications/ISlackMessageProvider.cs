using System.Collections.Generic;
using Octokit;

namespace PerfectGym.AutomergeBot.Notifications.SlackNotifications
{
    public interface ISlackMessageProvider
    {
        string CreateNotifyUserAboutPendingPullRequestMessage(
            string authorId, 
            IEnumerable<PullRequestModel> pullRequests);

        string CreatePullRequestMessage(
            string headerOfMessage,
            string authorId,
            IEnumerable<PullRequest> pullRequests);

    }
}
