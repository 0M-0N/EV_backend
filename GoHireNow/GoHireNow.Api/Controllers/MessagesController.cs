using GoHireNow.Api.Filters;
using GoHireNow.Database;
using GoHireNow.Identity.Data;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.ConfigurationModels;
using GoHireNow.Models.MailModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PusherServer;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using Newtonsoft.Json;
using System.Globalization;
using Quartz;

namespace GoHireNow.Api.Controllers
{
    [Authorize]
    [Route("messages")]
    [ApiController]
    [CustomExceptionFilter]
    public class MessagesController : BaseController
    {
        private readonly IMailService _mailService;
        private readonly PusherSettings _pusherSettings;
        private readonly ISchedulerFactory _schedulerFactory;
        private IHostingEnvironment _hostingEnvironment;
        private readonly IContractService _contractService;
        private readonly ICustomLogService _customLogService;
        public IConfiguration _configuration { get; }
        private Pusher pusher;

        public MessagesController(IMailService mailService,
            IOptions<PusherSettings> pusherSettings,
            IContractService contractService,
            ISchedulerFactory schedulerFactory,
            ICustomLogService customLogService,
            IHostingEnvironment environment,
            IConfiguration configuration)
        {
            _mailService = mailService;
            _contractService = contractService;
            _customLogService = customLogService;
            _schedulerFactory = schedulerFactory;
            _pusherSettings = pusherSettings.Value;
            _hostingEnvironment = environment;
            _configuration = configuration;
            if (string.IsNullOrWhiteSpace(_hostingEnvironment.WebRootPath))
            {
                _hostingEnvironment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration["FileRootFolder"]);
            }
            var options = new PusherOptions
            {
                Cluster = _pusherSettings.AppCluster,
                Encrypted = true
            };

            pusher = new Pusher(
                _pusherSettings.AppId,
                _pusherSettings.AppKey,
                _pusherSettings.AppSecret,
                options
            );
        }

        [HttpPost]
        [Route("auth")]
        public IActionResult Auth([FromForm] string channel_name, [FromForm] string socket_id)
        {
            LogErrorRequest error;
            try
            {
                var auth = pusher.Authenticate(channel_name, socket_id);
                var json = auth.ToJson();
                return new ContentResult { Content = json, ContentType = "application/json" };
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/auth",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [PricingPlanFilter(EntryType = "Contacts")]
        [HttpPost]
        [Route("intial/send")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest message)
        {
            LogErrorRequest error;
            try
            {
                if (message.fromUserId != UserId || message.fromUserId == message.toUserId)
                {
                    return Ok(new { Status = "error", Response = "Message is saved but not sent successfully" });
                }

                var send = await _mailService.InitialSendMessage(message);
                if (send > 0)
                {
                    await SendMessageToUser(send);
                    return Ok(new { Status = "success", Code = "1", Response = "Message sent successfully" });
                }
                if (send == -2)
                {
                    return Ok(new { Status = "error", Code = "-1", Response = "You have reached your maximum new contacts for today" });
                }
                return Ok(new { Status = "error", Code = "0", Response = "Message is saved but not sent successfully" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/intial/send",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("typing/{mailId}/{typing}")]
        [Authorize]
        public async Task<IActionResult> Typing(int mailId, bool typing)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var mail = await _context.Mails.FirstOrDefaultAsync(m => (m.UserIdFrom == UserId || m.UserIdTo == UserId) && m.Id == mailId && !m.IsDeleted);
                    if (mail != null)
                    {
                        var toUserId = mail.UserIdTo == UserId ? mail.UserIdFrom : mail.UserIdTo;
                        await pusher.TriggerAsync(
                            $"typing-{toUserId}",
                            "typing",
                            new
                            {
                                mailId = mail.Id,
                                typing = typing
                            }
                        );
                    }
                }
                return Ok();
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/typing",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("loadchatsv2")]
        [Authorize]
        public async Task<IActionResult> LoadMailsV2(int size, int page, int jobid)
        {
            try
            {
                var mails = await _mailService.LoadMailsV2(UserId, FilePathRoot, jobid, size, page);
                return Ok(mails);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/loadchatsv2",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("loadcertainchatv2/{userId}")]
        public async Task<IActionResult> LoadCertainMailV2(string userId, int size = 1000, int page = 1)
        {
            LogErrorRequest error;
            try
            {
                var mails = await _mailService.LoadCertainMailV2(userId, UserId, FilePathRoot, size, page);
                return Ok(mails);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/loadcertainchatv2/{userId}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("abletoupload/{mailid}")]
        [Authorize]
        public async Task<IActionResult> AbleToUpload(int mailid)
        {
            try
            {
                var able = await _mailService.AbleToUpload(mailid, UserId);
                return Ok(able);
            }
            catch (System.Exception ex)
            {
                LogErrorRequest error;
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/abletoupload/{mailid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("loadmessagesV2/{mailid}")]
        [Authorize]
        public async Task<IActionResult> LoadMessagesV2(int mailid, int page = 1, int size = 5)
        {
            LogErrorRequest error;
            try
            {
                var messages = await _mailService.LoadMessagesV2(mailid, UserId, FilePathRoot, page, size);
                return Ok(messages);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/loadmessagesV2/{mailid}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("report")]
        public async Task<IActionResult> ReportScam([FromBody] ReportRequest model)
        {
            LogErrorRequest error;
            try
            {
                var send = await _mailService.ReportScam(model);
                return Ok(send);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/report",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("sendmessage")]
        [Authorize]
        public async Task<IActionResult> SendMailMessage([FromForm] SendMailMessageRequest model)
        {
            LogErrorRequest error;
            try
            {
                var files = Request.Form.Files;
                if (!await _customLogService.ValidateFiles(files) || (model.fromUserId != UserId && model.toUserId != UserId) || model.fromUserId == model.toUserId)
                {
                    return BadRequest();
                }

                if (files.Any())
                {
                    var file = files[0];
                    if (file != null)
                    {
                        var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Resources", "MessageAttachments");
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        path += "\\";

                        var fNAme = Guid.NewGuid();
                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{fNAme}{ext}";
                        if (System.IO.File.Exists(path + fileName))
                        {
                            System.IO.File.Delete(path + fileName);
                        }
                        using (var fileStream = new FileStream(path + fileName, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        model.FileName = file.FileName;
                        model.FilePath = fileName;
                    }
                }
                var send = await _mailService.SendMailMessage(model);
                if (send > 0)
                {
                    await SendMessageToUser(send);
                    var x = await _mailService.GetMessageById(send);
                    var fileName = x.FileName;
                    var filePath = !string.IsNullOrEmpty(x.FilePath) ? FilePathRoot.Replace("Resources", "") + "Home/Download/MessageAttachment?id=" + x.Id : "";
                    var fileExtension = !string.IsNullOrEmpty(x.FilePath) && !string.IsNullOrEmpty(x.FileName)
                                ? (
                                    LookupService.GetFileImage(Path.GetExtension(x.FileName), "") != "img"
                                        ? Path.GetExtension(x.FileName).Replace(".", "") : ""
                                ) : "";
                    var fileImage = !string.IsNullOrEmpty(x.FilePath) && !string.IsNullOrEmpty(x.FileName)
                                ? (
                                    LookupService.GetFileImage(Path.GetExtension(x.FileName), "") == "img"
                                        ? $"{FilePathRoot}/MessageAttachments/{x.FilePath}" : ""
                                ) : "";
                    using (var _context = new GoHireNowContext())
                    {
                        var list = await _mailService.UnreadMailList(model.fromUserId);
                        await pusher.TriggerAsync(
                            $"unread-{model.fromUserId}",
                            "unread",
                            new
                            {
                                list = list
                            }
                        );
                    }

                    return Ok(new { Status = "success", Response = "Message sent successfully", messageId = send, fileName, filePath, fileExtension, fileImage });
                }
                return Ok(new { Status = "error", Response = "Message is saved but not sent successfully", Id = send });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/sendmessage",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        [Route("editmessage")]
        [Authorize]
        public async Task<IActionResult> EditMailMessage([FromForm] EditMailMessageRequest model)
        {
            LogErrorRequest error;
            try
            {
                var result = await _mailService.EditMailMessage(model, UserId);
                if (result)
                {
                    using (var _context = new GoHireNowContext())
                    {
                        var message = await _context.MailMessages.FirstOrDefaultAsync(x => x.Id == model.messageId);
                        if (message != null)
                        {
                            var toUserId = message.ToUserId == UserId ? message.FromUserId : message.ToUserId;
                            await pusher.TriggerAsync(
                                $"editmessage-{toUserId}",
                                "editmessage",
                                new
                                {
                                    messageId = model.messageId,
                                    message = model.message,
                                    mailId = message.MailId
                                });
                        }
                    }
                }

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/editmessage",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("sendmessageByJob")]
        public async Task<IActionResult> SendMailMessageByJobId([FromBody] SendMailMessageRequest message)
        {
            LogErrorRequest error;
            try
            {
                var send = await _mailService.SendMailMessageByJob(message);
                if (send > 0)
                {
                    await SendMessageToUser(send);
                    return Ok(new { Status = "success", Response = "Message sent successfully" });
                }
                return Ok(new { Status = "error", Response = "Message is saved but not sent successfully" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/sendmessageByJob",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpDelete]
        [Route("deletemessage/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            LogErrorRequest error;
            try
            {
                var send = await _mailService.DeleteMessage(id, UserId);

                if (send)
                {
                    using (var _context = new GoHireNowContext())
                    {
                        var message = await _context.MailMessages.FirstOrDefaultAsync(x => x.Id == id);
                        if (message != null)
                        {
                            var toUserId = message.ToUserId == UserId ? message.FromUserId : message.ToUserId;
                            await pusher.TriggerAsync(
                                $"deletemessage-{toUserId}",
                                "deletemessage",
                                new
                                {
                                    messageId = id,
                                    mailId = message.MailId
                                });
                        }
                    }

                    return Ok(new { Status = "success", Response = "Chat is deleted successfully" });
                }
                return Ok(new { Status = "error", Response = "Error occured during deleting chat" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/deletechat/{id}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpDelete]
        [Route("deletechat/{id}")]
        public async Task<IActionResult> DeleteChat(int id)
        {
            LogErrorRequest error;
            try
            {
                var send = await _mailService.DeleteMail(id, UserId);
                if (send)
                {
                    return Ok(new { Status = "success", Response = "Chat is deleted successfully" });
                }
                return Ok(new { Status = "error", Response = "Error occured during deleting chat" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/deletechat/{id}",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("unreadList")]
        public async Task<IActionResult> UnreadMailList()
        {
            var list = await _mailService.UnreadMailList(UserId);
            return Ok(list);
        }

        [HttpPost]
        [Route("unreadmail")]
        [Authorize]
        public async Task<IActionResult> UnreadMail([FromQuery] int id)
        {
            LogErrorRequest error;
            try
            {
                var send = await _mailService.UnreadMail(UserId, id);
                using (var _context = new GoHireNowContext())
                {
                    var mail = await _context.Mails.Where(x => x.Id == id).FirstOrDefaultAsync();
                    var listFrom = await _mailService.UnreadMailList(mail.UserIdFrom);
                    var listTo = await _mailService.UnreadMailList(mail.UserIdTo);
                    await pusher.TriggerAsync(
                        $"unread-{mail.UserIdFrom}",
                        "unread",
                        new
                        {
                            list = listFrom
                        }
                    );
                    await pusher.TriggerAsync(
                        $"unread-{mail.UserIdTo}",
                        "unread",
                        new
                        {
                            list = listTo
                        }
                    );
                }
                if (send)
                {
                    return Ok(new { Status = "success", Response = "Chat is read successfully" });
                }
                return Ok(new { Status = "error", Response = "Error occured during deleting read" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/unreadmail",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("confirmschedule")]
        [Authorize]
        public async Task<IActionResult> ConfirmSchedule([FromBody] ConfirmScheduleRequest request)
        {
            LogErrorRequest error;
            try
            {
                var interview = await _mailService.ConfirmSchedule(request.interviewId, request.datetime, request.messageId, request.timezone, request.offset, UserId);
                string format = "yyyy-MM-dd HH:mm:ss";

                if (interview != null)
                {
                    await pusher.TriggerAsync(
                        $"incomingInterview-{interview.FromId}",
                        "incomingInterview",
                        new
                        {
                            interview = new
                            {
                                id = interview.Id,
                                fromId = interview.FromId,
                                toUserId = interview.ToUserId,
                                fromLink = interview.FromLink,
                                toLink = interview.ToLink,
                                status = interview.Status,
                                mailId = interview.MailId,
                                dateTime = interview.DateTime,
                                createdDate = interview.CreatedDate,
                                isDeleted = interview.IsDeleted,
                                fromTimezone = interview.FromTimezone,
                                toTimezone = interview.ToTimezone,
                                fromOffset = interview.FromOffset,
                                toOffset = interview.ToOffset
                            }
                        }
                    );

                    DateTime datetime = DateTime.ParseExact(request.datetime, format, CultureInfo.InvariantCulture);
                    DateTimeOffset scheduledTime = new DateTimeOffset(datetime, TimeSpan.Zero).AddMinutes(-20);
                    if (scheduledTime > DateTime.UtcNow)
                    {
                        IScheduler scheduler = await _schedulerFactory.GetScheduler();
                        await scheduler.Start();

                        IJobDetail job1 = JobBuilder.Create<SendMeetingLinkJob>()
                            .WithIdentity("Interview job - " + request.interviewId, "Interview")
                            .UsingJobData("InterviewId", request.interviewId)
                            .Build();

                        ITrigger trigger1 = TriggerBuilder.Create()
                            .WithIdentity("Interview trigger - " + request.interviewId, "Interview")
                            .StartAt(scheduledTime)
                            .Build();

                        await scheduler.ScheduleJob(job1, trigger1);
                    }
                    else
                    {
                        SendMeetingLink(request.interviewId);
                    }

                    return Ok(true);
                }

                return Ok(false);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/confirmschedule",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("declineschedule/{interviewId}/{messageId}")]
        [Authorize]
        public async Task<IActionResult> DeclineSchedule(int interviewId, int messageId)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var interview = await _context.Interviews.Where(x => x.Id == interviewId && x.IsDeleted == 0 && (x.FromId == UserId || x.ToUserId == UserId)).FirstOrDefaultAsync();
                    if (interview == null)
                    {
                        return Ok();
                    }

                    var schedules = await _context.InterviewsSchedules.Where(x => x.InterviewId == interviewId && x.IsDeleted == 0).ToListAsync();
                    if (schedules != null && schedules.Count() > 0)
                    {
                        foreach (var item in schedules)
                        {
                            item.IsDeleted = 1;
                        }
                    }

                    var mailMessage = await _context.MailMessages.Where(x => x.Id == messageId && x.IsDeleted == false).FirstOrDefaultAsync();

                    if (mailMessage != null)
                    {
                        mailMessage.CustomIdType = 4;
                    }

                    await _context.SaveChangesAsync();

                    var worker = await _context.AspNetUsers.Where(x => x.Id == interview.ToUserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    var company = await _context.AspNetUsers.Where(x => x.Id == interview.FromId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (worker != null && company != null)
                    {
                        string subject = "Interview declined - " + worker.FullName;
                        string headtitle = "Interview declined - " + worker.FullName;
                        string message = worker.FullName + " has declined your interview proposed days and times.";
                        string description = "You can propose new dates and times at any time by creating a new interview.";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/messages/" + interview.MailId;
                        string buttoncaption = "VIEW MESSAGES";
                        await _contractService.NewMailService(0, 43, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }

                    return Ok(interviewId);
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/declineschedule",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getincomingschedule/{mailId}")]
        [Authorize]
        public async Task<IActionResult> GetIncomingInterview(int mailId)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var interview = await _context.Interviews.Where(x => x.Status == 1 && x.DateTime != null && x.MailId == mailId && x.IsDeleted == 0 && (x.FromId == UserId || x.ToUserId == UserId)).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                    if (interview != null && DateTime.Parse(interview.DateTime.ToString()) > DateTime.UtcNow)
                    {
                        return Ok(interview);
                    }

                    return Ok();
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/getincomingschedule",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("cancelinterview/{interviewId}")]
        [Authorize]
        public async Task<IActionResult> CancelInterview(int interviewId)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var interview = await _context.Interviews.Where(x => x.Id == interviewId && x.IsDeleted == 0 && (x.FromId == UserId || x.ToUserId == UserId)).FirstOrDefaultAsync();

                    if (interview != null)
                    {
                        interview.Status = 3;

                        var message1 = await _context.MailMessages.Where(x => x.CustomLink == interview.FromLink && x.IsDeleted == false).FirstOrDefaultAsync();
                        if (message1 != null)
                        {
                            message1.CustomId = -1;
                        }
                        var message2 = await _context.MailMessages.Where(x => x.CustomLink == interview.ToLink && x.IsDeleted == false).FirstOrDefaultAsync();
                        if (message2 != null)
                        {
                            message2.CustomId = -1;
                        }
                        IScheduler scheduler = await _schedulerFactory.GetScheduler();
                        TriggerKey triggerKey = new TriggerKey("Interview trigger - " + interviewId, "Interview");
                        ITrigger trigger = await scheduler.GetTrigger(triggerKey);
                        if (trigger != null)
                        {
                            await scheduler.UnscheduleJob(trigger.Key);
                        }

                        await _context.SaveChangesAsync();

                        var userId = interview.FromId == UserId ? interview.ToUserId : interview.FromId;

                        await pusher.TriggerAsync(
                            $"incomingInterview-{userId}",
                            "incomingInterview",
                            new
                            {
                                interview = new
                                {
                                    id = -1,
                                }
                            }
                        );

                        var from = await _context.AspNetUsers.Where(x => x.Id == UserId && x.IsDeleted == false).FirstOrDefaultAsync();
                        var to = await _context.AspNetUsers.Where(x => x.Id == (interview.FromId == UserId ? interview.ToUserId : interview.FromId) && x.IsDeleted == false).FirstOrDefaultAsync();
                        if (from != null && to != null)
                        {
                            string subject = "The scheduled interview is canceled - " + (from.UserType == 1 ? from.Company : from.FullName);
                            string headtitle = "The scheduled interview is canceled - " + (from.UserType == 1 ? from.Company : from.FullName);
                            string message = (from.UserType == 1 ? from.Company : from.FullName) + " has canceled the scheduled interview.";
                            string description = "";
                            string buttonurl = _configuration.GetValue<string>("WebDomain") + "/messages/" + interview.MailId;
                            string buttoncaption = "VIEW MESSAGES";
                            await _contractService.NewMailService(0, 44, to.Email, to.UserType == 1 ? to.Company : to.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                        }

                        return Ok(interviewId);
                    }

                    return Ok();
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/cancelinterview",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("getschedules/{interviewId}")]
        public async Task<IActionResult> GetSchedules(int interviewId)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var schedules = await _context.InterviewsSchedules.Where(x => x.InterviewId == interviewId && x.IsDeleted == 0).ToListAsync();

                    return Ok(schedules);
                }
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/getschedules",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("createroom")]
        public async Task<IActionResult> CreateRoom()
        {
            LogErrorRequest error;
            try
            {
                var token = GetToken();
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var createRequest = new CreateRoomRequest();
                createRequest.name = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                createRequest.description = "evirtualassistants";
                createRequest.template_id = "64ee54fff84abaf06fb3697a";

                var createResponse = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("100MSAPIDomain") + "/rooms", createRequest);
                string response = await createResponse.Content.ReadAsStringAsync();
                CreateRoomResponse result = JsonConvert.DeserializeObject<CreateRoomResponse>(response);

                if (!string.IsNullOrEmpty(result.id))
                {
                    var createRoomCodeResponse = await httpClient.PostAsJsonAsync(_configuration.GetValue<string>("100MSAPIDomain") + "/room-codes/room/" + result.id, new { });
                    string roomCodeResponse = await createRoomCodeResponse.Content.ReadAsStringAsync();
                    object roomCodeData = JsonConvert.DeserializeObject<object>(roomCodeResponse);

                    return Ok(roomCodeData);
                }

                return Ok(response);
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/createroom",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("sendschedule")]
        [Authorize]
        public async Task<IActionResult> SendSchedule([FromBody] StartNowRequest request)
        {
            LogErrorRequest error;
            try
            {
                var id = await _mailService.SendSchedule(request);
                if (id > 0 && request.fromId == UserId)
                {
                    return Ok(new { Status = "success", Response = "Ok", MeetingId = id });
                }
                return Ok(new { Status = "error", Response = "Bad Reqeust" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/sendschedule",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpPost]
        [Route("startnow")]
        public async Task<IActionResult> StartNowMeeting([FromBody] StartNowRequest request)
        {
            LogErrorRequest error;
            try
            {
                var id = await _mailService.StartNowMeeting(request);
                if (id > 0)
                {
                    return Ok(new { Status = "success", Response = "Ok", MeetingId = id });
                }
                return Ok(new { Status = "error", Response = "Bad Reqeust" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/startnow",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        [HttpGet]
        [Route("unreadmailcount")]
        public async Task<IActionResult> UnreadMailCount()
        {
            LogErrorRequest error;
            try
            {
                var unread = await _mailService.GetUnreadMailCount(UserId);
                if (unread > -1)
                {
                    return Ok(new { Status = "success", count = unread });
                }
                return Ok(new { Status = "error", count = 0, Response = "Some Error occured" });
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/unreadmailcount",
                    UserId = UserId
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        #region Send Message to User

        public async Task<bool> SendMessageToUser(int id)
        {
            var message = await _mailService.GetMessageById(id);
            if (message == null)
            {
                return false;
            }
            await pusher.TriggerAsync(
                $"private-{message.ToUserId}",
                "message",
                new
                {
                    message = message.Message,
                    from = message.FromUserId,
                    name = message.FromUser?.UserType == 1 ? message.FromUser.Company : message.FromUser.FullName,
                    date = message.CreateDate,
                    mailId = message.MailId,
                    picture = !string.IsNullOrEmpty(message.FromUser?.ProfilePicture)
                                    ? $"{FilePathRoot}/Profile-Pictures/" + message.FromUser?.ProfilePicture
                                    : "",
                    sent = false,
                    id = message.MailId, // This is for if we don't have chat then we have to append a chat
                    title = message.FromUser?.UserTitle, // This is for if we don't have chat then we have to append a chat
                    lastLogin = message.FromUser?.LastLoginTime, // This is for if we don't have chat then we have to append a chat,
                    mailDate = message.Mail?.CreateDate.ToShortDateString(), // This is used to sort mails on the client side.
                    fromUserId = message.ToUserId,
                    toUserId = message.FromUserId,
                    isRead = false,
                    myPicture = !string.IsNullOrEmpty(message.ToUser?.ProfilePicture)
                                    ? $"{FilePathRoot}/Profile-Pictures/" + message.ToUser?.ProfilePicture
                                    : "",
                    userType = (int)message.FromUser?.UserType,
                    fileName = message.FileName,
                    filePath = !string.IsNullOrEmpty(message.FilePath) ? FilePathRoot.Replace("Resources", "") + "Home/Download/MessageAttachment?id=" + message.Id : "",
                    fileExtension = !string.IsNullOrEmpty(message.FilePath) && !string.IsNullOrEmpty(message.FileName)
                                        ? (
                                            LookupService.GetFileImage(Path.GetExtension(message.FileName), "") != "img"
                                                ? Path.GetExtension(message.FileName).Replace(".", "") : ""
                                        ) : "",
                    fileImage = !string.IsNullOrEmpty(message.FilePath) && !string.IsNullOrEmpty(message.FileName)
                                        ? (
                                            LookupService.GetFileImage(Path.GetExtension(message.FileName), "") == "img"
                                                ? $"{FilePathRoot}/MessageAttachments/{message.FilePath}" : ""
                                        ) : "",
                    messageId = message.Id,
                    customId = message.CustomId,
                    customIdType = message.CustomIdType,
                    customLink = message.CustomLink
                });
            return true;
        }

        #endregion

        private string GetToken()
        {
            var appAccessKey = _configuration.GetValue<string>("appAccessKey");
            var appSecret = _configuration.GetValue<string>("appSecret");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(appSecret);
            string jwtid = Guid.NewGuid().ToString();

            var claims = new[]
            {
                new Claim("access_key", appAccessKey),
                new Claim("type", "management"),
                new Claim("jti", jwtid)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return tokenString;
        }

        public async Task SendMeetingLink(int interviewId)
        {
            LogErrorRequest error;
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var interview = await _context.Interviews.FirstOrDefaultAsync(i => i.Id == interviewId && i.IsDeleted == 0);
                    if (interview == null)
                    {
                        return;
                    }

                    var worker = await _context.AspNetUsers.Where(x => x.Id == interview.ToUserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    var company = await _context.AspNetUsers.Where(x => x.Id == interview.FromId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (worker != null && company != null)
                    {
                        string subject = "Interview starting in 20 minutes - " + worker.FullName;
                        string headtitle = "Interview starting in 20 minutes - " + worker.FullName;
                        string message = "You have an interview starting in 20 minutes with " + worker.FullName + ", click on this button to start the interview.";
                        string description = "";
                        string buttonurl = interview.FromLink;
                        string buttoncaption = "START NOW";
                        await _contractService.NewMailService(0, 47, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");

                        subject = "Interview starting in 20 minutes - " + company.Company;
                        headtitle = "Interview starting in 20 minutes - " + company.Company;
                        message = "You have an interview starting in 20 minutes with " + company.Company + ", click on this button to start the interview.";
                        buttonurl = interview.ToLink;

                        await _contractService.NewMailService(0, 47, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }

                    var messageModel = new SendMailMessageRequest()
                    {
                        fromUserId = interview.FromId,
                        toUserId = interview.ToUserId,
                        mailId = interview.MailId,
                        customLink = interview.ToLink,
                        customIdType = 2,
                        message = "INTERVIEW IN 20 MINUTES\n Please click on this button\n to start the interview."
                    };
                    var messageId1 = await _mailService.SendMailMessage(messageModel);

                    messageModel = new SendMailMessageRequest()
                    {
                        fromUserId = interview.ToUserId,
                        toUserId = interview.FromId,
                        mailId = interview.MailId,
                        customLink = interview.FromLink,
                        customIdType = 2,
                        message = "INTERVIEW IN 20 MINUTES\n Please click on this button\n to start the interview."
                    };
                    var messageId2 = await _mailService.SendMailMessage(messageModel);

                    if (messageId1 > 0)
                    {
                        await SendMessageToUser(messageId1);
                    }
                    if (messageId2 > 0)
                    {
                        await SendMessageToUser(messageId2);
                    }
                };
            }
            catch (System.Exception ex)
            {
                error = new LogErrorRequest()
                {
                    ErrorMessage = ex.Message.ToString(),
                    ErrorUrl = "/messages/sendmeetinglink",
                };
                _customLogService.LogError(error);
                throw;
            }
        }

        public class CreateRoomRequest
        {
            public string name { get; set; }
            public string description { get; set; }
            public string template_id { get; set; }
        }

        public class CreateRoomResponse
        {
            public string id { get; set; }
            public string name { get; set; }
            public bool enabled { get; set; }
            public string description { get; set; }
            public string customer_id { get; set; }
            public string app_id { get; set; }
            public string template_id { get; set; }
            public string template { get; set; }
            public object recording_info { get; set; }
            public string region { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
            public string customer { get; set; }
        }
    }
}