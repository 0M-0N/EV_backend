using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class JobApplications
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int JobId { get; set; }
        public string Intro { get; set; }
        public string Resume { get; set; }
        public string CoverLetter { get; set; }
        public decimal? Salary { get; set; }
        public int? SalaryTypeId { get; set; }
        public bool? IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? Rating { get; set; }

        public virtual Jobs Job { get; set; }
        public virtual SalaryTypes SalaryType { get; set; }
        public virtual AspNetUsers User { get; set; }
    }
}
