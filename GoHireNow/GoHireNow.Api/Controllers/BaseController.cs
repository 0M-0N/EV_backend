using GoHireNow.Models.CommonModels.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoHireNow.Api.Controllers
{
    [ApiController] // abc
    public class BaseController : ControllerBase
    {
        //testingComent
        public string UserId {
            get {
                return User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }
        public int RoleId {
            get {
                return User.FindFirstValue(ClaimTypes.Role)=="Worker"?(int)UserTypeEnum.Worker: (int)UserTypeEnum.Client;
            }
        }
        public string FilePathRoot {
            get {
                return $"{Request.Scheme}://{Request.Host}/Resources";
            }
        }
    }
}