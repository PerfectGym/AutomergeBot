using System;
using System.Collections.Generic;
using System.Text;
using PerfectGym.AutomergeBot.Models;

namespace PerfectGym.AutomergeBot
{
    public class SlackUserMappingsConfiguration
    {
        public List<EmailToSomeUserNameMapping> UserMappings { get; set; }
    }
}
