using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class GlobalSkills
    {
        public GlobalSkills()
        {
            JobSkills = new HashSet<JobSkills>();
            UserSkills = new HashSet<UserSkills>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<JobSkills> JobSkills { get; set; }
        public virtual ICollection<UserSkills> UserSkills { get; set; }
    }
}
