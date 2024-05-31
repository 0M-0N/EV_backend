using System;

namespace GoHireNow.Models.StripeModels
{
    public class TransactionResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Receipt { get; set; }
        public int GlobalPlanId { get; set; }
        public string GlobalPlanName { get; set; }
        public decimal Amount { get; set; }
        public string CardName { get; set; }
        public string ReceiptId { get; set; }
        public string Status { get; set; }
        public int? CustomId { get; set; }
        public int? CustomType { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
