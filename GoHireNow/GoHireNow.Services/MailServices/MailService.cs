using GoHireNow.Database;
using GoHireNow.Models.CommonModels.Enums;
using GoHireNow.Models.MailModels;
using GoHireNow.Service.CommonServices;
using GoHireNow.Service.Interfaces;
using GoHireNow.Models.ExceptionModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GoHireNow.Database.GoHireNowTools;
using GoHireNow.Database.GoHireNowTools.Models;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GoHireNow.Service.MailServices
{
    public class MailService : IMailService
    {
        private IConfiguration _configuration { get; }
        private readonly IContractService _contractService;
        private readonly IPricingService _pricingService;
        public MailService(IContractService contractService, IConfiguration configuration, IPricingService pricingService)
        {
            _contractService = contractService;
            _pricingService = pricingService;
            _configuration = configuration;
        }

        public async Task<int> InitialSendMessage(SendMessageRequest message)
        {
            var _context = new GoHireNowContext();
            var mail = await _context.Mails.FirstOrDefaultAsync(x =>
                x.UserIdFrom == message.fromUserId &&
                x.UserIdTo == message.toUserId &&
                x.IsDeleted == false);

            var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == message.fromUserId);
            if (user != null)
            {
                user.LastLoginTime = DateTime.UtcNow;
            }

            if (mail != null)
            {
                mail.IsRead = false;
                mail.ModifiedDate = DateTime.UtcNow;
                var mailMessage = new MailMessages
                {
                    FromUserId = message.fromUserId,
                    ToUserId = message.toUserId,
                    Message = message.message,
                    CreateDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    IsDeleted = false,
                    IsRead = false,
                    CustomId = message.customId,
                    CustomIdType = message.customIdType,
                    CustomLink = message.customLink,
                    MailId = mail.Id
                };
                await _context.MailMessages.AddAsync(mailMessage);
                await _context.SaveChangesAsync();
                return mailMessage.Id;
            }
            else
            {
                if (message.isDirectMessage && user.UserType == 1)
                {
                    var isApplied = false;
                    var jobs = await _context.Jobs.Where(j => j.UserId == user.Id && !j.IsDeleted && j.JobStatusId < 3).Include(j => j.JobApplications).ToListAsync();
                    foreach (var job in jobs)
                    {
                        var userIds = job.JobApplications.Where(x => !x.IsDeleted).Select(ja => ja.UserId).ToList();
                        if (userIds.Contains(message.toUserId))
                        {
                            isApplied = true;
                            break;
                        }
                    }

                    if (!isApplied)
                    {
                        var planDetail = await _pricingService.GetSubscriptionDetails(user.Id);
                        var maximumLimit = 5;
                        if (planDetail?.SubscriptionStatus?.Id == 3 || planDetail?.SubscriptionStatus?.Id == 21)
                        {
                            maximumLimit = 35;
                        }
                        else
                        {
                            if (planDetail?.SubscriptionStatus?.Id == 5 || planDetail?.SubscriptionStatus?.Id == 22)
                            {
                                maximumLimit = 70;
                            }
                        }

                        var directMessagesToday = await _context.Mails.Where(m => m.UserIdFrom == message.fromUserId && !m.IsDeleted && m.IsDirect == 1 && m.CreateDate >= DateTime.UtcNow.Date).ToListAsync();
                        if (directMessagesToday != null && directMessagesToday.Count() >= maximumLimit)
                        {
                            return -2;
                        }
                    }
                }
                mail = new Mails
                {
                    UserIdFrom = message.fromUserId,
                    UserIdTo = message.toUserId,
                    CreateDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Ipaddress = "123",
                    IsActive = true,
                    IsDeleted = false,
                    IsRead = false,
                    Title = "Message",
                    IsDirect = message.isDirectMessage ? 1 : 0,
                };
                await _context.Mails.AddAsync(mail);
                var mailMessage = new MailMessages
                {
                    FromUserId = message.fromUserId,
                    ToUserId = message.toUserId,
                    Message = message.message,
                    CreateDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    IsDeleted = false,
                    IsRead = false,
                    CustomId = message.customId,
                    CustomIdType = message.customIdType,
                    CustomLink = message.customLink,
                    //JobId = message.jobId,
                    MailId = mail.Id
                };
                await _context.MailMessages.AddAsync(mailMessage);
                await _context.SaveChangesAsync();
                TimeSpan diff = DateTime.Now.AddMinutes(10) - DateTime.Now;
                double seconds = diff.TotalSeconds;
                var unreadMessages = _context.MailMessages
                    .Include(o => o.Mail).Where(o => o.ToUserId == mailMessage.ToUserId
                    && o.MailId == mailMessage.MailId && o.Mail.IsDeleted == false && o.IsRead == false)
                    .Count();
                if (unreadMessages > 0)
                {
                    await Task.Factory.StartNew(async () =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(seconds)).ContinueWith(async (t) =>
                        {
                            var status = _context.spGetBoolResult.FromSql("spGetUsersUnreadMessageEmailStatus @FromUserId = {0},@ToUserId = {1},@CreatedDate = {2}",
                                mailMessage.FromUserId, mailMessage.ToUserId, DateTime.UtcNow).FirstOrDefault();
                            if (status.Result)
                            {
                                var result = _context.MailMessages.Where(o => o.Id == mailMessage.Id).Include(o => o.FromUser).Include(o => o.ToUser).FirstOrDefault();
                                string htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "NewMessage.html");
                                var image = (int)UserTypeEnum.Client == result.FromUser.UserType ? LookupService.FilePaths.ClientDefaultImageFilePath : LookupService.FilePaths.WorkerDefaultImageFilePath;
                                result.FromUser.ProfilePicture = !string.IsNullOrEmpty(result.FromUser.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{result.FromUser.ProfilePicture}" : $"{image}";

                                StringBuilder sb = new StringBuilder();
                                var name = (int)UserTypeEnum.Client == result.FromUser.UserType ? result.FromUser.Company : result.FromUser.FullName;
                                var workertitle = (int)UserTypeEnum.Client == result.FromUser.UserType ? "" : result.FromUser.UserTitle;
                                sb.AppendLine("<table style=\"border-collapse: collapse;\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\">");
                                sb.AppendLine("            <tr style=\"text-align:center; height: 128px; width: 70%\">");
                                sb.AppendLine("                <td width=\"15%\" style=\"border: none\"></td>");
                                sb.AppendLine("                <td style=\"padding-left: 40px; width: 82px; border: 1px solid #DCDCDC;border-right: none\";><img width='82' height='82' style=\"");
                                sb.AppendLine("                                ");
                                sb.AppendLine("                                max-height: 82px;");
                                sb.AppendLine("                                border-radius: 50%;");
                                sb.AppendLine("                                min-height: 60px;\" src=\"" + result.FromUser.ProfilePicture + "\" alt=\"\">");
                                sb.AppendLine("                </td>");
                                sb.AppendLine("                <td style=\"font-family: Lato, sans-serif;font-size: 18px;height: 100px;padding-left: 40px;text-align: start;border: 1px solid #DCDCDC;border-right: none;border-left: none\">");
                                sb.AppendLine("                    <div style=\"color: #333333;font-family: 'Lato-Black',sans-serif;font-size: 18px;font-weight: 900;line-height: 18px;padding-bottom: 10px\">" + name + "</div>");
                                sb.AppendLine("                    <div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px\">" + workertitle + "</div>");
                                sb.AppendLine("                </td>");
                                sb.AppendLine("                <td style=\"");
                                sb.AppendLine("                    text-align: end;");
                                sb.AppendLine("                    font-family: Lato, sans-serif;");
                                sb.AppendLine("                    font-size: 18px;");
                                sb.AppendLine("                    padding-right: 60px;");
                                sb.AppendLine("                    height: 100px;");
                                sb.AppendLine("                    border: 1px solid #DCDCDC;");
                                sb.AppendLine("                    border-left: none;\"");
                                sb.AppendLine("                >");
                                sb.AppendLine("                   ");
                                sb.AppendLine("                </td>");
                                sb.AppendLine("                <td width=\"15%\" style=\"border: none\"></td>");
                                sb.AppendLine("            </tr>");
                                sb.AppendLine("          ");
                                sb.AppendLine("        </table>");
                                htmlContent = htmlContent.Replace("[From]", sb.ToString());
                                htmlContent = htmlContent.Replace("[UserName]", result.FromUser.UserType == (int)UserTypeEnum.Client ? result.FromUser.Company : result.FromUser.FullName);
                                htmlContent = htmlContent.Replace("[Url]", LookupService.FilePaths.MessageUrl);

                                using (var _toolsContext = new GoHireNowToolsContext())
                                {
                                    var sender = new mailer_sender();
                                    sender.ms_custom_id = 0;
                                    sender.ms_custom_type = 12;
                                    sender.ms_date = DateTime.Now;
                                    sender.ms_send_date = DateTime.Now;
                                    sender.ms_email = result.ToUser.Email;
                                    sender.ms_name = "";
                                    sender.ms_subject = "You received a new message";
                                    sender.ms_message = htmlContent;
                                    sender.ms_from_email = "no-reply@evirtualassistants.com";
                                    sender.ms_from_name = "eVirtualAssistants";
                                    sender.ms_priority = 1;
                                    sender.ms_issent = 0;
                                    sender.ms_unsubscribe = Guid.NewGuid();

                                    await _toolsContext.mailer_sender.AddAsync(sender);
                                    await _toolsContext.SaveChangesAsync();
                                }
                            }
                        });
                    });
                }

                return mailMessage.Id;
            }
        }

        public async Task<MailMessages> GetMessageById(int id)
        {
            using (var _context = new GoHireNowContext())
            {
                return await _context.MailMessages
                    .Include(x => x.Mail)
                    .Include(x => x.FromUser)
                    .Include(x => x.ToUser)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
        }

        public async Task<Tuple<string, string>> GetMessageAttachment(int id)
        {
            using (var _context = new GoHireNowContext())
            {
                var message = await _context.MailMessages
                    .FirstOrDefaultAsync(x => x.Id == id);
                return Tuple.Create<string, string>(message.FileName, message.FilePath);
            }
        }

        public async Task<List<MailChatResponse>> LoadMailsV2(string userId, string path, int jobid, int size = 5, int page = 1)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    user.LastLoginTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                int skip = page > 1 ? ((page - 1) * size) : 0;

                var list = new List<MailChatResponse>();
                var appliedUserIds = new List<string>();
                if (jobid > 0)
                {
                    appliedUserIds = await _context.JobApplications.Where(j => j.JobId == jobid && !j.IsDeleted).Select(x => x.UserId).ToListAsync();
                }
                var mails = await _context.Mails
                    .Include(x => x.UserIdFromNavigation)
                    .Include(x => x.UserIdToNavigation)
                    .Where(x => (x.UserIdFrom == userId || x.UserIdTo == userId) && x.IsDeleted == false && !x.UserIdFromNavigation.IsDeleted && !x.UserIdToNavigation.IsDeleted && (jobid == 0 ? true : appliedUserIds.Contains(x.UserIdFrom == userId ? x.UserIdTo : x.UserIdFrom)))
                    .GroupBy(x => x.Id)
                    .ToListAsync();
                var count = mails != null ? mails.Count : 0;
                var orderedMails = mails.OrderByDescending(x => x.FirstOrDefault().ModifiedDate).Skip(skip).Take(size);
                foreach (var item in orderedMails)
                {
                    var res = new MailChatResponse();

                    res.Id = item.FirstOrDefault().Id;
                    res.Name = item.FirstOrDefault().UserIdFrom == userId ?
                                            item.FirstOrDefault().UserIdToNavigation.FullName ?? item.FirstOrDefault().UserIdToNavigation.Company :
                                            item.FirstOrDefault().UserIdFromNavigation.FullName ?? item.FirstOrDefault().UserIdFromNavigation.Company;
                    res.Title = item.FirstOrDefault().UserIdFrom == userId ?
                                        item.FirstOrDefault().UserIdToNavigation.UserTitle :
                                        item.FirstOrDefault().UserIdFromNavigation.UserTitle;
                    res.Picture = item.FirstOrDefault().UserIdFrom == userId ?
                                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdToNavigation.ProfilePicture)
                                            ? $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdToNavigation.ProfilePicture
                                            : "")
                                        :
                                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdFromNavigation.ProfilePicture)
                                        ? $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdFromNavigation.ProfilePicture
                                        : "");
                    res.LastLogin = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdToNavigation.LastLoginTime.ToString() : item.FirstOrDefault().UserIdFromNavigation.LastLoginTime.ToString();
                    res.ToTimezone = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdToNavigation.TimeZone : item.FirstOrDefault().UserIdFromNavigation.TimeZone;
                    res.FromUserId = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdFrom : item.FirstOrDefault().UserIdTo;
                    res.ToUserId = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdTo : item.FirstOrDefault().UserIdFrom;
                    res.IsSuspended = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdToNavigation.IsSuspended : item.FirstOrDefault().UserIdFromNavigation.IsSuspended;
                    res.MyPicture = item.FirstOrDefault().UserIdFrom == userId ?
                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdFromNavigation.ProfilePicture)
                            ? $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdFromNavigation.ProfilePicture : "")
                        :
                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdToNavigation.ProfilePicture) ?
                            $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdToNavigation.ProfilePicture
                            : "");
                    res.MyName = item.FirstOrDefault().UserIdFrom == userId ?
                        item.FirstOrDefault().UserIdFromNavigation.UserType == 1 ? item.FirstOrDefault().UserIdFromNavigation.Company : item.FirstOrDefault().UserIdFromNavigation.FullName
                        :
                        item.FirstOrDefault().UserIdToNavigation.UserType == 1 ? item.FirstOrDefault().UserIdToNavigation.Company : item.FirstOrDefault().UserIdToNavigation.FullName;
                    res.MailDate = item.FirstOrDefault().CreateDate;
                    res.IsRead = !(_context.MailMessages.Any(x => x.MailId == item.FirstOrDefault().Id &&
                                    x.IsRead == false &&
                                    x.ToUserId == userId
                                    ));
                    res.UserType = item.FirstOrDefault().UserIdFrom == userId ?
                        (int)item.FirstOrDefault().UserIdToNavigation.UserType :
                        (int)item.FirstOrDefault().UserIdFromNavigation.UserType;
                    var lastMessage = _context.MailMessages.Where(x => x.MailId == item.FirstOrDefault().Id).LastOrDefault();
                    if (lastMessage != null)
                    {
                        res.LastMessage = !string.IsNullOrEmpty(lastMessage.Message) ? lastMessage.Message : "Attachment";
                        res.LastMessageTime = lastMessage.CreateDate;
                    }
                    res.totalCount = count;
                    res.Exchangable = false;

                    var company = item.FirstOrDefault().UserIdFromNavigation.UserType == 1 ? item.FirstOrDefault().UserIdFromNavigation : item.FirstOrDefault().UserIdToNavigation;
                    var worker = item.FirstOrDefault().UserIdFromNavigation.UserType == 2 ? item.FirstOrDefault().UserIdFromNavigation : item.FirstOrDefault().UserIdToNavigation;
                    var contracts = await _context.Contracts.Where(x => x.IsDeleted == 0 && x.isAccepted == 1 && x.AutomaticBilling == 1 && x.CompanyId == company.Id && x.UserId == worker.Id).ToListAsync();
                    if (contracts != null && contracts.Count() > 0)
                    {
                        res.Exchangable = true;
                    }
                    else
                    {
                        var toUserPlan = await _pricingService.GetCurrentPlan(company.Id);
                        if (toUserPlan.Name.Contains("Enterprise") || toUserPlan.Name.Contains("Agency"))
                        {
                            res.Exchangable = false;
                        }
                    }


                    list.Add(res);
                }
                return list;
            }
        }

        public async Task<List<MailChatResponse>> LoadCertainMailV2(string userId, string UserId, string path, int size = 5, int page = 1)
        {
            using (var _context = new GoHireNowContext())
            {
                int skip = page > 1 ? ((page - 1) * size) : 0;

                var list = new List<MailChatResponse>();
                var mails = await _context.Mails
                    .Include(x => x.UserIdFromNavigation)
                    .Include(x => x.UserIdToNavigation)
                    .Where(x => ((x.UserIdFrom == userId && x.UserIdTo == UserId) || (x.UserIdTo == userId && x.UserIdFrom == UserId)) && x.IsDeleted == false && x.UserIdFromNavigation.IsDeleted == false
                        && x.UserIdToNavigation.IsDeleted == false)
                    .GroupBy(x => x.Id)
                    .Skip(skip).Take(size)
                    .ToListAsync();
                var orderedMails = mails.OrderByDescending(x => x.FirstOrDefault().ModifiedDate);
                foreach (var item in orderedMails)
                {
                    var res = new MailChatResponse();

                    res.Id = item.FirstOrDefault().Id;
                    res.Name = item.FirstOrDefault().UserIdFrom == userId ?
                                            item.FirstOrDefault().UserIdToNavigation.FullName ?? item.FirstOrDefault().UserIdToNavigation.Company :
                                            item.FirstOrDefault().UserIdFromNavigation.FullName ?? item.FirstOrDefault().UserIdFromNavigation.Company;
                    res.Title = item.FirstOrDefault().UserIdFrom == userId ?
                                        item.FirstOrDefault().UserIdToNavigation.UserTitle :
                                        item.FirstOrDefault().UserIdFromNavigation.UserTitle;
                    res.Picture = item.FirstOrDefault().UserIdFrom == userId ?
                                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdToNavigation.ProfilePicture)
                                            ? $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdToNavigation.ProfilePicture
                                            : "")
                                        :
                                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdFromNavigation.ProfilePicture)
                                        ? $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdFromNavigation.ProfilePicture
                                        : "");
                    res.LastLogin = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdToNavigation.LastLoginTime.ToString() : item.FirstOrDefault().UserIdFromNavigation.LastLoginTime.ToString();
                    res.ToTimezone = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdToNavigation.TimeZone : item.FirstOrDefault().UserIdFromNavigation.TimeZone;
                    res.FromUserId = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdFrom : item.FirstOrDefault().UserIdTo;
                    res.IsSuspended = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdToNavigation.IsSuspended : item.FirstOrDefault().UserIdFromNavigation.IsSuspended;
                    res.ToUserId = item.FirstOrDefault().UserIdFrom == userId ? item.FirstOrDefault().UserIdTo : item.FirstOrDefault().UserIdFrom;
                    res.MyPicture = item.FirstOrDefault().UserIdFrom == userId ?
                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdFromNavigation.ProfilePicture)
                            ? $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdFromNavigation.ProfilePicture : "")
                        :
                        (!string.IsNullOrEmpty(item.FirstOrDefault().UserIdToNavigation.ProfilePicture) ?
                            $"{path}/Profile-Pictures/" + item.FirstOrDefault().UserIdToNavigation.ProfilePicture
                            : "");
                    res.MyName = item.FirstOrDefault().UserIdFrom == userId ?
                        item.FirstOrDefault().UserIdFromNavigation.UserType == 1 ? item.FirstOrDefault().UserIdFromNavigation.Company : item.FirstOrDefault().UserIdFromNavigation.FullName
                        :
                        item.FirstOrDefault().UserIdToNavigation.UserType == 1 ? item.FirstOrDefault().UserIdToNavigation.Company : item.FirstOrDefault().UserIdToNavigation.FullName;
                    res.MailDate = item.FirstOrDefault().CreateDate;
                    res.IsRead = !(_context.MailMessages.Any(x => x.MailId == item.FirstOrDefault().Id &&
                                    x.IsRead == false &&
                                    (x.FromUserId == userId ||
                                    x.ToUserId == userId)
                                    ));
                    res.UserType = item.FirstOrDefault().UserIdFrom == userId ?
                        (int)item.FirstOrDefault().UserIdToNavigation.UserType :
                        (int)item.FirstOrDefault().UserIdFromNavigation.UserType;
                    var lastMessage = _context.MailMessages.Where(x => x.MailId == item.FirstOrDefault().Id).LastOrDefault();
                    if (lastMessage != null)
                    {
                        res.LastMessage = !string.IsNullOrEmpty(lastMessage.Message) ? lastMessage.Message : "Attachment";
                        res.LastMessageTime = lastMessage.CreateDate;
                    }

                    list.Add(res);
                }
                return list;
            }
        }

        public async Task<bool> AbleToUpload(int mailId, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var mail = await _context.Mails.FirstOrDefaultAsync(m => m.Id == mailId && !m.IsDeleted);
                if (mail != null)
                {
                    var companyId = mail.UserIdFrom == userId ? mail.UserIdTo : mail.UserIdFrom;
                    var result = await _pricingService.GetSubscriptionDetails(companyId);
                    if (result != null && (result.SubscriptionStatus.PlanName.Contains("Enterprise") || result.SubscriptionStatus.PlanName.Contains("Agency")))
                    {
                        return true;
                    }

                    var contracts = await _context.Contracts.Where(x => x.IsDeleted == 0 && x.isAccepted == 1 && x.AutomaticBilling == 1 && x.CompanyId == companyId && x.UserId == userId).ToListAsync();
                    if (contracts != null && contracts.Count() > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public async Task<List<MailMessageResponse>> LoadMessagesV2(int mailId, string userId, string path, int page = 1, int size = 5)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    user.LastLoginTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var messages = _context.MailMessages
                    .Include(x => x.FromUser)
                    .Include(x => x.ToUser)
                    .Where(x => x.MailId == mailId && !x.IsDeleted && (x.FromUserId == userId || x.ToUserId == userId))
                    .OrderByDescending(x => x.CreateDate)
                    .ToList()
                    .Take(size * page)
                    .Select(x => new MailMessageResponse
                    {
                        MessageId = x.Id,
                        Date = x.CreateDate.ToString(),
                        IsEdited = x.ModifiedDate != x.CreateDate,
                        Message = x.Message,
                        Picture = x.FromUserId == userId
                            ?
                                (!string.IsNullOrEmpty(x.ToUser.ProfilePicture)
                                ? $"{path}/Profile-Pictures/" + x.ToUser.ProfilePicture
                                : ""
                                )
                            :
                                (!string.IsNullOrEmpty(x.FromUser.ProfilePicture)
                                    ? $"{path}/Profile-Pictures/" + x.FromUser.ProfilePicture
                                    : ""
                                ),
                        Sent = x.FromUserId == userId ? true : false,
                        From = x.ToUserId == userId ? x.FromUserId : x.ToUserId,
                        MailId = x.MailId,
                        Email = "",
                        Name = x.ToUserId == userId ?
                            x.FromUser.UserType == 1 ? x.FromUser.Company : x.FromUser.FullName :
                            x.ToUser.UserType == 1 ? x.ToUser.Company : x.ToUser.FullName,
                        UserType = x.ToUserId == userId ?
                            (int)x.FromUser.UserType :
                            (int)x.ToUser.UserType,
                        FileName = x.FileName,
                        FilePath = !string.IsNullOrEmpty(x.FilePath) ? path.Replace("Resources", "") + "Home/Download/MessageAttachment?id=" + x.Id : "",
                        FileExtension = !string.IsNullOrEmpty(x.FilePath) && !string.IsNullOrEmpty(x.FileName)
                            ? (
                                LookupService.GetFileImage(Path.GetExtension(x.FileName), "") != "img"
                                    ? Path.GetExtension(x.FileName).Replace(".", "") : ""
                                ) : "",
                        FileImage = !string.IsNullOrEmpty(x.FilePath) && !string.IsNullOrEmpty(x.FileName)
                            ? (
                                LookupService.GetFileImage(Path.GetExtension(x.FileName), "") == "img"
                                    ? $"{path}/MessageAttachments/{x.FilePath}" : ""
                                ) : "",
                        CustomId = x.CustomId,
                        CustomIdType = x.CustomIdType,
                        CustomLink = x.CustomLink,
                    })
                    .OrderBy(x => x.Date)
                    .ToList();
                return messages;
            }
        }

        public async Task<int> ReportScam(ReportRequest model)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.AspNetUsers.Where(x => x.Id == model.fromId && x.IsDeleted == false).FirstOrDefaultAsync();
                if (user == null)
                {
                    return 0;
                }

                string name = (user.UserType == 1) ? user.Company : user.FullName;
                var reportEmail = await _context.Emails.FirstOrDefaultAsync(m => m.UserId == model.fromId && m.Type == 1 && m.IsDeleted == 0);
                if (reportEmail == null)
                {
                    var email = new Emails()
                    {
                        Type = 1,
                        UserId = model.fromId,
                        ToUserId = model.toId,
                        IsDeleted = 0,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _context.Emails.AddAsync(email);
                    await _context.SaveChangesAsync();

                    string subject = "A friendly warning...";
                    string template = (user.UserType == 1) ? "FriendlyWarningForCompany.html" : "FriendlyWarningForWorker.html";
                    _contractService.PersonalEmailService(0, 52, user.Email, name, subject, "", name, "julia.d@evirtualassistants.com", "Julia", 1, template);
                }

                var scamMessages = await _context.MailMessagesScams.Where(x => x.FromId == model.fromId && x.IsDeleted == 0 && x.CreatedDate >= DateTime.UtcNow.AddHours(-24)).ToListAsync();
                if (scamMessages.Count() > 3)
                {
                    user.IsSuspended = 1;

                    string subject = "Your account has been suspended";
                    string headtitle = "Security Issue";
                    string message = "Our system has restricted your account due to attempts to exchange contact information. Please note that exchanging contact details is prohibited on our platform without an active contract or an Enterprise plan. Thank you for your understanding.";
                    string description = "";
                    string buttonurl = _configuration.GetValue<string>("WebDomain") + "/suspended";
                    string buttoncaption = "REQUEST A REVIEW";
                    await _contractService.NewMailService(0, 39, user.Email, name, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                }

                var newScamMessage = new MailMessagesScams
                {
                    FromId = model.fromId,
                    ToId = model.toId,
                    Message = model.message,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.MailMessagesScams.AddAsync(newScamMessage);
                await _context.SaveChangesAsync();

                return newScamMessage.Id;
            }
        }

        public async Task<int> SendMailMessage(SendMailMessageRequest message)
        {
            try
            {
                var _context = new GoHireNowContext();
                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == message.fromUserId);
                if (user != null)
                {
                    user.LastLoginTime = DateTime.UtcNow;
                }

                var mail = await _context.Mails.FirstOrDefaultAsync(x => x.Id == message.mailId && !x.IsDeleted && (x.UserIdFrom == message.fromUserId || x.UserIdTo == message.fromUserId));
                if (mail != null)
                {
                    mail.IsRead = false;
                    mail.ModifiedDate = DateTime.UtcNow;
                    var mailMessage = new MailMessages
                    {
                        FromUserId = message.fromUserId,
                        ToUserId = message.toUserId,
                        Message = message.message,
                        CreateDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        IsDeleted = false,
                        IsRead = false,
                        MailId = mail.Id,
                        CustomId = message.customId,
                        CustomIdType = message.customIdType,
                        CustomLink = message.customLink,
                        FileName = message.FileName,
                        FilePath = message.FilePath
                    };
                    await _context.MailMessages.AddAsync(mailMessage);
                    await _context.SaveChangesAsync();
                    TimeSpan diff = DateTime.Now.AddMinutes(10) - DateTime.Now;
                    double seconds = diff.TotalSeconds;
                    var unreadMessages = _context.MailMessages
                   .Include(o => o.Mail).Where(o => o.ToUserId == mailMessage.ToUserId
                   && o.MailId == mailMessage.MailId && o.Mail.IsDeleted == false && o.IsRead == false)
                   .Count();
                    if (unreadMessages > 0)
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            Task.Delay(TimeSpan.FromSeconds(seconds)).ContinueWith(async (t) =>
                            {
                                var status = _context.spGetBoolResult.FromSql("spGetUsersUnreadMessageEmailStatus @FromUserId = {0},@ToUserId = {1},@CreatedDate = {2}",
                                      mailMessage.FromUserId, mailMessage.ToUserId, DateTime.UtcNow).FirstOrDefault();
                                if (status.Result)
                                {
                                    var result = _context.MailMessages.Where(o => o.Id == mailMessage.Id).Include(o => o.FromUser).Include(o => o.ToUser).FirstOrDefault();
                                    string htmlContent = new System.Net.WebClient().DownloadString(LookupService.FilePaths.EmailTemplatePath + "NewMessage.html");
                                    var image = (int)UserTypeEnum.Client == result.FromUser.UserType ? LookupService.FilePaths.ClientDefaultImageFilePath : LookupService.FilePaths.WorkerDefaultImageFilePath;
                                    result.FromUser.ProfilePicture = !string.IsNullOrEmpty(result.FromUser.ProfilePicture) ? $"{LookupService.FilePaths.ProfilePictureUrl}{result.FromUser.ProfilePicture}" : $"{image}";

                                    StringBuilder sb = new StringBuilder();
                                    var name = (int)UserTypeEnum.Client == result.FromUser.UserType ? result.FromUser.Company : result.FromUser.FullName;
                                    var workertitle = (int)UserTypeEnum.Client == result.FromUser.UserType ? "" : result.FromUser.UserTitle;
                                    sb.AppendLine("<table style=\"border-collapse: collapse;\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\">");
                                    sb.AppendLine("            <tr style=\"text-align:center; height: 128px; width: 70%\">");
                                    sb.AppendLine("                <td width=\"15%\" style=\"border: none\"></td>");
                                    sb.AppendLine("                <td style=\"padding-left: 40px; width: 82px; border: 1px solid #DCDCDC;border-right: none\";><img width='82' height='82' style=\"");
                                    sb.AppendLine("                                ");
                                    sb.AppendLine("                                max-height: 82px;");
                                    sb.AppendLine("                                border-radius: 50%;");
                                    sb.AppendLine("                                min-height: 60px;\" src=\"" + result.FromUser.ProfilePicture + "\" alt=\"\">");
                                    sb.AppendLine("                </td>");
                                    sb.AppendLine("                <td style=\"font-family: Lato, sans-serif;font-size: 18px;height: 100px;padding-left: 40px;text-align: start;border: 1px solid #DCDCDC;border-right: none;border-left: none\">");
                                    sb.AppendLine("                    <div style=\"color: #333333;font-family: 'Lato-Black',sans-serif;font-size: 18px;font-weight: 900;line-height: 18px;padding-bottom: 10px\">" + name + "</div>");
                                    sb.AppendLine("                    <div style=\"font-family: 'Lato-Regular',sans-serif;padding-bottom: 10px\">" + workertitle + "</div>");
                                    sb.AppendLine("                </td>");
                                    sb.AppendLine("                <td style=\"");
                                    sb.AppendLine("                    text-align: end;");
                                    sb.AppendLine("                    font-family: Lato, sans-serif;");
                                    sb.AppendLine("                    font-size: 18px;");
                                    sb.AppendLine("                    padding-right: 60px;");
                                    sb.AppendLine("                    height: 100px;");
                                    sb.AppendLine("                    border: 1px solid #DCDCDC;");
                                    sb.AppendLine("                    border-left: none;\"");
                                    sb.AppendLine("                >");
                                    sb.AppendLine("                   ");
                                    sb.AppendLine("                </td>");
                                    sb.AppendLine("                <td width=\"15%\" style=\"border: none\"></td>");
                                    sb.AppendLine("            </tr>");
                                    sb.AppendLine("          ");
                                    sb.AppendLine("        </table>");
                                    htmlContent = htmlContent.Replace("[From]", sb.ToString());
                                    htmlContent = htmlContent.Replace("[UserName]", result.FromUser.UserType == (int)UserTypeEnum.Client ? result.FromUser.Company : result.FromUser.FullName);
                                    htmlContent = htmlContent.Replace("[Url]", LookupService.FilePaths.MessageUrl);

                                    using (var _toolsContext = new GoHireNowToolsContext())
                                    {
                                        var sender = new mailer_sender();
                                        sender.ms_custom_id = 0;
                                        sender.ms_custom_type = 12;
                                        sender.ms_date = DateTime.Now;
                                        sender.ms_send_date = DateTime.Now;
                                        sender.ms_email = result.ToUser.Email;
                                        sender.ms_name = "";
                                        sender.ms_subject = "You received a new message";
                                        sender.ms_message = htmlContent;
                                        sender.ms_from_email = "no-reply@evirtualassistants.com";
                                        sender.ms_from_name = "eVirtualAssistants";
                                        sender.ms_priority = 1;
                                        sender.ms_issent = 0;
                                        sender.ms_unsubscribe = Guid.NewGuid();

                                        await _toolsContext.mailer_sender.AddAsync(sender);
                                        await _toolsContext.SaveChangesAsync();
                                    }

                                    // using (MailMessage messageObj = new MailMessage
                                    // {
                                    //     IsBodyHtml = true,
                                    //     BodyEncoding = System.Text.Encoding.UTF8,
                                    //     From = new MailAddress("no-reply@evirtualassistants.com", "eVirtualAssistants")
                                    // })
                                    // {
                                    //     messageObj.To.Add(new MailAddress(result.ToUser.Email));
                                    //     messageObj.Subject = "You received a new message";
                                    //     messageObj.Body = htmlContent;
                                    //     using (var client = new SmtpClient("email-smtp.us-east-1.amazonaws.com", 587))
                                    //     {
                                    //         client.Credentials =
                                    //             new NetworkCredential("AKIAIA5SSI4EFREFBKVQ", "AjeA3B59Qu1048gPkxN+xD5nw1uxduuYsVaojohTUa0A");
                                    //         client.EnableSsl = true;
                                    //         try
                                    //         {
                                    //             client.Send(messageObj);

                                    //         }
                                    //         catch (Exception ex)
                                    //         {

                                    //         }
                                    //     }
                                    // }
                                }

                            });
                        });
                    }

                    return mailMessage.Id;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task<bool> EditMailMessage(EditMailMessageRequest model, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    user.LastLoginTime = DateTime.UtcNow;
                }

                var message = await _context.MailMessages.FirstOrDefaultAsync(m => m.Id == model.messageId && !m.IsDeleted && m.FromUserId == userId);
                if (message == null) return false;

                message.Message = model.message;
                message.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> DeleteMessage(int messageId, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var user = await _context.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    user.LastLoginTime = DateTime.UtcNow;
                }

                var message = await _context.MailMessages.FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted && m.FromUserId == userId);
                if (message == null) return false;

                message.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> DeleteMail(int mailId, string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var mail = await _context.Mails.FindAsync(mailId);
                    if (mail == null)
                        throw new CustomException((int)HttpStatusCode.NotFound, "Mail not found");

                    if (mail.UserIdFrom != userId)
                        throw new CustomException((int)HttpStatusCode.Forbidden, "You do not have permission to delete this mail");

                    if (mail != null)
                    {
                        mail.IsDeleted = true;
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UnreadMail(string userId, int mailId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var mail = await _context.Mails.Include(x => x.MailMessages).FirstOrDefaultAsync(x => x.Id == mailId && !x.IsDeleted && (x.UserIdFrom == userId || x.UserIdTo == userId));
                    if (mail == null)
                        throw new CustomException((int)HttpStatusCode.NotFound, "Mail not found");
                    if (mail != null)
                    {
                        mail.IsRead = true;
                        if (mail.MailMessages.Any())
                        {
                            foreach (var item in mail.MailMessages)
                            {
                                if (item.ToUserId == userId)
                                {
                                    item.IsRead = true;
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<int>> UnreadMailList(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var list = await _context.MailMessages
                        .Include(o => o.FromUser)
                        .Include(o => o.Mail)
                        .Where(x => x.FromUser.Id == userId && x.Mail.IsDeleted == false && x.IsRead == false)
                        .Select(x => x.Mail.Id)
                        .Distinct()
                        .ToListAsync();
                    return list;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> GetUnreadMailCount(string userId)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    var unreadMessages = await _context.MailMessages
                        .Include(o => o.FromUser)
                        .Include(o => o.Mail)
                        .Where(o => o.FromUser.IsDeleted == false && o.ToUserId == userId && o.Mail.IsDeleted == false && o.IsRead == false)
                        .GroupBy(o => o.Mail.Id)
                        .CountAsync();
                    return unreadMessages;
                }
            }
            catch (Exception)
            {
                return -1;
            }

        }

        public async Task<Interviews> ConfirmSchedule(int interviewId, string datetime, int messageId, string timezone, decimal offset, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                var interview = await _context.Interviews.Where(x => x.Id == interviewId && x.IsDeleted == 0 && (x.ToUserId == userId || x.FromId == userId)).FirstOrDefaultAsync();
                string format = "yyyy-MM-dd HH:mm:ss";

                if (interview != null)
                {
                    interview.Status = 1;
                    interview.ToTimezone = timezone;
                    interview.ToOffset = offset;
                    interview.DateTime = DateTime.ParseExact(datetime, format, CultureInfo.InvariantCulture);

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
                        DateTime utcDateTime = DateTime.Parse(datetime, null, System.Globalization.DateTimeStyles.RoundtripKind);
                        DateTime dt = utcDateTime.AddHours((double)interview.FromOffset);
                        string subject = "The interview has been confirmed - " + worker.FullName;
                        string headtitle = "The interview has been confirmed - " + worker.FullName;
                        string message = "The interview is confirmed for " + dt.ToString("yyyy-MM-dd HH:mm") + " on " + interview.FromTimezone + " timezone.";
                        string description = "You can expect to receive the interview link in your email and chat message 20 minutes prior to the interview.";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/messages/" + interview.MailId;
                        string buttoncaption = "VIEW SCHEDULE";
                        await _contractService.NewMailService(0, 45, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");

                        dt = utcDateTime.AddHours((double)interview.ToOffset);
                        message = "The interview is confirmed for " + dt.ToString("yyyy-MM-dd HH:mm") + " on " + interview.ToTimezone + " timezone.";
                        subject = "The interview has been confirmed - " + company.Company;
                        headtitle = "The interview has been confirmed - " + company.Company;
                        await _contractService.NewMailService(0, 45, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }

                    return interview;
                }

                return null;
            }
        }

        public async Task<int> SendSchedule(StartNowRequest request)
        {
            using (var _context = new GoHireNowContext())
            {
                var newInterview = new Interviews
                {
                    FromId = request.fromId,
                    ToUserId = request.toUserId,
                    FromLink = request.fromLink,
                    FromTimezone = request.dateTime[0].timezone,
                    FromOffset = request.dateTime[0].offset,
                    ToLink = request.toLink,
                    MailId = request.mailId,
                    Status = 0,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.Interviews.AddAsync(newInterview);
                await _context.SaveChangesAsync();

                string format = "yyyy-MM-dd HH:mm:ss";

                if (request.dateTime != null && request.dateTime.Count() > 0)
                {
                    foreach (var item in request.dateTime)
                    {
                        var newSchedule = new InterviewsSchedules
                        {
                            InterviewId = newInterview.Id,
                            DatesTimes = DateTime.ParseExact(item.datetime, format, CultureInfo.InvariantCulture),
                            Timezone = item.timezone,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = 0
                        };

                        await _context.InterviewsSchedules.AddAsync(newSchedule);
                        await _context.SaveChangesAsync();
                    }

                    var worker = await _context.AspNetUsers.Where(x => x.Id == request.toUserId && x.IsDeleted == false).FirstOrDefaultAsync();
                    var company = await _context.AspNetUsers.Where(x => x.Id == request.fromId && x.IsDeleted == false).FirstOrDefaultAsync();
                    if (worker != null && company != null)
                    {
                        string subject = " You have received an interview offer - " + company.Company;
                        string headtitle = "You have received an interview offer - " + company.Company;
                        string message = "";
                        string description = "A company would like to interview you on our platform.";
                        string buttonurl = _configuration.GetValue<string>("WebDomain") + "/messages/" + request.mailId;
                        string buttoncaption = "VIEW SCHEDULE";
                        await _contractService.NewMailService(0, 48, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                    }
                }

                return newInterview.Id;
            }
        }

        public async Task<int> StartNowMeeting(StartNowRequest request)
        {
            using (var _context = new GoHireNowContext())
            {
                var newInterview = new Interviews
                {
                    FromId = request.fromId,
                    ToUserId = request.toUserId,
                    FromLink = request.fromLink,
                    ToLink = request.toLink,
                    MailId = request.mailId,
                    Status = 1,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = 0
                };

                await _context.Interviews.AddAsync(newInterview);
                await _context.SaveChangesAsync();

                var worker = await _context.AspNetUsers.Where(x => x.Id == request.toUserId && x.IsDeleted == false).FirstOrDefaultAsync();
                var company = await _context.AspNetUsers.Where(x => x.Id == request.fromId && x.IsDeleted == false).FirstOrDefaultAsync();
                if (worker != null && company != null)
                {
                    string subject = "Interview starting now";
                    string headtitle = "Interview starting now - " + worker.FullName;
                    string message = "";
                    string description = "You have an interview starting now with " + worker.FullName + ", click on the button to start.";
                    string buttonurl = request.fromLink;
                    string buttoncaption = "START NOW";
                    await _contractService.NewMailService(0, 49, company.Email, company.Company, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");

                    headtitle = "Interview starting now - " + company.Company;
                    description = "You have an interview starting now with " + company.Company + ", click on the button to start.";
                    buttonurl = request.toLink;
                    await _contractService.NewMailService(0, 49, worker.Email, worker.FullName, subject, headtitle, buttonurl, buttoncaption, description, message, "no-reply@evirtualassistants.com", "eVirtualAssistants", 1, "ContractStatuses.html");
                }

                return newInterview.Id;
            }
        }

        public async Task<int> SendMailMessageByJob(SendMailMessageRequest message)
        {
            try
            {
                using (var _context = new GoHireNowContext())
                {
                    Mails mail;
                    mail = await _context.Mails.FirstOrDefaultAsync(x =>
                        x.UserIdTo == message.toUserId &&
                        x.UserIdFrom == message.fromUserId
                    );
                    if (mail == null)
                    {
                        mail = await _context.Mails.FirstOrDefaultAsync(x =>
                            x.UserIdFrom == message.toUserId &&
                            x.UserIdTo == message.fromUserId
                        );
                    }

                    if (mail != null)
                    {
                        mail.IsRead = false;
                        mail.ModifiedDate = DateTime.UtcNow;
                        var mailMessage = new MailMessages
                        {
                            FromUserId = message.fromUserId,
                            ToUserId = message.toUserId,
                            Message = message.message,
                            CreateDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            IsDeleted = false,
                            IsRead = false,
                            MailId = mail.Id
                        };
                        await _context.MailMessages.AddAsync(mailMessage);
                        await _context.SaveChangesAsync();
                        return mailMessage.Id;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public List<MailResponse> GetAllUnreadMails()
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.Mails.Where(x => x.IsRead == false && x.IsDeleted == false).Select(x => new MailResponse
                {
                    Id = x.Id,
                    IsRead = x.IsRead,
                    CreateDate = x.CreateDate,
                    Ipaddress = x.Ipaddress,
                    Title = x.Title,
                    UserIdFrom = x.UserIdFrom,
                    UserIdTo = x.UserIdTo
                }).ToList();
            }
        }

        public List<MailResponse> GetAllUnreadMails(string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.Mails
                    .Where(x => x.IsRead == false && x.IsDeleted == false && x.UserIdTo == userId)
                    .Select(x => new MailResponse
                    {
                        Id = x.Id,
                        IsRead = x.IsRead,
                        CreateDate = x.CreateDate,
                        Ipaddress = x.Ipaddress,
                        Title = x.Title,
                        UserIdFrom = x.UserIdFrom,
                        UserIdTo = x.UserIdTo
                    }).ToList();
            }
        }

        public List<MailResponse> GetAllUnreadMailMessages(int jobId, string userId)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.MailMessages.Where(x => x.IsRead == false && x.IsDeleted == false
                && x.JobId == jobId && x.ToUserId == userId).Select(x => new MailResponse
                {
                    Id = x.Id,
                    IsRead = x.IsRead,
                    CreateDate = x.CreateDate,
                    Ipaddress = x.Ipaddress,
                    Title = x.Mail.Title,
                    UserIdFrom = x.Mail.UserIdFrom,
                    UserIdTo = x.Mail.UserIdTo
                }).ToList();
            }
        }

        public bool MarkRead(List<Database.Mails> mailList)
        {
            List<int> mailIds = mailList.Select(x => x.Id).ToList();

            using (var _context = new GoHireNowContext())
            {
                var mails = _context.Mails.Where(x => mailIds.Contains(x.Id));

                if (mails == null)
                    return true;
                foreach (var mail in mails)
                {
                    mail.IsRead = true;
                }
                _context.SaveChanges();
            }
            return true;
        }

        public Database.Mails SaveMail(Database.Mails mail)
        {
            using (var _context = new GoHireNowContext())
            {
                var obj = _context.Mails.Add(new Database.Mails
                {
                    UserIdFrom = mail.UserIdFrom,
                    IsRead = false,
                    Title = mail.Title,
                    UserIdTo = mail.UserIdTo
                });
                _context.SaveChanges();
                return obj.Entity;
            }
        }

        public int GetAllMailsCountFromPlanDate(DateTime plandate)
        {
            using (var _context = new GoHireNowContext())
            {
                return _context.Mails.Count(x => x.CreateDate >= plandate);
            }
        }
    }
}
