using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class SendMessageRequest
    {
        [Required]
        public string fromUserId { get; set; }
        [Required]
        public string toUserId { get; set; }
        [Required]
        public string message { get; set; }
        public string customLink { get; set; }
        public int? jobId { get; set; }
        public int? customId { get; set; }
        public int? customIdType { get; set; }
        public bool isDirectMessage { get; set; }
    }
}
