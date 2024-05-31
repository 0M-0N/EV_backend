using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class PayoutRecipients
    {
        public int id { get; set; }
        public string userId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int statusId { get; set; }
        public string currency { get; set; }
        public string ispersonal { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public string country { get; set; }
        public string WiseCustomerId { get; set; }
        public int? autodeposit { get; set; }
        public int isdeleted { get; set; }
    }
}