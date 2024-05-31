using GoHireNow.Models.CommonModels;
using GoHireNow.Models.WorkerModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.JobsModels
{
    public class JobTitleRelatedGlobalJobsModel
    {
        public JobTitleRelatedGlobalJobsModel()
        {
            Jobs = new List<JobTitleRelatedGlobalJobsListModel>();
        }
        public string JobDescripton { get; set; }
        public string JobTitle { get; set; }
        public string JobBigTitle { get; set; }

        public List<JobTitleRelatedGlobalJobsListModel> Jobs { get; set; }
       
    }
}
