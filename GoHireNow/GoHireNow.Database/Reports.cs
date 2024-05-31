using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class Reports
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int? ReportTypeId { get; set; }
        public int CustomId { get; set; }
        public int AcceptReport { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsDeleted { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
