using System;

namespace GoHireNow.Models.AccountModels
{
    public class ClientAccountResponse
    {
        public string UserId { get; set; }
        public string CompanyName { get; set; }
        public int? CountryId { get; set; }
        public string CountryName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public int UserTypeId { get; set; }
        public string UserType { get; set; }
        public string Timezone { get; set; }
        public bool IsDeleted { get; set; }
        public int SmsFactorEnabled { get; set; }
        public int ScamCount { get; set; }
        public int IsSuspended { get; set; }
        public DateTime? LastReviewDate { get; set; }
    }
}
