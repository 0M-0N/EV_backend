using GoHireNow.Models.CommonModels;
using System;

namespace GoHireNow.Models.HomeModels
{
    public class ProfileResponse
    {
        public object introduction { get; set; }
        public string fullName { get; set; }
        public object company { get; set; }
        public object description { get; set; }
        public int countryId { get; set; }
        public string CountryName { get; set; }
        public object profilePicture { get; set; }
        public int userType { get; set; }
        public object customerStripeId { get; set; }
        public object userResume { get; set; }
        public object companyLogo { get; set; }
        public object createdDate { get; set; }
        public object modifiedDate { get; set; }
        public object whatWeDo { get; set; }
        public object userTitle { get; set; }
        public object linkedin { get; set; }
        public object skype { get; set; }
        public object facebook { get; set; }
        public object userSalary { get; set; }
        public object userAvailiblity { get; set; }
        public object lastLoginTime { get; set; }
        public DateTime registrationDate { get; set; }
        public string id { get; set; }
        public string userName { get; set; }
        public string email { get; set; }

        public object phoneNumber { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public int featured { get; set; }
        public decimal rating { get; set; }
        public SkillResponse[] Skills { get; set; }

    }
}
