using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.StripeModels
{
    public class CreateCardRequest
    {
        public string userEmail { get; set; }
        public string stripeToken { get; set; }
        public string cardNumber { get; set; }
        public string cardCVC { get; set; }
        public int? cardExpMonth { get; set; }
        public int? cardExpYear { get; set; }
    }
}
