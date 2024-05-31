namespace GoHireNow.Models.ClientModels
{
    public class SubscriptionStatusResponse
    {
        public int PostedJobs { get; set; }
        public int AllowedJobs { get; set; }
        public int CurrentContacts { get; set; }
        public int AllowedContacts { get; set; }
        public int MaxApplicantsPerJob { get; set; }
        public int Id { get; set; }
        public string PlanName { get; set; }
        public int AllowPromotion { get; set; }
        
    }
}
