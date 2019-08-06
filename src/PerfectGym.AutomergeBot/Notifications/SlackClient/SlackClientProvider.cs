using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PerfectGym.AutomergeBot.Notifications.UserNotifications;
using SlackClientStandard;
using SlackClientStandard.Entities;

namespace PerfectGym.AutomergeBot.Notifications.SlackClient
{
    public interface ISlackClientProvider
    {
        ISlackClient Create();
    }

    public class SlackClientProvider : ISlackClientProvider
    {
        private readonly ILogger<UserNotifier> _logger;
        private readonly SlackClientStandard.SlackClientProvider _slackClientProvider = new SlackClientStandard.SlackClientProvider();
        private readonly AutomergeBotConfiguration _cfg;
        private readonly SlackUserMappingsConfiguration _mappingsCfg;

        public SlackClientProvider(
            ILogger<UserNotifier> logger,
            IOptionsMonitor<AutomergeBotConfiguration> cfg,
            IOptionsMonitor<SlackUserMappingsConfiguration> mappingsCfg)
        {
            _logger = logger;
            _cfg = cfg.CurrentValue;
            _mappingsCfg = mappingsCfg.CurrentValue;
        }

        public ISlackClient Create()
        {
            if (IsSlackConfigured())
            {
                return CreateSlackClient();
            }

            _logger.LogDebug($"Use {nameof(NullSlackClient)} because Slack configuration is missing or incomplete");
            return new NullSlackClient();
        }

        private bool IsSlackConfigured()
        {
            return !string.IsNullOrWhiteSpace(_cfg.PullRequestGovernorConfiguration?.SlackToken);
        }


        private ISlackClient CreateSlackClient()
        {
            return _slackClientProvider.CreateClient(
                _cfg.PullRequestGovernorConfiguration.SlackToken,
                _cfg.AutomergeBotGitHubUserName,
                GetSlackUserMappings());
        }

        private List<SlackUserMapping> GetSlackUserMappings()
        {
            return _mappingsCfg?.UserMappings?
                .Select(m => new SlackUserMapping
                {
                    SomeUserName = m.GitHubUserName,
                    Email = m.SlackUserEmail
                })
                .ToList();
        }
    }
}