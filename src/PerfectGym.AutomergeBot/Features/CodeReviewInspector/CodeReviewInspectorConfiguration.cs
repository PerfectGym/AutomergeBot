using System;
using System.Collections.Generic;
using System.Text;

namespace PerfectGym.AutomergeBot.Features.CodeReviewInspector
{
    public class CodeReviewInspectorConfiguration
    {
        public bool IsEnabled { get; set; }

        public List<string> TimeDefinitionsWhenNotificationsAreSent { get; set; }
    }
}
