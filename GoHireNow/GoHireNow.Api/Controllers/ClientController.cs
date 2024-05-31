using System;
using GoHireNow.Database;
using GoHireNow.Api.Filters;
using GoHireNow.Identity.Data;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;

namespace GoHireNow.Api.Controllers
{
    [Route("client")]
    [Authorize(Roles = "Client")]
    [ApiController]
    [CustomExceptionFilter]
    public class ClientController : BaseController
    {
        private readonly IClientService _clientService;
        private readonly IWorkerService _workerService;
        private readonly IFavoritesService _favoritesService;
        private readonly IPricingService _pricingService;
        private readonly IUserSecurityCheckService _userSecurityCheckService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICustomLogService _customLogService;
        private readonly IUserRoleService _userRoleService;

        public ClientController(IClientService clientService,
            IWorkerService workerService,
            IFavoritesService favoritesService,
            IPricingService pricingService,
            IUserSecurityCheckService userSecurityCheckService,
            UserManager<ApplicationUser> usreManager,
            ICustomLogService customLogService,
            IUserRoleService userRoleService)
        {
            _clientService = clientService;
            _workerService = workerService;
            _favoritesService = favoritesService;
            _pricingService = pricingService;
            _userSecurityCheckService = userSecurityCheckService;
            _userManager = usreManager;
            _customLogService = customLogService;
            _userRoleService = userRoleService;
        }

        [HttpGet]
        [Route("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            LogErrorRequest error;
            try
            {
                var dashboard = await _clientService.GetDashboard(UserId, RoleId);
                return Ok(dashboard);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/dashboard",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("subscription")]
        [Authorize]
        public async Task<IActionResult> Subscription()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _pricingService.GetSubscriptionDetails(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/subscription",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("usersecuritychecks")]
        public async Task<IActionResult> UserSecurityCheck()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _userSecurityCheckService.GetAllUserSecurityCheck(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/usersecuritychecks",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("search-workers")]
        public async Task<IActionResult> SearchWorkers([FromQuery] SearchWorkerRequest model)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _clientService.SearchWorkers(model, RoleId, UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/search-workers",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("featured-workers")]
        public async Task<IActionResult> FeaturedWorkers([FromQuery] SearchWorkerRequest model)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _clientService.FeaturedWorkers(model, RoleId, UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/featured-workers",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("worker-profile/{id}")]
        public async Task<IActionResult> GetWorkerProfile(string id)
        {
            LogErrorRequest error;
            try
            {
                var worker = await _userManager.FindByIdAsync(id);

                if (worker == null || worker.IsDeleted)
                    return NotFound("User not found");

                if (UserId != null)
                {
                    var user = await _userManager.FindByIdAsync(UserId);
                    if (user != null && user.UserType == 2)
                    {
                        return Ok(await MapUserToWrokerProfileResponse(worker, false));
                    }

                    var notification = new UserNotifications()
                    {
                        UserId = id,
                        CompanyId = UserId,
                        CustomId = 1,
                        CustomName = user.Company,
                        CreatedDate = DateTime.UtcNow,
                        IsDelerte = 0
                    };
                    var _context = new GoHireNowContext();
                    await _context.UserNotifications.AddAsync(notification);
                    await _context.SaveChangesAsync();

                    var result = await _pricingService.GetSubscriptionDetails(UserId);
                    if (result != null && (result.SubscriptionStatus.PlanName.Contains("Enterprise") || result.SubscriptionStatus.PlanName.Contains("Agency")))
                    {
                        return Ok(await MapUserToWrokerProfileResponse(worker));
                    }
                }
                else
                {
                    if (worker.IsHidden)
                    {
                        return Ok(-1);
                    }
                }

                return Ok(await MapUserToWrokerProfileResponse(worker, false));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/worker-profile/" + id,
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("sentRequest")]
        public async Task<IActionResult> SentRequest([FromBody] EmailsRequest model)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _clientService.SentRequest(model));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/sentRequest/",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("requestReferences")]
        [Authorize]
        public async Task<IActionResult> RequestReferences([FromBody] EmailsRequest model)
        {
            LogErrorRequest error;
            try
            {
                if (model.FromId != UserId)
                {
                    return BadRequest();
                }
                else
                {
                    return Ok(_clientService.RequestReferences(model));
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/requestReferences/",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Route("GetPricingPlanCapabilityStatus")]
        [HttpPost]
        public ActionResult GetPricingPlanCapabilityStatus([FromBody] ClientPricingPlanCapabilityStatusResponse model)
        {
            LogErrorRequest error;
            try
            {
                UserCapablePricingPlanResponse userPricingPlan = null;
                if (!string.IsNullOrEmpty(model.EntryType))
                {
                    string toUserId = null;
                    if (model.EntryType == "Contacts")
                    {
                        toUserId = model.MessageToUserId;
                    }
                    userPricingPlan = _pricingService.IsCapable(UserId, model.EntryType, toUserId);
                }
                return Ok(userPricingPlan);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/GetPricingPlanCapabilityStatus",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Route("update-to-freeplan")]
        [HttpGet]
        public async Task<ActionResult> UpdateToFreePlan()
        {
            LogErrorRequest error;
            try
            {
                var result = await _clientService.UpdateToFreePlan(UserId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/update-to-freeplan",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }

        }

        [Route("process-current-pricingplan")]
        [HttpGet]
        public async Task<ActionResult> ProcessCurrentPricingPlan()
        {
            LogErrorRequest error;
            try
            {
                var result = await _clientService.ProcessCurrentPricingPlan(UserId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/process-current-pricingplan",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        private async Task<WorkerProfileForClientResponse> MapUserToWrokerProfileResponse(ApplicationUser user, bool isFull = true)
        {
            var worker = new WorkerProfileForClientResponse();

            worker.UserId = user.Id;
            // worker.UserUniqueId = user.UserUniqueId;
            worker.FullName = user.FullName;
            worker.Title = !string.IsNullOrEmpty(UserId) ? !string.IsNullOrEmpty(user.UserTitle) ? user.UserTitle.ReplaceInformation(RoleId, _userRoleService.TextFilterCondition(UserId, RoleId, user.Id).Result) : null : user.UserTitle.ReplaceGlobalJobTitleInformation();
            worker.Description = !string.IsNullOrEmpty(UserId) ? !string.IsNullOrEmpty(user.Description) ? user.Description.ReplaceInformation(RoleId, _userRoleService.TextFilterCondition(UserId, RoleId, user.Id).Result) : null : user.Description.ReplaceGlobalJobTitleInformation();
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
                Facebool = "",// user.Facebook,
                Skype = "",//user.Skype,
                LinkedIn = ""//user.Linkedin
            };

            worker.Skills = await _workerService.GetWorkerSkills(user.Id);
            worker.Portfolios = await _workerService.GetPortfolios(user.Id, isFull);
            worker.Youtubes = await _workerService.GetYoutubes(user.Id, isFull);
            worker.IsFavorite = await _favoritesService.IsWorkerInMyFavorite(UserId, user.Id);
            worker.EnableMessage = await _workerService.ChatExist(user.Id, UserId);

            var _context = new GoHireNowContext();
            var profile = _context.AspNetUsers.Where(u => u.Id == user.Id).FirstOrDefault();
            worker.IsSecurityChecked = _userSecurityCheckService.UserChecked(UserId, profile.UserUniqueId);

            var references = _context.UserReferences.Where(u => u.UserId == user.Id && u.IsAccepted == 1 && u.IsDeleted == 0).ToList();
            worker.ReferencesCount = references.Count();
            worker.IsSuspended = user.IsSuspended;

            return worker;
        }
    }
}