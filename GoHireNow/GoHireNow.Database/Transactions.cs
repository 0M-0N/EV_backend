using System;

namespace GoHireNow.Database
{
    public partial class Transactions
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Receipt { get; set; }
        public int GlobalPlanId { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountBalance { get; set; }
        public int AmountBalanceId { get; set; }
        public string CardName { get; set; }
        public string ReceiptId { get; set; }
        public string Status { get; set; }
        public string ChargeId { get; set; }
        public string RefundId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? CustomType { get; set; }
        public int? CustomId { get; set; }
        public virtual TransactionsType TransactionsType { get; set; }
        public virtual GlobalPlans GlobalPlan { get; set; }
        public virtual AspNetUsers User { get; set; }
    }
}
