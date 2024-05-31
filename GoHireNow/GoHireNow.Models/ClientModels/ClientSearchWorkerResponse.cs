using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    //TODO: User WorkerSummaryForclientResposne or WorkerProfileForClientResponse model. Remove one of them
    public class ClientSearchWorkerResponse
    {
        public ClientSearchWorkerResponse()
        {
            Workers = new List<WorkerSummaryForClientResponse>();
        }
        public int TotalWorkers { get; set; }
        public List<WorkerSummaryForClientResponse> Workers { get; set; }
    }
}
