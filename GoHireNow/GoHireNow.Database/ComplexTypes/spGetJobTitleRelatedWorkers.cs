using System;
using System.Collections.Generic;
using System.Text;

namespace GoHireNow.Database.ComplexTypes
{
    public class spGetJobTitleRelatedWorkers
    {
        public string JobDescripton { get; set; }
        public string JobTitle { get; set; }
        public string SEOText { get; set; }
        public string Video  { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public int? CountryId { get; set; }
        public string Id { get; set; }
        public int UserUniqueId { get; set; }
        public string FullName { get; set; }
        public string UserSalary { get; set; }
        public string UserTitle { get; set; }
        public string UserAvailiblity { get; set; }
        public string ProfilePicture { get; set; }
        public string UserSkills { get; set; }
        public string JobBigTitle { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public int featured { get; set; }
        public decimal rating { get; set; }
        public int TotalWorkers { get; set; }
    }

}
