using System;

namespace GoHireNow.Models.AccountModels
{
    public class WorkerAccountResponse
    {
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public string FullName { get; set; }
        public int? CountryId { get; set; }
        public string CountryName { get; set; }
        public string Email { get; set; }
        public int UserTypeId { get; set; }
        public string UserType { get; set; }
        public string Timezone { get; set; }
        public bool IsDeleted { get; set; }
        public int ScamCount { get; set; }
        public int IsSuspended { get; set; }
        public DateTime? LastReviewDate { get; set; }
    }
}
