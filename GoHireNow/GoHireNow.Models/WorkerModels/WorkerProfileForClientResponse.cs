using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    //TODO: User WorkerSummaryForclientResposne or WorkerProfileForClientResponse model. Remove one of them
    public class WorkerProfileForClientResponse
    {
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public string FullName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? MemberSince { get; set; }
        public string Availability { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public int featured { get; set; }
        public decimal rating { get; set; }
        public int? CountryId { get; set; }
        public string CountryName { get; set; }
        public string ProfilePicturePath { get; set; }
        public string Salary { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public List<SkillResponse> Skills { get; set; }
        public SocialMediaLinksResponse SocialMediaLinks { get; set; }
        public List<PortfolioResponse> Portfolios { get; set; }
        public List<YoutubeResponse> Youtubes { get; set; }
        public bool IsFavorite { get; set; }
        public bool EnableMessage { get; set; }
        public bool IsSecurityChecked { get; set; }
        public int ReferencesCount { get; set; }
        public int IsSuspended { get; set; }
    }
}
