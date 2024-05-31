using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class StartNowRequest
    {
        public string fromId { get; set; }
        public string toUserId { get; set; }
        public string fromLink { get; set; }
        public string toLink { get; set; }
        public int mailId { get; set; }
        public DateTimeZone[] dateTime { get; set; }
    }

    public class DateTimeZone
    {
        public string datetime { get; set; }
        public string timezone { get; set; }
        public decimal offset { get; set; }
    }
}
