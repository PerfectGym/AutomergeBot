using System;
using System.Collections.Generic;
using System.Text;

namespace PerfectGym.AutomergeBot.Features.CodeReviewInspector
{
    public class CodeReviewInspectorConfiguration
    {
        public bool IsEnable { get; set; }

        public List<string> SendNotificationsToReviewersAtTimes { get; set; }
    }
}
