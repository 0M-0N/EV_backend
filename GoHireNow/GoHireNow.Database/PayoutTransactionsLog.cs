using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class PayoutTransactionsLog
    {
        public int id { get; set; }
        public int? event_id { get; set; }
        public int? profile_id { get; set; }
        public int? account_id { get; set; }
        public string type { get; set; }
        public string state { get; set; }
        public DateTime? eventDate { get; set; }
        public DateTime? createddate { get; set; }
    }
}
