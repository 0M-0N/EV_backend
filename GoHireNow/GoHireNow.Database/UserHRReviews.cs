using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserHRReviews
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int HRStars { get; set; }
        public string HRComment { get; set; }
        public string HRName { get; set; }
        public string HRCompany { get; set; }
    }
}
