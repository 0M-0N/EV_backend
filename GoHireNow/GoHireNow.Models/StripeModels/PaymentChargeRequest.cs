using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Models.StripeModels
{
    public class PaymentChargeRequest
    {
        public string userEmail { get; set; }
        public string stripeToken { get; set; }
        public string planId { get; set; }
        public string cardId { get; set; }
        public string cardName { get; set; }
        public string cardNumber { get; set; }
        public bool isNewCard { get; set; }
        public string cardCVC { get; set; }
        public string customData { get; set; }
        public string customType { get; set; }
        public int amount { get; set; }
        public int balance { get; set; }
        public bool byCard { get; set; }
        public int paymentType { get; set; }
        public int? cardExpMonth { get; set; }
        public int? cardExpYear { get; set; }
    }
}
