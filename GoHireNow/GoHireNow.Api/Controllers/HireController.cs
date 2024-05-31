using GoHireNow.Api.Filters;
using GoHireNow.Service.Interfaces;
using GoHireNow.Models.HireModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using GoHireNow.Identity.Data;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using Microsoft.AspNetCore.Authorization;
using GoHireNow.Models.CommonModels;

namespace GoHireNow.Api.Controllers
{
    [Route("hire")]
    [ApiController]
    [CustomExceptionFilter]
    [AllowAnonymous]
    public class HireController : BaseController
    {
        private readonly IHireService _hireService;
        private readonly IWorkerService _workerService;
        private readonly IFavoritesService _favoritesService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomLogService _customLogService;
        private readonly IUserRoleService _userRoleService;
        public HireController(
            IHireService hireService,
            IWorkerService workerService,
            ICustomLogService customLogService,
            IFavoritesService favoritesService,
            UserManager<ApplicationUser> usreManager, IUserRoleService userRoleService)
        {
            _hireService = hireService;
            _userManager = usreManager;
            _customLogService = customLogService;
            _workerService = workerService;
            _favoritesService = favoritesService;
            _userRoleService = userRoleService;
        }

        [HttpGet]
        [Route("job-titles")]
        public async Task<List<GlobalJobCategoriesListModel>> GetGlobalJobTitles()
        {
            LogErrorRequest error;
            try
            {
                var titles = await _hireService.GetGlobalJobTitles();
                return titles;
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/hire/job-titles",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("workers/{jobTitleId}")]
        public async Task<IActionResult> GetJobTitleRelatedWorkers(int jobTitleId, int size, int page)
        {
            LogErrorRequest error;
            try
            {
                var workers = await _hireService.GetJobTitleRelatedWorkers(jobTitleId, size, page, UserId, RoleId);
                return Ok(workers);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/hire/worker/" + jobTitleId,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("relatedworkers/{UserId}")]
        public async Task<IActionResult> GetAllRelatedWorkers(string UserId, int size, int page)
        {
            LogErrorRequest error;
            try
            {
                var workers = await _hireService.GetRelatedWorkersOffline(UserId, size, page);
                return Ok(workers);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/hire/relatedworkers/" + UserId,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("worker-profile/{userid}")]
        public async Task<IActionResult> GetWorkerProfile(int userid)
        {
            LogErrorRequest error;
            try
            {
                var userUniqueId = userid;
                string userId = await _userRoleService.GetUserId(userUniqueId);
                if (userId == null)
                {
                    return NotFound("Worker not found");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    return NotFound("Worker not found");

                return Ok(await MapUserToWrokerProfileResponse(user, UserId, RoleId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/hire/worker-profile/" + userid,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        private async Task<WorkerProfileForClientResponse> MapUserToWrokerProfileResponse(ApplicationUser user, string userId, int roleId)
        {
            var worker = new WorkerProfileForClientResponse();
            worker.UserId = user.Id;
            worker.FullName = user.FullName;
            if (userId != null)
            {
                worker.Title = !string.IsNullOrEmpty(user.UserTitle) ? user.UserTitle.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, worker.UserId).Result) : null;
                worker.Description = !string.IsNullOrEmpty(user.Description) ? user.Description.ReplaceInformation(roleId, _userRoleService.TextFilterCondition(userId, roleId, worker.UserId).Result) : null;
            }
            else
            {
                worker.Title = !string.IsNullOrEmpty(user.UserTitle) ? user.UserTitle.ReplaceGlobalJobTitleInformation() : null;
                worker.Description = !string.IsNullOrEmpty(user.Description) ? user.Description.ReplaceGlobalJobTitleInformation() : null;
            }
            worker.MemberSince = user.RegistrationDate;
            worker.Availability = user.UserAvailiblity.ToAvailabilityType();
            worker.Education = user.Education;
            worker.Experience = user.Experience;
            worker.featured = user.featured;
            worker.rating = user.rating;
            worker.CountryId = user.CountryId;
            worker.CountryName = user.CountryId.HasValue ? user.CountryId.Value.ToCountryName() : string.Empty;
            worker.ProfilePicturePath = !string.IsNullOrEmpty(user.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{user.ProfilePicture}" : $"{LookupService.FilePaths.WorkerDefaultImageFilePath}";
            worker.LastLoginTime = user.LastLoginTime;
            worker.Salary = user.UserSalary;
            worker.SocialMediaLinks = new SocialMediaLinksResponse()
            {
                Facebool = user.Facebook,
                Skype = user.Skype,
                LinkedIn = user.Linkedin
            };
            worker.Skills = await _workerService.GetWorkerSkills(user.Id);
            worker.Portfolios = await _workerService.GetPortfolios(user.Id);
            worker.IsSuspended = user.IsSuspended;
            //worker.IsFavorite = await _favoritesService.IsWorkerInMyFavorite(UserId, user.Id);
            //worker.EnableMessage = await _workerService.ChatExist(user.Id, UserId);
            return worker;
        }
    }
}
