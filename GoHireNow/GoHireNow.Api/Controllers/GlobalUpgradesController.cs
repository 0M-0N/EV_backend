using GoHireNow.Api.Filters;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using GoHireNow.Models.CommonModels;

namespace GoHireNow.Api.Controllers
{
    [Route("globalupgrades")]
    [ApiController]
    [CustomExceptionFilter]

    public class GlobalUpgradesController : BaseController
    {
        private readonly IGlobalUpgradesService _globalUpgradesService;
        private readonly ICustomLogService _customLogService;

        public GlobalUpgradesController(IGlobalUpgradesService globalUpgradesService, ICustomLogService customLogService)
        {
            _globalUpgradesService = globalUpgradesService;
            _customLogService = customLogService;
        }

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetGlobalUpgrades()
        {
            LogErrorRequest error;
            try
            {
                return Ok(await _globalUpgradesService.GetGlobalUpgrades());
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/globalupgrades/get",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }
    }
}