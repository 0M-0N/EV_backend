using GoHireNow.Models.ClientModels;
using System.Collections.Generic;

namespace GoHireNow.Models.HireModels
{
    public class WorkerSummaryForHireResponse
    {
        public WorkerSummaryForHireResponse()
        {
            Workers = new List<WorkerSummaryForClientResponse>();
        }
        public int TotalWorkers { get; set; }
        public string JobDescripton { get; set; }
        public string JobTitle { get; set; }
        public string JobBigTitle { get; set; }
        public string SEOText { get; set; }
        public string Video { get; set; }

        public List<WorkerSummaryForClientResponse> Workers { get; set; }
    }
}
