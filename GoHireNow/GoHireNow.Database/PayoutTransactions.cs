using System;

namespace GoHireNow.Database
{
    public partial class PayoutTransactions
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal AmountUSD { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal Amount { get; set; }
        public decimal FFees { get; set; }
        public string Currency { get; set; }
        public DateTime? ArrivingBy { get; set; }
        public int? IsApproved { get; set; }
        public int IsPaid { get; set; }
        public int IsDeposited { get; set; }
        public int DepositedMethod { get; set; }
        public int PayoutMethod { get; set; }
        public string DepositedReceipt { get; set; }
        public DateTime? DepositedDatetime { get; set; }
        public bool IsDeleted { get; set; }
        public int StatusId { get; set; }
    }
}
