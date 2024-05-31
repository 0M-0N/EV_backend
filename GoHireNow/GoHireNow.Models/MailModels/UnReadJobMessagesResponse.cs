using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class UnReadJobMessagesResponse
    {
        public string SenderName { get; set; }
        public string JobTitle { get; set; }
        public string MessageDateTime { get; set; }
    }
}
