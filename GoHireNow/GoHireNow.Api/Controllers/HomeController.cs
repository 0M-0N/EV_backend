using GoHireNow.Api.Filters;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.HomeModels;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GoHireNow.Api.Controllers
{
    [Route("home")]
    [ApiController]
    [CustomExceptionFilter]
    public class HomeController : BaseController
    {
        private readonly IHomeService _homeService;
        private readonly IClientJobService _clientJobService;
        private readonly ICustomLogService _customLogService;
        private readonly IMailService _mailService;
        public HomeController(IHomeService homeService, IClientJobService clientJobService, IMailService mailService, ICustomLogService customLogService)
        {
            _customLogService = customLogService;
            _homeService = homeService;
            _clientJobService = clientJobService;
            _mailService = mailService;
        }

        [Route("contactus")]
        [HttpPost]
        public string ContactUs(ContactUsResponse model)
        {
            LogErrorRequest error;
            try
            {
                _homeService.SubmitInquiry(model);
                return "Success";
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/home/contactus",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Route("Download/JobAttachment")]
        [HttpGet]
        public async Task<IActionResult> GetJobBlobDownload([FromQuery] int id)
        {
            LogErrorRequest error;
            try
            {
                var items = await _clientJobService.GetAttachmentUrl(id);
                if (items != null)
                {
                    var net = new System.Net.WebClient();
                    var data = net.DownloadData(items.Item2);
                    var content = new System.IO.MemoryStream(data);
                    var contentType = "APPLICATION/octet-stream";
                    var fileName = items.Item1;
                    return File(content, contentType, fileName);
                }
                return NotFound("File not found");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/home/Download/JobAttachment",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [Route("Download/MessageAttachment")]
        [HttpGet]
        public async Task<IActionResult> GetMessageBlobDownload([FromQuery] int id)
        {
            LogErrorRequest error;
            try
            {
                var items = await _mailService.GetMessageAttachment(id);
                if (items != null && !string.IsNullOrEmpty(items.Item1) && !string.IsNullOrEmpty(items.Item2))
                {
                    var net = new System.Net.WebClient();
                    var data = net.DownloadData(FilePathRoot + "/MessageAttachments/" + items.Item2);
                    var content = new System.IO.MemoryStream(data);
                    var contentType = "APPLICATION/octet-stream";
                    var fileName = items.Item1;
                    return File(content, contentType, fileName);
                }
                return NotFound("File not found");
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/home/Download/MessageAttachment",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }
    }
}