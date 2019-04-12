using System;

namespace PerfectGym.AutomergeBot.Notifications.SlackNotifications
{
    public class PullRequestModel
    {
        public string Url { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
