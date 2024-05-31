using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserPortfolios
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
