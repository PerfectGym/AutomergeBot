using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Octokit;
using SlackClientStandard;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Options;
using PerfectGym.AutomergeBot.Notifications.SlackNotifications;
using PerfectGym.AutomergeBot.Notifications.UserNotifications;
using ISlackClientProvider = PerfectGym.AutomergeBot.Notifications.SlackClient.ISlackClientProvider;

namespace PerfectGym.AutomergeBot.Tests
{
    [TestFixture]
    public class SlackNotificationsTests
    {
        [Test]
        public void NotifyUser_WhenUserHasMultipleOpenPRs_SendOneMessageOnly()
        {
            //Arrange
            var slackClientMock = new Mock<ISlackClient>();
            slackClientMock.Setup(c=>c.SendMessageToChannels(It.IsAny<string>(), It.IsAny<string[]>()))
                .Verifiable();
            var slackClient=slackClientMock.Object;

            var slackClientProviderMock = new Mock<ISlackClientProvider>();
            slackClientProviderMock
                .Setup(s => s.Create())
                .Returns(slackClient);
            var slackClientProvider = slackClientProviderMock.Object;

            var nowMock = new Mock<INow>();
            nowMock.Setup(n => n.Now())
                .Returns(DateTimeOffset.MinValue);


            var automergeBotConfigurationMock = new Mock<OptionsMonitor<AutomergeBotConfiguration>>();

            var userNotifier = new UserNotifier(new NullLogger<UserNotifier>(),slackClientProvider, new SlackMessageProvider(nowMock.Object)
                , automergeBotConfigurationMock.Object);
            var pullRequests = GetFakePRCollection().ToList();
            
            //Act
            userNotifier.NotifyAboutOpenAutoMergePullRequests(pullRequests);

            //Assert
            var usersPullRequestsCount = pullRequests.Count();
            slackClientMock.Verify(c=>c.SendMessageToChannels(It.Is<string>(m =>
                CountStringOccurrences(WebUtility.UrlDecode(m), "PR open for")==usersPullRequestsCount
                ),It.IsAny<string[]>()),Times.Once);

        }

        public static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        [TestCase(1,1,48,0,"PR open for: 1 day(s) 1 hour(s) 48 min(s)")]
        [TestCase(0,1,48,0,"PR open for: 1 hour(s) 48 min(s)")]
        [TestCase(0,0,48,0,"PR open for: 48 min(s)")]
        [TestCase(0,0,0,5,"PR open for: 5 s")]
        public void MessageProvider_ForOpenPR_DurationIsCalculated(int days,int hours,int minutes,int seconds, string durationText)
        {
            //Arrrange
            var nowMock = new Mock<INow>();
            var nowDateTime = new DateTimeOffset(2018, 10, 20, 12, 30, 00,TimeSpan.Zero);
            nowMock.Setup(m => m.Now())
                   .Returns(nowDateTime);

            var messageProvider = new SlackMessageProvider(nowMock.Object);
            var prDuration = new TimeSpan(days,hours,minutes,seconds);
            var prModel = new PullRequestModel {CreatedAt = nowDateTime.Subtract(prDuration), Url = "https://12"};
            
            //Act
            var message = messageProvider.CreateNotifyUserAboutPendingPullRequestMessage("testUser",new []{prModel});
            var decodedMessage = WebUtility.UrlDecode(message);
            
            //Assert
            Assert.IsTrue(decodedMessage.Contains(durationText+" "+prModel.Url));

        }

        [Test]
        public void MessageProvider_WhenUserHasTwoOpenPR_ResolveConflictPhraseOccursOnceAndPROpenOccursTwice()
        {
            //Arrrange
            var nowMock = new Mock<INow>();
            var nowDateTime = new DateTime(2018, 10, 20, 12, 30, 00);
            nowMock.Setup(m => m.Now())
                .Returns(nowDateTime);

            var messageProvider = new SlackMessageProvider(nowMock.Object);
            
            var pullRequests = new[]
            {
                new PullRequestModel {CreatedAt = nowDateTime.Subtract(TimeSpan.FromMinutes(12)), Url = "https://12"},
                new PullRequestModel {CreatedAt = nowDateTime.Subtract(TimeSpan.FromMinutes(4)), Url = "https://4"}
            };

            //Act
            var messageEncoded = messageProvider.CreateNotifyUserAboutPendingPullRequestMessage("testUser",pullRequests);

            //Assert
            var message = WebUtility.UrlDecode(messageEncoded);
            var occurrences = message.Select((c, i) => message.Substring(i))
                .Count(sub => sub.StartsWith("resolve"));

            Assert.AreEqual(1,occurrences, "Message contains more than 1 'resolve' word");

            var prOpenOccurrences =  message.Select((c, i) => message.Substring(i))
                .Count(sub => sub.StartsWith("PR open for"));

            var pullRequestsCount = pullRequests.Length;
            Assert.AreEqual(pullRequestsCount, prOpenOccurrences, "Message mentions wrong number of the pull requests");

        }

        private static IEnumerable<PullRequest> GetFakePRCollection()
        {
            return new List<PullRequest>()
            {
                GetFakePR($"https://github.com/repo/repo/pull/17684",GetFakeGitHubUser("testUser")),
                GetFakePR($"https://github.com/repo/repo/pull/23333",GetFakeGitHubUser("testUser"))
            };
        }

        private static PullRequest GetFakePR(string htmlUrl,User user)
        {
            return new PullRequest(0, "",htmlUrl, "", "", "", "", 0,
                ItemState.Open, ""
                , "",
                DateTimeOffset.MinValue, 
                DateTimeOffset.MinValue, null, null, null, null,
                user
                , user,
                new []{user}.ToImmutableList() , 
                null, null, null, "", 0, 0, 0, 0, 0, null, false, null);
        }

        private static User GetFakeGitHubUser(string login)
        {
            return new User("", "", "", 0, "", DateTimeOffset.MinValue, DateTimeOffset.MinValue, 0, null, 0, 0, null, "",
                0, 123,"", login, "", 0, null, 0, 0, 0, "", null, false, "", null);
        }
    }
}
