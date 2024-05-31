using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class CompanyBalance
    {
        public int Id { get; set; }
        public string CompanyId { get; set; }
        public Decimal Amount { get; set; }
        public int Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public int IsDeleted { get; set; }
    }
}
