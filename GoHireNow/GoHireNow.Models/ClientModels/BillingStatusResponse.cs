using System;

namespace GoHireNow.Models.ClientModels
{
    public class BillingStatusResponse
    {
        public decimal? PlanPrice { get; set; }
        public DateTime NextBillingDate { get; set; }
    }
}
