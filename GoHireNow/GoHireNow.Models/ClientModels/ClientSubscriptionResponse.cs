using GoHireNow.Models.StripeModels;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class ClientCurrentPlanResponse
    {
        public List<TransactionResponse> Transactions { get; set; }
        public SubscriptionStatusResponse SubscriptionStatus { get; set; }
        public BillingStatusResponse BillingStatus { get; set; }
    }
}
