using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class Jobs
    {
        public Jobs()
        {
            FavoriteJobs = new HashSet<FavoriteJobs>();
            JobApplications = new HashSet<JobApplications>();
            JobAttachments = new HashSet<JobAttachments>();
            JobSkills = new HashSet<JobSkills>();
            MailMessages = new HashSet<MailMessages>();
            Mails = new HashSet<Mails>();
        }

        public int Id { get; set; }
        public string UserId { get; set; }
        public int JobTypeId { get; set; }
        public int Duration { get; set; }
        public decimal Salary { get; set; }
        public int SalaryTypeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool? IsActive { get; set; }
        public int JobStatusId { get; set; }
        public int? IsDashboard { get; set; }
        public int? IsEmail { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ActiveDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public virtual JobStatuses JobStatus { get; set; }
        public virtual JobTypes JobType { get; set; }
        public virtual AspNetUsers User { get; set; }
        public virtual ICollection<FavoriteJobs> FavoriteJobs { get; set; }
        public virtual ICollection<JobApplications> JobApplications { get; set; }
        public virtual ICollection<JobAttachments> JobAttachments { get; set; }
        public virtual ICollection<JobSkills> JobSkills { get; set; }
        public virtual ICollection<MailMessages> MailMessages { get; set; }
        public virtual ICollection<Mails> Mails { get; set; }
    }
}
