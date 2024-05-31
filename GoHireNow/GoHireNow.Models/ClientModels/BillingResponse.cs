using System;

namespace GoHireNow.Models.ClientModels
{
    public class BillingResponse
    {
        public int TransactionId { get; set; }
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public string TransactionStatus { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
