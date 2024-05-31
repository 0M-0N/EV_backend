using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserExperiences
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string CompanyName { get; set; }
        public string JobDescription { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool StillWorking { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
