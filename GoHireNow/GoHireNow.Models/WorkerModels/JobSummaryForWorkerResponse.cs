using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class JobSummaryForWorkerResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        //public int Applicants { get; set; }
        //public int AllowedApplicants { get; set; }
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
        public bool IsFeatured { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsPrivate { get; set; }
        //TODO: Add is favorite, so user can unfav from list

    }
}
