using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class CompanyInvites
    {
        public int Id { get; set; }
        public string CompanyId { get; set; }
        public string InviteeName { get; set; }
        public string InviteeEmail { get; set; }
        public int Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public int IsDeleted { get; set; }
    }
}
