using GoHireNow.Models.CommonModels;
using GoHireNow.Models.WorkerModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.JobsModels
{
    public class JobTitleRelatedGlobalJobsListModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public int TypeId { get; set; }
        public string Type { get; set; }
        public int SalaryTypeId { get; set; }
        public string SalaryType { get; set; }
        public decimal Salary { get; set; }

        public int Duration { get; set; }

        public string Description { get; set; }
        public DateTime? ActiveDate { get; set; }
        public List<SkillResponse> Skills { get; set; }
        public string ProfilePicturePath { get; set; }
        public JobClientSummaryResponse Client { get; set; }
    }
}
