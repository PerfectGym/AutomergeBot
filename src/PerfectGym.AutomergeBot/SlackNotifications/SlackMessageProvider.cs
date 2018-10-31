using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PerfectGym.AutomergeBot.SlackNotifications
{
    public class SlackMessageProvider : ISlackMessageProvider
    {
        private readonly INow _now;

        public SlackMessageProvider(INow now)
        {
            _now = now;
        }

        public string CreateNotifyUserAboutPendingPullRequestMessage(
            string authorId,
            IEnumerable<PullRequestModel> pullRequests)
        {
            var stringBuilder = new StringBuilder();
            
            stringBuilder.AppendLine(
                $"<@{authorId}> please resolve conflicts manually, approve review and merge pull request");
            foreach (var pullRequestUrl in pullRequests)
            {
                var duration = _now.Now() - pullRequestUrl.CreatedAt;
                stringBuilder.Append($"PR open for: {duration.FormatAsDescriptiveForHuman()}");
                stringBuilder.AppendLine(pullRequestUrl.Url);
            }

            return WebUtility.UrlEncode(stringBuilder.ToString());
        }
    }

    internal static class TimeSpanExt
    {
        public static string FormatAsDescriptiveForHuman(this TimeSpan duration)
        {
            var sb = new StringBuilder();
            if (duration.Days > 0)
            {
                sb.Append(duration.Days + " day(s) ");
            }

            if (duration.Hours > 0)
            {
                sb.Append(duration.Hours + " hour(s) ");
            }
            if (duration.Minutes > 0)
            {
                sb.Append(duration.Minutes + " min(s) ");
            }

            if (duration > TimeSpan.Zero && duration.TotalMinutes < 1)
            {
                sb.Append(duration.Seconds + " s ");
            }

            return sb.ToString();
        }
    }
}