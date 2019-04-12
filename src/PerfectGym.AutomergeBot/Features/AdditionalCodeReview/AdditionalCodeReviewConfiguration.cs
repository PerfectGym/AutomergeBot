using System;
using System.Collections.Generic;
using System.Text;

namespace PerfectGym.AutomergeBot.Features.AdditionalCodeReview
{
    public class AdditionalCodeReviewConfiguration
    {
        /// <summary>
        /// 0-100
        /// </summary>
        public int AdditionalReviewProbability { get; set; }
        public List<string> Reviewers { get; set; }
        public string NeedAdditionalReviewLabel { get; set; }
        public string NoNeedAdditionalReviewLabel { get; set; }
    }
}
