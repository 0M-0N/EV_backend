using GoHireNow.Models.CommonModels;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    public class ClientFavoritesResponse
    {
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public string Name { get; set; }
        public List<SkillResponse> Skills { get; set; }
        public string Country { get; set; }
        public int? CountryId { get; set; }
        public string Title { get; set; }
        public string LastLoginDate { get; set; }
        public string Salary { get; set; }
        public string ProfilePicturePath { get; set; }
        public string CreatedDate { get; set; }
    }
}
