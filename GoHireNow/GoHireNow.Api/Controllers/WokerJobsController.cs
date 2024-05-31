using GoHireNow.Api.Filters;
using GoHireNow.Identity.Data;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.WorkerModels;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("worker/jobs")]
    [ApiController]
    [CustomExceptionFilter]
    public class WokerJobsController : BaseController
    {
        private readonly ICustomLogService _customLogService;
        private readonly IWorkerJobService _workerJobService;
        private readonly UserManager<ApplicationUser> _userManager;
        private new string FilePathRoot
        {
            get
            {
                return $"{Request.Scheme}://{Request.Host}";
            }
        }

        public WokerJobsController(IWorkerJobService workerJobService,
            ICustomLogService customLogService,
            UserManager<ApplicationUser> userManager)
        {
            _customLogService = customLogService;
            _workerJobService = workerJobService;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("detail/{jobId}")]
        public async Task<IActionResult> GetJob(int jobId)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _workerJobService.GetWorkerJobDetails(jobId, FilePathRoot, UserId, RoleId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/detail/{jobId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("inviteworkers")]
        public async Task<IActionResult> InviteUsers([FromBody] InviteCompaniesRequest model)
        {
            try
            {
                _workerJobService.InviteWorkers(model, UserId);

                return Ok();
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/inviteworkers",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Route("latest")]
        [Authorize]
        public async Task<IActionResult> GetNewJobs(int page = 1, int size = 10)
        {
            LogErrorRequest error;
            try
            {
                //TODO: Check if required by worker skill
                return Ok(await _workerJobService.GetNewJobs((int)JobStatusEnum.Published, UserId, RoleId, page, size));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/latest",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("ai-assistant")]
        [Authorize]
        public async Task<IActionResult> AIAssistant([FromBody] AIAssistantRequest model)
        {
            try
            {
                var result = await _workerJobService.AIAssistant(model, UserId);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/ai-assistant",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("{jobId}/attachments")]
        [Authorize]
        public async Task<IActionResult> GetJobAttachments(int jobId)
        {
            LogErrorRequest error;
            try
            {
                var jobAttachments = await _workerJobService.GetJobAttachments(jobId, FilePathRoot);
                return Ok(jobAttachments);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/{jobId}/attachments",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("matching")]
        public async Task<IActionResult> MatchingJobs(int page = 1, int size = 5)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _workerJobService.GetMatchingJobs(UserId, RoleId, page, size));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/matching",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("applied")]
        public async Task<IActionResult> AppliedJobs(int page = 1, int size = 5)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _workerJobService.GetAppliedJobs(UserId, RoleId, page, size));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/applied",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("apply")]
        public async Task<IActionResult> ApplyJob([FromBody] ApplyJobRequest model)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _workerJobService.ApplyJob(model, UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/apply",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("savetemplate")]
        public async Task<IActionResult> SaveTemplate([FromBody] SaveTemplateRequest model)
        {
            LogErrorRequest error;
            try
            {
                var res = await _workerJobService.CreateTemplate(UserId, model);

                return Ok(res);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/savetemplate",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("deletetemplate/{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            LogErrorRequest error;
            try
            {
                var res = await _workerJobService.DeleteTemplate(UserId, id);

                return Ok(res);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/deletetemplate/{id}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("{jobId}/applicants")]
        public async Task<IActionResult> GetJobApplicants(int jobId)
        {
            LogErrorRequest error;
            try
            {
                var jobApplicants = await _workerJobService.GetJobApplicants(UserId, jobId, RoleId);
                return Ok(jobApplicants);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/{jobId}/applicants",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("templates")]
        public async Task<IActionResult> GetTemplates()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _workerJobService.GetTemplates(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/worker/jobs/templates",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }
    }
}
