using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI_API;
using OpenAI_API.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.WorkerServices
{
    public class WorkerJobService : IWorkerJobService
    {
        private readonly IPricingService _pricingService;
        private readonly IUserRoleService _userRoleService;
        private readonly ICustomLogService _customLogService;
        private IConfiguration _configuration { get; }
        private readonly IContractService _contractService;
        public WorkerJobService(IConfiguration configuration, ICustomLogService customLogService, IPricingService pricingService, IUserRoleService userRoleService, IContractService contractService)
        {
            _pricingService = pricingService;
            _configuration = configuration;
            _customLogService = customLogService;
            _userRoleService = userRoleService;
            _contractService = contractService;
        }

        public async Task<JobDetailForWorkerResponse> GetWorkerJobDetails(int jobId, string rootPath, string userId, int roleId)
        {
            Jobs job;
            using (var _context = new GoHireNowContext())
            {
                job = await _context.Jobs.Include(o => o.User)
                    .Include(x => x.JobSkills)
                    .Include(x => x.JobAttachments)
                    .FirstOrDefaultAsync(x => x.Id == jobId && x.JobStatusId != (int)JobStatusEnum.Closed && !x.User.IsDeleted && x.User.IsSuspended == 0);

                if (job == null)
                    return null;

                List<int> skillIds = job.JobSkills.Select(x => x.SkillId).ToList();

                var jobApplication = await _context.JobApplications.FirstOrDefaultAsync(ja => ja.JobId == jobId && !ja.IsDeleted && ja.UserId == userId);
                var favorite = await _context.FavoriteJobs.FirstOrDefaultAsync(ja => ja.JobId == jobId && !ja.IsDeleted && ja.UserId == userId);

                int i = 1;
                var res = new JobDetailForWorkerResponse();
                res.Id = job.Id;
                res.UserId = userId; //job.UserId; // Simple user id to use on the front end message send. Client id is also in the client section
                res.Title = job.Title;
                res.Description = job.Description;
                res.CreateDate = job.CreateDate;
                res.Type = job.JobTypeId.ToJobTypeName();
                res.JobTypeId = job.JobTypeId;
                res.SalaryTypeId = job.SalaryTypeId;
                res.SalaryType = job.SalaryTypeId.ToSalaryTypeName();
                res.Salary = Convert.ToString(job.Salary);
                res.Status = (job.JobStatusId > 0 && job.JobStatusId <= 3) ? job.JobStatusId.ToJobStatuseName() : "";
                res.JobSkills = job.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList();
                res.Client = await GetJobClientSummaryAsync(job.UserId, userId, jobId);
                res.OtherJobsByClient = await GetOtherClientJobs(job.UserId, jobId, userId, roleId);
                res.Attachments = job.JobAttachments.Any()
                                    ?
                                    job.JobAttachments.Where(x => x.IsDeleted == false && x.IsActive == true)
                                    .Select(x => new AttachmentResponse()
                                    {
                                        Id = x.Id,
                                        FileName = x.Title,
                                        Counter = i++,
                                        FilePath = $"{LookupService.FilePaths.JobAttachmentUrl}{job.Id}/{x.AttachedFile}",
                                        Icon = LookupService.GetFileImage(Path.GetExtension(x.AttachedFile), rootPath) == "img"
                                                            ? $"{LookupService.FilePaths.JobAttachmentUrl}{job.Id}/{x.AttachedFile}"
                                                            : LookupService.GetFileImage(Path.GetExtension(x.AttachedFile), rootPath),
                                        FileExtension = Path.GetExtension(x.AttachedFile).Replace(".", "")
                                    })
                                    .ToList()
                                    :
                                    new List<AttachmentResponse>();
                res.ProfilePicturePath = !string.IsNullOrEmpty(job.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{job.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}";
                res.SimilarJobs = await GetMatchinJobsBySkillIds(skillIds, jobId, roleId, userId, 1, 6);
                res.IsFeatured = false;
                res.IsUrgent = false;
                res.IsPrivate = false;
                res.IsDeleted = job.IsDeleted;
                res.IsApplied = jobApplication == null ? false : true;
                res.IsFavorite = favorite == null ? false : true;

                var transactions = await _context.Transactions
                    .Include(x => x.GlobalPlan)
                    .Where(x => x.CustomId == jobId && x.CustomType == 1 && x.Status == "paid" && !x.IsDeleted)
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

                TimeSpan ts = DateTime.UtcNow - job.CreateDate;
                var currentPlan = await _pricingService.GetCurrentPlan(job.UserId);

                if (currentPlan != null && currentPlan.CreateDate < job.CreateDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                {
                    res.IsFeatured = true;
                }

                return res;
            }
        }

        public async Task<List<JobSummaryForWorkerResponse>> GetNewJobs(int status, string userId, int roleId, int page = 1, int size = 10)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;

            using (var _context = new GoHireNowContext())
            {
                var jobs = await _context.Jobs.Include(o => o.User)
                    .Include(x => x.JobSkills)
                    .Where(j => j.JobStatusId == status && j.User.IsDeleted == false && j.IsDashboard == 1 && j.User.IsSuspended == 0)
                    .OrderByDescending(j => j.CreateDate)
                    .Skip(skip).Take(size)
                    .Select(j => new JobSummaryForWorkerResponse()
                    {
                        Id = j.Id,
                        Title = !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.UserId).Result) : null,
                        StatusId = j.JobStatusId,
                        Status = (j.JobStatusId > 0 && j.JobStatusId <= 3) ? j.JobStatusId.ToJobStatuseName() : "",
                        TypeId = j.JobTypeId,
                        Type = j.JobTypeId.ToJobTypeName(),
                        SalaryTypeId = j.SalaryTypeId,
                        SalaryType = j.SalaryTypeId.ToSalaryTypeName(),
                        Salary = j.Salary,
                        ActiveDate = j.ActiveDate,
                        ProfilePicturePath = !string.IsNullOrEmpty(j.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}",
                        Skills = j.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList(),
                        Client = GetJobClientSummary(j.UserId, userId),
                        IsFeatured = false,
                        IsUrgent = false,
                        IsPrivate = false
                    })
                    .ToListAsync();

                foreach (var item in jobs)
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

                    TimeSpan ts = (TimeSpan)(DateTime.UtcNow - item.ActiveDate);
                    var currentPlan = await _pricingService.GetCurrentPlan(item.Client.Id);

                    if (currentPlan != null && currentPlan.CreateDate < item.ActiveDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }

                return jobs;
            }
        }

        public async Task<List<AttachmentResponse>> GetJobAttachments(int jobId, string rootPath)
        {
            int i = 0;
            using (var _context = new GoHireNowContext())
            {
                return await _context.JobAttachments.Where(j => j.JobId == jobId && j.IsActive == true && j.IsDeleted == false)
                        .Select(j => new AttachmentResponse
                        {
                            Id = j.Id,
                            FileName = j.Title,
                            Counter = (i + 1),
                            FilePath = $"{LookupService.FilePaths.JobAttachmentUrl}{j.JobId}/{j.AttachedFile}",
                            Icon = LookupService.GetFileImage(Path.GetExtension(j.AttachedFile), rootPath) == "img"
                                                        ? $"{LookupService.FilePaths.JobAttachmentUrl}{j.JobId}/{j.AttachedFile}"
                                                        : "", //LookupService.GetFileImage(Path.GetExtension(j.AttachedFile), rootPath),
                            FileExtension = LookupService.GetFileImage(Path.GetExtension(j.AttachedFile), rootPath) != "img"
                                            ? Path.GetExtension(j.AttachedFile).Replace(".", "")
                                            : ""
                        })
                        .ToListAsync();
            }
        }

        public async Task<int> ApplyJob(ApplyJobRequest model, string UserId)
        {
            using (var _context = new GoHireNowContext())
            {
                var job = await _context.Jobs.Include(x => x.JobApplications)
                    .FirstOrDefaultAsync(x => x.Id == model.JobId && x.IsActive == true && x.JobStatusId == (int)JobStatusEnum.Published && x.IsDeleted == false);

                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == UserId && !u.IsDeleted);
                if (job == null || user == null)
                    return -1;

                if (job.JobApplications.Any())
                {
                    var currentApplication = job.JobApplications.Where(x => x.UserId == UserId && !x.IsDeleted).FirstOrDefault();
                    if (currentApplication != null)
                    {
                        return -2;
                    }
                }

                var application = new JobApplications
                {
                    JobId = model.JobId,
                    UserId = UserId,
                    Resume = model.Resume ? user.UserResume : string.Empty,
                    CreateDate = DateTime.UtcNow,
                    IsDeleted = false,
                    ModifiedDate = DateTime.UtcNow,
                    CoverLetter = model.CoverLetter
                };

                await _context.JobApplications.AddAsync(application);
                await _context.SaveChangesAsync();

                return application.Id;
            }
        }

        public async Task<List<JobSummaryForWorkerResponse>> GetMatchingJobs(string userId, int roleId, int page = 1, int size = 5)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;
            using (var _context = new GoHireNowContext())
            {
                var skillIds = await _context.UserSkills.Where(x => x.UserId == userId).Select(s => s.SkillId).ToListAsync();
                DateTime startDate = DateTime.Now.AddDays(-90);

                var jobIds = _context.JobSkills
                    .Include(x => x.Job).ThenInclude(j => j.JobSkills)
                    .Where(x => skillIds.Contains(x.SkillId) && x.Job.JobStatusId != (int)JobStatusEnum.Closed && x.Job.JobStatusId != (int)JobStatusEnum.NotApproved && x.Job.JobStatusId != 6)
                    .Distinct()
                    .OrderByDescending(j => j.CreateDate)
                    .Skip(skip).Take(size)
                    .Select(x => x.JobId)
                    .ToList();

                var jobApplications = _context.JobApplications
                    .Where(x => x.UserId == userId && x.IsDeleted == false)
                    .Select(x => x.JobId)
                    .ToList();

                var jobs = await _context.Jobs.Include(o => o.User)
                    .Include(x => x.JobSkills)
                    .Include(x => x.JobApplications)
                    .Where(x => jobIds.Contains(x.Id) && x.User.IsDeleted == false && !jobApplications.Contains(x.Id) && x.CreateDate >= startDate && x.User.IsSuspended == 0)
                    .Select(j => new JobSummaryForWorkerResponse()
                    {
                        Id = j.Id,
                        Title = !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.UserId).Result) : null,
                        StatusId = j.JobStatusId,
                        Status = j.JobStatusId.ToJobStatuseName(),
                        TypeId = j.JobTypeId,
                        Type = j.JobTypeId.ToJobTypeName(),
                        SalaryTypeId = j.SalaryTypeId,
                        SalaryType = j.SalaryTypeId.ToSalaryTypeName(),
                        Salary = j.Salary,
                        ActiveDate = j.ActiveDate,
                        ProfilePicturePath = !string.IsNullOrEmpty(j.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}",
                        Skills = j.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList(),
                        Client = GetJobClientSummary(j.UserId, userId),
                        IsFeatured = false,
                        IsUrgent = false,
                        IsPrivate = false
                    })
                    .OrderByDescending(x => x.ActiveDate)
                    .ToListAsync();

                foreach (var item in jobs)
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

                    TimeSpan ts = (TimeSpan)(DateTime.UtcNow - item.ActiveDate);
                    var currentPlan = await _pricingService.GetCurrentPlan(item.Client.Id);

                    if (currentPlan != null && currentPlan.CreateDate < item.ActiveDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }

                return jobs;
            }
        }

        public async Task<List<JobApplicantResponse>> GetJobApplicants(string userId, int jobId, int roleId)
        {
            using (var _context = new GoHireNowContext())
            {
                var currentPlan = await _context.sp_getWorkerSubscription.FromSql("sp_getWorkerSubscription @userID = {0}", userId).FirstOrDefaultAsync();
                if (currentPlan == null || !(currentPlan.planid >= (int)GlobalPlanEnum.Junior && currentPlan.planid <= (int)GlobalPlanEnum.RockstarByBalance))
                {
                    return new List<JobApplicantResponse>();
                }

                int maxApplicants = 0;
                var query = _context.JobApplications
                        .Include(o => o.User)
                        .Where(x => x.JobId == jobId && x.User.IsDeleted == false && x.User.IsSuspended == 0 && x.UserId == userId && !x.IsDeleted);

                if (query == null)
                {
                    return new List<JobApplicantResponse>();
                }

                var re = await query.Select(ja => new JobApplicantResponse
                {
                    Id = ja.Id,
                    UserId = ja.UserId,
                    CountryName = ja.User.CountryId != null ? ja.User.CountryId.Value.ToCountryName() : "",
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
                }).ToListAsync();

                foreach (var item in re)
                {
                    var u = _context.AspNetUsers.Include(t => t.UserReferences).Where(e => e.Id == item.UserId && e.IsDeleted == false).FirstOrDefault();

                    item.ReferencesCount = u.UserReferences.Where(t => t.IsAccepted == 1 && t.IsDeleted == 0).ToList().Count();
                }

                return re;
            }
        }

        public async Task<List<JobSummaryForWorkerResponse>> GetMatchinJobsBySkillIds(List<int> skillIds, int jobId, int roleId, string userId, int page = 1, int size = 5)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;
            using (var _context = new GoHireNowContext())
            {
                var list = await _context.JobSkills
                    .Include(x => x.Job).ThenInclude(j => j.JobSkills)
                    .Include(x => x.Job).ThenInclude(j => j.User)
                    .Where(x => skillIds.Contains(x.SkillId) && x.JobId != jobId && x.Job.JobStatusId == (int)JobStatusEnum.Published && x.Job.User.IsDeleted == false && x.Job.User.IsSuspended == 0)
                    .OrderByDescending(u => u.CreateDate)
                    .Skip(skip).Take(size)
                    .GroupBy(x => x.JobId).ToListAsync();

                var res = list.Select(j => new JobSummaryForWorkerResponse()
                {
                    Id = j.FirstOrDefault().Job.Id,
                    Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(j.FirstOrDefault().Job.Title) ? j.FirstOrDefault().Job.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.FirstOrDefault().Job.UserId).Result) : null : j.FirstOrDefault().Job.Title.ReplaceGlobalJobTitleInformation(),
                    StatusId = j.FirstOrDefault().Job.JobStatusId,
                    Status = j.FirstOrDefault().Job.JobStatusId.ToJobStatuseName(),
                    TypeId = j.FirstOrDefault().Job.JobTypeId,
                    Type = j.FirstOrDefault().Job.JobTypeId.ToJobTypeName(),
                    SalaryTypeId = j.FirstOrDefault().Job.SalaryTypeId,
                    SalaryType = j.FirstOrDefault().Job.SalaryTypeId.ToSalaryTypeName(),
                    Salary = j.FirstOrDefault().Job.Salary,
                    ActiveDate = j.FirstOrDefault().Job.ActiveDate,
                    ProfilePicturePath = !string.IsNullOrEmpty(j.FirstOrDefault().Job.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.FirstOrDefault().Job.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}",
                    Skills = j.FirstOrDefault().Job.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList(),
                    Client = GetJobClientSummary(j.FirstOrDefault().Job.User.Id, userId),
                    IsFeatured = false,
                    IsUrgent = false,
                    IsPrivate = false
                }).Distinct().ToList();

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

                    TimeSpan ts = (TimeSpan)(DateTime.UtcNow - item.ActiveDate);
                    var currentPlan = await _pricingService.GetCurrentPlan(item.Client.Id);

                    if (currentPlan != null && currentPlan.CreateDate < item.ActiveDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }

                return res;
            }
        }

        public async Task<List<JobSummaryForWorkerResponse>> GetAppliedJobs(string userId, int roleId, int page = 1, int size = 5)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;
            using (var _context = new GoHireNowContext())
            {
                var jobApplications = await _context.JobApplications.Include(o => o.User)
                    .Include(x => x.Job).ThenInclude(j => j.JobSkills)
                    .Include(x => x.Job).ThenInclude(j => j.User)
                    .Where(x => x.UserId == userId && x.Job.JobStatusId != (int)JobStatusEnum.Closed && x.Job.JobStatusId != (int)JobStatusEnum.NotApproved && x.Job.JobStatusId != 6 && x.User.IsDeleted == false && x.Job.User.IsDeleted == false && x.Job.User.IsSuspended == 0 && !x.IsDeleted)
                    .OrderByDescending(j => j.CreateDate).Take(6)
                    .Select(j => new JobSummaryForWorkerResponse()
                    {
                        Id = j.Job.Id,
                        Title = !string.IsNullOrEmpty(j.Job.Title) ? j.Job.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.Job.UserId).Result) : null,
                        StatusId = j.Job.JobStatusId,
                        Status = j.Job.JobStatusId.ToJobStatuseName(),
                        TypeId = j.Job.JobTypeId,
                        Type = j.Job.JobTypeId.ToJobTypeName(),
                        SalaryTypeId = j.Job.SalaryTypeId,
                        SalaryType = j.Job.SalaryTypeId.ToSalaryTypeName(),
                        Salary = j.Job.Salary,
                        ProfilePicturePath = !string.IsNullOrEmpty(j.Job.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.Job.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}",
                        ActiveDate = j.CreateDate, //j.Job.ActiveDate, //changed to application date
                        Skills = j.Job.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList(),
                        Client = GetJobClientSummary(j.UserId, userId),
                        IsFeatured = false,
                        IsUrgent = false,
                        IsPrivate = false
                    }).ToListAsync();

                foreach (var item in jobApplications)
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

                    TimeSpan ts = (TimeSpan)(DateTime.UtcNow - item.ActiveDate);
                    var currentPlan = await _pricingService.GetCurrentPlan(item.Client.Id);

                    if (currentPlan != null && currentPlan.CreateDate < item.ActiveDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }

                return jobApplications;
            }
        }

        public async Task<UserIntros> CreateTemplate(string userId, SaveTemplateRequest model)
        {
            UserIntros intro = new UserIntros
            {
                UserId = userId,
                name = model.name,
                text = model.text,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = 0
            };

            using (var _context = new GoHireNowContext())
            {
                await _context.UserIntros.AddAsync(intro);
                await _context.SaveChangesAsync();
                return intro;
            }
        }

        public async Task<bool> DeleteTemplate(string userId, int id)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var intro = await _context.UserIntros.FindAsync(id);
                    if (intro != null)
                    {
                        intro.IsDeleted = 1;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<List<UserIntros>> GetTemplates(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var intros = await _context.UserIntros.Where(x => x.UserId == userId && x.IsDeleted == 0).ToListAsync();
                return intros;
            }
        }

        public async Task<string> AIAssistant(AIAssistantRequest model, string userId)
        {
            var _context = new GoHireNowContext();
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == model.jobId && !j.IsDeleted);
            var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.UserType == 2);
            if (job == null || user == null)
            {
                return null;
            }

            var jobSkills = "";
            foreach (var item in job.JobSkills)
            {
                jobSkills += item.SkillId.ToGlobalSkillName() + " ";
            }

            var userSkills = "";
            foreach (var item in user.UserSkills)
            {
                userSkills += item.SkillId.ToGlobalSkillName() + " ";
            }

            var request = new ChatRequest();
            request.Model = OpenAI_API.Models.Model.GPT4_Turbo;
            request.MaxTokens = 1024;
            request.Temperature = 0.4;
            request.Messages = new List<ChatMessage>();
            string content = "Our company provides virtual assistants for companies in the Philippines and America. Every day, we receive job descriptions from our clients and virtual assistants' cover letter applications applying for these jobs. We want you to be a human resources expert and analyze the application cover for a specific job. You will tell the virtual assistant applicant what is missing on their application cover letter to be a better candidate for this job posting. \n\n";
            content += "Here are the detail information of the job.\n\n";
            content += "First, job title - " + job.Title;
            content += "\n\n. Job description - " + job.Description;
            content += "\n\n.Job type - " + (job.SalaryTypeId == 1 ? "Full-Time job" : job.SalaryTypeId == 2 ? "Part-Time job" : "Freelance job");
            content += "\n\n. This is the draft of worker's proposal - " + model.proposal;
            content += "\n\n. Here is the detail information of this user\n";
            content += ". He/She is " + user.FullName + " from " + user.CountryId.Value.ToCountryName();
            if (!string.IsNullOrEmpty(user.Description) && !string.IsNullOrEmpty(user.UserTitle))
            {
                content += "\n\n. The title is " + user.UserTitle + " and the description of the user - " + user.Description;
            }
            if (!string.IsNullOrEmpty(user.Education))
            {
                content += ", the education status of the user - " + user.Education;
            }
            if (!string.IsNullOrEmpty(user.Experience))
            {
                content += ", has " + user.Experience + " of experience.";
            }
            content += "\n\n Based on these information, please answer the question. (Sometimes the job description may look fake)";

            request.Messages.Add(new ChatMessage()
            {
                Role = ChatMessageRole.System,
                Content = content
            });

            request.Messages.Add(new ChatMessage()
            {
                Role = ChatMessageRole.User,
                Content = "Please only send me a bullet point list of the top 5 things that must be changed or added to the cover letter. Export these bullet point into a Json format and also include in this Json a grade from one being the worst and 10 being the best. In the Json name  the Improvement:  improvements and grade: grade."
            });

            var openAI = new OpenAIAPI(_configuration.GetValue<string>("OpenAIApiKey"));
            ChatResult result = await openAI.Chat.CreateChatCompletionAsync(request);

            var applicationTip = new JobApplicationsAI()
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = 0
            };

            await _context.JobApplicationsAI.AddAsync(applicationTip);
            await _context.SaveChangesAsync();

            return result.ToString();
        }

        #region Local Functions
        private async Task<JobClientSummaryResponse> GetJobClientSummaryAsync(string clientId, string userId, int jobId)
        {
            AspNetUsers client;
            Mails mail = null;
            bool chatExist = false;
            using (var _context = new GoHireNowContext())
            {
                client = await _context.AspNetUsers.FirstOrDefaultAsync(x => x.Id == clientId);
                mail = await _context.Mails.FirstOrDefaultAsync(x => x.UserIdFrom == clientId &&
                                                                     x.UserIdTo == userId && x.IsDeleted == false &&
                                                                     x.JobId == jobId
                                                                     );
                chatExist = mail != null ? true : false;
            }

            if (client == null)
                return null;

            var res = new JobClientSummaryResponse()
            {
                Id = client.Id,
                CompanyName = client.Company,
                Logo = client.ProfilePicture,
                MemberSince = client.RegistrationDate,
                LastLoginDate = client.LastLoginTime,
                EnableMessage = chatExist,
                MailId = mail != null ? mail.Id : 0,
                ProfilePicturePath = !string.IsNullOrEmpty(client.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{client.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
            };

            return res;
        }

        private JobClientSummaryResponse GetJobClientSummary(string clientId, string userId)
        {
            AspNetUsers client;
            Mails mail = null;
            bool chatExist = false;
            using (var _context = new GoHireNowContext())
            {
                client = _context.AspNetUsers.FirstOrDefault(x => x.Id == clientId);
                mail = _context.Mails.FirstOrDefault(x => x.UserIdFrom == clientId && x.UserIdTo == userId && x.IsDeleted == false);
                chatExist = mail != null ? true : false;
            }

            if (client == null)
                return null;

            var res = new JobClientSummaryResponse()
            {
                Id = client.Id,
                CompanyName = client.Company,
                Logo = client.ProfilePicture,
                MemberSince = client.RegistrationDate,
                LastLoginDate = client.LastLoginTime,
                EnableMessage = chatExist,
                MailId = mail != null ? mail.Id : 0,
                ProfilePicturePath = !string.IsNullOrEmpty(client.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{client.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
            };

            return res;
        }

        public async Task<bool> InviteWorkers(InviteCompaniesRequest model, string workerId)
        {
            using (var _context = new GoHireNowContext())
            {
                var newInvite = new UserInvites()
                {
                    UserId = workerId,
                    InviteeName = model.user1,
                    InviteeEmail = model.email1,
                    Type = 1,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.UserInvites.AddAsync(newInvite);
                await _context.SaveChangesAsync();

                newInvite = new UserInvites()
                {
                    UserId = workerId,
                    InviteeName = model.user2,
                    InviteeEmail = model.email2,
                    Type = 1,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.UserInvites.AddAsync(newInvite);
                await _context.SaveChangesAsync();

                var worker = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == workerId && !u.IsDeleted);
                if (worker != null)
                {
                    var subject = worker.FullName + " invited you to eVirtualAssistants";
                    _contractService.PersonalEmailService(0, 54, model.email1, model.user1, subject, worker.FullName, model.user1, "no-reply.d@evirtualassistants.com", "eVirtualAssistants", 1, "InviteWorkerEmail.html");
                    _contractService.PersonalEmailService(0, 54, model.email2, model.user2, subject, worker.FullName, model.user2, "no-reply.d@evirtualassistants.com", "eVirtualAssistants", 1, "InviteWorkerEmail.html");
                }

                return true;
            }
        }

        private async Task<List<JobSummaryForWorkerResponse>> GetOtherClientJobs(string userId, int jobId, string loggedInUserId, int roleId)
        {
            List<Jobs> jobs;

            using (var _context = new GoHireNowContext())
            {
                jobs = await _context.Jobs.Include(o => o.User)
                    .Include(x => x.JobSkills)
                    .Where(x => x.UserId == userId && x.Id != jobId && x.JobStatusId != (int)JobStatusEnum.Closed && x.User.IsDeleted == false && x.User.IsSuspended == 0).OrderBy(j => j.CreateDate).Take(6).ToListAsync();

                var res = jobs.Select(j => new JobSummaryForWorkerResponse()
                {
                    Id = j.Id,
                    Title = !string.IsNullOrEmpty(loggedInUserId) ? !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(loggedInUserId, roleId, userId).Result) : null : j.Title.ReplaceGlobalJobTitleInformation(),
                    StatusId = j.JobStatusId,
                    Status = (j.JobStatusId > 0 && j.JobStatusId <= 3) ? j.JobStatusId.ToJobStatuseName() : "",
                    TypeId = j.JobTypeId,
                    Type = j.JobTypeId.ToJobTypeName(),
                    SalaryTypeId = j.SalaryTypeId,
                    SalaryType = j.SalaryTypeId.ToSalaryTypeName(),
                    Salary = j.Salary,
                    ActiveDate = j.ActiveDate,
                    Skills = j.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList(),
                    Client = GetJobClientSummary(j.UserId, userId),
                    IsFeatured = false,
                    IsUrgent = false,
                    IsPrivate = false
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

                    TimeSpan ts = (TimeSpan)(DateTime.UtcNow - item.ActiveDate);
                    var currentPlan = await _pricingService.GetCurrentPlan(item.Client.Id);

                    if (currentPlan != null && currentPlan.CreateDate < item.ActiveDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }

                return res;
            }

        }
        #endregion
    }
}
