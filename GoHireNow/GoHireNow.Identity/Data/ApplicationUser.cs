using Microsoft.AspNetCore.Identity;
using System;

namespace GoHireNow.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string Introduction { get; set; }
        public string FullName { get; set; }
        public string Company { get; set; }
        public string Description { get; set; }
        public int? CountryId { get; set; }
        public string ProfilePicture { get; set; }
        public int? UserType { get; set; }
        public string CustomerStripeId { get; set; }
        public string UserResume { get; set; }
        public string CompanyLogo { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string WhatWeDo { get; set; }
        public string UserTitle { get; set; }
        public string Linkedin { get; set; }
        public string Skype { get; set; }
        public string Facebook { get; set; }
        public string UserSalary { get; set; }
        public string UserAvailiblity { get; set; }
        public string UserIP { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public DateTime? LastReviewDate { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public string TimeZone { get; set; }
        public int featured { get; set; }
        public int IsSuspended { get; set; }
        public decimal rating { get; set; }
        public int? GlobalPlanId { get; set; }
        public string RefUrl { get; set; }
        public int SmsFactorEnabled { get; set; }
        public bool IsHidden { get; set; }
    }
}
