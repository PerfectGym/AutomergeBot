using System.Collections.Generic;

namespace PerfectGym.AutomergeBot.Notifications.SlackNotifications
{
    public interface ISlackMessageProvider
    {
        string CreateNotifyUserAboutPendingPullRequestMessage(
            string authorId, 
            IEnumerable<PullRequestModel> pullRequests);
    }
}
