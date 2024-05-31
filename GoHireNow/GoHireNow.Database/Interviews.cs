using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class Interviews
    {
        public Interviews()
        {
            InterviewsSchedules = new HashSet<InterviewsSchedules>();
        }

        public int Id { get; set; }
        public string FromId { get; set; }
        public string ToUserId { get; set; }
        public string FromLink { get; set; }
        public string ToLink { get; set; }
        public string FromTimezone { get; set; }
        public decimal FromOffset { get; set; }
        public string ToTimezone { get; set; }
        public decimal ToOffset { get; set; }
        public string BackgroundJobId { get; set; }
        public int Status { get; set; }
        public int MailId { get; set; }
        public DateTime? DateTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public int IsDeleted { get; set; }
        public virtual ICollection<InterviewsSchedules> InterviewsSchedules { get; set; }
    }
}
