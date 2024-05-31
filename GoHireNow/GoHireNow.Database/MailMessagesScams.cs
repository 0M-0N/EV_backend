using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class MailMessagesScams
    {
        public int Id { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Message { get; set; }
        public int IsDeleted { get; set; }
    }
}
