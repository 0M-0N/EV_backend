using System;

namespace GoHireNow.Models.CommonModels
{
    public class GlobalPlanDetailResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int JobPosts { get; set; }
        public int ViewApplicants { get; set; }
        public int WorkerApplications { get; set; }
        public int WorkerAIapp { get; set; }
        public int WorkerSkills { get; set; }
        public int AddFavorites { get; set; }
        public int ContactApplicants { get; set; }
        public int Hire { get; set; }
        public int MaxApplicants { get; set; }
        public int MaxDays { get; set; }
        public string AccessId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? MaxContacts { get; set; }
        public int? Dedicated { get; set; }
        public int TotalPostedJobs { get; set; }
        public int TotalUsedContacts { get; set; }
        public DateTime? TransactionCreatedDate { get; set; }
        public DateTime? FreePlanSubscriptionDate { get; set; }
        public int AllowPromotion { get; set; }
        public string BillingName { get; set; }
        public string BillingSpecs { get; set; }
        public bool UpgradeGroup { get; set; }
        public bool WorkerGroup { get; set; }
        public bool PlanGroup { get; set; }
        public bool SecurityGroup { get; set; }
        public bool Renewable { get; set; }
        public decimal ProcessingFeeRate { get; set; }
        public bool IsProcessingFee { get; set; }
    }
}
