using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spGetCurrentPricingPlan
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int JobPosts { get; set; }
        public int ViewApplicants { get; set; }
        public int AddFavorites { get; set; }
        public int ContactApplicants { get; set; }
        public int Hire { get; set; }
        public int MaxApplicants { get; set; }
        public int MaxDays { get; set; }
        public string AccessId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int MaxContacts { get; set; }
        public int Dedicated { get; set; }
        public int TotalDaysLeft { get; set; }
        public int TotalPostedJobs { get; set; }
        public int TotalUsedContacts { get; set; }
        public DateTime? TransactionCreatedDate { get; set; }
        public DateTime? FreePlanSubscriptionDate { get; set; }
        public int AllowPromotion { get; set; }
        
    }
}
