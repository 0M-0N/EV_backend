using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.JobModels;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GoHireNow.Service.ClientServices
{
    public class ClientJobsService : IClientJobService
    {
        private readonly IPricingService _pricingService;
        private readonly IUserRoleService _userRoleService;
        private readonly IContractService _contractService;
        private IConfiguration _configuration { get; }

        public ClientJobsService(IPricingService pricingService, IContractService contractService, IConfiguration configuration, IUserRoleService userRoleService)
        {
            _pricingService = pricingService;
            _configuration = configuration;
            _contractService = contractService;
            _userRoleService = userRoleService;
        }

        public async Task<Jobs> GetJob(int id, int roleId)
        {
            using (var _context = new GoHireNowContext())
            {
                var result = await _context.Jobs
                    .Include(j => j.JobSkills)
                    .Include(j => j.User)
                    .FirstOrDefaultAsync(j => j.Id == id && j.IsDeleted == false && !j.User.IsDeleted && j.User.IsSuspended == 0);
                result.Title = !string.IsNullOrEmpty(result.Title) ? result.Title.ReplaceInformation(roleId, roleId == 1 ? false : _userRoleService.TextFilterCondition(result.UserId, roleId, null).Result) : null;
                result.Description = !string.IsNullOrEmpty(result.Description) ? result.Description.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(result.UserId, roleId, null).Result) : null;

                var transactions = await _context.Transactions
                    .Include(x => x.GlobalPlan)
                    .Where(x => x.CustomId == result.Id && x.CustomType == 1 && x.Status == "paid" && !x.IsDeleted)
                    .ToListAsync();

                return result;
            }
        }

        public async Task<decimal> GetAverageApplicants()
        {
            using (var _context = new GoHireNowContext())
            {
                var jobs = await _context.Jobs.Where(x => x.CreateDate >= DateTime.UtcNow.AddDays(-7)).ToListAsync();
                var jobApplicants = await _context.JobApplications.Where(x => x.CreateDate >= DateTime.UtcNow.AddDays(-7) && !x.IsDeleted).ToListAsync();

                return (jobApplicants?.Count() > 0 && jobs?.Count() > 0)
                    ? Math.Round((decimal)jobApplicants.Count() / jobs.Count(), 1)
                    : 0.0m;
            }
        }

        public async Task<int> AddJob(Jobs job)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    await _context.Jobs.AddAsync(job);
                    await _context.SaveChangesAsync();
                    return job.Id;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<ClientJobDetailResponse> GetJob(int jobId, string userId, string rootPath, int roleId)
        {
            Jobs job;
            int contacts = 0;
            using (var _context = new GoHireNowContext())
            {
                job = await _context.Jobs.Include(o => o.User)
                    .Include(x => x.JobAttachments)
                    .Include(x => x.JobSkills)
                    .Include(x => x.JobApplications)
                    .FirstOrDefaultAsync(x => x.Id == jobId && x.UserId == userId && x.IsDeleted == false && x.User.IsDeleted == false && x.User.IsSuspended == 0 && x.User.IsSuspended == 0);
                contacts = await _context.Mails.Where(x => x.JobId == jobId).CountAsync();
            }

            if (job == null)
                return null;

            var currentPlan = await _pricingService.GetCurrentPlan(userId);
            int allowedApplicants = currentPlan == null ? LookupService.PageSize.FreeJobPosts : currentPlan.MaxApplicants;
            int maxContacts = currentPlan == null ? 10 : currentPlan.ContactApplicants;

            var res = new ClientJobDetailResponse();

            res.Id = job.Id;
            res.UserId = job.UserId;
            res.Title = job.Title;
            res.Description = job.Description;
            res.ApplicationCount = job.JobApplications.Where(o => o.IsDeleted == false).Count();
            res.AllowedApplicantions = allowedApplicants;
            res.StatusId = job.JobStatusId;
            res.Status = (job.JobStatusId > 0 && job.JobStatusId <= 6) ? job.JobStatusId.ToJobStatuseName() : "";
            res.TypeId = job.JobTypeId;
            res.Type = job.JobTypeId.ToJobTypeName();
            res.SalaryTypeId = job.SalaryTypeId;
            res.SalaryType = job.SalaryTypeId.ToSalaryTypeName();
            res.Salary = job.Salary;
            res.CreateDate = job.CreateDate;
            res.ActiveDate = job.ActiveDate;
            res.ModifiedDate = job.ModifiedDate;
            res.Skills = LookupService.GetSkillsById(job.JobSkills.Select(s => s.SkillId).ToList());
            int i = 1;
            res.Attachments = job.JobAttachments.Any() ?
                job.JobAttachments.Where(x => x.IsDeleted == false && x.IsActive == true)
                    .Select(x => new AttachmentResponse()
                    {
                        Id = x.Id,
                        FileName = x.Title,
                        Counter = i++,
                        FilePath = $"{LookupService.FilePaths.JobAttachmentUrl}{job.Id}/{x.AttachedFile}",
                        Icon = LookupService.GetFileImage(Path.GetExtension(x.AttachedFile), rootPath) == "img"
                                        ? $"{LookupService.FilePaths.JobAttachmentUrl}{job.Id}/{x.AttachedFile}"
                                        : "",
                        FileExtension = LookupService.GetFileImage(Path.GetExtension(x.AttachedFile), rootPath) != "img"
                                        ? Path.GetExtension(x.AttachedFile).Replace(".", "")
                                        : ""
                    })
                    .ToList()
                : new List<AttachmentResponse>();

            res.JobContacts = contacts;
            res.JobContactsMax = maxContacts;
            res.IsFeatured = false;
            res.IsUrgent = false;
            res.IsPrivate = false;

            using (var _context = new GoHireNowContext())
            {
                var transactions = await _context.Transactions
                    .Include(x => x.GlobalPlan)
                    .Where(x => x.CustomId == res.Id && x.CustomType == 1 && x.Status == "paid" && !x.IsDeleted)
                    .ToListAsync();

                if (transactions.Count() > 0)
                {
                    foreach (var subItem in transactions)
                    {
                        switch (subItem.GlobalPlanId.ToGlobalPlanName())
                        {
                            case "Urgent": res.IsUrgent = true; break;
                            case "Private": res.IsPrivate = true; break;
                            default: break;
                        }
                        if (subItem.GlobalPlanId.ToGlobalPlanName() == "Featured" && (DateTime.UtcNow - subItem.CreateDate).TotalDays < 30)
                        {
                            res.IsFeatured = true;
                        }
                    }
                }
            }

            TimeSpan ts = DateTime.UtcNow - job.CreateDate;

            if (currentPlan != null && currentPlan.CreateDate < job.CreateDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
            {
                res.IsFeatured = true;
            }

            return res;
        }

        public async Task<bool> UpdateJob(string userId, int jobId, PostJobRequest model)
        {
            using (var _context = new GoHireNowContext())
            {
                var dbJob = await _context.Jobs
                    .Include(x => x.JobSkills)
                    .FirstOrDefaultAsync(x => x.Id == jobId);

                if (dbJob == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Job not found");

                if (dbJob.UserId != userId)
                    throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have permission to update this job");

                dbJob.Title = model.Title;
                dbJob.Description = model.Description;
                dbJob.JobTypeId = model.JobTypeId;
                dbJob.SalaryTypeId = model.SalaryTypeId;
                dbJob.Salary = model.Salary;
                dbJob.ModifiedDate = DateTime.UtcNow;
                var sameSkills = SameSkills(dbJob.JobSkills.Select(x => x.SkillId).ToList(), model.JobSkillIds);

                if (!sameSkills)
                {
                    _context.JobSkills.RemoveRange(dbJob.JobSkills);
                    dbJob.JobSkills = MapJobSkills(model.JobSkillIds);
                }

                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> Rate(int jobApplicantId, int rate)
        {
            using (var _context = new GoHireNowContext())
            {
                var applicant = await _context.JobApplications.Include(a => a.Job).Where(x => x.Id == jobApplicantId && !x.IsDeleted).FirstOrDefaultAsync();

                if (applicant != null)
                {
                    applicant.Rating = rate;

                    var notification = new UserNotifications()
                    {
                        UserId = applicant.UserId,
                        CompanyId = applicant.Job.UserId,
                        CustomId = 5,
                        CustomName = rate.ToString(),
                        CreatedDate = DateTime.UtcNow,
                        IsDelerte = 0
                    };
                    await _context.UserNotifications.AddAsync(notification);
                    await _context.SaveChangesAsync();

                    return true;
                }

                return false;
            }
        }

        private bool SameSkills(List<int> oldSkills, List<int> newSkills)
        {
            var distinctA = oldSkills.Except(newSkills).ToList();

            var distinctB = newSkills.Except(oldSkills).ToList();

            if (distinctA.Count() == 0 && distinctB.Count() == 0)
                return true;

            return false;
        }

        private List<JobSkills> MapJobSkills(List<int> skillIds)
        {
            var skills = LookupService.GlobalSkills.Where(x => skillIds.Contains(x.Id))
                .Select(j => new JobSkills()
                {
                    SkillId = j.Id,
                    CreateDate = DateTime.UtcNow
                }).ToList();

            if (skills.Count == 0 || skills == null)
                throw new CustomException(400, "Invalid skillIds");

            return skills;
        }

        public async Task<List<Jobs>> GetAllJobs(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                DateTime sixMonthsAgo = DateTime.Now.AddMonths(-6);

                var jobs = await _context.Jobs.Where(j => j.UserId == userId && !j.IsDeleted && j.CreateDate >= sixMonthsAgo && !j.User.IsDeleted && j.User.IsSuspended == 0).OrderByDescending(j => j.CreateDate).ToListAsync();

                return jobs;
            }
        }

        public async Task<List<JobSummaryResponse>> GetActiveJobs(string userId, int page, int size, int statusId, int roleId, bool isactive = true)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;

            List<Jobs> jobs;

            using (var _context = new GoHireNowContext())
            {
                if (statusId == (int)JobStatusEnum.Published)
                {
                    jobs = await _context.Jobs.Include(o => o.User)
                        .Include(x => x.JobApplications).Include(x => x.User)
                        .Where(x => x.UserId == userId && (x.JobStatusId == (int)JobStatusEnum.Published || x.JobStatusId == (int)JobStatusEnum.NotApproved || x.JobStatusId == 6) && x.IsActive == true && x.IsDeleted == false && x.User.IsDeleted == false && x.User.IsSuspended == 0)
                        .OrderByDescending(j => j.CreateDate)
                        .ToListAsync();
                }
                else
                {
                    jobs = await _context.Jobs.Include(o => o.User)
                           .Include(x => x.JobApplications).Include(x => x.User)
                           .Where(x => x.UserId == userId && (x.JobStatusId == statusId) && x.IsActive == true && x.IsDeleted == false && x.User.IsDeleted == false && x.User.IsSuspended == 0)
                           .OrderByDescending(j => j.CreateDate)
                           .ToListAsync();
                }
            }

            if (jobs == null || jobs.Count() == 0)
                return null;
            var currentPlan = await _pricingService.GetCurrentPlan(userId);
            int allowedApplicants = currentPlan == null ? LookupService.PageSize.FreeJobPosts : currentPlan.MaxApplicants;


            using (var _context = new GoHireNowContext())
            {
                var res = jobs.Select(j =>
                {
                    var count = _context.JobInvites.Where(x => x.CompanyId == userId && x.JobId == j.Id && x.IsDeleted == 0).Count();

                    return new JobSummaryResponse
                    {
                        Id = j.Id,
                        UserId = j.UserId,
                        Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.UserId).Result) : null : j.Title.ReplaceGlobalJobTitleInformation(),
                        ApplicationCount = j.JobApplications.Where(o => o.Job.User.IsDeleted == false && o.IsDeleted == false).Count(),
                        AllowedApplicantions = allowedApplicants,
                        StatusId = j.JobStatusId,
                        Status = (j.JobStatusId > 0 && j.JobStatusId <= 6) ? j.JobStatusId.ToJobStatuseName() : "",
                        TypeId = j.JobTypeId,
                        Type = j.JobTypeId.ToJobTypeName(),
                        SalaryTypeId = j.SalaryTypeId,
                        SalaryType = j.SalaryTypeId.ToSalaryTypeName(),
                        Salary = j.Salary,
                        CreateDate = j.CreateDate,
                        appliedUsers = j.JobApplications.Select(u => u.UserId).ToList(),
                        ActiveDate = j.ActiveDate,
                        ModifiedDate = j.ModifiedDate,
                        IsFeatured = false,
                        IsUrgent = false,
                        IsPrivate = false,
                        InvitesCount = count
                    };
                }).ToList();

                foreach (var item in res)
                {
                    var transactions = await _context.Transactions
                        .Include(x => x.GlobalPlan)
                        .Where(x => x.CustomId == item.Id && x.CustomType == 1 && x.Status == "paid" && !x.IsDeleted)
                        .ToListAsync();

                    if (transactions.Count() > 0)
                    {
                        foreach (var subItem in transactions)
                        {
                            switch (subItem.GlobalPlanId.ToGlobalPlanName())
                            {
                                case "Urgent": item.IsUrgent = true; break;
                                case "Private": item.IsPrivate = true; break;
                                default: break;
                            }
                            if (subItem.GlobalPlanId.ToGlobalPlanName() == "Featured" && (DateTime.UtcNow - subItem.CreateDate).TotalDays < 30)
                            {
                                item.IsFeatured = true;
                            }
                        }
                    }

                    TimeSpan ts = DateTime.UtcNow - item.CreateDate;

                    if (currentPlan != null && currentPlan.CreateDate < item.CreateDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }

                return res;
            }
        }

        public async Task<List<JobSummaryResponse>> GetJobs(string userId, int page, int size, int status, int roleId, bool isactive = true)
        {

            int skip = page > 1 ? ((page - 1) * size) : 0;

            List<Jobs> jobs;

            using (var _context = new GoHireNowContext())
            {
                jobs = await _context.Jobs.Include(o => o.User)
                    .Include(x => x.JobApplications).Include(x => x.User)
                    .Where(x => x.UserId == userId && x.JobStatusId == status && x.IsActive == true && x.IsDeleted == false && x.User.IsDeleted == false && x.User.IsSuspended == 0)
                    .OrderByDescending(j => j.CreateDate)
                    .ToListAsync();
            }

            if (jobs == null || jobs.Count() == 0)
                return null;
            var currentPlan = await _pricingService.GetCurrentPlan(userId);
            int allowedApplicants = currentPlan == null ? LookupService.PageSize.FreeJobPosts : currentPlan.MaxApplicants;

            var res = jobs.Select(j => new JobSummaryResponse
            {
                Id = j.Id,
                UserId = j.UserId,
                Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.UserId).Result) : null : j.Title.ReplaceGlobalJobTitleInformation(),
                ApplicationCount = j.JobApplications.Where(o => o.Job.User.IsDeleted == false && o.IsDeleted == false).Count(),
                AllowedApplicantions = allowedApplicants,
                StatusId = j.JobStatusId,
                Status = (j.JobStatusId > 0 && j.JobStatusId <= 6) ? j.JobStatusId.ToJobStatuseName() : "",
                TypeId = j.JobTypeId,
                Type = j.JobTypeId.ToJobTypeName(),
                SalaryTypeId = j.SalaryTypeId,
                SalaryType = j.SalaryTypeId.ToSalaryTypeName(),
                Salary = j.Salary,
                CreateDate = j.CreateDate,
                ActiveDate = j.ActiveDate,
                ModifiedDate = j.ModifiedDate,
            }).ToList();

            using (var _context = new GoHireNowContext())
            {
                foreach (var item in res)
                {
                    var transactions = await _context.Transactions
                        .Include(x => x.GlobalPlan)
                        .Where(x => x.CustomId == item.Id && x.CustomType == 1 && x.Status == "paid" && !x.IsDeleted)
                        .ToListAsync();

                    if (transactions.Count() > 0)
                    {
                        foreach (var subItem in transactions)
                        {
                            switch (subItem.GlobalPlanId.ToGlobalPlanName())
                            {
                                case "Urgent": item.IsUrgent = true; break;
                                case "Private": item.IsPrivate = true; break;
                                default: break;
                            }
                            if (subItem.GlobalPlanId.ToGlobalPlanName() == "Featured" && (DateTime.UtcNow - subItem.CreateDate).TotalDays < 30)
                            {
                                item.IsFeatured = true;
                            }
                        }
                    }

                    TimeSpan ts = DateTime.UtcNow - item.CreateDate;

                    if (currentPlan != null && currentPlan.CreateDate < item.CreateDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }
            }

            return res;
        }

        public async Task<List<JobApplicantResponse>> GetJobApplicants(ApplicantFilterRequest model, string userId, int jobId, int roleId, int page = 1, int size = 5)
        {
            using (var _context = new GoHireNowContext())
            {

                var currentPlan = await _pricingService.GetCurrentPlan(userId);

                int maxApplicants = 0;
                if (currentPlan == null)
                {
                    var freePlan = await _context.GlobalPlans.Where(o => o.Id == 1).FirstOrDefaultAsync(); //Free Plan
                    maxApplicants = freePlan.ViewApplicants == 1 ? freePlan.MaxApplicants : 0;
                }
                else
                {
                    maxApplicants = currentPlan.ViewApplicants == 1 ? currentPlan.MaxApplicants : 0;
                }

                var query = _context.JobApplications
                        .Include(o => o.User)
                        .Where(x => x.JobId == jobId && x.IsDeleted == false && x.User.IsDeleted == false && x.User.IsSuspended == 0);

                if (model.KeywordIn != null && model.KeywordIn.Count() > 0)
                {
                    query = query.Where(x => model.KeywordIn.All(k => x.CoverLetter.ToUpper().Contains(k.ToUpper())));
                }

                if (model.KeywordOut != null && model.KeywordOut.Count() > 0)
                {
                    query = query.Where(x => model.KeywordOut.All(k => !x.CoverLetter.ToUpper().Contains(k.ToUpper())));
                }

                if (!string.IsNullOrWhiteSpace(model.Education))
                {
                    query = query.Where(x => x.User.Education == model.Education);
                }

                if (!string.IsNullOrWhiteSpace(model.Experience))
                {
                    query = query.Where(x => x.User.Experience == model.Experience);
                }

                if (model.SkillsIn != null && model.SkillsIn.Count() > 0)
                {
                    query = query.Where(x => model.SkillsIn.All(id => x.User.UserSkills.Any(u => u.SkillId == id)));
                }

                if (model.SkillsOut != null && model.SkillsOut.Count() > 0)
                {
                    query = query.Where(x => !x.User.UserSkills.Any(s => model.SkillsOut.Contains(s.SkillId)));
                }

                var re = await query.Select(ja => new JobApplicantResponse
                {

                    Id = ja.Id,
                    UserId = ja.UserId,
                    CountryName = ja.User.CountryId != null ? ja.User.CountryId.Value.ToCountryName() : "",
                    // CoverLetter = !string.IsNullOrEmpty(ja.CoverLetter) ? ja.CoverLetter.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, ja.UserId).Result) : null,
                    CoverLetter = !string.IsNullOrEmpty(ja.CoverLetter) ? ja.CoverLetter : null,
                    Name = ja.User.FullName,
                    Rate = ja.User.UserSalary,
                    Title = !string.IsNullOrEmpty(ja.User.UserTitle) ? ja.User.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, ja.UserId).Result) : null,
                    LastLoginDateTime = ja.User.LastLoginTime,
                    Education = ja.User.Education,
                    Experience = ja.User.Experience,
                    Rating = ja.User.Rating,
                    ApplicationRating = ja.Rating,
                    Featured = ja.User.Featured,
                    CreateDate = ja.CreateDate,
                    ProfilePicturePath = !string.IsNullOrEmpty(ja.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{ja.User.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}",
                    UserSkills = ja.User.UserSkills != null && ja.User.UserSkills.Any() ? ja.User.UserSkills.Select(us => new SkillResponse
                    {
                        Id = us.SkillId,
                        Name = us.Skill.Name
                    }).ToList() : new List<SkillResponse>(),
                    IsDeleted = ja.IsDeleted,
                    IsActive = ja.IsActive,
                }).Take(maxApplicants)
                        .ToListAsync();

                foreach (var item in re)
                {
                    var u = _context.AspNetUsers.Include(t => t.UserReferences).Where(e => e.Id == item.UserId && e.IsDeleted == false).FirstOrDefault();

                    item.ReferencesCount = u.UserReferences.Where(t => t.IsAccepted == 1 && t.IsDeleted == 0).ToList().Count();
                }

                return re;
            }
        }

        public async Task<int> PauseJob(string userId, int jobId)
        {
            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs.Include(j => j.JobApplications).Where(x => x.Id == jobId && !x.IsDeleted && x.UserId == userId).FirstOrDefaultAsync();

                if (job != null)
                {
                    job.JobStatusId = job.JobStatusId == 6 ? 2 : 6;

                    if (job.JobStatusId == 6)
                    {
                        foreach (var item in job.JobApplications)
                        {
                            var notification = new UserNotifications()
                            {
                                UserId = item.UserId,
                                CompanyId = job.UserId,
                                CustomId = 4,
                                CustomName = job.Title,
                                CreatedDate = DateTime.UtcNow,
                                IsDelerte = 0
                            };
                            await _context.UserNotifications.AddAsync(notification);
                        }
                    }
                    await _context.SaveChangesAsync();

                    return job.JobStatusId;
                }
                else
                {
                    return 0;
                }
            }
        }

        public async Task<bool> DeleteJobApplicant(string userId, int jobId, string workerId)
        {
            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(x => x.Id == jobId && x.UserId == userId);
                if (job == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Job not found");

                var fav = await _context.JobApplications.FirstOrDefaultAsync(x => x.JobId == jobId && x.UserId == workerId && !x.IsDeleted);

                if (fav == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Application not found");

                fav.IsActive = !fav.IsActive;
                var affectedRow = await _context.SaveChangesAsync();

                if (affectedRow > 0 && fav.IsActive == false)
                {
                    var notification = new UserNotifications()
                    {
                        UserId = workerId,
                        CompanyId = userId,
                        CustomId = 3,
                        CustomName = job.Title,
                        CreatedDate = DateTime.UtcNow,
                        IsDelerte = 0
                    };
                    await _context.UserNotifications.AddAsync(notification);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
        }

        public async Task<bool> Applied(string userId, string companyId)
        {
            using (var _context = new GoHireNowContext())
            {
                var jobApplicants = await _context.JobApplications
                    .Include(x => x.User)
                    .Include(x => x.Job)
                    .Where(j => j.UserId == userId && j.IsDeleted == false && j.Job.UserId == companyId && j.Job.IsDeleted == false && !j.User.IsDeleted && j.User.IsSuspended == 0)
                    .ToListAsync();
                if (jobApplicants != null && jobApplicants.Count() > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<List<int>> GetInvitedJobs(string companyId, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var invitedJobs = await _context.JobInvites.Where(x => x.CompanyId == companyId && x.UserId == userId && x.IsDeleted == 0).Select(x => x.JobId).ToListAsync();
                return invitedJobs;
            }
        }

        public async Task<bool> Invite(InviteRequest model, string companyId)
        {
            using (var _context = new GoHireNowContext())
            {
                foreach (var item in model.jobs)
                {
                    var invite = new JobInvites();
                    invite.CompanyId = companyId;
                    invite.UserId = model.userId;
                    invite.JobId = item;
                    invite.CreatedDate = DateTime.UtcNow;
                    invite.IsDeleted = 0;
                    await _context.JobInvites.AddAsync(invite);
                    await _context.SaveChangesAsync();

                    var job = await _context.Jobs.Where(x => x.Id == item && x.IsDeleted == false).FirstOrDefaultAsync();
                    var worker = await _context.AspNetUsers.Where(x => x.Id == model.userId && x.IsDeleted == false).FirstOrDefaultAsync();
                    string headtitle = model.companyName + " invited you to apply on this job";
                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/job-details-work/" + item;
                    string buttoncaption = "Apply on this job";
                    string description = "";
                    var subject = model.companyName + " is inviting you to apply";
                    var text = job.Title;
                    await _contractService.NewMailService(0, 30, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, text, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                }
                return true;
            }
        }

        public async Task<bool> InviteCompanies(InviteCompaniesRequest model, string companyId)
        {
            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == model.jobId && !j.IsDeleted);

                if (job != null)
                {
                    job.JobStatusId = 2;
                    job.IsEmail = 1;
                    await _context.SaveChangesAsync();
                }

                var newInvite = new CompanyInvites()
                {
                    CompanyId = companyId,
                    InviteeName = model.user1,
                    InviteeEmail = model.email1,
                    Type = 1,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.CompanyInvites.AddAsync(newInvite);
                await _context.SaveChangesAsync();

                newInvite = new CompanyInvites()
                {
                    CompanyId = companyId,
                    InviteeName = model.user2,
                    InviteeEmail = model.email2,
                    Type = 1,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.CompanyInvites.AddAsync(newInvite);
                await _context.SaveChangesAsync();

                var company = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == companyId && !u.IsDeleted);
                if (company != null)
                {
                    var subject = "An invitation from " + company.Company;
                    _contractService.PersonalEmailService(0, 41, model.email1, model.user1, subject, company.Company, model.user1, "julia.d@evirtualassistants.com", "Julia", 1, "InviteCompanyEmail.html");
                    _contractService.PersonalEmailService(0, 41, model.email2, model.user2, subject, company.Company, model.user2, "julia.d@evirtualassistants.com", "Julia", 1, "InviteCompanyEmail.html");
                }

                return true;
            }
        }

        public async Task<bool> AddJobAttachment(JobAttachments jobAttachment)
        {
            using (var _context = new GoHireNowContext())
            {
                await _context.JobAttachments.AddAsync(jobAttachment);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<int> DeleteJobAttachments(string userId, int jobId)
        {
            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs
                    .Include(x => x.JobAttachments)
                    .FirstOrDefaultAsync(x => x.Id == jobId && x.UserId == userId);

                if (job == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Job not found");

                if (job.JobAttachments == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Attachments not found");


                var jobAttachment = job.JobAttachments;

                foreach (var attachment in job.JobAttachments)
                {
                    attachment.IsDeleted = true;
                }

                await _context.SaveChangesAsync();
                return 1;
            }
        }

        public async Task<string> DeleteJobAttachment(string userId, int jobId, int attachmentId)
        {
            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs
                    .Include(x => x.JobAttachments)
                    .FirstOrDefaultAsync(x => x.Id == jobId && x.UserId == userId);

                if (job == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Job not found");

                if (job.JobAttachments == null || job.JobAttachments.Where(a => a.Id == attachmentId && a.IsDeleted != true).Count() == 0)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Attachments not found");

                var jobAttachment = job.JobAttachments.Where(a => a.Id == attachmentId).FirstOrDefault();

                jobAttachment.IsDeleted = true;

                await _context.SaveChangesAsync();

                return jobAttachment.AttachedFile;
            }
        }

        public async Task<bool> UpdateJobStatus(int jobId, string userId, int statusId)
        {
            if (!Enum.IsDefined(typeof(JobStatusEnum), statusId))
                throw new CustomException((int)HttpStatusCode.BadRequest, "Invalid JobStatus");

            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs.Include(j => j.JobApplications).FirstOrDefaultAsync(x => x.Id == jobId);
                if (job == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Job not found");

                if (job.UserId != userId)
                    throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have access to edit this job");

                if (job.JobStatusId == statusId)
                    return true;

                job.JobStatusId = statusId;

                if (statusId == 3)
                {
                    foreach (var item in job.JobApplications)
                    {
                        var notification = new UserNotifications()
                        {
                            UserId = item.UserId,
                            CompanyId = job.UserId,
                            CustomId = 4,
                            CustomName = job.Title,
                            CreatedDate = DateTime.UtcNow,
                            IsDelerte = 0
                        };
                        await _context.UserNotifications.AddAsync(notification);
                    }
                }
                await _context.SaveChangesAsync();

                return true;
            }
        }

        public async Task<bool> DeleteJob(int jobId, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(x => x.Id == jobId);
                if (job == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Job not found");

                if (job.UserId != userId)
                    throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have access to edit this job");

                if (job.IsActive.HasValue && job.IsActive == false)
                    return true;

                job.IsActive = false;
                await _context.SaveChangesAsync();

                return true;
            }
        }

        public async Task<Tuple<string, string>> GetAttachmentUrl(int attachmentId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var attachment = await _context.JobAttachments.FindAsync(attachmentId);
                    if (attachment != null)
                    {
                        //Tuple.Create(1,2);
                        var url = $"{LookupService.FilePaths.JobAttachmentUrl}{attachment.JobId}/{attachment.AttachedFile}";
                        return Tuple.Create(attachment.Title, url);
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}