using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class JobDetailForWorkerResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public string Type { get; set; }
        public int JobTypeId { get; set; }
        public string Salary { get; set; }
        public int SalaryTypeId { get; set; }
        public string SalaryType { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string ProfilePicturePath { get; set; }
        public DateTime CreateDate { get; set; }
        public List<string> Skills { get; set; }
        public List<SkillResponse> JobSkills { get; set; }
        public JobClientSummaryResponse Client { get; set; }
        public List<AttachmentResponse> Attachments { get; set; }
        public List<JobSummaryForWorkerResponse> OtherJobsByClient { get; set; }
        public List<JobSummaryForWorkerResponse> SimilarJobs { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsApplied { get; set; }
        public bool IsFavorite { get; set; }
    }
}
