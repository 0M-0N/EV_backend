using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserSkills
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int SkillId { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsDeleted { get; set; }

        public virtual GlobalSkills Skill { get; set; }
        public virtual AspNetUsers User { get; set; }
    }
}
