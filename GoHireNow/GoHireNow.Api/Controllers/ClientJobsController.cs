using GoHireNow.Api.Filters;
using GoHireNow.Api.Handlers.ClientHandlers.Interfaces;
using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("client/jobs")]
    [Authorize(Roles = "Client")]
    [ApiController]
    [CustomExceptionFilter]
    public class ClientJobsController : BaseController
    {
        private readonly IClientJobService _clientJobService;
        private readonly IClientJobHandler _clientJobHandler;
        private IHostingEnvironment _hostingEnvironment;
        private readonly IContractService _contractService;
        private readonly IPricingService _pricingService;
        private readonly ICustomLogService _customLogService;
        public IConfiguration _configuration { get; }
        private new string FilePathRoot
        {
            get
            {
                return $"{Request.Scheme}://{Request.Host}";
            }
        }

        public ClientJobsController(IClientJobService clientJobService,
            IClientJobHandler clientJobHandler,
            IContractService contractService,
            IHostingEnvironment environment,
            ICustomLogService customLogService,
            IConfiguration configuration,
            IPricingService pricingService)
        {
            _clientJobService = clientJobService;
            _clientJobHandler = clientJobHandler;
            _contractService = contractService;
            _customLogService = customLogService;
            _hostingEnvironment = environment;
            _configuration = configuration;
            _pricingService = pricingService;
            if (string.IsNullOrWhiteSpace(_hostingEnvironment.WebRootPath))
            {
                _hostingEnvironment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration["FileRootFolder"]);
            }
        }

        [HttpGet]
        [Route("average-applicants")]
        public async Task<IActionResult> GetAverageApplicants()
        {
            try
            {
                return Ok(await _clientJobService.GetAverageApplicants());
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/average-applicants",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("detail/{jobId}")]
        public async Task<IActionResult> GetJob(int jobId)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _clientJobService.GetJob(jobId, UserId, FilePathRoot, RoleId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/detail/{jobId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("all")]
        [Authorize]
        public async Task<IActionResult> GetAllJobs()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _clientJobService.GetAllJobs(UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/all",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetJobs(int? status, int page = 1, int size = 5, bool isactive = true)
        {
            LogErrorRequest error;
            try
            {
                int statusId = (status != null && status.Value > 0 && status.Value <= 3) ? status.Value : 2;
                return Ok(await _clientJobService.GetActiveJobs(UserId, page, size, statusId, RoleId, isactive));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/list",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [PricingPlanFilter(EntryType = "Job")]
        [HttpPost, DisableRequestSizeLimit]
        [Route("create")]
        public async Task<IActionResult> CreateJob([FromForm] PostJobRequest model)
        {
            LogErrorRequest error;
            try
            {
                var files = Request.Form.Files;
                if (!await _customLogService.ValidateFiles(files))
                {
                    return BadRequest();
                }

                var id = await _clientJobHandler.PostJob(UserId, model);
                if (files.Any() && id > 0)
                {
                    //var path = Path.Combine(_hostingEnvironment.WebRootPath, $"Jobs\\{id}");                
                    var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", $"Jobs\\{id}");

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    path += "\\";

                    foreach (var file in files)
                    {
                        var name = Guid.NewGuid();
                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{name}{ext}";
                        using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        var jobAttachment = new JobAttachments
                        {
                            JobId = id,
                            AttachedFile = $"{fileName}",
                            IsActive = true,
                            IsDeleted = false,
                            IsModified = false,
                            AttachmentTypeId = 1,
                            Title = file.FileName
                        };
                        await _clientJobService.AddJobAttachment(jobAttachment);
                    }
                }

                if (id > 0)
                    return Ok(id);

                return Ok("Error");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/create",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }

        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("{jobId}/update")]
        public async Task<IActionResult> UpdateJob(int jobId, [FromForm] PostJobRequest model)
        {
            LogErrorRequest error;
            try
            {
                if (!model.JobSkillIds.Any() || !LookupService.GlobalSkills.Any(x => model.JobSkillIds.Contains(x.Id)))
                {
                    ModelState.AddModelError("JobSkillIds", "Invalid jobSkillIds");
                    return BadRequest(ModelState);
                }

                //TODO: What about status of the job in case if job has been expired
                var updated = await _clientJobService.UpdateJob(UserId, jobId, model);
                if (!updated)
                    return StatusCode(500);

                var files = Request.Form.Files;
                if (!await _customLogService.ValidateFiles(files))
                {
                    return BadRequest();
                }

                if (files.Any() && updated)
                {
                    var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", $"Jobs", jobId.ToString());
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    foreach (var file in files)
                    {
                        var name = Guid.NewGuid();
                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{name}{ext}";
                        var filePath = Path.Combine(path, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        var jobAttachment = new JobAttachments
                        {
                            JobId = jobId,
                            AttachedFile = fileName,
                            IsActive = true,
                            IsDeleted = false,
                            IsModified = false,
                            AttachmentTypeId = 1,
                            Title = Path.GetFileName(file.FileName)
                        };

                        await _clientJobService.AddJobAttachment(jobAttachment);
                    }
                }
                return Ok(updated);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = $"/client/jobs/{jobId}/update",
                    UserId = $"{UserId} {jobId}"
                };

                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("pause/{jobId}")]
        [Authorize]
        public async Task<IActionResult> Pause(int jobId)
        {
            LogErrorRequest error;
            try
            {
                var newStatus = await _clientJobService.PauseJob(UserId, jobId);
                return Ok(newStatus);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/pause/" + jobId.ToString(),
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("{jobId}/applicants")]
        public async Task<IActionResult> GetJobApplicants(ApplicantFilterRequest model, int jobId, int page = 1, int size = 5)
        {
            LogErrorRequest error;
            try
            {
                var jobApplicants = await _clientJobService.GetJobApplicants(model, UserId, jobId, RoleId, page, size);
                return Ok(jobApplicants);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/{jobId}/applicants",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpDelete]
        [Route("{jobId}/applicant/{applicantId}/delete")]
        public async Task<IActionResult> DeleteJobApplicant(int jobId, string applicantId)
        {
            LogErrorRequest error;
            try
            {
                var res = await _clientJobService.DeleteJobApplicant(UserId, jobId, applicantId);
                if (res)
                    return Ok("Applicant removed from job.");

                return StatusCode(500, "Applicant not removed, please try again!");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/{jobId}/applicant/{applicantId}/delete",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpDelete]
        [Route("attachment/delete/{jobId}")]
        public async Task<IActionResult> DeleteJobAttachments(int jobId)
        {
            LogErrorRequest error;
            try
            {
                //TODO: Clean this code
                var response = new ApiResponse<bool>();
                var del = await _clientJobService.DeleteJobAttachments(UserId, jobId);
                switch (del)
                {
                    case -1:
                        new ApiResponse<bool>
                        {
                            Response = false,
                            Success = false,
                            Message = "Error has been occured during delete job attachment."
                        };
                        break;
                    case 0:
                        new ApiResponse<bool>
                        {
                            Response = false,
                            Success = false,
                            Message = "Job Attachment not found."
                        };
                        break;
                    default:
                        response = new ApiResponse<bool>
                        {
                            Response = true,
                            Success = true,
                            Message = "Job Attachment is updated successfully."
                        };
                        break;
                }
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/attachment/delete/{jobId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("rate/{jobApplicantId}/{rate}")]
        public async Task<IActionResult> Rate(int jobApplicantId, int rate)
        {
            LogErrorRequest error;
            try
            {
                var result = await _clientJobService.Rate(jobApplicantId, rate);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/rate/{jobId}/{rate}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpDelete]
        [Route("{jobId}/attachment/delete/{attachmentId}")]
        public async Task<IActionResult> DeleteJobAttachment(int jobId, int attachmentId)
        {
            LogErrorRequest error;
            try
            {
                var fileName = await _clientJobService.DeleteJobAttachment(UserId, jobId, attachmentId);

                if (string.IsNullOrEmpty(fileName))
                    throw new Exception("Error");


                var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", $"Jobs\\{jobId}");
                var filePath = $"{path}\\{fileName}";

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                return Ok("Attachment deleted");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/{jobId}/attachment/delete/{attachmentId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("applied/{userId}")]
        public async Task<IActionResult> Applied(string userId)
        {
            try
            {
                var applied = await _clientJobService.Applied(userId, UserId);
                return Ok(applied);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/applied/{userId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpGet]
        [Route("invitedJobs/{userId}")]
        public async Task<IActionResult> GetInvitedJobs(string userId)
        {
            LogErrorRequest error;
            try
            {
                var invitedJobs = await _clientJobService.GetInvitedJobs(UserId, userId);
                return Ok(invitedJobs);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/invitedJobs/{userId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Route("invite")]
        public async Task<IActionResult> Invite([FromBody] InviteRequest model)
        {
            LogErrorRequest error;
            try
            {
                if (model.jobs.Count > 0)
                {
                    _clientJobService.Invite(model, UserId);
                }
                return Ok();
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/invite",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpPost]
        [Route("{jobId}/change-status/{statusId}")]
        public async Task<IActionResult> ChangeStatus(int jobId, int statusId)
        {
            LogErrorRequest error;
            try
            {
                return Accepted(await _clientJobService.UpdateJobStatus(jobId, UserId, statusId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/{jobId}/change-status/{statusId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("invitecompanies")]
        public async Task<IActionResult> InviteCompanies([FromBody] InviteCompaniesRequest model)
        {
            try
            {
                _clientJobService.InviteCompanies(model, UserId);

                return Ok();
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/invitecompanies",
                    UserId = UserId
                };
                _customLogService.LogError(error);

                throw;
            }
        }

        [HttpDelete]
        [Route("{jobId}/delete")]
        public async Task<IActionResult> Delete(int jobId)
        {
            LogErrorRequest error;
            try
            {
                return Accepted(await _clientJobService.DeleteJob(jobId, UserId));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/client/jobs/{jobId}/delete",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }
    }
}