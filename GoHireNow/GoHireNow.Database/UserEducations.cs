using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserEducations
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string InstitutionName { get; set; }
        public string DegreeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool StillStudying { get; set; }

        public virtual AspNetUsers User { get; set; }
    }
}
