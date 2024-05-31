using GoHireNow.Database;
using GoHireNow.Models.ClientModels;
using GoHireNow.Models.CommonModels;
using GoHireNow.Models.WorkerModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoHireNow.Service.Interfaces
{
    public interface IWorkerJobService
    {
        Task<JobDetailForWorkerResponse> GetWorkerJobDetails(int jobId, string rootPath, string userId, int roleId);
        Task<List<AttachmentResponse>> GetJobAttachments(int jobId, string rootPath);
        Task<int> ApplyJob(ApplyJobRequest model, string UserId);
        Task<List<JobSummaryForWorkerResponse>> GetNewJobs(int status, string userId, int roleId, int page = 1, int size = 5);
        Task<List<JobSummaryForWorkerResponse>> GetMatchingJobs(string userId, int roleId, int page = 1, int size = 5);
        Task<List<JobSummaryForWorkerResponse>> GetAppliedJobs(string userId, int roleId, int page = 1, int size = 5);
        Task<UserIntros> CreateTemplate(string userId, SaveTemplateRequest model);
        Task<bool> DeleteTemplate(string userId, int id);
        Task<List<JobApplicantResponse>> GetJobApplicants(string userId, int jobId, int roleId);
        Task<List<UserIntros>> GetTemplates(string userId);
        Task<bool> InviteWorkers(InviteCompaniesRequest model, string workerId);
        Task<string> AIAssistant(AIAssistantRequest model, string userId);
    }
}
