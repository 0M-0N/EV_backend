using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class SalaryTypes
    {
        public SalaryTypes()
        {
            JobApplications = new HashSet<JobApplications>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual ICollection<JobApplications> JobApplications { get; set; }
    }
}
