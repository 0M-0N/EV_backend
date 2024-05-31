using GoHireNow.Database;
using GoHireNow.Models.MailModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IMailService
    {
        Task<int> InitialSendMessage(SendMessageRequest message);
        Task<MailMessages> GetMessageById(int id);
        Task<List<MailChatResponse>> LoadMailsV2(string userId, string path, int jobid, int size, int page);
        Task<List<MailChatResponse>> LoadCertainMailV2(string userId, string UserId, string path, int size, int page);
        Task<List<MailMessageResponse>> LoadMessagesV2(int mailId, string userId, string path, int page, int size);
        Task<int> SendMailMessage(SendMailMessageRequest message);
        Task<bool> UnreadMail(string userId, int mailId);
        Task<bool> DeleteMail(int mailId, string userId);
        Task<bool> EditMailMessage(EditMailMessageRequest model, string userId);
        Task<bool> DeleteMessage(int messageId, string userId);
        Task<int> GetUnreadMailCount(string userId);
        Task<int> SendMailMessageByJob(SendMailMessageRequest message);
        Task<int> StartNowMeeting(StartNowRequest request);
        Task<int> SendSchedule(StartNowRequest request);
        Task<Interviews> ConfirmSchedule(int interviewId, string datetime, int messageId, string timezone, decimal offset, string userId);
        Task<List<int>> UnreadMailList(string userId);
        Task<bool> AbleToUpload(int mailId, string userId);
        Task<Tuple<string, string>> GetMessageAttachment(int id);
        List<MailResponse> GetAllUnreadMails();
        List<MailResponse> GetAllUnreadMails(string userId);
        List<MailResponse> GetAllUnreadMailMessages(int jobId, string userId);
        bool MarkRead(List<Database.Mails> mailList);
        Database.Mails SaveMail(Database.Mails mail);
        int GetAllMailsCountFromPlanDate(DateTime plandate);
        Task<int> ReportScam(ReportRequest model);
    }
}
