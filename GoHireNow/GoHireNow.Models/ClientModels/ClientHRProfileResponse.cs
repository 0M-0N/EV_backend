using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class ClientHRProfileResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public string CountryName { get; set; }
        public string HRTitle1 { get; set; }
        public string HRTitle2 { get; set; }
        public string HRTitle3 { get; set; }
        public string HRDesc1 { get; set; }
        public string HRDesc2 { get; set; }
        public string HRDesc3 { get; set; }
        public string HRPosition { get; set; }
        public decimal HRPrice { get; set; }
        public string HRVideo { get; set; }
        public int HRWpm { get; set; }
        public int HRMbps { get; set; }
        public string HRNotes { get; set; }
        public int HRHours { get; set; }
        public string HRType { get; set; }
        public List<HRSkillResponse> HRSkills { get; set; }
        public List<HRLanguageResponse> HRLanguages { get; set; }
        public List<HRReviewResponse> HRReviews { get; set; }
        public List<HRProfilesResponse> HRProfiles { get; set; }
    }
}
