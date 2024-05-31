using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class WorkerDashboardResponse
    {
        public List<JobSummaryForWorkerResponse> MatchingJobs { get; set; }
        public List<JobSummaryForWorkerResponse> AppliedJobs { get; set; }
        public List<JobSummaryForWorkerResponse> LatestJobs { get; set; }
    }
}
