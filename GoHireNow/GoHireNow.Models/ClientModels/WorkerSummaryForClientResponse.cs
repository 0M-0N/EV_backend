using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.ClientModels
{
    //TODO: User WorkerSummaryForclientResposne or WorkerProfileForClientResponse model. Remove one of them
    public class WorkerSummaryForClientResponse
    {
        public string UserId { get; set; }
        public int UserUniqueId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int? CountryId { get; set; }
        public string CountryName { get; set; }
        public List<SkillResponse> Skills { get; set; }
        //TODO: conver to decimal here and in database
        public string Salary { get; set; }
        public int? SalaryTypeId { get; set; }
        public string Availability { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string ProfilePicturePath { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public int featured { get; set; }
        public decimal rating { get; set; }
        public int ReferencesCount { get; set; }
    }
}
