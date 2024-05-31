using GoHireNow.Database;
using GoHireNow.Database.ComplexTypes;
using GoHireNow.Models.AccountModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.JobModels;
using GoHireNow.Models.JobsModels;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GoHireNow.Service.WorkerServices
{
    public class WorkerService : IWorkerService
    {
        private readonly IWorkerJobService _workerJobService;
        private readonly IUserRoleService _userRoleService;
        private readonly IContractService _contractService;
        private readonly IPricingService _pricingService;
        private IConfiguration _configuration { get; }

        public WorkerService(
            IConfiguration configuration, IContractService contractService, IWorkerJobService workerJobService, IUserRoleService userRoleService, IPricingService pricingService)
        {
            _configuration = configuration;
            _workerJobService = workerJobService;
            _contractService = contractService;
            _userRoleService = userRoleService;
            _pricingService = pricingService;
        }

        public async Task<WorkerDashboardResponse> GetDashboard(string userId, int roleId)
        {
            var appliedJobs = await _workerJobService.GetAppliedJobs(userId, roleId);
            var matchingJobs = await _workerJobService.GetMatchingJobs(userId, roleId);
            var latestJobs = await _workerJobService.GetNewJobs((int)JobStatusEnum.Published, userId, roleId);

            return new WorkerDashboardResponse()
            {
                AppliedJobs = appliedJobs,
                MatchingJobs = matchingJobs,
                LatestJobs = latestJobs
            };
        }

        public async Task<bool> AddYoutube(string userId, AddYoutubeModel model)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var newYoutube = new UserYoutubes
                    {
                        UserId = userId,
                        Url = model.url,
                        Name = model.name,
                        IsDeleted = 0,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _context.UserYoutubes.AddAsync(newYoutube);
                    await _context.SaveChangesAsync();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<int> AddReferenceForContract(string userId, AddReferenceForContractRequest request)
        {
            using (var _context = new GoHireNowContext())
            {
                var contract = await _context.Contracts.Where(x => x.Id == request.ContractId && x.IsDeleted == 0).FirstOrDefaultAsync();

                var newReference = new UserReferences
                {
                    UserId = request.UserId,
                    JobTitle = contract.Name,
                    FromDate = (DateTime)contract.CreatedDate,
                    ToDate = DateTime.UtcNow,
                    Company = request.Company,
                    Email = request.Email,
                    CreatedDate = DateTime.UtcNow,
                    Rating = request.Rate,
                    FeedBack = request.Feedback,
                    IsByInvitation = 0,
                    IsAccepted = 1,
                    IsDeleted = 0,
                };
                await _context.UserReferences.AddAsync(newReference);
                await _context.SaveChangesAsync();

                var worker = await _context.AspNetUsers.Where(x => x.Id == request.UserId && x.IsDeleted == false).FirstOrDefaultAsync();

                string subject = "You received a new feedback from " + request.Company;
                string headtitle = "New Feedback";
                string message = request.Company + " has posted a new feedback on your profile.";
                string description = "";
                string buttonurl = _configuration.GetValue<string>("WebDomain") + "/profile-work";
                string buttoncaption = "VIEW FEEDBACK";
                await _contractService.NewMailService(0, 38, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");

                return newReference.Id;
            }
        }

        public async Task<int> AddReference(string userId, AddReferenceRequest request)
        {
            using (var _context = new GoHireNowContext())
            {
                var InviteId = Guid.NewGuid();
                var newReference = new UserReferences
                {
                    UserId = userId,
                    JobTitle = request.JobTitle,
                    FromDate = DateTime.ParseExact(request.FromDate, "yyyy-MM", CultureInfo.InvariantCulture),
                    ToDate = DateTime.ParseExact(request.ToDate, "yyyy-MM", CultureInfo.InvariantCulture),
                    Company = request.CompanyName,
                    Contact = request.ContactName,
                    Email = request.Email,
                    CreatedDate = DateTime.UtcNow,
                    InviteID = InviteId,
                    IsByInvitation = 1,
                    IsDeleted = 0,
                };
                await _context.UserReferences.AddAsync(newReference);
                await _context.SaveChangesAsync();

                var worker = await _context.AspNetUsers.Where(x => x.Id == userId && x.IsDeleted == false).FirstOrDefaultAsync();
                if (worker != null)
                {
                    string subject = "We need references for " + worker.FullName;
                    string headtitle = "We need references for " + worker.FullName;
                    string message = "Hello " + request.ContactName + ", " + worker.FullName + " is currently looking for a job on our platform.<br/>It would really help if you could leave a feedback from " + request.CompanyName;
                    string description = "Reference: " + request.JobTitle + ", " + request.FromDate + " ~ " + request.ToDate + ", " + request.CompanyName + ".<br/>It only takes 30 seconds to leave a feedback on " + worker.FullName + "'s profile.";
                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/work-profile/" + userId + "/" + InviteId;
                    string buttoncaption = "Leave Feedback";
                    string img = !string.IsNullOrEmpty(worker.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{worker.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}";
                    await _contractService.NewMailService(0, 50, request.Email, request.CompanyName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ReferenceRequireTemplate.html", "", "", img);

                    newReference.IsInvited = 1;
                    await _context.SaveChangesAsync();
                }

                return newReference.Id;
            }
        }

        public async Task<bool> AddPortifolios(string userId, string path, IFormFile[] portifolios)
        {
            using (var _context = new GoHireNowContext())
            {
                foreach (var file in portifolios)
                {
                    try
                    {
                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{file.FileName}";
                        using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        ISupportedImageFormat format = new JpegFormat { Quality = 70 };
                        Size size = new Size(150, 150);
                        var resizeLayer = new ResizeLayer(size, ResizeMode.Min);
                        byte[] photoBytes = File.ReadAllBytes(path + fileName);
                        try
                        {
                            using (MemoryStream inStream = new MemoryStream(photoBytes))
                            {
                                using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                                {
                                    imageFactory.Load(inStream)
                                                .Resize(resizeLayer)
                                                .Format(format)
                                                .Save(path + "thumb_" + fileName);
                                }
                            }

                        }
                        catch { }

                        var title = file.Name.Split("_");
                        var portifolio = new UserPortfolios
                        {
                            UserId = userId,
                            Description = file.Name,
                            Title = title[1], //fileName,
                            Link = $"{fileName}",
                            IsDeleted = false,
                            CreateDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow
                        };
                        await _context.UserPortfolios.AddAsync(portifolio);
                    }
                    catch { }
                }
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<int> DeleteYoutube(string userId, int id)
        {
            using (var _context = new GoHireNowContext())
            {
                var youtube = await _context.UserYoutubes.FirstOrDefaultAsync(x => x.Id == id);
                if (youtube == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Youtube not found");

                if (youtube.UserId != userId)
                    throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have permission to delete this Youtube");

                if (youtube != null)
                {
                    youtube.IsDeleted = 1;
                    await _context.SaveChangesAsync();
                    return youtube.Id;
                }
                return 0;
            }
        }

        public async Task<int> DeletePortifolio(string userId, int id)
        {
            using (var _context = new GoHireNowContext())
            {
                var portifolio = await _context.UserPortfolios.FirstOrDefaultAsync(x => x.Id == id);
                if (portifolio == null)
                    throw new CustomException((int)HttpStatusCode.NotFound, "Portifolio not found");

                if (portifolio.UserId != userId)
                    throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have permission to delete this Portifolio");

                if (portifolio != null)
                {
                    portifolio.IsDeleted = true;
                    await _context.SaveChangesAsync();
                    return portifolio.Id;
                }
                return 0;
            }
        }

        public async Task<List<SkillResponse>> GetWorkerSkills(string userId)
        {
            List<int> userSkillIds;
            using (var _context = new GoHireNowContext())
            {
                userSkillIds = await _context.UserSkills.Where(x => x.UserId == userId).Select(x => x.SkillId).ToListAsync();

                if (userSkillIds == null)
                    return null;
            }

            return LookupService.GetSkillsById(userSkillIds);
            // return LookupService.GetSkillsCatById(userSkillIds);
        }

        public async Task<List<PortfolioResponse>> GetPortfolios(string userId, bool isFull = true)
        {
            using (var _context = new GoHireNowContext())
            {
                var portfolios = await _context.UserPortfolios.Where(x => x.UserId == userId && x.IsDeleted != true)
                    .Select(x => new PortfolioResponse()
                    {
                        Id = x.Id,
                        FileName = x.Title,
                        FilePath = isFull ? $"{LookupService.FilePaths.PortfolioFileUrl}{x.UserId}/{x.Link}" : null,
                        Thumbnail = (isFull && LookupService.GetFileImage(Path.GetExtension(x.Link), "") == "img")
                                    ? $"{LookupService.FilePaths.PortfolioFileUrl}{x.UserId}/thumb_{x.Link}"
                                    : "",
                        FileExtension = LookupService.GetFileImage(Path.GetExtension(x.Link), "") != "img"
                                        ? Path.GetExtension(x.Link).Replace(".", "")
                                        : ""
                    }).ToListAsync();

                return portfolios;
            }
        }

        public async Task<List<YoutubeResponse>> GetYoutubes(string userId, bool isFull = true)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var Youtubes = await _context.UserYoutubes.Where(x => x.UserId == userId && x.IsDeleted != 1)
                        .Select(x => new YoutubeResponse()
                        {
                            Id = x.Id,
                            Url = isFull ? x.Url : null,
                            Name = x.Name
                        }).ToListAsync();

                    return Youtubes;
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<WorkerSearchJobResponse> SearchJobs(SearchJobRequest model, string rootPath, int roleId, string userId)
        {
            model.size = model.size > 0 ? model.size : 10;
            int skip = model.page < 1 ? 0 : (model.page * model.size) - model.size;
            List<int> searchSkillIds = new List<int>();

            using (var _context = new GoHireNowContext())
            {
                var query = _context.Jobs
                    .Include(x => x.JobSkills)
                    .Include(x => x.User)
                    .Where(x => x.IsDeleted == false && x.IsActive == true && x.JobStatusId == (int)JobStatusEnum.Published && x.User.IsDeleted == false && x.User.IsSuspended == 0);

                if (!string.IsNullOrEmpty(model.Keyword))
                {
                    var keywordSkillIds = LookupService.GlobalSkills
                        .Where(s => model.Keyword.Contains(s.Name, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Id).ToList();
                    query = query.Where(x =>
                                            (!string.IsNullOrEmpty(x.Title) && x.Title.Contains(model.Keyword, StringComparison.OrdinalIgnoreCase)) ||
                                            (!string.IsNullOrEmpty(x.Description) && x.Description.Contains(model.Keyword, StringComparison.OrdinalIgnoreCase)) ||
                                            (keywordSkillIds.Count > 0 && x.JobSkills.Any(j => keywordSkillIds.Contains(j.SkillId)))
                                        );
                }

                if (!string.IsNullOrEmpty(model.SkillIds))
                {
                    var skills = model.SkillIds.Split(",").Select(x => Convert.ToInt32(x)).ToArray();
                    if (skills.Count() > 0)
                        searchSkillIds = skills.ToList();

                    if (searchSkillIds.Count > 0)
                        query = query.Where(x => x.JobSkills.Select(y => y.SkillId).Intersect(searchSkillIds).Count() == searchSkillIds.Count());

                    //If you want to select any of the given skills then uncomment these line and comment lines above withing this if block
                    //var skills = model.SkillIds.Split(",").Select(x => Convert.ToInt32(x)).ToList();
                    //query = query.Where(x => x.JobSkills.Any(s => skills.Contains(s.SkillId)));
                }

                if (model.CountryId.HasValue && model.CountryId.Value > 0)
                    query = query.Where(x => x.UserId != null && x.User.CountryId.Value == model.CountryId.Value);

                if (model.WorkerTypeId.HasValue && model.WorkerTypeId.Value > 0)
                    query = query.Where(x => x.JobTypeId == model.WorkerTypeId.Value);

                if (model.MinSalary.HasValue && model.MinSalary.Value >= 0)
                    query = query.Where(x => x.Salary >= model.MinSalary.Value);

                if (model.MaxSalary.HasValue && model.MaxSalary.Value > 0)
                    query = query.Where(x => x.Salary <= model.MaxSalary.Value);
                var totalJobs = query.Count();
                var Jobs = await query
                    .OrderByDescending(o => (o.ModifiedDate != null ? o.ModifiedDate : o.CreateDate))
                    .Skip(skip).Take(model.size)
                    .Select(x => new JobSummaryForWorkerResponse()
                    {
                        Id = x.Id,
                        ActiveDate = x.ActiveDate,
                        Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(x.Title) ? x.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, x.UserId).Result) : null : x.Title.ReplaceGlobalJobTitleInformation(),
                        Description = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(x.Description) ? x.Description.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, x.UserId).Result) : null : x.Description.ReplaceGlobalJobTitleInformation(),
                        Duration = x.Duration,
                        Salary = x.Salary,
                        SalaryTypeId = x.SalaryTypeId,
                        SalaryType = x.SalaryTypeId.ToSalaryTypeName(),
                        Status = x.JobStatusId.ToJobStatuseName(),
                        StatusId = x.JobStatusId,
                        Type = x.JobTypeId.ToJobTypeName(),
                        TypeId = x.JobTypeId,
                        ProfilePicturePath = x.UserId != null && !string.IsNullOrEmpty(x.User.ProfilePicture)
                                        ? $"{rootPath}/Profile-Pictures/{x.User.ProfilePicture}"
                                        : null,
                        Skills = x.JobSkills.Count > 0
                                        ? LookupService.GetSkillsById(x.JobSkills.Select(s => s.SkillId).ToList())
                                        : new List<SkillResponse>(),
                        Client = new JobClientSummaryResponse()
                        {
                            Id = x.UserId,
                            CompanyName = x.User.Company,
                            Logo = x.User.ProfilePicture,
                            MemberSince = x.User.RegistrationDate,
                            LastLoginDate = x.User.LastLoginTime,
                            ProfilePicturePath = !string.IsNullOrEmpty(x.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{x.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
                        },
                        IsFeatured = false,
                        IsUrgent = false,
                        IsPrivate = false,
                    }).ToListAsync();

                foreach (var item in Jobs)
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

                var searchJobModel = new WorkerSearchJobResponse()
                {
                    TotalJobs = totalJobs,
                    Jobs = Jobs
                };

                //POC                
                //var newJobs = new List<JobSummaryForWorkerResponse>();
                //List<int> jsIds = new List<int>();
                //bool hasAllSkills = false;
                //foreach (var job in jobList)
                //{
                //    jsIds = job.SkillIds;
                //    hasAllSkills = searchSkillIds.Intersect(jsIds).Count() == searchSkillIds.Count();
                //    if (hasAllSkills)
                //    {
                //        newJobs.Add(job);
                //    }
                //}

                return searchJobModel;
            }
        }

        public async Task<WorkerFeaturedJobResponse> FeaturedJobs(SearchJobRequest model, string rootPath, int roleId, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var jobs = await _context.Jobs
                    .Include(x => x.JobSkills)
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && (bool)x.IsActive && x.JobStatusId == (int)JobStatusEnum.Published && !x.User.IsDeleted && x.User.IsSuspended == 0)
                    .OrderByDescending(o => o.ModifiedDate ?? o.CreateDate)
                    .ToListAsync();

                var result = new List<JobSummaryForWorkerResponse>();
                foreach (var item in jobs)
                {
                    var currentPlan = await _pricingService.GetCurrentPlan(item.UserId);
                    if (currentPlan != null && currentPlan.CreateDate < item.CreateDate && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && (DateTime.UtcNow - item.CreateDate).TotalDays < 30)
                    {
                        var job = new JobSummaryForWorkerResponse()
                        {
                            Id = item.Id,
                            ActiveDate = item.ActiveDate,
                            Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(item.Title) ? item.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, item.UserId).Result) : null : item.Title.ReplaceGlobalJobTitleInformation(),
                            Description = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(item.Description) ? item.Description.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, item.UserId).Result) : null : item.Description.ReplaceGlobalJobTitleInformation(),
                            Duration = item.Duration,
                            Salary = item.Salary,
                            SalaryTypeId = item.SalaryTypeId,
                            SalaryType = item.SalaryTypeId.ToSalaryTypeName(),
                            Status = item.JobStatusId.ToJobStatuseName(),
                            StatusId = item.JobStatusId,
                            Type = item.JobTypeId.ToJobTypeName(),
                            TypeId = item.JobTypeId,
                            ProfilePicturePath = item.UserId != null && !string.IsNullOrEmpty(item.User.ProfilePicture)
                                                ? $"{rootPath}/Profile-Pictures/{item.User.ProfilePicture}"
                                                : null,
                            Skills = item.JobSkills.Count > 0
                                                ? LookupService.GetSkillsById(item.JobSkills.Select(s => s.SkillId).ToList())
                                                : new List<SkillResponse>(),
                            Client = new JobClientSummaryResponse()
                            {
                                Id = item.UserId,
                                CompanyName = item.User.Company,
                                Logo = item.User.ProfilePicture,
                                MemberSince = item.User.RegistrationDate,
                                LastLoginDate = item.User.LastLoginTime,
                                ProfilePicturePath = !string.IsNullOrEmpty(item.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{item.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
                            },
                            IsFeatured = true,
                            IsUrgent = false,
                            IsPrivate = false,
                        };

                        result.Add(job);
                        continue;
                    }

                    var transaction = _context.Transactions
                        .Include(x => x.GlobalPlan)
                        .Where(x => x.CustomId == item.Id && x.CustomType == 1 && x.Status == "paid" && !x.IsDeleted && x.GlobalPlanId.ToGlobalPlanName() == "Featured" && (DateTime.UtcNow - x.CreateDate).TotalDays < 30)
                        .FirstOrDefault();

                    if (transaction != null)
                    {
                        var job = new JobSummaryForWorkerResponse()
                        {
                            Id = item.Id,
                            ActiveDate = item.ActiveDate,
                            Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(item.Title) ? item.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, item.UserId).Result) : null : item.Title.ReplaceGlobalJobTitleInformation(),
                            Description = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(item.Description) ? item.Description.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, item.UserId).Result) : null : item.Description.ReplaceGlobalJobTitleInformation(),
                            Duration = item.Duration,
                            Salary = item.Salary,
                            SalaryTypeId = item.SalaryTypeId,
                            SalaryType = item.SalaryTypeId.ToSalaryTypeName(),
                            Status = item.JobStatusId.ToJobStatuseName(),
                            StatusId = item.JobStatusId,
                            Type = item.JobTypeId.ToJobTypeName(),
                            TypeId = item.JobTypeId,
                            ProfilePicturePath = item.UserId != null && !string.IsNullOrEmpty(item.User.ProfilePicture)
                                                ? $"{rootPath}/Profile-Pictures/{item.User.ProfilePicture}"
                                                : null,
                            Skills = item.JobSkills.Count > 0
                                                ? LookupService.GetSkillsById(item.JobSkills.Select(s => s.SkillId).ToList())
                                                : new List<SkillResponse>(),
                            Client = new JobClientSummaryResponse()
                            {
                                Id = item.UserId,
                                CompanyName = item.User.Company,
                                Logo = item.User.ProfilePicture,
                                MemberSince = item.User.RegistrationDate,
                                LastLoginDate = item.User.LastLoginTime,
                                ProfilePicturePath = !string.IsNullOrEmpty(item.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{item.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
                            },
                            IsFeatured = true,
                            IsUrgent = false,
                            IsPrivate = false,
                        };

                        result.Add(job);
                    }
                }

                Random rnd = new Random();
                var shuffledResult = result.OrderBy(x => rnd.Next()).ToList();

                var searchJobModel = new WorkerFeaturedJobResponse()
                {
                    FeaturedJobs = shuffledResult.Take(4).ToList()
                };

                return searchJobModel;
            }
        }

        public async Task<bool> ChatExist(string userId, string clientId)
        {
            bool chatExist = false;
            using (var _context = new GoHireNowContext())
            {
                var mail = await _context.Mails.FirstOrDefaultAsync(x => x.UserIdFrom == clientId && x.UserIdTo == userId && x.IsDeleted == false);
                chatExist = mail != null ? true : false;
            }
            return chatExist;
        }

        public async Task<spGetGlobalGroupByCountry> GetGlobalGroupByCountry(int countryId)
        {
            using (var _context = new GoHireNowContext())
            {
                var globalGroup = await _context.spGetGlobalGroupByCountry.FromSql("spGetGlobalGroupByCountry @CountryId = {0}", countryId).FirstOrDefaultAsync();
                return globalGroup;
            }
        }

        public async Task<spGetTotalAccountBalanced> GetAccountBalance(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var result = await _context.spGetTotalAccountBalanced.FromSql("spGetTotalAccountBalanced @UserID = {0}", userId).FirstOrDefaultAsync();
                return result;
            }
        }

        public async Task<WorkerProfileProgressResponse> GetProfileProgress(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.AspNetUsers
                    .Include(x => x.UserSkills)
                    .Include(x => x.UserPortfolios)
                    .Include(x => x.JobApplications)
                    .FirstOrDefaultAsync(x => x.Id == userId && x.UserType == 2);
                if (user == null)
                    throw new CustomException((int)StatusCodes.Status404NotFound, "User not found");

                int progress = 0;
                var response = new WorkerProfileProgressResponse();
                response.Id = user.Id;
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    progress += 20;
                    response.ProfilePicture = true;
                }

                if (!string.IsNullOrEmpty(user.UserTitle))
                {
                    progress += 20;
                    response.Title = true;
                }

                if (!string.IsNullOrEmpty(user.Description))
                {
                    progress += 20;
                    response.Description = true;
                }

                //if (!string.IsNullOrEmpty(user.UserSalary))
                //{
                //    progress += 5;
                //    response.Salary = true;
                //}

                //if (!string.IsNullOrEmpty(user.UserAvailiblity))
                //{
                //    if (user.UserAvailiblity != "0")
                //    {
                //        progress += 5;
                //        response.Availability = true;
                //    }
                //}

                //if (!string.IsNullOrEmpty(user.Education))
                //{
                //    progress += 5;
                //    response.Education = true;
                //}

                //if (!string.IsNullOrEmpty(user.Experience))
                //{
                //    progress += 5;
                //    response.Experience = true;
                //}

                if (user.UserSkills.Count() > 0)
                {
                    progress += 20;
                    response.Skills = true;
                }

                if (user.UserPortfolios.Count() > 0)
                {
                    progress += 10;
                    response.Portfolio = true;
                }

                var applicationCount = await _context.JobApplications.CountAsync(x => x.UserId == userId);

                if (applicationCount > 0)
                {
                    progress += 10;
                    response.AppliedJob = true;
                }
                response.Progress = progress;
                return response;
            }

        }

        public async Task<List<JobSummaryResponse>> GetJobs(string userId, int page, int size, int status, int roleId, string workerId, bool isactive = true)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;

            List<Jobs> jobs;

            using (var _context = new GoHireNowContext())
            {
                jobs = await _context.Jobs.Include(o => o.User)
                            .Include(x => x.JobApplications).Include(x => x.User)
                            .Where(x => x.UserId == userId && x.JobStatusId == status && x.IsActive == true && x.IsDeleted == false && x.User.IsDeleted == false)
                            .OrderByDescending(j => j.CreateDate)
                            .ToListAsync();
            }

            if (jobs == null || jobs.Count() == 0)
                return null;

            var currentPlan = await _pricingService.GetCurrentPlan(userId);
            int allowedApplicants = currentPlan != null ? currentPlan.MaxApplicants : 0;

            var res = jobs.Select(j => new JobSummaryResponse
            {
                Id = j.Id,
                UserId = j.UserId,
                Title = !string.IsNullOrEmpty(workerId) ? !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, roleId == (int)UserTypeEnum.Client ? false : _userRoleService
                .TextFilterCondition(roleId == (int)UserTypeEnum.Client ? userId : workerId, roleId, roleId == (int)UserTypeEnum.Client ? workerId : userId).Result) : null : j.Title.ReplaceGlobalJobTitleInformation(),
                ApplicationCount = j.JobApplications.Where(o => o.Job.User.IsDeleted == false).Count(),
                AllowedApplicantions = allowedApplicants,
                StatusId = j.JobStatusId,
                Status = (j.JobStatusId > 0 && j.JobStatusId <= 3) ? j.JobStatusId.ToJobStatuseName() : "",
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

        public async Task<List<JobTitleRelatedGlobalJobsListModel>> GetSkillsRelatedJobs(string skillIds, string userId, int roleId)
        {
            using (var _context = new GoHireNowContext())
            {
                var jobs = await _context.spGetSkillsRelatedJobs.FromSql("spGetJobsWithSkills @skillIds = {0}", skillIds).OrderByDescending(o => o.LastLoginTime).ToListAsync();
                if (jobs.Count() > 0)
                {
                    var jobsList = jobs.Select(j => new JobTitleRelatedGlobalJobsListModel()
                    {
                        Id = j.Id,
                        Title = !string.IsNullOrEmpty(j.Title) ? j.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, j.ClientId).Result) : null,
                        StatusId = j.JobStatusId,
                        Status = (j.JobStatusId > 0 && j.JobStatusId <= 3) ? j.JobStatusId.ToJobStatuseName() : "",
                        TypeId = j.JobTypeId,
                        Type = j.JobTypeId.ToJobTypeName(),
                        SalaryTypeId = j.SalaryTypeId,
                        SalaryType = j.SalaryTypeId.ToSalaryTypeName(),
                        Salary = j.Salary,
                        ActiveDate = j.ActiveDate,
                        ProfilePicturePath = !string.IsNullOrEmpty(j.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}",
                        Skills = LookupService.GetSkillsById(j.JobSkills.Split(',').Select(int.Parse).ToList()),
                        Client = new JobClientSummaryResponse()
                        {
                            Id = j.ClientId,
                            CountryName = j.CountryId.HasValue ? j.CountryId.Value.ToCountryName() : string.Empty,
                            UserUniqueId = j.UserUniqueId,
                            CompanyName = j.CompanyName,
                            Logo = j.ProfilePicture,
                            MemberSince = j.RegistrationDate,
                            LastLoginDate = j.LastLoginTime,
                            ProfilePicturePath = !string.IsNullOrEmpty(j.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{j.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
                        }
                    }).ToList();
                    return jobsList;
                }
                return null;
            }
        }

        public async Task<List<UserNotifications>> GetLastNotifications(string userId)
        {
            var _context = new GoHireNowContext();
            var notifications = await _context.UserNotifications
                .Where(n => n.UserId == userId && n.IsDelerte == 0)
                .OrderByDescending(n => n.CreatedDate)
                .Take(3)
                .ToListAsync();

            return notifications;
        }

        public async Task<List<UserNotifications>> GetNotifications(string userId)
        {
            var _context = new GoHireNowContext();
            var notifications = await _context.UserNotifications
                .Where(n => n.UserId == userId && n.IsDelerte == 0)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return notifications;
        }

        public async Task<List<Academy>> GetAcademies(string userId)
        {
            var _context = new GoHireNowContext();
            var academies = await _context.Academy.Where(a => a.IsDeleted == 0).ToListAsync();
            foreach (var academy in academies)
            {
                academy.CoverImg = LookupService.FilePaths.AcademyUrl + "covers/" + academy.CoverImg;
            }
            return academies;
        }
    }
}
