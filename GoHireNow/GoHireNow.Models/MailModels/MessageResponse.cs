using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GoHireNow.Models.MailModels
{
    public class MessageResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public int ObjectId { get; set; }
        public bool IsUpdated { get; set; }
        public List<object> Result { get; set; }
    }
}
