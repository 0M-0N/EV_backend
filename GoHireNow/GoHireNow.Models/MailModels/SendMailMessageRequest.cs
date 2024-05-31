using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class SendMailMessageRequest
    {
        public string fromUserId { get; set; }
        public string toUserId { get; set; }
        public int mailId { get; set; }
        public string message { get; set; }
        public int JobId { get; set; }
        public int? customId { get; set; }
        public int? customIdType { get; set; }
        public string customLink { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
