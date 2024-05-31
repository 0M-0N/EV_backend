using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class MailMessages
    {
        public int Id { get; set; }
        public int MailId { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsRead { get; set; }
        public string Message { get; set; }
        public int? JobId { get; set; }
        public int? CustomId { get; set; }
        public int? CustomIdType { get; set; }
        public string CustomLink { get; set; }
        public string Ipaddress { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public virtual AspNetUsers FromUser { get; set; }
        public virtual Jobs Job { get; set; }
        public virtual Mails Mail { get; set; }
        public virtual AspNetUsers ToUser { get; set; }
    }
}
