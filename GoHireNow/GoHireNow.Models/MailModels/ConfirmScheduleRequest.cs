using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class ConfirmScheduleRequest
    {
        public string datetime { get; set; }
        public string timezone { get; set; }
        public int interviewId { get; set; }
        public int messageId { get; set; }
        public decimal offset { get; set; }
    }
}
