﻿using System;

namespace PerfectGym.AutomergeBot.Notifications.SlackNotifications
{
    public interface INow
    {
        DateTimeOffset Now();
    }

    public class DateTimeNow : INow
    {
        public DateTimeOffset Now()
        {
            return DateTime.Now;
        }
    }
}