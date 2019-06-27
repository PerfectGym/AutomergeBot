using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PerfectGym.AutomergeBot.Models
{
    /// <summary>
    /// info amount event https://developer.github.com/v3/activity/events/types/#checkrunevent
    /// </summary>
    public class CheckRunInfoModel : InfoModelBase
    {

        public string Name { get; }
        public int PullRequestNumber { get; }
        public string Status { get; }
        public string Conclusion { get; }

        public CheckRunInfoModel(string name, int pullRequestNumber, string status, string conclusion)
        {
            Name = name;
            PullRequestNumber = pullRequestNumber;
            Status = status;
            Conclusion = conclusion;
        }

        public static CheckRunInfoModel CreateFromPayload(JObject pushPayload)
        {
            return new CheckRunInfoModel(
                SafeGet<string>(pushPayload, "check_run.state"),
                SafeGet<int>(pushPayload, "pull_request.number"),
                SafeGet<string>(pushPayload, "check_run.status"),
                SafeGet<string>(pushPayload, "check_run.user.conclusion")
            );
        }
        
        public static class StatusValues
        {
            public const string Queued = "queued";
            public const string InProgress = "in_progress";
            public const string Completed = "completed";
        }

        public static class ConclusionValues
        {
            public const string Success = "success";
            public const string Failure = "failure";
            public const string Neutral = "neutral";
            public const string Cancelled = "cancelled";
            public const string TimedOut = "timed_out";
            public const string ActionRequired = "action_required";
        }
    }
}
