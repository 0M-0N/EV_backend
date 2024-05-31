using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class FavoriteWorkers
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string WorkerId { get; set; }
        public int Type { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }

        public virtual AspNetUsers User { get; set; }
        public virtual AspNetUsers Worker { get; set; }
    }
}
