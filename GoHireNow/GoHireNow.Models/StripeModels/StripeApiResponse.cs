using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GoHireNow.Models.StripeModels
{
    public class StripeApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public int ObjectId { get; set; }
        public bool IsUpdated { get; set; }
        public List<object> Result { get; set; }
    }
}
