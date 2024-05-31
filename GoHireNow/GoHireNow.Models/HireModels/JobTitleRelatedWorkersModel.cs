
using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.HireModels
{
    public class JobTitleRelatedWorkersModel
    {
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int? CountryId { get; set; }
        //TODO: conver to decimal here and in database
        public string Salary { get; set; }
        public int? SalaryTypeId { get; set; }
        public int? UserAvailiblity { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public List<SkillResponse> Skills { get; set; }
        public string CountryName { get; set; }
        public string ProfilePicturePath { get; set; }
        public string UserSkills { get; set; }
    }
}
