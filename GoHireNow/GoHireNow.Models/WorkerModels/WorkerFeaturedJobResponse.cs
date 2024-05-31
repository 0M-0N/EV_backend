using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class WorkerFeaturedJobResponse
    {
        public WorkerFeaturedJobResponse()
        {
            FeaturedJobs = new List<JobSummaryForWorkerResponse>();
        }
        public List<JobSummaryForWorkerResponse> FeaturedJobs { get; set; }
    }
}
