using GoHireNow.Api.Filters;
using GoHireNow.Models.CommonModels;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("favorites")]
    [ApiController]
    [CustomExceptionFilter]
    public class FavoritesController : BaseController
    {
        private readonly IFavoritesService _favoritesService;
        private readonly IUserRoleService _userRoleService;
        private readonly ICustomLogService _customLogService;

        public FavoritesController(IFavoritesService favoritesService, ICustomLogService customLogService, IUserRoleService userRoleService)
        {
            _favoritesService = favoritesService;
            _customLogService = customLogService;
            _userRoleService = userRoleService;
        }

        [Authorize(Roles = "Client")]
        [HttpGet]
        [Route("client/workers")]
        public async Task<IActionResult> GetFavoriteWorkers()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _favoritesService.GetFavoriteWorkers(UserId, RoleId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/favorites/client/workers",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [Route("client/add/{userid}")]
        public async Task<IActionResult> AddClientFavorite(string userid)
        {
            LogErrorRequest error;
            try
            {
                //var userUniqueId = userid;
                //string userId = await _userRoleService.GetUserId(userUniqueId);
                if (string.IsNullOrEmpty(userid))
                    return BadRequest();
                var id = await _favoritesService.AddClientFavotite(UserId, userid);
                return Ok(id);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/favorites/client/add/{userid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Authorize(Roles = "Client")]
        [HttpDelete]
        [Route("client/remove/{userid}")]
        public async Task<IActionResult> RemoveClientFavorite(string userid)
        {
            LogErrorRequest error;
            try
            {
                //var userUniqueId = userid;
                //string userId = await _userRoleService.GetUserId(userUniqueId);
                if (string.IsNullOrEmpty(userid))
                    return BadRequest("User id not found");
                var response = await _favoritesService.RemoveClientFavotite(UserId, userid);
                if (response)
                    return Ok(response.ToString());
                return Ok("Error");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/favorites/client/remove/{userid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        #region Worker Favorite Region
        [Authorize(Roles = "Worker")]
        [HttpGet]
        [Route("worker/jobs")]
        public async Task<IActionResult> GetFavoriteJobs()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _favoritesService.GetFavoriteJobs(UserId, RoleId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/favorites/worker/jobs",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Authorize(Roles = "Worker")]
        [HttpPost]
        [Route("worker/add/{jobId}")]
        public async Task<IActionResult> AddFavoriteJob(int jobId)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _favoritesService.AddFavoriteJob(UserId, jobId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/favorites/worker/add/{jobId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Authorize(Roles = "Worker")]
        [HttpDelete]
        [Route("worker/remove/{jobId}")]
        public async Task<IActionResult> RemoveFavoriteJob(int jobId)
        {
             LogErrorRequest error;
            try
            {
                return Ok(await _favoritesService.RemoveFavoriteJob(UserId, jobId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/favorites/worker/remove/{jobId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }
        #endregion
    }
}