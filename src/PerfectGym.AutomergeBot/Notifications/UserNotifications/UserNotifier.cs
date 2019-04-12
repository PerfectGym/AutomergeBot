using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
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

        void NotifyAboutOpenPullRequests(IEnumerable<PullRequest> filteredPullRequests);
    }

    public class UserNotifier : IUserNotifier
    {
        private readonly ILogger<UserNotifier> _logger;
        private readonly ISlackClientProvider _clientProvider;
        private readonly ISlackMessageProvider _messageProvider;

        public UserNotifier(
            ILogger<UserNotifier> logger,
            ISlackClientProvider clientProvider,
            ISlackMessageProvider messageProvider)
        {
            _logger = logger;
            _clientProvider = clientProvider;
            _messageProvider = messageProvider;
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
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"UserNotifications\MessageTemplates", messageType + ".txt");
            return File.ReadAllText(path);
        }

        public void NotifyAboutOpenPullRequests(IEnumerable<PullRequest> filteredPullRequests)
        {
            var pullRequests = filteredPullRequests.ToList();
            var users = pullRequests.SelectMany(pr => pr.Assignees)
                                              .Distinct(new UserComparer())
                                              .ToList();

            using (var client = _clientProvider.Create())
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

        private void NotifyAssignedUsersBySlack(Account user, IEnumerable<PullRequest> pullRequests, ISlackClient client)
        {
            var prs = pullRequests.Select(pr => new PullRequestModel { Url = pr.HtmlUrl, CreatedAt = pr.CreatedAt });
            var contact = user.Email ?? user.Login;
            var author = client.FindUser(contact);
            var message = _messageProvider.CreateNotifyUserAboutPendingPullRequestMessage(author, prs);
            client.SendMessage(message);
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