using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserLogins
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
