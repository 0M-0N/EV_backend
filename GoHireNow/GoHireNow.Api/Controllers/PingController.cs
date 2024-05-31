using GoHireNow.Api.Filters;
using GoHireNow.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("ping")]
    [ApiController]
    [CustomExceptionFilter]
    public class PingController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public PingController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Ping()
        {
            try
            {
                var user = _userManager.Users.FirstOrDefault();
                if (user != null)
                {
                    return Ok("ping");
                }
                else
                {
                    return BadRequest();
                }
            }
            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
