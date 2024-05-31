using System;

namespace GoHireNow.Database
{
    public partial class UserReports
    {
        public int Id { get; set; }
        public int CustomTypeId { get; set; }
        public int CustomId { get; set; }
        public string Reason { get; set; }
        public string UserId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int IsDeleted { get; set; }
    }
}
