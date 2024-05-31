using GoHireNow.Api.Filters;
using GoHireNow.Service.Interfaces;
using GoHireNow.Models.HireModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using GoHireNow.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using GoHireNow.Models.CommonModels;

namespace GoHireNow.Api.Controllers
{
    [Route("jobs")]
    [ApiController]
    [CustomExceptionFilter]
    [AllowAnonymous]

    public class GlobalJobsController : BaseController
    {
        private readonly IGlobalJobsService _globalJobsService;
        private readonly IWorkerService _workerService;
        private readonly IFavoritesService _favoritesService;
        private readonly ICustomLogService _customLogService;
        private readonly UserManager<ApplicationUser> _userManager;
        private string FilePathRoot
        {
            get
            {
                return $"{Request.Scheme}://{Request.Host}";
            }
        }
        public GlobalJobsController(
            IGlobalJobsService globalJobsService,
            IWorkerService workerService,
            ICustomLogService customLogService,
            IFavoritesService favoritesService,
            UserManager<ApplicationUser> usreManager)
        {
            _globalJobsService = globalJobsService;
            _customLogService = customLogService;
            _userManager = usreManager;
            _workerService = workerService;
            _favoritesService = favoritesService;
        }

        [HttpGet]
        [Route("global-job-titles")]
        public async Task<List<GlobalJobCategoriesListModel>> GetGlobalJobTitles()
        {
            LogErrorRequest error;
            try
            {
                var titles = await _globalJobsService.GetGlobalJobTitles();
                return titles;
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/jobs/global-job-titles",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("global-jobs/{jobTitleId}")]
        public async Task<IActionResult> GetJobTitleRelatedJobs(int jobTitleId)
        {
            LogErrorRequest error;
            try
            {
                var jobs = await _globalJobsService.GetJobTitleRelatedJobs(jobTitleId, UserId, RoleId);
                return Ok(jobs);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/jobs/global-jobs/{jobTitleId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("global-job/{jobId}")]
        public async Task<IActionResult> GetJobSummary(int jobId)
        {
            LogErrorRequest error;
            try
            {
                var job = await _globalJobsService.GetJob(jobId, FilePathRoot, UserId, RoleId);
                if (job != null)
                {
                    return Ok(job);
                }
                return Ok("No Job Found");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/jobs/global-job/{jobId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }
    }
}