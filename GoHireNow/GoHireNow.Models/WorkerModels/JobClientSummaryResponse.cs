using System;

namespace GoHireNow.Models.WorkerModels
{
    public class JobClientSummaryResponse
    {
        public string Id { get; set; }
        public int UserUniqueId { get; set; }
        public string CompanyName { get; set; }
        public string CountryName { get; set; }
        public string Logo { get; set; }
        public DateTime? MemberSince { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool EnableMessage { get; set; }
        public int MailId { get; set; }
        public string ProfilePicturePath { get; set; }
    }

}
