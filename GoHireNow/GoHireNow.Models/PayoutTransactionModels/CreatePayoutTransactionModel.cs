using System;

namespace GoHireNow.Models.PayoutTransactionModels
{
    public class CreatePayoutTransactionModel
    {
        public decimal amountUSD { get; set; }
        public decimal exchangeRate { get; set; }
        public decimal amount { get; set; }
        public decimal fee { get; set; }
        public string currency { get; set; }
        public string transactionId { get; set; }
        public int payoutMethod { get; set; }
        public DateTime? arrivingBy { get; set; }
    }
}
