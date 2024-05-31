using GoHireNow.Database;
using GoHireNow.Database.ComplexTypes;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.ClientServices
{
    public class ClientService : IClientService
    {
        private readonly IContractService _contractService;
        private readonly IFavoritesService _favoritesService;
        private readonly IWorkerService _workerService;
        private readonly IUserRoleService _userRoleService;
        public ClientService(
            IContractService contractService,
            IFavoritesService favoritesService,
            IWorkerService workerService,
            IUserRoleService userRoleService)
        {
            _contractService = contractService;
            _favoritesService = favoritesService;
            _workerService = workerService;
            _userRoleService = userRoleService;
        }

        public async Task<ClientDashboardResponse> GetDashboard(string userId, int roleId)
        {
            var favoriteWorkers = await _favoritesService.GetFavoriteWorkersNew(userId, roleId, 1, 5);
            var relatedWorkers = await GetRelatedWorker(userId, roleId, 1, 8);
            ClientDashboardResponse clientDashboardResponse = new ClientDashboardResponse()
            {
                // ActiveJobs = activeJobs,
                FavoriteWorkers = favoriteWorkers,
                RelatedWorkers = relatedWorkers
            };

            return clientDashboardResponse;
        }

        public async Task<List<Transactions>> GetClientTransaction(string id)
        {
            using (var _context = new GoHireNowContext())
            {
                return await _context.Transactions
                    .Include(x => x.GlobalPlan)
                    .Where(x => x.UserId == id).ToListAsync();

            }
        }

        public async Task<List<WorkerSummaryForClientResponse>> GetWorkersByIds(List<string> ids, int roleId, string userId)
        {
            List<AspNetUsers> allUsers;
            using (var _context = new GoHireNowContext())
            {
                allUsers = await _context.AspNetUsers
                .Include(x => x.UserSkills)
                .Where(x => ids.Contains(x.Id)).ToListAsync();
            }

            if (allUsers == null)
                return null;

            var allWorkers = new List<WorkerSummaryForClientResponse>();
            var worker = new WorkerSummaryForClientResponse();
            List<SkillResponse> skillsList = new List<SkillResponse>();
            foreach (var u in allUsers)
            {
                try
                {
                    worker = new WorkerSummaryForClientResponse();
                    worker.UserId = u.Id;
                    worker.UserUniqueId = u.UserUniqueId;
                    worker.Name = u.FullName;
                    worker.Title = !string.IsNullOrEmpty(u.UserTitle) ? u.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, u.Id).Result) : null;
                    worker.CountryId = u.CountryId;
                    worker.CountryName = u.CountryId.HasValue ? worker.CountryId.Value.ToCountryName() : string.Empty;
                    worker.Salary = u.UserSalary;
                    //worker.SalaryTypeId = u.UserSalary;
                    worker.LastLoginDate = u.LastLoginTime;
                    List<int> skillIds = (u.UserSkills != null && u.UserSkills.Count() > 0) ? u.UserSkills.Select(x => x.SkillId).ToList() : null; ;
                    skillsList = skillIds != null && skillIds.Count() > 0 ? LookupService.GetSkillsById(skillIds) : null;
                    worker.Skills = skillsList;
                    allWorkers.Add(worker);
                }
                catch (Exception)
                {

                }
            }
            return allWorkers;

        }

        public async Task<List<WorkerSummaryForClientResponse>> GetRelatedWorker(string clientId, int roleId, int page = 1, int size = 5)
        {
            int skip = page > 1 ? ((page - 1) * size) : 0;
            using (var _context = new GoHireNowContext())
            {
                var workers = await (from ws in _context.AspNetUsers
                                     join us in _context.UserSkills on ws.Id equals us.UserId
                                     join sk in _context.GlobalSkills on us.SkillId equals sk.Id
                                     select ws).Distinct()
                    .Where(o => o.IsDeleted == false && o.IsSuspended == 0)
                    .Where(o => o.Rating > 3.9m)
                    .OrderByDescending(x => x.LastLoginTime)
                    .Skip(skip).Take(size)
                .Select(x => new WorkerSummaryForClientResponse()
                {
                    UserId = x.Id,
                    UserUniqueId = x.UserUniqueId,
                    CountryId = x.CountryId,
                    CountryName = x.CountryId.Value.ToCountryName(),
                    LastLoginDate = x.LastLoginTime,
                    Education = x.Education,
                    Experience = x.Experience,
                    featured = x.Featured,
                    rating = x.Rating,
                    Name = x.FullName,
                    Salary = x.UserSalary,
                    SalaryTypeId = (int)SalaryTypeEnum.Monthly,
                    Title = !string.IsNullOrEmpty(x.UserTitle) ? x.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(clientId, roleId, x.Id).Result) : null,
                    Skills = LookupService.GetSkillsById(x.UserSkills.Select(s => s.SkillId).ToList()),
                    ProfilePicturePath = !string.IsNullOrEmpty(x.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{x.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}"
                }).ToListAsync();

                return workers;
            }
        }

        public async Task<ClientSearchWorkerResponse> SearchWorkers(SearchWorkerRequest model, int roleId, string userId)
        {
            try
            {
                model.size = model.size > 0 ? model.size : 10;
                int skip = model.page < 1 ? 0 : (model.page * model.size) - model.size;
                using (var _context = new GoHireNowContext())
                {
                    var query = await _context.spSearchWorkers.FromSql("spSearchWorkers @Keyword = {0},@SkillIds = {1},@CountryId = {2},@WorkerTypeId = {3}," +
                        "@MinSalary = {4},@MaxSalary = {5},@Take = {6},@Skip = {7}, @Education = {8}, @Experience = {9}, @MinRating = {10}", model.Keyword, model.SkillIds, model.CountryId.HasValue ? model.CountryId.Value : 0, model.WorkerTypeId.HasValue ? model.WorkerTypeId.Value : 0,
                        model.MinSalary.HasValue ? model.MinSalary.Value : 0, model.MaxSalary.HasValue ? model.MaxSalary.Value : 0, model.size, skip, model.Education, model.Experience, model.MinRating).ToListAsync();

                    var clientWorkerModel = new ClientSearchWorkerResponse()
                    {
                        TotalWorkers = query.Count() > 0 ? query.FirstOrDefault().TotalWorkers : 0,
                        Workers = query.OrderByDescending(o => o.rating)
                            .Select(x =>
                            {
                                var user = _context.AspNetUsers.Include(u => u.UserReferences).Where(u => u.Id == x.UserId && u.IsDeleted == false).FirstOrDefault();

                                return new WorkerSummaryForClientResponse()
                                {
                                    UserId = x.UserId,
                                    UserUniqueId = x.UserUniqueId,
                                    CountryId = x.CountryId,
                                    CountryName = x.CountryId.Value.ToCountryName(),
                                    LastLoginDate = x.LastLoginTime,
                                    Name = x.FullName,
                                    Education = x.Education,
                                    Experience = x.Experience,
                                    featured = x.featured,
                                    rating = x.rating,
                                    Salary = x.UserSalary,
                                    SalaryTypeId = (int)SalaryTypeEnum.Monthly,
                                    Availability = x.UserAvailiblity.ToAvailabilityType(),
                                    Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(x.UserTitle) ? x.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, x.UserId).Result) : null : x.UserTitle.ReplaceGlobalJobTitleInformation(),
                                    Skills = LookupService.GetSkillsById(x.WorkerSkills.Split(',').Select(int.Parse).ToList()),
                                    ProfilePicturePath = !string.IsNullOrEmpty(x.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{x.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}",
                                    ReferencesCount = user.UserReferences.Where(z => z.IsAccepted == 1 && z.IsDeleted == 0).ToList().Count()
                                };
                            }).ToList()
                    };
                    return clientWorkerModel;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<WorkerSummaryForClientResponse>> FeaturedWorkers(SearchWorkerRequest model, int roleId, string userId)
        {
            var _context = new GoHireNowContext();
            var workers = await _context.AspNetUsers.Include(u => u.UserReferences).Where(u => !u.IsDeleted && u.IsSuspended == 0 && u.Rating >= 4.5m).ToListAsync();

            var result = new List<WorkerSummaryForClientResponse>();
            foreach (var worker in workers)
            {
                var currentPlan = await _context.sp_getWorkerSubscription.FromSql("sp_getWorkerSubscription @userID = {0}", worker.Id).FirstOrDefaultAsync();
                if (currentPlan != null && (currentPlan.planid == (int)GlobalPlanEnum.Rockstar || currentPlan.planid == (int)GlobalPlanEnum.RockstarByBalance))
                {
                    var temp = new WorkerSummaryForClientResponse()
                    {
                        UserId = worker.Id,
                        UserUniqueId = worker.UserUniqueId,
                        CountryId = worker.CountryId,
                        CountryName = worker.CountryId.Value.ToCountryName(),
                        LastLoginDate = worker.LastLoginTime,
                        Name = worker.FullName,
                        Education = worker.Education,
                        Experience = worker.Experience,
                        featured = worker.Featured,
                        rating = worker.Rating,
                        Salary = worker.UserSalary,
                        SalaryTypeId = (int)SalaryTypeEnum.Monthly,
                        Availability = worker.UserAvailiblity.ToAvailabilityType(),
                        Title = !string.IsNullOrEmpty(userId) ? !string.IsNullOrEmpty(worker.UserTitle) ? worker.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, worker.Id).Result) : null : worker.UserTitle.ReplaceGlobalJobTitleInformation(),
                        Skills = await _workerService.GetWorkerSkills(worker.Id),
                        ProfilePicturePath = !string.IsNullOrEmpty(worker.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{worker.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}",
                        ReferencesCount = worker.UserReferences.Where(z => z.IsAccepted == 1 && z.IsDeleted == 0).ToList().Count()
                    };

                    result.Add(temp);
                }
            }

            Random rnd = new Random();
            var shuffledResult = result.OrderBy(x => rnd.Next()).ToList();

            return shuffledResult.Take(2).ToList();
        }

        private bool HasSearchParameters(SearchWorkerRequest model)
        {
            bool hasParameter = false;
            if (!string.IsNullOrEmpty(model.Keyword))
                hasParameter = true;

            if (!string.IsNullOrEmpty(model.SkillIds))
                hasParameter = true;

            if (model.CountryId.HasValue && model.CountryId > 0)
                hasParameter = true;

            if (model.WorkerTypeId.HasValue && model.WorkerTypeId > 0)
                hasParameter = true;

            if (model.MinSalary.HasValue && model.MinSalary > 0)
                hasParameter = true;

            if (model.MaxSalary.HasValue && model.MaxSalary > 0)
                hasParameter = true;

            return hasParameter;
        }

        public async Task<ClientProfileProgressResponse> GetProfileProgress(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.AspNetUsers
                    .Include(x => x.Jobs)
                    .FirstOrDefaultAsync(x => x.Id == userId && x.UserType == 1);
                if (user == null)
                    throw new CustomException((int)StatusCodes.Status404NotFound, "User not found");

                int progress = 0;
                var response = new ClientProfileProgressResponse();
                response.Id = user.Id;
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    progress += 20;
                    response.Logo = true;
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

                if (user.Jobs.Count() > 0)
                {
                    progress += 20;
                    response.Jobs = true;
                }

                if (await _context.Transactions.CountAsync(x => x.UserId == userId) > 0)
                {
                    progress += 20;
                    response.Paid = true;
                }
                response.Progress = progress;
                return response;
            }

        }

        public async Task<bool> UpdateToFreePlan(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    await _context.Database.ExecuteSqlCommandAsync("spUpdateUserPricingPlanToFree @UserId = {0}", userId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> ProcessCurrentPricingPlan(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    await _context.Database.ExecuteSqlCommandAsync("spProcessCurrentPricingPlan @UserId = {0}", userId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<HRProfilesResponse>> GetHRAccounts(int size, int page)
        {
            using (var _context = new GoHireNowContext())
            {
                var skip = page > 1 ? (page - 1) * size : 0;
                var profiles = await _context.UserHRProfile.Include(u => u.User).Skip(skip).Take(size).ToListAsync();
                var response = new List<HRProfilesResponse>();
                foreach (var profile in profiles)
                {
                    var item = new HRProfilesResponse()
                    {
                        Id = profile.User.Id,
                        Name = profile.User?.FullName ?? string.Empty,
                        Picture = (!string.IsNullOrEmpty(profile.User?.ProfilePicture)) ? $"{LookupService.FilePaths.ProfilePictureUrl}{profile.User.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}",
                        Title = profile.User?.UserTitle ?? string.Empty,
                        Description = profile.HRDesc1
                    };
                    response.Add(item);
                }

                return response;
            }
        }

        public async Task<ClientHRProfileResponse> GetHRAccountDetail(string id)
        {
            using (var _context = new GoHireNowContext())
            {
                var profile = await _context.UserHRProfile.FirstOrDefaultAsync(x => x.UserId == id);
                if (profile == null)
                {
                    return null;
                }

                var user = await _context.AspNetUsers.Where(u => u.Id == id && !u.IsDeleted).FirstOrDefaultAsync();

                var extras = await _context.UserHRProfile.Where(x => x.UserId != id).OrderBy(x => Guid.NewGuid()).Take(3).ToListAsync();
                var languages = await _context.UserHRLanguages.Where(x => x.UserId == id).ToListAsync();
                var reviews = await _context.UserHRReviews.Where(x => x.UserId == id).ToListAsync();
                var skills = await _context.UserHRSkills.Where(x => x.UserId == id).ToListAsync();

                var response = new ClientHRProfileResponse()
                {
                    Id = id,
                    Name = user?.FullName ?? string.Empty,
                    Picture = (!string.IsNullOrEmpty(user?.ProfilePicture)) ? $"{LookupService.FilePaths.ProfilePictureUrl}{user.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}",
                    CountryName = (user?.CountryId.HasValue == true) ? user.CountryId.Value.ToCountryName() : string.Empty,
                    HRTitle1 = profile.HRTitle1,
                    HRTitle2 = profile.HRTitle2,
                    HRTitle3 = profile.HRTitle3,
                    HRDesc1 = profile.HRDesc1,
                    HRDesc2 = profile.HRDesc2,
                    HRDesc3 = profile.HRDesc3,
                    HRPrice = profile.HRPrice,
                    HRVideo = profile.HRVideo,
                    HRWpm = profile.HRWpm,
                    HRMbps = profile.HRMbps,
                    HRNotes = profile.HRNotes,
                    HRPosition = profile.HRPosition,
                    HRHours = profile.HRHours,
                    HRType = profile.HRType,
                    HRLanguages = languages.Select((s) => new HRLanguageResponse()
                    {
                        HRLang = s.HRLang,
                        HRSpeak = s.HRSpeak,
                        HRWrite = s.HRWrite
                    }).ToList(),
                    HRReviews = reviews.Select((s) => new HRReviewResponse()
                    {
                        HRComment = s.HRComment,
                        HRStars = s.HRStars,
                        HRName = s.HRName,
                        HRCompany = s.HRCompany,
                    }).ToList(),
                    HRSkills = skills.Select((s) =>
                    {
                        var skill = _context.GlobalJobTitlesSkills.Where(x => x.GlobalSkillId == s.HRSkillId).FirstOrDefault();

                        return new HRSkillResponse()
                        {
                            HRSkillId = s.HRSkillId,
                            HRRate = s.HRRate,
                            Name = skill?.Name
                        };
                    }).ToList(),
                    HRProfiles = extras.Select((e) =>
                    {
                        var u = _context.AspNetUsers.Where(x => x.Id == e.UserId && x.IsDeleted == false).FirstOrDefault();

                        return new HRProfilesResponse()
                        {
                            Id = u?.Id ?? string.Empty,
                            Name = u?.FullName ?? string.Empty,
                            Picture = (!string.IsNullOrEmpty(u?.ProfilePicture)) ? $"{LookupService.FilePaths.ProfilePictureUrl}{u.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}",
                            Title = u?.UserTitle ?? string.Empty,
                            Description = e.HRDesc1
                        };
                    }).ToList()
                };

                return response;
            }
        }

        public async Task<spGetTotalCoAccountBalanced> GetAccountBalance(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var result = await _context.spGetTotalCoAccountBalanced.FromSql("spGetTotalCoAccountBalanced @UserID = {0}", userId).FirstOrDefaultAsync();
                return result;
            }
        }

        public async Task<bool> SentRequest(EmailsRequest model)
        {
            using (var _context = new GoHireNowContext())
            {
                var emails = await _context.Emails.FirstOrDefaultAsync(e => e.Type == 2 && e.IsDeleted == 0 && e.UserId == model.FromId && e.ToUserId == model.ToId);
                if (emails != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<bool> RequestReferences(EmailsRequest model)
        {
            using (var _context = new GoHireNowContext())
            {
                var worker = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == model.ToId && !u.IsDeleted);
                if (worker != null)
                {
                    var email = new Emails()
                    {
                        Type = 2,
                        UserId = model.FromId,
                        ToUserId = model.ToId,
                        IsDeleted = 0,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _context.Emails.AddAsync(email);
                    await _context.SaveChangesAsync();

                    string subject = "A company requested your references";
                    _contractService.PersonalEmailService(0, 53, worker.Email, worker.FullName, subject, "", "", "julia.d@evirtualassistants.com", "Julia", 1, "RequestReferencesTemplate.html");

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<int> PayByBalance(decimal amount, string userId, int type)
        {
            using (var _context = new GoHireNowContext())
            {
                var cp = new CompanyBalance()
                {
                    CompanyId = userId,
                    Amount = amount,
                    Type = type,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.CompanyBalance.AddAsync(cp);
                await _context.SaveChangesAsync();

                return cp.Id;
            }
        }
    }
}
