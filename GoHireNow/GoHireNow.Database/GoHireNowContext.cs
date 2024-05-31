using System;
using System.Collections.Generic;
using GoHireNow.Database.ComplexTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GoHireNow.Database
{
    public partial class GoHireNowContext : DbContext
    {
        public GoHireNowContext()
        {
        }

        public GoHireNowContext(DbContextOptions<GoHireNowContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AspNetRoleClaims> AspNetRoleClaims { get; set; }
        public virtual DbSet<AspNetRoles> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaims> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogins> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUserRoles> AspNetUserRoles { get; set; }
        public virtual DbSet<AspNetUserTokens> AspNetUserTokens { get; set; }
        public virtual DbSet<AspNetUsers> AspNetUsers { get; set; }
        public virtual DbSet<AttachmentTypes> AttachmentTypes { get; set; }
        public virtual DbSet<Countries> Countries { get; set; }
        public virtual DbSet<FavoriteJobs> FavoriteJobs { get; set; }
        public virtual DbSet<FavoriteWorkers> FavoriteWorkers { get; set; }
        public virtual DbSet<GlobalErrors> GlobalErrors { get; set; }
        public virtual DbSet<GlobalPlans> GlobalPlans { get; set; }
        public virtual DbSet<TransactionsType> TransactionsType { get; set; }
        public virtual DbSet<GlobalSkills> GlobalSkills { get; set; }
        public virtual DbSet<GlobalJobTitles> GlobalJobTitles { get; set; }
        public virtual DbSet<GlobalJobTitlesSkills> GlobalJobTitlesSkills { get; set; }
        public virtual DbSet<Ipaddresses> Ipaddresses { get; set; }
        public virtual DbSet<JobApplications> JobApplications { get; set; }
        public virtual DbSet<JobAttachments> JobAttachments { get; set; }
        public virtual DbSet<JobSkills> JobSkills { get; set; }
        public virtual DbSet<JobStatuses> JobStatuses { get; set; }
        public virtual DbSet<JobTypes> JobTypes { get; set; }
        public virtual DbSet<Jobs> Jobs { get; set; }
        public virtual DbSet<MailMessages> MailMessages { get; set; }
        public virtual DbSet<Mails> Mails { get; set; }
        public virtual DbSet<Interviews> Interviews { get; set; }
        public virtual DbSet<InterviewsSchedules> InterviewsSchedules { get; set; }
        public virtual DbSet<Reports> Reports { get; set; }
        public virtual DbSet<ContractsDisputes> ContractsDisputes { get; set; }
        public virtual DbSet<SalaryTypes> SalaryTypes { get; set; }
        public virtual DbSet<Transactions> Transactions { get; set; }
        public virtual DbSet<UserAttachments> UserAttachments { get; set; }
        public virtual DbSet<UserEducations> UserEducations { get; set; }
        public virtual DbSet<UserExperiences> UserExperiences { get; set; }
        public virtual DbSet<UserLogins> UserLogins { get; set; }
        public virtual DbSet<UserPictures> UserPictures { get; set; }
        public virtual DbSet<UserPortfolios> UserPortfolios { get; set; }
        public virtual DbSet<UserResumes> UserResumes { get; set; }
        public virtual DbSet<UserSecurityCheck> UserSecurityCheck { get; set; }
        public virtual DbSet<UserSkills> UserSkills { get; set; }
        public virtual DbSet<Contracts> Contracts { get; set; }
        public virtual DbSet<UserIntros> UserIntros { get; set; }
        public virtual DbSet<StripePayments> StripePayments { get; set; }
        public virtual DbSet<ContractsHours> ContractsHours { get; set; }
        public virtual DbSet<UserReports> UserReports { get; set; }
        public virtual DbSet<UserYoutubes> UserYoutubes { get; set; }
        public virtual DbSet<PayoutRecipients> PayoutRecipients { get; set; }
        public virtual DbSet<EmailsUnsubscribe> EmailsUnsubscribe { get; set; }
        public virtual DbSet<AdminKeyIPs> AdminKeyIPs { get; set; }
        public virtual DbSet<PayoutTransactions> PayoutTransactions { get; set; }
        public virtual DbSet<PayoutTransactionsLog> PayoutTransactionsLog { get; set; }
        public virtual DbSet<GlobalJobCategories> GlobalJobCategories { get; set; }
        public virtual DbSet<GlobalUpgrades> GlobalUpgrades { get; set; }
        public virtual DbSet<JobInvites> JobInvites { get; set; }
        public virtual DbSet<Actions> Actions { get; set; }
        public virtual DbSet<UserSMS> UserSMS { get; set; }
        public virtual DbSet<MailMessagesScams> MailMessagesScams { get; set; }
        public virtual DbSet<UserReferences> UserReferences { get; set; }
        public virtual DbSet<UserHRProfile> UserHRProfile { get; set; }
        public virtual DbSet<UserHRLanguages> UserHRLanguages { get; set; }
        public virtual DbSet<UserHRReviews> UserHRReviews { get; set; }
        public virtual DbSet<UserHRSkills> UserHRSkills { get; set; }
        public virtual DbSet<CompanyBalance> CompanyBalance { get; set; }
        public virtual DbSet<HRPremiumContracts> HRPremiumContracts { get; set; }
        public virtual DbSet<Emails> Emails { get; set; }
        public virtual DbSet<ContractsSecured> ContractsSecured { get; set; }
        public virtual DbSet<ContractsInvoices> ContractsInvoices { get; set; }
        public virtual DbSet<CompanyInvites> CompanyInvites { get; set; }
        public virtual DbSet<UserInvites> UserInvites { get; set; }
        public virtual DbSet<UserAgreements> UserAgreements { get; set; }
        public virtual DbSet<ReferalEvents> ReferalEvents { get; set; }
        public virtual DbSet<Referal> Referal { get; set; }
        public virtual DbSet<PockytPaymentInformation> PockytPaymentInformation { get; set; }
        public virtual DbSet<JobApplicationsAI> JobApplicationsAI { get; set; }
        public virtual DbSet<Academy> Academy { get; set; }
        public virtual DbSet<UserNotifications> UserNotifications { get; set; }

        #region ComplexTypes
        public virtual DbSet<spGetJobTitleRelatedWorkers> spGetJobTitleRelatedWorkers { get; set; }
        public virtual DbSet<spIsCapableClient> spIsCapableClient { get; set; }
        public virtual DbSet<spGetCurrentPricingPlan> spGetCurrentPricingPlan { get; set; }
        public virtual DbSet<spGetGlobalGroupByCountry> spGetGlobalGroupByCountry { get; set; }
        public virtual DbSet<spGetTotalAccountBalanced> spGetTotalAccountBalanced { get; set; }
        public virtual DbSet<spGetTotalCoAccountBalanced> spGetTotalCoAccountBalanced { get; set; }
        public virtual DbSet<spGetRegisteredUsersLastWeekWithMatchingSkills> spGetRegisteredUsersLastWeekWithMatchingSkills { get; set; }
        public virtual DbSet<spGetLastHourRecentApplicantsOfJobs> spGetLastHourRecentApplicantsOfJobs { get; set; }
        public virtual DbSet<spGetBoolResult> spGetBoolResult { get; set; }
        public virtual DbSet<spSearchWorkers> spSearchWorkers { get; set; }
        public virtual DbSet<spGetGlobalJobTitlesWithCategories> SpGetGlobalJobTitlesWithCategories { get; set; }
        public virtual DbSet<spGetJobTitleRelatedJobs> spGetJobTitleRelatedJobs { get; set; }
        public virtual DbSet<spGetSkillsRelatedJobs> spGetSkillsRelatedJobs { get; set; }
        public virtual DbSet<spGetContractSecured> spGetContractSecured { get; set; }
        public virtual DbSet<spGetContractUnbilled> spGetContractUnbilled { get; set; }
        public virtual DbSet<spGetContractLastPayment> spGetContractLastPayment { get; set; }
        public virtual DbSet<spGetAccountBalanced> spGetAccountBalanced { get; set; }
        public virtual DbSet<spGetContractBalanced> spGetContractBalanced { get; set; }
        public virtual DbSet<spGetIsSecured> spGetIsSecured { get; set; }
        public virtual DbSet<sp_ContractsEndsWeekly> sp_ContractsEndsWeekly { get; set; }
        public virtual DbSet<sp_ContractPayoutWeekly> sp_ContractPayoutWeekly { get; set; }
        public virtual DbSet<sp_UsersPayoutWeekly> sp_UsersPayoutWeekly { get; set; }
        public virtual DbSet<spGetContractBilledTotal> spGetContractBilledTotal { get; set; }
        public virtual DbSet<spGetContractCommission> spGetContractCommission { get; set; }
        public virtual DbSet<spGetUserIdResult> spGetUserIdResult { get; set; }
        public virtual DbSet<sp_actionPayouts> sp_actionPayouts { get; set; }
        public virtual DbSet<sp_hr_charge> sp_hr_charge { get; set; }
        public virtual DbSet<sp_getWorkerSubscription> sp_getWorkerSubscription { get; set; }
        #endregion
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=15.235.86.108; Database=gohirenow_dev;User Id=db_ghn_dev; Password=8p5hDEuhxr86t;", options => options.EnableRetryOnFailure());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity<AspNetRoleClaims>(entity =>
            {
                entity.HasIndex(e => e.RoleId);

                entity.Property(e => e.RoleId).IsRequired();

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<AspNetRoles>(entity =>
            {
                entity.HasIndex(e => e.NormalizedName)
                    .HasName("RoleNameIndex")
                    .IsUnique()
                    .HasFilter("([NormalizedName] IS NOT NULL)");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.NormalizedName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetUserClaims>(entity =>
            {
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserLogins>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserRoles>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasIndex(e => e.RoleId);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.RoleId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserTokens>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserTokens)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUsers>(entity =>
            {
                entity.HasIndex(e => e.NormalizedEmail)
                    .HasName("EmailIndex");

                entity.HasIndex(e => e.NormalizedUserName)
                    .HasName("UserNameIndex")
                    .IsUnique()
                    .HasFilter("([NormalizedUserName] IS NOT NULL)");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Email).HasMaxLength(256);

                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);

                entity.Property(e => e.UserIp).HasColumnName("UserIP");

                entity.Property(e => e.UserName).HasMaxLength(256);

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.AspNetUsers)
                    .HasForeignKey(d => d.CountryId)
                    .HasConstraintName("FK_AspNetUsers_Countries");
            });

            modelBuilder.Entity<AttachmentTypes>(entity =>
            {
                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Countries>(entity =>
            {
                entity.Property(e => e.Code).HasMaxLength(3);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<FavoriteJobs>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.FavoriteJobs)
                    .HasForeignKey(d => d.JobId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavoriteJobs_Jobs");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.FavoriteJobs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavoriteJobs_AspNetUsers");
            });

            modelBuilder.Entity<FavoriteWorkers>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.WorkerId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.FavoriteWorkersUser)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavoriteWorkers_Client");

                entity.HasOne(d => d.Worker)
                    .WithMany(p => p.FavoriteWorkersWorker)
                    .HasForeignKey(d => d.WorkerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FavoriteWorkers_Worker");
            });

            modelBuilder.Entity<GlobalErrors>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ErrorUrl).HasMaxLength(4000);

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.UserId).HasMaxLength(450);
            });

            modelBuilder.Entity<GlobalPlans>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
            });

            modelBuilder.Entity<TransactionsType>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<GlobalSkills>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Ipaddresses>(entity =>
            {
                entity.ToTable("IPAddresses");

                entity.Property(e => e.CountryCode).HasMaxLength(3);

                entity.Property(e => e.Ipfrom).HasColumnName("IPFrom");

                entity.Property(e => e.Ipto).HasColumnName("IPTo");
            });

            modelBuilder.Entity<JobApplications>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.Salary).HasColumnType("decimal(18, 0)");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.JobApplications)
                    .HasForeignKey(d => d.JobId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_JobApplications_Jobs");

                entity.HasOne(d => d.SalaryType)
                    .WithMany(p => p.JobApplications)
                    .HasForeignKey(d => d.SalaryTypeId)
                    .HasConstraintName("FK_JobApplications_SalaryTypes");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.JobApplications)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_JobApplications_AspNetUsers");
            });

            modelBuilder.Entity<JobAttachments>(entity =>
            {
                entity.Property(e => e.AttachedFile).IsRequired();

                entity.Property(e => e.Title).HasMaxLength(500);

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.JobAttachments)
                    .HasForeignKey(d => d.JobId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_JobAttachments_Jobs");
            });

            modelBuilder.Entity<JobSkills>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.JobSkills)
                    .HasForeignKey(d => d.JobId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_JobSkills_Jobs");

                entity.HasOne(d => d.Skill)
                    .WithMany(p => p.JobSkills)
                    .HasForeignKey(d => d.SkillId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_JobSkills_GlobalSkills");
            });

            modelBuilder.Entity<JobStatuses>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<JobTypes>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Contracts>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Contracts_AspNetUsers");
            });

            modelBuilder.Entity<UserHRProfile>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.UserId).IsRequired();
                entity.HasOne(d => d.User)
                    .WithOne(p => p.UserHRProfile)
                    .HasForeignKey<UserHRProfile>(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserHRProfiles_AspNetUsers");
            });

            modelBuilder.Entity<Jobs>(entity =>
            {
                entity.HasIndex(e => e.CreateDate);

                entity.HasIndex(e => e.JobStatusId);

                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.ActiveDate).HasColumnType("datetime");

                entity.Property(e => e.CloseDate).HasColumnType("datetime");

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.Salary).HasColumnType("decimal(18, 0)");

                entity.Property(e => e.Title).IsRequired();

                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(d => d.JobStatus)
                    .WithMany(p => p.Jobs)
                    .HasForeignKey(d => d.JobStatusId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Jobs_JobStatuses");

                entity.HasOne(d => d.JobType)
                    .WithMany(p => p.Jobs)
                    .HasForeignKey(d => d.JobTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Jobs_JobTypes");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Jobs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Jobs_AspNetUsers");
            });

            modelBuilder.Entity<MailMessages>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FileName).HasMaxLength(200);

                entity.Property(e => e.FilePath).HasMaxLength(200);

                entity.Property(e => e.FromUserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.Ipaddress)
                    .HasColumnName("IPAddress")
                    .HasMaxLength(256);

                // entity.Property(e => e.Message).IsRequired();

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.ToUserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.FromUser)
                    .WithMany(p => p.MailMessagesFromUser)
                    .HasForeignKey(d => d.FromUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MailMessages_AspNetUsers");

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.MailMessages)
                    .HasForeignKey(d => d.JobId)
                    .HasConstraintName("FK_MailMessages_Jobs");

                entity.HasOne(d => d.Mail)
                    .WithMany(p => p.MailMessages)
                    .HasForeignKey(d => d.MailId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MailMessages_Mails");

                entity.HasOne(d => d.ToUser)
                    .WithMany(p => p.MailMessagesToUser)
                    .HasForeignKey(d => d.ToUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MailMessages_AspNetUsers1");
            });

            modelBuilder.Entity<Mails>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Ipaddress)
                    .IsRequired()
                    .HasColumnName("IPAddress")
                    .HasMaxLength(255);

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.Title).IsRequired();

                entity.Property(e => e.UserIdFrom)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.UserIdTo)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.Job)
                    .WithMany(p => p.Mails)
                    .HasForeignKey(d => d.JobId)
                    .HasConstraintName("FK_Mails_Jobs");

                entity.HasOne(d => d.UserIdFromNavigation)
                    .WithMany(p => p.MailsUserIdFromNavigation)
                    .HasForeignKey(d => d.UserIdFrom)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Mails_AspNetUsers");

                entity.HasOne(d => d.UserIdToNavigation)
                    .WithMany(p => p.MailsUserIdToNavigation)
                    .HasForeignKey(d => d.UserIdTo)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Mails_AspNetUsers1");
            });

            modelBuilder.Entity<Reports>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Reports_AspNetUsers");
            });

            modelBuilder.Entity<SalaryTypes>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Transactions>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.CardName)
                     .HasMaxLength(255);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.ReceiptId).HasMaxLength(1000);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.GlobalPlan)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.GlobalPlanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transactions_GlobalPlans");

                entity.HasOne(d => d.TransactionsType)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.CustomType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transactions_TransactionsType");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transactions_AspNetUsers");
            });

            modelBuilder.Entity<UserAttachments>(entity =>
            {
                entity.Property(e => e.AttachedFile).IsRequired();

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Type).HasMaxLength(255);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserAttachments)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserAttachments_AspNetUsers");
            });

            modelBuilder.Entity<UserEducations>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.DegreeName)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.InstitutionName)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserEducations)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserEducations_AspNetUsers");
            });

            modelBuilder.Entity<UserExperiences>(entity =>
            {
                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.EndDate).HasColumnType("datetime");

                entity.Property(e => e.JobDescription).IsRequired();

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.StartDate).HasColumnType("datetime");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserExperiences)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserExperiences_AspNetUsers");
            });

            modelBuilder.Entity<UserLogins>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserLogins)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserLogins_AspNetUsers");
            });

            modelBuilder.Entity<UserPictures>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PictureFile).IsRequired();

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserPictures)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserPictures_AspNetUsers");
            });

            modelBuilder.Entity<InterviewsSchedules>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.HasOne(d => d.Interview)
                    .WithMany(p => p.InterviewsSchedules)
                    .HasForeignKey(d => d.InterviewId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_InterviewsSchedules_Interviews");
            });

            modelBuilder.Entity<UserReferences>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserReferences)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserReferences_AspNetUsers");
            });

            modelBuilder.Entity<UserPortfolios>(entity =>
            {
                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Description).IsRequired();

                entity.Property(e => e.Link)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

                entity.Property(e => e.Title).IsRequired();

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserPortfolios)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserPortfolios_AspNetUsers");
            });

            modelBuilder.Entity<UserYoutubes>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserYoutubes)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserYoutubes_AspNetUsers");
            });

            modelBuilder.Entity<ContractsInvoices>(entity =>
            {
                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.ContractId)
                    .IsRequired();

                entity.HasOne(d => d.Contract)
                    .WithMany(p => p.ContractsInvoices)
                    .HasForeignKey(d => d.ContractId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ContractsInvoices_Contracts");
            });

            modelBuilder.Entity<UserResumes>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ResumeFile).IsRequired();

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserResumes)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserResumes_AspNetUsers");
            });

            modelBuilder.Entity<UserSecurityCheck>(entity =>
            {
                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.CompanyId)
                    .IsRequired()
                    .HasMaxLength(450);
            });

            modelBuilder.Entity<UserSkills>(entity =>
            {
                entity.Property(e => e.CreateDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.Skill)
                    .WithMany(p => p.UserSkills)
                    .HasForeignKey(d => d.SkillId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserSkills_GlobalSkills");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserSkills)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserSkills_AspNetUsers");
            });
        }
    }
}
