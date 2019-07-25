using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using PerfectGym.AutomergeBot.Notifications.UserNotifications;
using PerfectGym.AutomergeBot.RepositoryConnection;

namespace PerfectGym.AutomergeBot.Features.CodeReviewInspector
{
    public class CodeReviewInspector
    {
        private readonly IRepositoryConnectionProvider _repositoryConnectionProvider;
        private readonly IUserNotifier _userNotifier;
        private readonly ILogger<CodeReviewInspector> _logger;
        private readonly AutomergeBotConfiguration _cfg;
        TimeScheduler  _timeScheduler= new TimeScheduler();

        public CodeReviewInspector(
            ILogger<CodeReviewInspector> logger,
            IOptionsMonitor<AutomergeBotConfiguration> cfg,
            IRepositoryConnectionProvider repositoryConnectionProvider,
            IUserNotifier userNotifier
            )
        {
            _logger = logger;
            _repositoryConnectionProvider = repositoryConnectionProvider;
            _userNotifier = userNotifier;
            _cfg = cfg.CurrentValue;
        }

        public void StartWorker()
        {
            _logger.LogInformation($"Loading configuration for {this.GetType().Name}");

            if (_cfg.CodeReviewInspectorConfiguration != null && _cfg.CodeReviewInspectorConfiguration.IsEnable)
            {
                foreach (var time in _cfg.CodeReviewInspectorConfiguration.SendNotificationsToReviewersAtTimes)
                {
                    if (TimeSpan.TryParse(time, out TimeSpan timeAsTimeSpan))
                    {
                        _timeScheduler.ScheduledActions.Add(new TimeScheduler.ScheduleAction(timeAsTimeSpan, CheckWaitingPullRequestAndNotifyReviewers));
                    }
                    else
                    {
                        _logger.LogWarning("Could not parse time ({valeu}) form SendNotificationsToReviewersAtTimes. Expected format HH:MM", time);
                    }

                }

                _timeScheduler.Start();
            }
        }
        
        private void CheckWaitingPullRequestAndNotifyReviewers()
        {
            _logger.LogInformation("CheckWaitingPullRequestAndNotifyReviewers");
            using (var repoContext = _repositoryConnectionProvider.GetRepositoryConnection())
            {
                var openPullRequests = repoContext.GetOpenPullRequests();

                var filteredPullRequests = FilterPullRequests(openPullRequests);
                if (filteredPullRequests.Count > 0)
                {
                    _logger.LogInformation("Notifying users there is still open {count} pull requests", filteredPullRequests.Count);
                    //TODO:Sending nottifications
                }
            }
        }

        private List<PullRequest> FilterPullRequests(IReadOnlyList<PullRequest> openPullRequests)
        {
            return openPullRequests.ToList();
        }

    }
}
