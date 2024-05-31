using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.JobModels
{
    public class JobSummaryResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public int ApplicationCount { get; set; }
        public int AllowedApplicantions { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public int TypeId { get; set; }
        public string Type { get; set; }
        public int SalaryTypeId { get; set; }
        public string SalaryType { get; set; }
        public Decimal Salary { get; set; }
        //TODO: Do we have to add skills here?
        public List<SkillResponse> Skills { get; set; }
        public List<string> appliedUsers { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ActiveDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsPrivate { get; set; }
        public int InvitesCount { get; set; }

        //Is liked (by worker)
        //Is applied (by worker)
    }
}
