using System;
using System.ComponentModel.DataAnnotations;

namespace GoHireNow.Database.GoHireNowTools.Models
{
    public partial class mailer_sender
    {
        [Key]
        public int ms_id { get; set; }
        public DateTime? ms_date { get; set; }
        public int? ms_custom_id { get; set; }
        public Byte? ms_issent { get; set; }
        public int? ms_custom_type { get; set; }
        public DateTime? ms_send_date { get; set; }
        public DateTime? ms_sent_date { get; set; }
        public Guid? ms_unsubscribe { get; set; }
        public string ms_email { get; set; }
        public string ms_name { get; set; }
        public string ms_subject { get; set; }
        public string ms_message { get; set; }
        public string ms_from_email { get; set; }
        public string ms_from_name { get; set; }
        public int? ms_priority { get; set; }
    }
}
