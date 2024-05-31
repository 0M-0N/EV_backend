using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserSecurityCheck
    {
        public int Id { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool isDeleted { get; set; }
    }
}
