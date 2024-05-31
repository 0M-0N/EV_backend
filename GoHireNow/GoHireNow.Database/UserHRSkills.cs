using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserHRSkills
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int HRSkillId { get; set; }
        public int HRRate { get; set; }
    }
}
