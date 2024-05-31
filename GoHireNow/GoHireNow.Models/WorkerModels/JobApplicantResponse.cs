using GoHireNow.Models.CommonModels;
using System;
using System.Collections.Generic;

namespace GoHireNow.Models.WorkerModels
{
    public class JobApplicantResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }

        //TODO Delete string Skills and fix issues
        public string Skills { get; set; }
        public List<SkillResponse> UserSkills { get; set; }
        public string CoverLetter { get; set; }
        public string Portfolios { get; set; }
        public string Rate { get; set; }
        public string CountryName { get; set; }
        public DateTime? LastLoginDateTime { get; set; }
        public DateTime? CreateDate { get; set; }
        public string ProfilePicturePath { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public decimal Rating { get; set; }
        public int? ApplicationRating { get; set; }
        public int Featured { get; set; }
        public int ReferencesCount { get; set; }
        public bool IsDeleted { get; set; }
        public bool? IsActive { get; set; }
    }
}
