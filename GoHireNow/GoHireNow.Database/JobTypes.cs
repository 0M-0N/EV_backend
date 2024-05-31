using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class JobTypes
    {
        public JobTypes()
        {
            Jobs = new HashSet<Jobs>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Jobs> Jobs { get; set; }
    }
}
