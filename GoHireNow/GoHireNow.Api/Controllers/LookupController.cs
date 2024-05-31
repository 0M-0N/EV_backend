using GoHireNow.Api.Filters;
using GoHireNow.Database;
using GoHireNow.Models.CommonModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("lookup")]
    [ApiController]
    [CustomExceptionFilter]
    public class LookupController : ControllerBase
    {
        private readonly ICustomLogService _customLogService;
        private readonly IPricingService _pricingService;
        public LookupController(IPricingService pricingService, ICustomLogService customLogService)
        {
            _pricingService = pricingService;
            _customLogService = customLogService;
        }

        [HttpGet]
        [Route("api-status")]
        public async Task<IActionResult> Get()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult("Welcome to API (v1)"));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/api-status",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("skills")]
        public async Task<IActionResult> GetSkills(int a = 1)
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult(LookupService.GlobalJobTitlesSkills));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/skills",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("countries")]
        public async Task<IActionResult> GetCountries()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult(LookupService.Countries));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/countries",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("plans")]
        public async Task<IActionResult> GetGlobalPlans()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult(LookupService.GlobalPlans));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/plans",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("plans-details")]
        public async Task<IActionResult> GetGlobalPlanDetails()
        {
            LogErrorRequest error;
            try
            {
                var result = await _pricingService.GetGlobalPlanDetails();
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/plans-details",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("job-statuses")]
        public async Task<IActionResult> GetJobStatuses()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult(LookupService.JobStatuses));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/job-statuses",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("job-types")]
        public async Task<IActionResult> GetJobTypes()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult(LookupService.JobTypes));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/job-types",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("salary-types")]
        public async Task<IActionResult> GetSalaryTypes()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult(LookupService.SalaryTypes));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/salary-types",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getreferal/{refId}")]
        public async Task<IActionResult> GetReferal(int refId)
        {
            try
            {
                var _context = new GoHireNowContext();
                var referal = await _context.Referal.FirstOrDefaultAsync(r => r.Id == refId);
                return Ok(referal);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/getreferal",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("user-types")]
        public async Task<IActionResult> GetUserTypes()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await Task.FromResult(LookupService.UserTypes));
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/lookup/user-types",
                };
                _customLogService.LogError(error);
                throw;
            }
        }
    }
}
