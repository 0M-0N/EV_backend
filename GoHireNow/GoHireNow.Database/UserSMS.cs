using System;

namespace GoHireNow.Database
{
    public partial class UserSMS
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Area { get; set; }
        public string Number { get; set; }
        public int TempCode { get; set; }
        public int IsDeleted { get; set; }
        public DateTime? LastVerifiedDate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
