using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserReferences
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public Guid InviteID { get; set; }
        public string JobTitle { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Company { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Picture { get; set; }
        public string FeedBack { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Rating { get; set; }
        public int IsAccepted { get; set; }
        public int IsByInvitation { get; set; }
        public int IsInvited { get; set; }
        public int IsDeleted { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
