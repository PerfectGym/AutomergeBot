using System;
using Microsoft.Extensions.Logging;
using Octokit;
using PerfectGym.AutomergeBot.RepositoryConnection;
using SlackClientStandard;
using System.Collections.Generic;
using System.Linq;
using PerfectGym.AutomergeBot.SlackNotifications;
using ISlackClientProvider = PerfectGym.AutomergeBot.SlackClient.ISlackClientProvider;

namespace PerfectGym.AutomergeBot
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
            var comment = $"Cannot merge automatically. @{gitHubUserName} please resolve conflicts manually, approve review and merge pull request."
                          + "\r\n"
                          + "Be aware that when someone's PR has not been merged before yours PR and then has been merged,"
                          + "\r\n"
                          + "yours PR still has info about conflicts."
                          + "\r\n"
                          + "Github does not update it and lies that conflicts are present. "
                          + "\r\n\r\n"
                          + "How to do it (using the GIT command line):\r\n"
                          + $"1. Fetch changes from server and checkout '{destinationBranch}' branch\r\n"
                          + "   ```\r\n"
                          + $"   git fetch -q && git checkout -q {destinationBranch} && " + "git reset -q --hard @{u}\r\n"
                          + "   ```\r\n"
                          + $"2. Merge 'origin/{pullRequestBranchName}' branch and resolve conflicts\r\n"
                          + "   ```\r\n"
                          + $"   git merge --no-ff origin/{pullRequestBranchName}\r\n"
                          + "   ```\r\n"
                          + $"4. Approve [pull request]({pullRequestUrl}/files#submit-review) review\r\n"
                          + $"5. Push changes to {destinationBranch}\r\n"
                          + "   ```\r\n"
                          + $"   git push origin {destinationBranch}\r\n"
                          + "   ```\r\n";

            repoContext.AddPullRequestComment(pullRequestNumber, comment);
            repoContext.AddReviewerToPullRequest(pullRequestNumber, new[] { gitHubUserName });
            repoContext.AssignUsersToPullRequest(pullRequestNumber, new[] { gitHubUserName });
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
                    var userPullRequests = pullRequests.Where(pr => pr.Assignees.Contains(user));
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
            var prs = pullRequests.Select(pr=>new PullRequestModel{Url= pr.HtmlUrl, CreatedAt = pr.CreatedAt});
            var contact = user.Email ?? user.Login;
            var author = client.FindUser(contact);
            var message =_messageProvider.CreateNotifyUserAboutPendingPullRequestMessage(author,prs);
            client.SendMessage(message);
        }

    }

    internal class UserComparer : IEqualityComparer<User>
    {
        public bool Equals(User x, User y)
        {
            if(x == null && y == null)
            {
                return true;
            }

            if(x == null || y == null)
            {
                return false;
            }

            return string.Equals(x.Email,y.Email,StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(x.Login,y.Login,StringComparison.InvariantCultureIgnoreCase);

        }

        public int GetHashCode(User obj)
        {
            return (obj.Email?.ToLowerInvariant() + obj.Login?.ToLowerInvariant()).GetHashCode();
        }
    }
}