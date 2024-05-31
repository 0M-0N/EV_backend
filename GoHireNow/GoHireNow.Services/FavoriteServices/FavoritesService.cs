using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.ExceptionModels;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Service.FavoriteServices
{
    public class FavoritesService : IFavoritesService
    {
        private readonly IPricingService _pricingService;
        private readonly IUserRoleService _userRoleService;
        public FavoritesService(IPricingService pricingService, IUserRoleService userRoleService)
        {
            _pricingService = pricingService;
            _userRoleService = userRoleService;
        }
        public async Task<int> AddClientFavotite(string clientId, string workerId)
        {
            using (var _context = new GoHireNowContext())
            {
                var exists = await _context.FavoriteWorkers
                           .FirstOrDefaultAsync(uf => uf.UserId == clientId && uf.WorkerId == workerId && uf.IsDeleted == false);
                if (exists != null)
                    return exists.Id;
                var favorite = new FavoriteWorkers
                {
                    UserId = clientId,
                    WorkerId = workerId,
                    IsDeleted = false,
                    CreateDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Type = 1
                };
                await _context.FavoriteWorkers.AddAsync(favorite);
                await _context.SaveChangesAsync();
                return favorite.Id;
            }
        }

        public async Task<bool> RemoveClientFavotite(string clientId, string workerId)
        {
            bool res = false;
            using (var _context = new GoHireNowContext())
            {
                var favorite = await _context.FavoriteWorkers
                            .FirstOrDefaultAsync(uf => uf.UserId == clientId && uf.WorkerId == workerId);
                if (favorite != null)
                {
                    favorite.IsDeleted = true;
                    await _context.SaveChangesAsync();
                    res = true;
                }
            }
            return res;
        }

        public async Task<List<ClientFavoritesResponse>> GetFavoriteWorkers(string userId, int roleId)
        {
            List<FavoriteWorkers> favorites;
            bool applyFilter = false;
            using (var _context = new GoHireNowContext())
            {
                favorites = await _context.FavoriteWorkers
                    .Include(fw => fw.User)
                        .ThenInclude(u => u.UserSkills).ThenInclude(u => u.Skill)
                    .Include(fw => fw.Worker)
                        .ThenInclude(u => u.UserSkills).ThenInclude(u => u.Skill)
                    .Where(fw => fw.UserId == userId && fw.IsDeleted == false && fw.Worker.IsDeleted == false && fw.User.IsDeleted == false)
                    .OrderByDescending(fw => fw.CreateDate)
                    .ToListAsync();
            }


            return favorites.Select(fw => new ClientFavoritesResponse
            {

                Country = fw.Worker.CountryId != null ? fw.Worker.CountryId.Value.ToCountryName() : "",
                Name = fw.Worker.FullName,
                Skills = fw.Worker.UserSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList(),
                UserId = fw.WorkerId,
                Title = !string.IsNullOrEmpty(fw.Worker.UserTitle) ? fw.Worker.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, fw.WorkerId).Result) : null,
                Salary = fw.Worker.UserSalary,
                //LastLoginDate = fw.Worker.LastLoginTime != null ? fw.Worker.LastLoginTime.Value.ToShortDateString() : "",
                LastLoginDate = fw.Worker.LastLoginTime.ToString(),
                CreatedDate = fw.Worker.CreatedDate.ToString(),
                ProfilePicturePath = !string.IsNullOrEmpty(fw.Worker.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{fw.Worker.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
            }).ToList();
        }

        //Please discuss with Rao, about two getfavoriteworkers methods.
        public async Task<List<WorkerSummaryForClientResponse>> GetFavoriteWorkersNew(string userId, int roleId, int page = 1, int size = 5)
        {
            List<FavoriteWorkers> clientFavorites;
            List<AspNetUsers> allUsers;
            int skip = page > 1 ? ((page - 1) * size) : 0;

            using (var _context = new GoHireNowContext())
            {
                clientFavorites = await _context.FavoriteWorkers.Include(o => o.Worker)
                    .Where(x => x.UserId == userId && x.IsDeleted == false && x.Worker.IsDeleted == false && x.Worker.IsSuspended == 0)
                    .OrderByDescending(x => x.CreateDate)
                    .Skip(skip).Take(size)
                    .ToListAsync();

                if (clientFavorites == null && clientFavorites.Count() == 0)
                    return null;

                var favWokerIds = clientFavorites.Select(x => x.WorkerId).ToList();

                allUsers = await _context.AspNetUsers
                .Include(x => x.UserSkills)
                .Where(x => favWokerIds.Contains(x.Id)).ToListAsync();

                if (allUsers == null)
                    return null;
            }

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
                    worker.LastLoginDate = u.LastLoginTime;
                    List<int> skillIds = (u.UserSkills != null && u.UserSkills.Count() > 0) ? u.UserSkills.Select(x => x.SkillId).ToList() : null; ;
                    skillsList = skillIds != null && skillIds.Count() > 0 ? LookupService.GetSkillsById(skillIds) : null;
                    worker.Skills = skillsList;
                    worker.ProfilePicturePath = !string.IsNullOrEmpty(u.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{u.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}";
                    allWorkers.Add(worker);
                }
                catch (Exception)
                { }
            }
            return allWorkers;
        }

        public async Task<bool> IsWorkerInMyFavorite(string clientId, string workerId)
        {
            using (var _context = new GoHireNowContext())
            {
                var exists = await _context.FavoriteWorkers
                           .FirstOrDefaultAsync(uf => uf.UserId == clientId && uf.WorkerId == workerId && uf.IsDeleted == false);

                return exists != null ? true : false;
            }
        }

        #region Worker Favorite Section

        public async Task<int> AddFavoriteJob(string userId, int jobId)
        {
            using (var _context = new GoHireNowContext())
            {
                var existingFavoriteJob = await _context.FavoriteJobs.FirstOrDefaultAsync(x => x.JobId == jobId && x.UserId == userId && x.IsDeleted == false);

                if (existingFavoriteJob != null)
                    return existingFavoriteJob.Id;

                var newFavoriteJob = new FavoriteJobs()
                {
                    JobId = jobId,
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _context.FavoriteJobs.AddAsync(newFavoriteJob);
                await _context.SaveChangesAsync();

                return newFavoriteJob.Id;
            }
        }

        public async Task<bool> RemoveFavoriteJob(string userId, int jobId)
        {
            using (var _context = new GoHireNowContext())
            {
                var existingFavoriteJob = await _context.FavoriteJobs.FirstOrDefaultAsync(x => x.JobId == jobId && x.UserId == userId && x.IsDeleted == false);

                if (existingFavoriteJob == null)
                    throw new CustomException(StatusCodes.Status404NotFound, "Favorite job with given Id not found");

                existingFavoriteJob.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<List<JobSummaryForWorkerResponse>> GetFavoriteJobs(string userId, int roleId)
        {
            List<FavoriteJobs> favoriteJobs;
            using (var _context = new GoHireNowContext())
            {
                favoriteJobs = await _context.FavoriteJobs
                    .Include(x => x.Job).ThenInclude(o => o.JobSkills).ThenInclude(o => o.Skill)
                    .Include(x => x.Job).ThenInclude(y => y.User)
                    .Where(x => x.UserId == userId && x.IsDeleted == false && x.Job.JobStatusId == (int)JobStatusEnum.Published && x.Job.User.IsDeleted == false)
                    .OrderByDescending(x => x.CreateDate)
                    .ToListAsync();
            }

            //Do we have to show only favorite jobs those are active
            if (favoriteJobs == null)
                return null;

            var res = favoriteJobs.Select(x => new JobSummaryForWorkerResponse()
            {
                Id = x.Job.Id,
                Title = !string.IsNullOrEmpty(x.Job.Title) ? x.Job.Title.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, x.Job.UserId).Result) : null,
                TypeId = x.Job.JobTypeId,
                Type = x.Job.JobTypeId.ToJobTypeName(),
                SalaryTypeId = x.Job.SalaryTypeId,
                SalaryType = x.Job.SalaryTypeId > 0 ? x.Job.SalaryTypeId.ToSalaryTypeName() : "",
                Salary = x.Job.Salary,
                ActiveDate = x.Job.ActiveDate,
                Skills = x.Job.JobSkills.Select(js => new SkillResponse() { Id = js.SkillId, Name = js.SkillId.ToGlobalSkillName() }).ToList(),
                //ProfilePicturePath = !string.IsNullOrEmpty(x.Job.User.ProfilePicture) ? $"{ LookupService.FilePaths.ProfilePictureUrl}{x.Job.User.ProfilePicture}" : string.Empty,
                Client = new JobClientSummaryResponse
                {
                    CompanyName = x.Job.User != null ? x.Job.User.Company : "",
                    Id = x.Job.UserId,
                    LastLoginDate = x.Job.User.LastLoginTime,
                    ProfilePicturePath = !string.IsNullOrEmpty(x.Job.User.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{x.Job.User.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}"
                },
                IsFeatured = false,
                IsUrgent = false,
                IsPrivate = false
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
                                case "Featured": item.IsFeatured = true; break;
                                case "Urgent": item.IsUrgent = true; break;
                                case "Private": item.IsPrivate = true; break;
                                default: break;
                            }
                            if (subItem.GlobalPlanId.ToGlobalPlanName() == "Featured" && (DateTime.UtcNow - subItem.CreateDate).TotalDays < 30)
                            {
                                item.IsFeatured = false;
                            }
                        }
                    }

                    TimeSpan ts = (TimeSpan)(DateTime.UtcNow - item.ActiveDate);
                    var currentPlan = await _pricingService.GetCurrentPlan(item.Client.Id);

                    if (currentPlan != null && (currentPlan.Name.Contains("Enterprise") || currentPlan.Name.Contains("Agency")) && ts.TotalDays < 30)
                    {
                        item.IsFeatured = true;
                    }
                }
            }

            return res;
        }

        #endregion
    }
}
