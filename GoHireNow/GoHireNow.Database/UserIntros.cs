using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class UserIntros
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string name { get; set; }
        public string text { get; set; }
        public DateTime CreatedDate { get; set; }
        public int IsDeleted { get; set; }
    }
}
