using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserResumes
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string ResumeFile { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsDeleted { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
