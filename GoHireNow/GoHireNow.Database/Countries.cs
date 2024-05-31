using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class Countries
    {
        public Countries()
        {
            AspNetUsers = new HashSet<AspNetUsers>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public virtual ICollection<AspNetUsers> AspNetUsers { get; set; }
    }
}
