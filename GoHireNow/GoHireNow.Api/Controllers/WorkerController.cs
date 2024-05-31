using GoHireNow.Api.Filters;
using GoHireNow.Identity.Data;
using GoHireNow.Models.AccountModels;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Quartz;
using System;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("worker")]
    [Authorize(Roles = "Worker")]
    [ApiController]
    [CustomExceptionFilter]
    public class WorkerController : BaseController
    {
        private readonly ICustomLogService _customLogService;
        private readonly IPricingService _pricingService;
        private readonly IWorkerService _workerService;
        private readonly IClientJobService _clientJobService;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserRoleService _userRoleService;
        public IConfiguration _configuration { get; }
        private new string FilePathRoot
        {
            get
            {
                return $"{Request.Scheme}://{Request.Host}/Resources";
            }
        }

        public WorkerController(IWorkerService workerService,
            ICustomLogService customLogService,
            IPricingService pricingService,
            IConfiguration configuration,
            IClientJobService clientJobService,
            ISchedulerFactory schedulerFactory,
            UserManager<ApplicationUser> userManager,
            IUserRoleService userRoleService)
        {
            _workerService = workerService;
            _clientJobService = clientJobService;
            _pricingService = pricingService;
            _configuration = configuration;
            _customLogService = customLogService;
            _schedulerFactory = schedulerFactory;
            _userManager = userManager;
            _userRoleService = userRoleService;
        }

        [HttpGet]
        [Route("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            LogErrorRequest error;
            try
            {
                var dashboard = await _workerService.GetDashboard(UserId, RoleId);
                return Ok(dashboard);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/dashboard",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Authorize]
        [Route("subscription")]
        public async Task<IActionResult> Subscription()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _pricingService.GetWorkerSubscriptionDetails(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/subscription",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("search-jobs")]
        public async Task<IActionResult> SearchJobs([FromQuery] SearchJobRequest model)
        {
            LogErrorRequest error;
            try
            {
                var search = await _workerService.SearchJobs(model, FilePathRoot, RoleId, UserId);
                return Ok(search);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/search-jobs",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("featured-jobs")]
        public async Task<IActionResult> FeaturedJobs([FromQuery] SearchJobRequest model)
        {
            LogErrorRequest error;
            try
            {
                var search = await _workerService.FeaturedJobs(model, FilePathRoot, RoleId, UserId);
                return Ok(search);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/featured-jobs",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("similar-jobs/{skillIds}")]
        public async Task<IActionResult> GetSkillsRelatedJobs(string skillIds)
        {
            LogErrorRequest error;
            try
            {
                var jobs = await _workerService.GetSkillsRelatedJobs(skillIds, UserId, RoleId);
                if (jobs != null)
                {
                    return Ok(jobs);
                }
                return Ok("No Jobs Found");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/similar-jobs/{skillIds}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("lastNotifications")]
        public async Task<IActionResult> GetLastNotifications()
        {
            try
            {
                var lastNotifications = await _workerService.GetLastNotifications(UserId);
                if (lastNotifications != null)
                {
                    return Ok(lastNotifications);
                }
                return Ok("No Notifications Found");
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/lastNotifications",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var notifications = await _workerService.GetNotifications(UserId);
                if (notifications != null)
                {
                    return Ok(notifications);
                }
                return Ok("No Notifications Found");
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/notifications",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Authorize]
        [Route("addreference")]
        public async Task<IActionResult> AddReference(AddReferenceRequest request)
        {
            try
            {
                var id = await _workerService.AddReference(UserId, request);
                if (id > 0)
                {
                    IScheduler scheduler = await _schedulerFactory.GetScheduler();
                    await scheduler.Start();

                    IJobDetail job1 = JobBuilder.Create<SendSecondEmailJob>()
                        .WithIdentity("Reference job 1 - " + id, "AddReferenceSchedule")
                        .UsingJobData("UserId", UserId)
                        .UsingJobData("ReferenceId", id)
                        .UsingJobData("Type", 2)
                        .Build();

                    ITrigger trigger1 = TriggerBuilder.Create()
                        .WithIdentity("Reference trigger 1 - " + id, "AddReferenceSchedule")
                        .StartAt(DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(5)))
                        .Build();

                    await scheduler.ScheduleJob(job1, trigger1);

                    IJobDetail job2 = JobBuilder.Create<SendSecondEmailJob>()
                        .WithIdentity("Reference job 2 - " + id, "AddReferenceSchedule")
                        .UsingJobData("UserId", UserId)
                        .UsingJobData("ReferenceId", id)
                        .UsingJobData("Type", 3)
                        .Build();

                    ITrigger trigger2 = TriggerBuilder.Create()
                        .WithIdentity("Reference trigger 2 - " + id, "AddReferenceSchedule")
                        .StartAt(DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(10)))
                        .Build();

                    await scheduler.ScheduleJob(job2, trigger2);

                    return Ok(new { Status = "success", Response = "Reference was saved" });
                }
                else
                    return Ok(new { Status = "error", Response = "Error occured during saving reference" });
            }
            catch (System.Exception ex)
            {
                var error = new LogErrorRequest()
                {
                    ErrorMessage = ex.ToString(),
                    ErrorUrl = "/account/addreference",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("client-profile/{id}")]
        public async Task<IActionResult> GetClientProfile(string id)
        {
            LogErrorRequest error;
            try
            {
                //var userUniqueId = id;
                //string userId = await _userRoleService.GetUserId(userUniqueId);
                var user = await _userManager.FindByIdAsync(id);
                if (user == null || user.IsDeleted)
                    return NotFound();

                return Ok(await MapUserToClientProfileResponse(user));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/client-profile/{id}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Authorize(Roles = "Worker")]
        [Route("academies")]
        public async Task<IActionResult> GetAcademies()
        {
            try
            {
                return Ok(await _workerService.GetAcademies(UserId));
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/academies",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        private async Task<ClientProfileForWorkerResponse> MapUserToClientProfileResponse(ApplicationUser user)
        {
            var client = new ClientProfileForWorkerResponse();
            client.EnableMessage = await _workerService.ChatExist(UserId, user.Id);
            client.UserId = user.Id;
            //client.UserUniqueId = user.UniqueId;
            client.CompanyName = user.Company;
            client.Introduction = !string.IsNullOrEmpty(UserId) ? !string.IsNullOrEmpty(user.Introduction) ? user.Introduction.ReplaceInformation(RoleId, _userRoleService.TextFilterCondition(UserId, RoleId, user.Id).Result) : null : user.Introduction.ReplaceGlobalJobTitleInformation();
            client.Description = !string.IsNullOrEmpty(UserId) ? !string.IsNullOrEmpty(user.Description) ? user.Description.ReplaceInformation(RoleId, _userRoleService.TextFilterCondition(UserId, RoleId, user.Id).Result) : null : user.Description.ReplaceGlobalJobTitleInformation();
            client.MemberSince = user.CreatedDate ?? user.RegistrationDate;
            client.LastLoginTime = user.LastLoginTime;
            client.CountryId = user.CountryId;
            client.CountryName = user.CountryId.HasValue ? user.CountryId.Value.ToCountryName() : string.Empty;
            client.ProfilePicturePath = !string.IsNullOrEmpty(user.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{user.ProfilePicture}" : $"{LookupService.FilePaths.ClientDefaultImageFilePath}";
            client.ActiveJobs = await _workerService.GetJobs(user.Id, 1, 5, (int)JobStatusEnum.Published, RoleId, UserId, true);
            client.IsSuspended = user.IsSuspended;
            return client;
        }
    }
}