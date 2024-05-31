using GoHireNow.Models.StripeModels;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
  public class WorkerCurrentPlanResponse
  {
    public List<TransactionResponse> Transactions { get; set; }
    public SubscriptionWorkerStatusResponse SubscriptionStatus { get; set; }
    public BillingStatusResponse BillingStatus { get; set; }
  }
}
