using GoHireNow.Models.JobModels;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class ClientDashboardResponse
    {
        public List<JobSummaryResponse> ActiveJobs { get; set; }
        public List<WorkerSummaryForClientResponse> FavoriteWorkers { get; set; }
        public List<WorkerSummaryForClientResponse> RelatedWorkers { get; set; }
    }
}
