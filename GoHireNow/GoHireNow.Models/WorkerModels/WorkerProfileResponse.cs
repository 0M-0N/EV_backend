using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class WorkerProfileResponse
    {
        public string UserId { get; set; }
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
        public string Timezone { get; set; }
        public string ProfilePicturePath { get; set; }
        public string Salary { get; set; }
        public int ReferencesCount { get; set; }
        public bool IsHidden { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public List<SkillResponse> Skills { get; set; }
        public SocialMediaLinksResponse SocialMediaLinks { get; set; }
        public List<PortfolioResponse> Portfolios { get; set; }
        public List<YoutubeResponse> Youtubes { get; set; }
    }
}
