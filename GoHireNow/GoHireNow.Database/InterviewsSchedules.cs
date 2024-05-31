using System;

namespace GoHireNow.Database
{
    public partial class InterviewsSchedules
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string Timezone { get; set; }
        public DateTime DatesTimes { get; set; }
        public DateTime CreatedDate { get; set; }
        public int IsDeleted { get; set; }
        public virtual Interviews Interview { get; set; }
    }
}
