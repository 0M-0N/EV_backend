using GoHireNow.Database;
using GoHireNow.Database.ComplexTypes;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.StripeModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GoHireNow.Service.ClientServices
{
    public class PricingService : IPricingService
    {
        public async Task<bool> CanClientPostJob(string userId)
        {
            int allowedJobs = await GetAllowedJobsByClientId(userId);
            using (var _context = new GoHireNowContext())
            {
                int currentJobs = await _context.Jobs.CountAsync(x => x.UserId == userId && x.IsDeleted == false && x.IsActive.Value == true);
                return currentJobs < allowedJobs;
            }
        }

        public async Task<bool> CanWorkerApplyForThisJob(int jobId, string clientId, string workerId)
        {
            //get job with plan details
            int allowedApplicants = await GetAllowedApplicationsByClientId(clientId);

            using (var _context = new GoHireNowContext())
            {
                var hasAlreadyApplied = await _context.JobApplications.FirstOrDefaultAsync(x => x.JobId == jobId && x.UserId == workerId && !x.IsDeleted);
                if (hasAlreadyApplied != null)
                    throw new CustomException((int)HttpStatusCode.Conflict, "User has already applied for this job");

                int currentApplicationCount = await _context.JobApplications.CountAsync(x => x.JobId == jobId);

                return currentApplicationCount < allowedApplicants;
            }
        }

        public async Task<WorkerCurrentPlanResponse> GetWorkerSubscriptionDetails(string userId)
        {
            var _context = new GoHireNowContext();

            var transactions = await _context.Transactions
                .Include(x => x.GlobalPlan)
                .Where(x => x.UserId == userId)
                .Select(x => new TransactionResponse
                {
                    Id = x.Id,
                    Amount = x.Amount,
                    UserId = x.UserId,
                    CardName = x.CardName,
                    GlobalPlanId = x.GlobalPlanId,
                    GlobalPlanName = x.GlobalPlanId.ToGlobalPlanName(),
                    Receipt = x.Receipt,
                    ReceiptId = x.ReceiptId,
                    Status = x.Status,
                    CreateDate = x.CreateDate,
                    CustomId = x.CustomId,
                    CustomType = x.CustomType,
                })
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();

            var currentPlan = await _context.sp_getWorkerSubscription.FromSql("sp_getWorkerSubscription @userID = {0}", userId).FirstOrDefaultAsync();
            if (currentPlan == null)
            {
                return new WorkerCurrentPlanResponse()
                {
                    Transactions = transactions,
                    SubscriptionStatus = null,
                };
            }

            var subscriptionStatus = new SubscriptionWorkerStatusResponse()
            {
                Id = currentPlan.planid,
                JobApplications = currentPlan.jobapplications,
                MaximumSkills = currentPlan.maximumskills,
                WithdrawAnytime = currentPlan.withdrawanytime,
                ProfileVisits = currentPlan.profilevisits,
                ApplicationStatus = currentPlan.applicationstatus,
                HideProfile = currentPlan.hideprofile,
                ViewApplications = currentPlan.viewapplications,
                AcademyAccess = currentPlan.academyaccess,
                FraudProtection = currentPlan.fraudprotection,
                EmailNotifications = currentPlan.emailnotifications,
                AiApplications = currentPlan.aiapplications,
                BlackAccess = currentPlan.blackaccess,
                Featured = currentPlan.featured,
                FirstMessage = currentPlan.firstmessage,
                RockstarBadge = currentPlan.rockstarbadge,
                LiveChat = currentPlan.livechat,
                CreditsLeft = currentPlan.creditsleft,
                SkillsLeft = currentPlan.skillsleft,
                PlanName = currentPlan.planname
            };

            BillingStatusResponse billingStatus = null;
            if (currentPlan.planid != (int)GlobalPlanEnum.FreeForWorker)
            {
                billingStatus = new BillingStatusResponse()
                {
                    PlanPrice = currentPlan.planprice,
                    NextBillingDate = currentPlan.lastpaid.AddMonths(1)
                };
            }

            return new WorkerCurrentPlanResponse()
            {
                Transactions = transactions,
                SubscriptionStatus = subscriptionStatus,
                BillingStatus = billingStatus
            };
        }

        public async Task<ClientCurrentPlanResponse> GetSubscriptionDetails(string userId)
        {
            var transactions = new List<TransactionResponse>();

            var currentPlan = await GetCurrentPlan(userId);

            if (currentPlan == null)
            {
                return new ClientCurrentPlanResponse()
                {
                    SubscriptionStatus = null,
                    Transactions = new List<TransactionResponse>()
                };
            }


            int currentJobCount = 0;//TODO update
            int currentContacts = 0;//TODO update

            using (var _context = new GoHireNowContext())
            {
                currentJobCount = currentPlan.TotalPostedJobs;
                currentContacts = currentPlan.TotalUsedContacts;
                transactions = await _context.Transactions
                    .Include(x => x.GlobalPlan)
                    .Where(x => x.UserId == userId)
                    .Select(x => new TransactionResponse
                    {
                        Id = x.Id,
                        Amount = x.Amount,
                        UserId = x.UserId,
                        CardName = x.CardName,
                        GlobalPlanId = x.GlobalPlanId,
                        GlobalPlanName = x.GlobalPlanId.ToGlobalPlanName(),
                        Receipt = x.Receipt,
                        ReceiptId = x.ReceiptId,
                        Status = x.Status,
                        CreateDate = x.CreateDate,
                        CustomId = x.CustomId,
                        CustomType = x.CustomType,
                    })
                    .OrderByDescending(x => x.CreateDate)
                    .ToListAsync();
            }

            var subscriptionStatus = new SubscriptionStatusResponse()
            {
                AllowedContacts = currentPlan.MaxContacts.Value,
                AllowedJobs = currentPlan.JobPosts,
                CurrentContacts = currentContacts,//TODO: update contacts
                PostedJobs = currentJobCount,
                Id = currentPlan.Id,
                PlanName = currentPlan.Name,
                MaxApplicantsPerJob = currentPlan.MaxApplicants,
                AllowPromotion = currentPlan.AllowPromotion
            };

            BillingStatusResponse billingStatus = null;
            if (currentPlan.Id != (int)GlobalPlanEnum.Free)
            {
                billingStatus = new BillingStatusResponse()
                {
                    PlanPrice = currentPlan.Price,
                    NextBillingDate = currentPlan.TransactionCreatedDate.Value.AddMonths(1)
                };
            }

            return new ClientCurrentPlanResponse()
            {
                Transactions = transactions,
                SubscriptionStatus = subscriptionStatus,
                BillingStatus = billingStatus
            };
        }

        public async Task<GlobalPlanDetailResponse> GetCurrentPlan(string userId)
        {
            spGetCurrentPricingPlan transaction;
            using (var _context = new GoHireNowContext())
            {
                transaction = await _context.spGetCurrentPricingPlan.FromSql("spGetCurrentPricingPlan @UserId = {0}", userId).FirstOrDefaultAsync();
            }

            if (transaction == null)
                return null;

            return new GlobalPlanDetailResponse()
            {
                Id = transaction.Id,
                Name = transaction.Name,
                Price = transaction.Price,
                JobPosts = transaction.JobPosts,
                ViewApplicants = transaction.ViewApplicants,
                AddFavorites = transaction.AddFavorites,
                ContactApplicants = transaction.ContactApplicants,
                Hire = transaction.Hire,
                MaxApplicants = transaction.MaxApplicants,
                MaxDays = transaction.MaxDays,
                AccessId = transaction.AccessId,
                IsActive = transaction.IsActive,
                CreateDate = transaction.CreateDate,
                ModifiedDate = transaction.ModifiedDate,
                Dedicated = transaction.Dedicated,
                MaxContacts = transaction.MaxContacts,
                TotalPostedJobs = transaction.TotalPostedJobs,
                TotalUsedContacts = transaction.TotalUsedContacts,
                TransactionCreatedDate = transaction.TransactionCreatedDate,
                FreePlanSubscriptionDate = transaction.FreePlanSubscriptionDate,
                AllowPromotion = transaction.AllowPromotion
            };
        }

        public UserCapablePricingPlanResponse IsCapable(string userId, string entryType, string toUserId)
        {
            spIsCapableClient result;
            using (var _context = new GoHireNowContext())
            {
                result = _context.spIsCapableClient.FromSql("spIsCapableClient @UserId =  {0},@EntryType = {1},@ToUserId = {2}", userId, entryType, toUserId)
                        .FirstOrDefault();
            }
            return new UserCapablePricingPlanResponse()
            {
                Result = result.IsCapable,
                Message = result.Message,
                Stat = result.Stat
            };
        }

        private async Task<int> GetAllowedApplicationsByClientId(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var transaction = await _context.Transactions
                        .Include(x => x.GlobalPlan)
                        .Where(x => x.UserId == userId)
                        .OrderByDescending(x => x.CreateDate)
                        .FirstOrDefaultAsync();

                return transaction == null ? 1 : transaction.GlobalPlan.MaxApplicants;
            }
        }

        public async Task<int> GetAllowedJobsByClientId(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var transaction = await _context.Transactions
                        .Include(x => x.GlobalPlan)
                        .Where(x => x.UserId == userId)
                        .OrderByDescending(x => x.CreateDate)
                        .FirstOrDefaultAsync();

                return transaction == null ? LookupService.PageSize.FreeJobPosts : transaction.GlobalPlan.JobPosts;
            }
        }

        public async Task<IEnumerable<GlobalPlanDetailResponse>> GetGlobalPlanDetails()
        {
            using (var _context = new GoHireNowContext())
            {
                var result = await _context.GlobalPlans.ToListAsync();
                return result.Select(x => new GlobalPlanDetailResponse()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Price,
                    JobPosts = x.JobPosts,
                    ViewApplicants = x.ViewApplicants,
                    AddFavorites = x.AddFavorites,
                    WorkerApplications = x.WorkerApplications,
                    WorkerAIapp = x.WorkerAIapp,
                    WorkerSkills = x.WorkerSkills,
                    ContactApplicants = x.ContactApplicants,
                    Hire = x.Hire,
                    MaxApplicants = x.MaxApplicants,
                    MaxDays = x.MaxDays,
                    AccessId = x.AccessId,
                    IsActive = x.IsActive,
                    CreateDate = x.CreateDate,
                    ModifiedDate = x.ModifiedDate,
                    MaxContacts = x.MaxContacts,
                    Dedicated = x.Dedicated,
                    BillingName = x.BillingName,
                    BillingSpecs = x.BillingSpecs,
                    PlanGroup = x.PlanGroup,
                    UpgradeGroup = x.UpgradeGroup,
                    WorkerGroup = x.WorkerGroup,
                    SecurityGroup = x.SecurityGroup,
                    Renewable = x.Renewable,
                    IsProcessingFee = x.IsProcessingFee,
                    ProcessingFeeRate = x.ProcessingFeeRate
                }).ToList();
            }
        }
    }
}
