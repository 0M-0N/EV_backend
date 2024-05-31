using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.JobModels;
using GoHireNow.Models.WorkerModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IClientJobService
    {
        Task<Jobs> GetJob(int id, int roleId);
        Task<ClientJobDetailResponse> GetJob(int jobId, string userId, string rootPath, int roleId);
        Task<List<JobSummaryResponse>> GetJobs(string userId, int page, int size, int status, int roleId, bool isactive = true);
        Task<int> AddJob(Jobs job);
        Task<bool> UpdateJob(string userId, int jobId, PostJobRequest model);
        Task<List<JobApplicantResponse>> GetJobApplicants(ApplicantFilterRequest model, string userId, int jobId, int roleId, int page = 1, int size = 5);
        Task<bool> DeleteJobApplicant(string userId, int jobId, string applicantId);
        Task<bool> AddJobAttachment(JobAttachments jobAttachment);
        Task<int> DeleteJobAttachments(string userId, int jobId);
        Task<string> DeleteJobAttachment(string userId, int jobId, int attachmentId);
        Task<bool> UpdateJobStatus(int jobId, string userId, int statusId);
        Task<bool> DeleteJob(int jobId, string userId);
        Task<bool> Applied(string userId, string companyId);
        Task<bool> Invite(InviteRequest model, string companyId);
        Task<bool> InviteCompanies(InviteCompaniesRequest model, string companyId);
        Task<int> PauseJob(string userId, int jobId);
        Task<List<int>> GetInvitedJobs(string companyId, string userId);
        Task<Tuple<string, string>> GetAttachmentUrl(int attachmentId);
        Task<List<JobSummaryResponse>> GetActiveJobs(string userId, int page, int size, int statusId, int roleId, bool isactive = true);
        Task<bool> Rate(int jobApplicantId, int rate);
        Task<decimal> GetAverageApplicants();
        Task<List<Jobs>> GetAllJobs(string userId);
    }
}
