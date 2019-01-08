using System.Collections.Generic;
using PerfectGym.AutomergeBot.Models;

namespace PerfectGym.AutomergeBot
{
    public class SlackUserMappingsConfiguration
    {
        public List<GitHubUserEmailToSlackUserNameMappingEntry> UserMappings { get; set; }
    }
}
