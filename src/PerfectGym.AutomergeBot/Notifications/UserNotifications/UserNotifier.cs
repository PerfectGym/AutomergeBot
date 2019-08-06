using System;
using System.Activities.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using PerfectGym.AutomergeBot.Notifications.SlackNotifications;
using PerfectGym.AutomergeBot.RepositoryConnection;
using SlackClientStandard;
using ISlackClientProvider = PerfectGym.AutomergeBot.Notifications.SlackClient.ISlackClientProvider;

namespace PerfectGym.AutomergeBot.Notifications.UserNotifications
{
    public interface IUserNotifier
    {
        void NotifyUserAboutPullRequestWithUnresolvedConflicts(
            int pullRequestNumber,
            string gitHubUserName,
            IRepositoryConnectionContext repoContext,
            string pullRequestBranchName,
            string destinationBranch,
            string pullRequestUrl);

        void NotifyAboutOpenAutoMergePullRequests(IEnumerable<PullRequest> filteredPullRequests);


        void SendDirectMessageAboutOpenPullRequests(IEnumerable<PullRequest> pullRequests);

    }

    public class UserNotifier : IUserNotifier
    {
        private readonly ILogger<UserNotifier> _logger;
        private readonly ISlackClientProvider _slackClientProvider;
        private readonly ISlackMessageProvider _messageProvider;
        private readonly AutomergeBotConfiguration _cfg;

        public UserNotifier(
            ILogger<UserNotifier> logger,
            ISlackClientProvider slackClientProvider,
            ISlackMessageProvider messageProvider,
            IOptionsMonitor<AutomergeBotConfiguration> cfg
            )
        {
            _logger = logger;
            _slackClientProvider = slackClientProvider;
            _messageProvider = messageProvider;
            _cfg = cfg.CurrentValue;
        }

        public void NotifyUserAboutPullRequestWithUnresolvedConflicts(
            int pullRequestNumber,
            string gitHubUserName,
            IRepositoryConnectionContext repoContext,
            string pullRequestBranchName,
            string destinationBranch,
            string pullRequestUrl)
        {

            var comment = CreateMessage(pullRequestNumber, gitHubUserName, pullRequestBranchName, destinationBranch, pullRequestUrl);

            repoContext.AddPullRequestComment(pullRequestNumber, comment);
            repoContext.AddReviewerToPullRequest(pullRequestNumber, new[] { gitHubUserName });
            repoContext.AssignUsersToPullRequest(pullRequestNumber, new[] { gitHubUserName });
        }

        private string CreateMessage(int pullRequestNumber, string gitHubUserName, string pullRequestBranchName, string destinationBranch, string pullRequestUrl)
        {
            const string fallbackMessage = "Cannot merge automatically. ";
            const string messageType = "NotifyUserAboutPullRequestWithUnresolvedConflicts";

            try
            {
                var comment = GetMessageTemplate(messageType);
                comment = comment
                    .Replace("{{pullRequestNumber}}", pullRequestNumber.ToString())
                    .Replace("{{gitHubUserName}}", gitHubUserName)
                    .Replace("{{pullRequestBranchName}}", pullRequestBranchName)
                    .Replace("{{destinationBranch}}", destinationBranch)
                    .Replace("{{pullRequestUrl}}", pullRequestUrl);
                return comment;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not create message {messageType} from template", messageType);
                return fallbackMessage;
            }
        }

        private static string GetMessageTemplate(string messageType)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Notifications\UserNotifications\MessageTemplates", messageType + ".txt");
            return File.ReadAllText(path);
        }

        public void NotifyAboutOpenAutoMergePullRequests(IEnumerable<PullRequest> filteredPullRequests)
        {
            var pullRequests = filteredPullRequests.ToList();
            var users = pullRequests.SelectMany(pr => pr.Assignees)
                                              .Distinct(new UserComparer())
                                              .ToList();

            using (var client = _slackClientProvider.Create())
            {
                foreach (var user in users)
                {
                    var userPullRequests = pullRequests.FindAll(pr => pr.Assignees.Contains(user, new UserComparer()));

                    try
                    {
                        NotifyAssignedUsersBySlack(user, userPullRequests, client);
                    }
                    catch (SlackApiErrorException e)
                    {
                        _logger.LogError(e, "Failed notifying user {User}", user);
                    }
                }
            }
        }

        public void SendDirectMessageAboutOpenPullRequests(IEnumerable<PullRequest> pullRequests)
        {
            foreach (var notify in GetNotificationsAboutOpenPullRequest(pullRequests))
            {
                using (var client = _slackClientProvider.Create())
                {
                    SendDirectMessageAboutPullRequests("Your open pull requests:", notify.Login, notify.OwnPullRequests, client);
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    SendDirectMessageAboutPullRequests("The pull requests waiting for your review:", notify.Login, notify.PullRequestsToReview, client);

                    Thread.Sleep(TimeSpan.FromSeconds(1));

                }

            }
        }

        private void SendDirectMessageAboutPullRequests(string headOfmessage, string login, IEnumerable<PullRequest> pullRequests, ISlackClient client)
        {
            if (pullRequests!=null && pullRequests.Any())
            {
                var message = _messageProvider.CreatePullRequestMessage(
                    headOfmessage,
                    login,
                    pullRequests
                );

                try
                {
                    client.SendMessageToUser(message, login);
                }
                catch (SlackApiErrorException e)
                {
                    _logger.LogError(e, "Failed notifying user {User}", login);
                }
            }
        }

        private IEnumerable<NotificationsAboutOpenPullRequest> GetNotificationsAboutOpenPullRequest(IEnumerable<PullRequest> pullRequests)
        {
            var result = new List<NotificationsAboutOpenPullRequest>();
            foreach (var pr in pullRequests.GroupBy(t=>t.User.Login))
            {
                result.Add(new NotificationsAboutOpenPullRequest()
                {
                   Login = pr.Key,
                   OwnPullRequests = pr.ToList(),
                   PullRequestsToReview = pullRequests.Where(r=>r.RequestedReviewers.Any(a=>a.Login==pr.Key)).ToList()
                });
            }

            //Create notify for users that do not have own pull requests
            foreach (var user in pullRequests
                .SelectMany(r=>r.RequestedReviewers.Select(rr=>rr.Login))
                .Distinct()
                .Except(result.Select(r=>r.Login)).ToList())
            {
                result.Add(new NotificationsAboutOpenPullRequest()
                {
                    Login = user,
                    OwnPullRequests = new List<PullRequest>(),
                    PullRequestsToReview = pullRequests.Where(r => r.RequestedReviewers.Any(a => a.Login == user)).ToList()
                });
            }


            return result;
        }

        public class NotificationsAboutOpenPullRequest
        {
            public string Login { get; set; }
            public IEnumerable<PullRequest> OwnPullRequests { get; set; }
            public IEnumerable<PullRequest> PullRequestsToReview { get; set; }
        }



        private void NotifyAssignedUsersBySlack(Account user, IEnumerable<PullRequest> pullRequests, ISlackClient client)
        {
            var prs = pullRequests.Select(pr => new PullRequestModel { Url = pr.HtmlUrl, CreatedAt = pr.CreatedAt });
            var contact = user.Email ?? user.Login;
            var author = client.FindUser(contact);
            var message = _messageProvider.CreateNotifyUserAboutPendingPullRequestMessage(author, prs);
            client.SendMessageToChannels(message, _cfg.PullRequestGovernorConfiguration.GetSlackChannelsParsed());
        }
    }

    internal class UserComparer : IEqualityComparer<User>
    {
        public bool Equals(User x, User y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return string.Equals(x.Email, y.Email, StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(x.Login, y.Login, StringComparison.InvariantCultureIgnoreCase);

        }

        public int GetHashCode(User obj)
        {
            return (obj.Email?.ToLowerInvariant() + obj.Login?.ToLowerInvariant()).GetHashCode();
        }
    }
}