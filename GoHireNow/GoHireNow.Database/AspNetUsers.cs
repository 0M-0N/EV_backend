using System;
using System.Collections.Generic;

namespace GoHireNow.Database
{
    public partial class AspNetUsers
    {
        public AspNetUsers()
        {
            AspNetUserClaims = new HashSet<AspNetUserClaims>();
            AspNetUserLogins = new HashSet<AspNetUserLogins>();
            AspNetUserRoles = new HashSet<AspNetUserRoles>();
            AspNetUserTokens = new HashSet<AspNetUserTokens>();
            FavoriteJobs = new HashSet<FavoriteJobs>();
            FavoriteWorkersUser = new HashSet<FavoriteWorkers>();
            FavoriteWorkersWorker = new HashSet<FavoriteWorkers>();
            JobApplications = new HashSet<JobApplications>();
            Jobs = new HashSet<Jobs>();
            Contracts = new HashSet<Contracts>();
            MailMessagesFromUser = new HashSet<MailMessages>();
            MailMessagesToUser = new HashSet<MailMessages>();
            MailsUserIdFromNavigation = new HashSet<Mails>();
            MailsUserIdToNavigation = new HashSet<Mails>();
            Reports = new HashSet<Reports>();
            Transactions = new HashSet<Transactions>();
            UserAttachments = new HashSet<UserAttachments>();
            UserEducations = new HashSet<UserEducations>();
            UserExperiences = new HashSet<UserExperiences>();
            UserLogins = new HashSet<UserLogins>();
            UserPictures = new HashSet<UserPictures>();
            UserPortfolios = new HashSet<UserPortfolios>();
            UserReferences = new HashSet<UserReferences>();
            UserYoutubes = new HashSet<UserYoutubes>();
            UserResumes = new HashSet<UserResumes>();
            UserSkills = new HashSet<UserSkills>();
        }

        public string Id { get; set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string Company { get; set; }
        public string CompanyLogo { get; set; }
        public int? CountryId { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CustomerStripeId { get; set; }
        public string Description { get; set; }
        public string Facebook { get; set; }
        public string FullName { get; set; }
        public string Introduction { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public string Linkedin { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string Skype { get; set; }
        public string UserAvailiblity { get; set; }
        public string UserIp { get; set; }
        public string UserResume { get; set; }
        public string UserSalary { get; set; }
        public string UserTitle { get; set; }
        public int? UserType { get; set; }
        public string WhatWeDo { get; set; }
        public string Education { get; set; }
        public string Experience { get; set; }
        public string TimeZone { get; set; }
        public decimal Rating { get; set; }
        public int Featured { get; set; }
        public int? GlobalPlanId { get; set; }
        public int UserUniqueId { get; set; }
        public int IsSuspended { get; set; }
        public int SmsFactorEnabled { get; set; }
        public bool IsHidden { get; set; }
        public DateTime? FreePlanSubscriptionDate { get; set; }
        public DateTime? LastReviewDate { get; set; }
        public virtual Countries Country { get; set; }
        public virtual UserHRProfile UserHRProfile { get; set; }
        public virtual ICollection<AspNetUserClaims> AspNetUserClaims { get; set; }
        public virtual ICollection<AspNetUserLogins> AspNetUserLogins { get; set; }
        public virtual ICollection<AspNetUserRoles> AspNetUserRoles { get; set; }
        public virtual ICollection<AspNetUserTokens> AspNetUserTokens { get; set; }
        public virtual ICollection<FavoriteJobs> FavoriteJobs { get; set; }
        public virtual ICollection<FavoriteWorkers> FavoriteWorkersUser { get; set; }
        public virtual ICollection<FavoriteWorkers> FavoriteWorkersWorker { get; set; }
        public virtual ICollection<JobApplications> JobApplications { get; set; }
        public virtual ICollection<Jobs> Jobs { get; set; }
        public virtual ICollection<Contracts> Contracts { get; set; }
        public virtual ICollection<MailMessages> MailMessagesFromUser { get; set; }
        public virtual ICollection<MailMessages> MailMessagesToUser { get; set; }
        public virtual ICollection<Mails> MailsUserIdFromNavigation { get; set; }
        public virtual ICollection<Mails> MailsUserIdToNavigation { get; set; }
        public virtual ICollection<Reports> Reports { get; set; }
        public virtual ICollection<Transactions> Transactions { get; set; }
        public virtual ICollection<UserAttachments> UserAttachments { get; set; }
        public virtual ICollection<UserEducations> UserEducations { get; set; }
        public virtual ICollection<UserExperiences> UserExperiences { get; set; }
        public virtual ICollection<UserLogins> UserLogins { get; set; }
        public virtual ICollection<UserPictures> UserPictures { get; set; }
        public virtual ICollection<UserPortfolios> UserPortfolios { get; set; }
        public virtual ICollection<UserReferences> UserReferences { get; set; }
        public virtual ICollection<UserYoutubes> UserYoutubes { get; set; }
        public virtual ICollection<UserResumes> UserResumes { get; set; }
        public virtual ICollection<UserSkills> UserSkills { get; set; }
    }
}
