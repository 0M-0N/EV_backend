using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class MailResponse
    {
        public int Id { get; set; }
        public string UserIdFrom { get; set; }
        public string UserIdTo { get; set; }
        public string Title { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreateDate { get; set; }
        public string Ipaddress { get; set; }
        
    }
}
