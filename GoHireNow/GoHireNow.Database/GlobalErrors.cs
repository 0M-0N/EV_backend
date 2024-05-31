using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class GlobalErrors
    {
        public int Id { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorUrl { get; set; }
        public string UserId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
