﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PerfectGym.AutomergeBot.AutomergeBot;

namespace PerfectGym.AutomergeBot.RepositoryConnection
{
    public interface IRepositoryConnectionProvider
    {
        IRepositoryConnectionContext GetRepositoryConnection();
    }

    public class RepositoryConnectionProvider : IRepositoryConnectionProvider
    {
        private readonly ILogger<IRepositoryConnectionProvider> _logger;
        private readonly AutomergeBotConfiguration _cfg;

        public RepositoryConnectionProvider(
            ILogger<IRepositoryConnectionProvider> logger,
            IOptionsMonitor<AutomergeBotConfiguration> cfg)
        {
            _logger = logger;
            _cfg = cfg.CurrentValue;
        }

        public IRepositoryConnectionContext GetRepositoryConnection()
        {
            return new RepositoryConnectionContext(_logger, _cfg.RepositoryName, _cfg.RepositoryOwner, _cfg.AuthToken);
        }
    }
}