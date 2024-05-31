using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class JobStatuses
    {
        public JobStatuses()
        {
            Jobs = new HashSet<Jobs>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual ICollection<Jobs> Jobs { get; set; }
    }
}
