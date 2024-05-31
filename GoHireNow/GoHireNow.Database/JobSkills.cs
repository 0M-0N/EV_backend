using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class JobSkills
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int SkillId { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual Jobs Job { get; set; }
        public virtual GlobalSkills Skill { get; set; }
    }
}
