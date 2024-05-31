using System;

namespace GoHireNow.Database
{
    public partial class JobInvites
    {
        public int Id { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public int JobId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int IsDeleted { get; set; }
    }
}
