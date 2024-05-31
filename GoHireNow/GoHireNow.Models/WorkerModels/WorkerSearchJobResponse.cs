using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class WorkerSearchJobResponse
    {
        public WorkerSearchJobResponse()
        {
            Jobs = new List<JobSummaryForWorkerResponse>();
        }
        public int TotalJobs { get; set; }
        public List<JobSummaryForWorkerResponse> Jobs { get; set; }
    }
}
