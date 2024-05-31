using GoHireNow.Models.CommonModels;
using GoHireNow.Models.WorkerModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class ClientJobDetailResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int ApplicationCount { get; set; }
        public int AllowedApplicantions { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public int TypeId { get; set; }
        public string Type { get; set; }
        public int SalaryTypeId { get; set; }
        public string SalaryType { get; set; }
        public Decimal Salary { get; set; }
        public List<SkillResponse> Skills { get; set; }
        public List<AttachmentResponse> Attachments { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ActiveDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public int JobContacts { get; set; }
        public int JobContactsMax { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsPrivate { get; set; }
    }
}
