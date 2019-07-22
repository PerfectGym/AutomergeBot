﻿using SlackClientStandard;

namespace PerfectGym.AutomergeBot.Notifications.SlackClient
{
    internal class NullSlackClient : ISlackClient
    {
        public void Dispose()
        {
            //nop
        }

        public void SendMessage(string message)
        {
            //nop
        }

        public string FindUser(string user)
        {
            return string.Empty;
        }
    }
}