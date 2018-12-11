using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using PerfectGym.AutomergeBot.RepositoryConnection;
using PerfectGym.AutomergeBot.SlackNotifications;
using SlackClientStandard;
using ISlackClientProvider = PerfectGym.AutomergeBot.SlackClient.ISlackClientProvider;

namespace PerfectGym.AutomergeBot.Tests
{
    [TestFixture]
    public class MailNotificationTests
    {
        [Test]
        public void UserNotifier_WhenConflictOccurs_CreateProperMessage()
        {
            //Arrange
            var slackClientProviderMock = new Mock<ISlackClientProvider>();
            slackClientProviderMock
                .Setup(s => s.Create())
                .Returns(new Mock<ISlackClient>().Object);

            var slackClientProvider = slackClientProviderMock.Object;
            var messageProvider = new SlackMessageProvider(new DateTimeNow());

            var userNotifier = new UserNotifier(new NullLogger<UserNotifier>(),slackClientProvider, messageProvider);
            var repoContextMock = new Mock<IRepositoryConnectionContext>();
            repoContextMock.Setup(r=>r.AddPullRequestComment(It.IsAny<int>(),It.IsAny<string>()))
                .Verifiable();
            repoContextMock.Setup(r=>r.AddReviewerToPullRequest(It.IsAny<int>(),It.IsAny<string[]>()))
                .Verifiable();
            repoContextMock.Setup(r=>r.AssignUsersToPullRequest(It.IsAny<int>(),It.IsAny<string[]>()))
                .Verifiable();

            var repoContext = repoContextMock.Object;
            var gitHubUserName = "testUser";
            var pullRequestUrl = "https://test/pull/1";
            var destinationBranch =  "release-R77";
            var pullRequestBranchName = $"AutomergeBot/release-R76.1_ba8221b6_to_release-R77";
            var pullRequestNumber = 1;
            
            //Act
            userNotifier.NotifyUserAboutPullRequestWithUnresolvedConflicts(
            pullRequestNumber, 
            gitHubUserName, 
            repoContext,
            pullRequestBranchName,
            destinationBranch,
            pullRequestUrl
            );

            //Assert
            var comment = $"Cannot merge automatically. @{gitHubUserName} please resolve conflicts manually, approve review and merge pull request."
                          + "\r\n"
                          + "Be aware that when someone's PR has not been merged before yours PR and then has been merged,"
                          + "\r\n"
                          + "yours PR still has info about conflicts."
                          + "\r\n"
                          + "Github does not update it and lies that conflicts are present. "
                          + "\r\n\r\n"
                          + "How to do it (using the GIT command line):\r\n"
                          + $"1. Fetch changes from server and checkout '{destinationBranch}' branch.\r\n"
                          + $"   Then merge 'origin/{pullRequestBranchName}' branch and resolve conflicts\r\n"
                          + "   ```\r\n"
                          + $"   git fetch -q && git checkout -q {destinationBranch} && " + "git reset -q --hard @{u} &&"+$" git merge --no-ff origin/{pullRequestBranchName}\r\n"
                          + "   ```\r\n"
                          + $"2. Approve [pull request]({pullRequestUrl}/files#submit-review) review\r\n"
                          + $"3. Push changes to {destinationBranch}\r\n"
                          + "   ```\r\n"
                          + $"   git push origin {destinationBranch}\r\n"
                          + "   ```\r\n";

            repoContextMock.Verify(r=>r.AddPullRequestComment(pullRequestNumber,comment),Times.Once);
            repoContextMock.Verify(r=>r.AddReviewerToPullRequest(pullRequestNumber,new []{gitHubUserName}),Times.Once());
            repoContextMock.Verify(r=>r.AssignUsersToPullRequest(pullRequestNumber, new []{gitHubUserName}), Times.Once);
        }
    }
}
