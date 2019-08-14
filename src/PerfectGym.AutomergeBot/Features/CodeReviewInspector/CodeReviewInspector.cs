using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using PerfectGym.AutomergeBot.Notifications.SlackClient;
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
        private readonly TimeScheduler _timeScheduler = new TimeScheduler();

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
            if (_cfg.CodeReviewInspectorConfiguration?.IsEnabled ?? false)
            {
                foreach (var time in _cfg.CodeReviewInspectorConfiguration.TimeDefinitionsWhenNotificationsAreSent)
                {
                    if (TimeSpan.TryParse(time, out TimeSpan timeAsTimeSpan))
                    {
                        _timeScheduler.ScheduledActions.Add(new TimeScheduler.ScheduleAction(timeAsTimeSpan, CheckOpenPullRequestsAndNotifyOwnersAndReviewers));
                    }
                    else
                    {
                      
                        _logger.LogError($"Could not parse time ({{value}}) of configuration property '{nameof(CodeReviewInspectorConfiguration.TimeDefinitionsWhenNotificationsAreSent)}'. Expected format HH:MM", time);
                    }

                }

                _timeScheduler.Start();
            }
        }
        
        private void CheckOpenPullRequestsAndNotifyOwnersAndReviewers()
        {
            _logger.LogInformation("CheckWaitingPullRequestAndNotifyReviewers");
            using (var repoContext = _repositoryConnectionProvider.GetRepositoryConnection())
            {
                var openPullRequests = repoContext.GetOpenPullRequests();
                if (openPullRequests.Count > 0)
                {
                    _userNotifier.SendDirectMessageAboutOpenPullRequests(openPullRequests);
                }
            }
        }
   
    }
}
