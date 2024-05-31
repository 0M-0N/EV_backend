using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Database
{
    public partial class EmailsUnsubscribe
    {

        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int EmailType { get; set; }

    }
}
