using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class Mails
    {
        public Mails()
        {
            MailMessages = new HashSet<MailMessages>();
        }

        public int Id { get; set; }
        public string UserIdFrom { get; set; }
        public string UserIdTo { get; set; }
        public string Title { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string Ipaddress { get; set; }
        public bool? IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? JobId { get; set; }
        public int? IsDirect { get; set; }

        public virtual Jobs Job { get; set; }
        public virtual AspNetUsers UserIdFromNavigation { get; set; }
        public virtual AspNetUsers UserIdToNavigation { get; set; }
        public virtual ICollection<MailMessages> MailMessages { get; set; }
    }
}
