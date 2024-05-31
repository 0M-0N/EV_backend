using GoHireNow.Models.JobModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class ClientProfileForWorkerResponse
    {
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public string CompanyName { get; set; }
        public string Introduction { get; set; }
        public string Description { get; set; }
        public DateTime? MemberSince { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public int? CountryId { get; set; }
        public string CountryName { get; set; }
        public string ProfilePicturePath { get; set; }
        public List<JobSummaryResponse> ActiveJobs { get; set; }
        public bool EnableMessage { get; set; }
        public int IsSuspended { get; set; }
    }
}
